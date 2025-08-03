using Godot;
using System.Collections.Generic;

public partial class Zombie : CharacterBody2D {
    [Export] public TileMapLayer TileLayer;
    [Export] public Player TargetPlayer;
    [Export] public float MoveTime = 0.1f;

    private Vector2 _targetPosition;
    private bool _isMoving = false;
    private float _elapsedTime = 0f;

    private Queue<Vector2I> _pathQueue = new Queue<Vector2I>();

    private enum State {
        Idle,
        ChaseInit,
        ChasePath
    }

    private State _state = State.Idle;

    private Vector2I _discoveryCell;

    public override void _Ready() {
        _targetPosition = Position;
        Position = TileLayer.MapToLocal(TileLayer.LocalToMap(Position));
        TargetPlayer.PathRecorded += OnPlayerPathRecorded;
        TargetPlayer.Moved += OnPlayerMoved; 
    }

    public override void _PhysicsProcess(double delta) {
        if (_isMoving) {
            _elapsedTime += (float)delta;
            Position = Position.Lerp(_targetPosition, _elapsedTime / MoveTime);
            if (_elapsedTime >= MoveTime) {
                Position = _targetPosition;
                _isMoving = false;
            }
        }
    }

    // 记录玩家路径（发现玩家后立即记录，而不是等进入 ChasePath 再记录）
    private void OnPlayerPathRecorded(Vector2I cell) {
        if (_state == State.ChaseInit || _state == State.ChasePath)
            _pathQueue.Enqueue(cell);
    }

    // 玩家每次移动后触发僵尸走一步
    private void OnPlayerMoved(Vector2I playerCell) {
        Vector2I zombieCell = TileLayer.LocalToMap(Position);

        if (_state == State.Idle) {
            if (CanSeePlayer(zombieCell, playerCell)) {
                _state = State.ChaseInit;
                _discoveryCell = playerCell; // 锁定发现位置
                MoveOneStep(zombieCell, _discoveryCell); // 立刻走一步
            }
        } else if (_state == State.ChaseInit) {
            if (zombieCell != _discoveryCell) {
                MoveOneStep(zombieCell, _discoveryCell);
            } else {
                // 到达发现位置后开始路径跟随
                _state = State.ChasePath;
            }
        } else if (_state == State.ChasePath && _pathQueue.Count > 0) {
            Vector2I nextCell = _pathQueue.Dequeue();
            if (!IsBlocked(nextCell))
                StartMove(nextCell);
        }
    }

    private void MoveOneStep(Vector2I fromCell, Vector2I toCell) {
        Vector2I dir = Vector2I.Zero;
        if (fromCell.X < toCell.X) dir = Vector2I.Right;
        else if (fromCell.X > toCell.X) dir = Vector2I.Left;
        else if (fromCell.Y < toCell.Y) dir = Vector2I.Down;
        else if (fromCell.Y > toCell.Y) dir = Vector2I.Up;

        Vector2I nextCell = fromCell + dir;
        if (!IsBlocked(nextCell))
            StartMove(nextCell);
    }

    private void StartMove(Vector2I targetCell) {
        _targetPosition = TileLayer.MapToLocal(targetCell);
        _isMoving = true;
        _elapsedTime = 0f;
    }

    private bool CanSeePlayer(Vector2I fromCell, Vector2I toCell) {
        if (fromCell.X == toCell.X) {
            int step = fromCell.Y < toCell.Y ? 1 : -1;
            for (int y = fromCell.Y + step; y != toCell.Y; y += step)
                if (IsBlocked(new Vector2I(fromCell.X, y)))
                    return false;
            return true;
        } else if (fromCell.Y == toCell.Y) {
            int step = fromCell.X < toCell.X ? 1 : -1;
            for (int x = fromCell.X + step; x != toCell.X; x += step)
                if (IsBlocked(new Vector2I(x, fromCell.Y)))
                    return false;
            return true;
        }
        return false;
    }

    private bool IsBlocked(Vector2I cell) {
        int sourceId = TileLayer.GetCellSourceId(cell);
        var tileData = TileLayer.GetCellTileData(cell);
        return sourceId != -1 && tileData != null && tileData.GetCollisionPolygonsCount(0) > 0;
    }
}