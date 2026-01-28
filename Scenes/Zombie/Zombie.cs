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
    private Queue<Vector2I> pathQueue = new Queue<Vector2I>();

    private enum State {
        Idle,
        ChaseInit,
        ChasePath
    }

    private State state = State.Idle;
    private Vector2I discoveryCell;

    public override void _Ready() {
        this.targetPosition = Position;
        Position = this.tileLayer.MapToLocal(this.tileLayer.LocalToMap(Position));
        this.targetPlayer.pathRecorded += OnPlayerpathRecorded;
        this.targetPlayer.moved += OnPlayermoved;
        var area = GetNode<Area2D>("Area2D");
        area.BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta) {
        if (this.isMoving) {
            this.elapsedTime += (float)delta;
            Position = Position.Lerp(this.targetPosition, this.elapsedTime / this.MoveTime);
            if (this.elapsedTime >= this.MoveTime) {
                Position = this.targetPosition;
                this.isMoving = false;
            }
        }
    }

    public async void ForceReposition(Vector2I cell) {
        if (this.tileLayer == null) return;
        this.isMoving = false;
        this.elapsedTime = 0f;
        Vector2 from = Position;
        Vector2 to = this.tileLayer.MapToLocal(cell);
        float duration = Mathf.Max(this.MoveTime, 0.08f);
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
        this.pathQueue.Clear();
        this.state = State.Idle;
    }

    private void OnPlayerpathRecorded(Vector2I cell) {
        if (this.state == State.ChaseInit || this.state == State.ChasePath)
            this.pathQueue.Enqueue(cell);
    }

    private void OnPlayermoved(Vector2I playerCell) {
        Vector2I zombieCell = this.tileLayer.LocalToMap(Position);
        if (this.state == State.Idle) {
            if (this.CanSeePlayer(zombieCell, playerCell)) {
                this.state = State.ChaseInit;
                this.discoveryCell = playerCell;
                MoveOneStep(zombieCell, this.discoveryCell);
            }
        } else if (this.state == State.ChaseInit) {
            if (zombieCell != this.discoveryCell) {
                this.MoveOneStep(zombieCell, this.discoveryCell);
            } else {
                this.state = State.ChasePath;
                if (this.pathQueue.Count > 0) {
                    Vector2I nextCell = this.pathQueue.Dequeue();
                    if (!this.IsBlocked(nextCell))
                        this.StartMove(nextCell);
                    if (this.pathQueue.Count > 0) {
                        Vector2I nextCell2 = this.pathQueue.Dequeue();
                        if (!this.IsBlocked(nextCell2))
                            this.StartMove(nextCell2);
                    }
                }
            }
        } else if (this.state == State.ChasePath && this.pathQueue.Count > 0) {
            Vector2I nextCell = this.pathQueue.Dequeue();
            if (!this.IsBlocked(nextCell))
                this.StartMove(nextCell);
        }
    }

    private void MoveOneStep(Vector2I fromCell, Vector2I toCell) {
        Vector2I dir = Vector2I.Zero;
        if (fromCell.X < toCell.X) dir = Vector2I.Right;
        else if (fromCell.X > toCell.X) dir = Vector2I.Left;
        else if (fromCell.Y < toCell.Y) dir = Vector2I.Down;
        else if (fromCell.Y > toCell.Y) dir = Vector2I.Up;
        Vector2I nextCell = fromCell + dir;
        if (!this.IsBlocked(nextCell))
            this.StartMove(nextCell);
    }

    private async void StartMove(Vector2I targetCell) {
        await ToSignal(GetTree().CreateTimer(this.MoveDelay), SceneTreeTimer.SignalName.Timeout);
        this.targetPosition = this.tileLayer.MapToLocal(targetCell);
        this.isMoving = true;
        this.elapsedTime = 0f;
    }

    private bool CanSeePlayer(Vector2I fromCell, Vector2I toCell) {
        if (fromCell.X == toCell.X) {
            int step = fromCell.Y < toCell.Y ? 1 : -1;
            for (int y = fromCell.Y + step; y != toCell.Y; y += step)
                if (this.IsBlocked(new Vector2I(fromCell.X, y)))
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

    private bool this.IsBlocked(Vector2I cell) {
        int sourceId = this.tileLayer.GetCellSourceId(cell);
        var tileData = this.tileLayer.GetCellTileData(cell);
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