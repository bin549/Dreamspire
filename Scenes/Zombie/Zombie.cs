using Godot;
using System.Collections.Generic;

public partial class Zombie : CharacterBody2D {
    [Export] public TileMapLayer tileLayer;
    [Export] public Player targetPlayer;
    [Export] public float MoveTime = 0.1f;
    [Export] public float MoveDelay = 0.2f;
    private Vector2 targetPosition;
    private bool isMoving = false;
    private float elapsedTime = 0f;
    private Queue<Vector2I> _pathQueue = new Queue<Vector2I>();
    private enum State {
        Idle,
        ChaseInit,
        ChasePath
    }
    private State _state = State.Idle;
    private Vector2I _discoveryCell;

    public override void _Ready() {
        this.targetPosition = Position;
        Position = tileLayer.MapToLocal(tileLayer.LocalToMap(Position));
        targetPlayer.pathRecorded += OnPlayerpathRecorded;
        targetPlayer.moved += OnPlayermoved;
        var area = GetNode<Area2D>("Area2D");
        area.BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta) {
        if (this.isMoving) {
            this.elapsedTime += (float)delta;
            Position = Position.Lerp(this.targetPosition, this.elapsedTime / MoveTime);
            if (this.elapsedTime >= MoveTime) {
                Position = this.targetPosition;
                this.isMoving = false;
            }
        }
    }

    public async void ForceReposition(Vector2I cell) {
        if (tileLayer == null) return;
        this.isMoving = false;
        this.elapsedTime = 0f;
        Vector2 from = Position;
        Vector2 to = tileLayer.MapToLocal(cell);
        float duration = Mathf.Max(MoveTime, 0.08f);
        float t = 0f;
        while (t < duration) {
            t += (float)GetProcessDeltaTime();
            float alpha = Mathf.Clamp(t / duration, 0f, 1f);
            float eased = Mathf.Ease(alpha, 0.8f);
            Position = from.Lerp(to, eased);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        Position = to;
        this.targetPosition = Position;
        _pathQueue.Clear();
        _state = State.Idle;
    }

    private void OnPlayerpathRecorded(Vector2I cell) {
        if (_state == State.ChaseInit || _state == State.ChasePath)
            _pathQueue.Enqueue(cell);
    }

    private void OnPlayermoved(Vector2I playerCell) {
        Vector2I zombieCell = tileLayer.LocalToMap(Position);
        if (_state == State.Idle) {
            if (CanSeePlayer(zombieCell, playerCell)) {
                _state = State.ChaseInit;
                _discoveryCell = playerCell;
                MoveOneStep(zombieCell, _discoveryCell);
            }
        } else if (_state == State.ChaseInit) {
            if (zombieCell != _discoveryCell) {
                this.MoveOneStep(zombieCell, _discoveryCell);
            } else {
                _state = State.ChasePath;
                if (_pathQueue.Count > 0) {
                    Vector2I nextCell = _pathQueue.Dequeue();
                    if (!IsBlocked(nextCell))
                        StartMove(nextCell);
                    if (_pathQueue.Count > 0) {
                        Vector2I nextCell2 = _pathQueue.Dequeue();
                        if (!IsBlocked(nextCell2))
                            StartMove(nextCell2);
                    }
                }
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

    private async void StartMove(Vector2I targetCell) {
        await ToSignal(GetTree().CreateTimer(MoveDelay), SceneTreeTimer.SignalName.Timeout);
        this.targetPosition = tileLayer.MapToLocal(targetCell);
        this.isMoving = true;
        this.elapsedTime = 0f;
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
                if (this.IsBlocked(new Vector2I(x, fromCell.Y)))
                    return false;
            return true;
        }
        return false;
    }

    private bool IsBlocked(Vector2I cell) {
        int sourceId = tileLayer.GetCellSourceId(cell);
        var tileData = tileLayer.GetCellTileData(cell);
        return sourceId != -1 && tileData != null && tileData.GetCollisionPolygonsCount(0) > 0;
    }

    public void OnDie() {
        Visible = false;
        SetProcess(false);
        SetPhysicsProcess(false);
        AudioManager.Instance.PlaySound("kill");
        this.isMoving = false;
    }

    public void Revive() {
        Visible = true;
        SetProcess(true);
        SetPhysicsProcess(true);
    }

    private void OnBodyEntered(Node body) {
        if (body is Player player) {
            Game game = GetNodeOrNull<Game>("/root/Game");
            if (game != null && game.IsUndoGraceActive) return;
            if (player.isCombatPower) this.OnDie();
            else player.OnDie();
        }
    }
}
