using Godot;
using System;
using System.Collections.Generic;

public partial class Player : CharacterBody2D {
    [Export] public TileMapLayer tileLayer;
    [Export] public float MoveTime = 0.1f;
    private Vector2 targetPosition;
    private bool isMoving = false;
    private float elapsedTime = 0f;
    public event Action<Vector2I> moved;
    public event Action<Vector2I> pathRecorded;
    private Vector2I lastCell;
    private PackedScene bloodScene;
    private Stack<Vector2I> moveHistory = new Stack<Vector2I>();
    [Export] public bool isCombatPower = false;
    private Game gameRef;
    private bool isUndoAnimating = false;
    private bool isDead = false;
    public bool IsDead => this.isDead;

    public override void _Ready() {
        Vector2 playerLocalToTile = this.tileLayer.ToLocal(GlobalPosition);
        this.lastCell = this.tileLayer.LocalToMap(playerLocalToTile);
        GlobalPosition = this.tileLayer.ToGlobal(this.tileLayer.MapToLocal(this.lastCell));
        this.targetPosition = GlobalPosition;
        this.bloodScene = GD.Load<PackedScene>("res://Scenes/Blood/Blood.tscn");
        this.moveHistory.Clear();
        this.gameRef = GetNodeOrNull<Game>("/root/Game");
    }

    public override void _PhysicsProcess(double delta) {
        if (this.isMoving) {
            this.elapsedTime += (float)delta;
            GlobalPosition = GlobalPosition.Lerp(this.targetPosition, this.elapsedTime / MoveTime);
            if (this.elapsedTime >= MoveTime) {
                GlobalPosition = this.targetPosition;
                this.isMoving = false;
                if (!this.isUndoAnimating) {
                    this.gameRef?.EndTurn();
                } else {
                    this.isUndoAnimating = false;
                }
            }
            return;
        }
        Vector2 dir = Vector2.Zero;
        if (Input.IsActionJustPressed("undo_step")) {
            if (this.gameRef != null && this.gameRef.CanUndo) {
                this.gameRef.UndoLastTurnAsync();
            }
            if (this.moveHistory.Count > 0) this.moveHistory.Pop();
            return;
        }
        if (this.isDead) return;
        if (Input.IsActionJustPressed("move_left")) dir = Vector2.Left;
        if (Input.IsActionJustPressed("move_right")) dir = Vector2.Right;
        if (Input.IsActionJustPressed("move_up")) dir = Vector2.Up;
        if (Input.IsActionJustPressed("move_down")) dir = Vector2.Down;
        if (dir != Vector2.Zero) {
            if (this.gameRef != null && !this.gameRef.CanStartTurn) return;
            Vector2I currentCell = tileLayer.LocalToMap(tileLayer.ToLocal(GlobalPosition));
            Vector2I targetCell = currentCell + new Vector2I((int)dir.X, (int)dir.Y);
            if (!this.IsBlocked(targetCell)) {
                this.gameRef?.BeginTurn(currentCell);
                this.moveHistory.Push(currentCell);
                this.targetPosition = tileLayer.ToGlobal(tileLayer.MapToLocal(targetCell));
                this.isMoving = true;
                this.elapsedTime = 0f;
                this.moved?.Invoke(targetCell);
                if (targetCell != this.lastCell) {
                    this.lastCell = targetCell;
                    pathRecorded?.Invoke(targetCell);
                }
                Vector2 spawnGlobal = tileLayer.ToGlobal(tileLayer.MapToLocal(currentCell));
                if (!this.BloodExistsAtPosition(spawnGlobal)) {
                    Node2D blood = this.bloodScene.Instantiate<Node2D>();
                    Node2D bloodsNode = GetBloodsContainer();
                    if (bloodsNode != null) {
                        bloodsNode.AddChild(blood);
                        blood.GlobalPosition = spawnGlobal;
                        blood.ZIndex = 0;
                        InitializeBloodSprite(blood);
                        this.gameRef?.RegisterBlood(blood);
                    }
                }
            }
        }
    }

    public void ClearHistory() {
        this.moveHistory.Clear();
    }

    public void SetToCellWithoutEvents(Vector2I cell) {
        if (tileLayer == null) return;
        this.isUndoAnimating = true;
        this.targetPosition = tileLayer.ToGlobal(tileLayer.MapToLocal(cell));
        this.isMoving = true;
        this.elapsedTime = 0f;
        this.lastCell = cell;
    }

    private bool IsBlocked(Vector2I cell) {
        int sourceId = this.tileLayer.GetCellSourceId(cell);
        var tileData = this.tileLayer.GetCellTileData(cell);
        return sourceId != -1 && tileData != null && tileData.GetCollisionPolygonsCount(0) > 0;
    }

    private bool BloodExistsAtPosition(Vector2 positionGlobal) {
        if (this.tileLayer == null) return false;
        Node levelNode = this.tileLayer.GetParent();
        if (levelNode == null) return false;
        Node2D bloodsNode = levelNode.GetNodeOrNull<Node2D>("Bloods");
        if (bloodsNode == null) return false;
        foreach (Node bloodChild in bloodsNode.GetChildren()) {
            if (bloodChild is Node2D blood && blood.GlobalPosition.DistanceTo(positionGlobal) < 1.0f) {
                return true;
            }
        }
        return false;
    }

    private void InitializeBloodSprite(Node2D bloodNode) {
        if (bloodNode is Sprite2D sprite) {
            if (sprite.Texture == null) {
                var tex = GD.Load<Texture2D>("res://Assets/Sprites/blood.png");
                sprite.Texture = tex;
            }
            sprite.Modulate = new Color(0.7f, 0.05f, 0.05f, 0.6f);
            sprite.Visible = true;
        }
    }

    private Node2D GetBloodsContainer() {
        if (this.tileLayer == null) return null;
        Node levelNode = this.tileLayer.GetParent();
        if (levelNode == null) return null;
        Node2D bloods = levelNode.GetNodeOrNull<Node2D>("Bloods");
        if (bloods == null) {
            bloods = new Node2D();
            bloods.Name = "Bloods";
            levelNode.AddChild(bloods);
        }
        return bloods;
    }

    public void OnDie() {
        if (this.isDead) return;
        AudioManager.Instance.PlaySound("die");
        this.isDead = true;
        Visible = false;
    }

    public void Revive() {
        this.isDead = false;
        Visible = true;
    }
}
