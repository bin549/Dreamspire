using Godot;
using System;

public partial class Player : CharacterBody2D {
    [Export] public TileMapLayer TileLayer;
    [Export] public float MoveTime = 0.1f;
    private Vector2 _targetPosition;
    private bool _isMoving = false;
    private float _elapsedTime = 0f;
    public event Action<Vector2I> Moved; 
    public event Action<Vector2I> PathRecorded; 
    private Vector2I _lastCell;
    private PackedScene _bloodScene;

    public override void _Ready() {
        _targetPosition = Position;
        _lastCell = TileLayer.LocalToMap(Position);
        Position = TileLayer.MapToLocal(_lastCell);
        _bloodScene = GD.Load<PackedScene>("res://Scenes/Blood/blood.tscn");
    }

    public override void _PhysicsProcess(double delta) {
        if (_isMoving) {
            _elapsedTime += (float)delta;
            Position = Position.Lerp(_targetPosition, _elapsedTime / MoveTime);
            if (_elapsedTime >= MoveTime) {
                Position = _targetPosition;
                _isMoving = false;
            }
            return;
        }
        Vector2 dir = Vector2.Zero;
        if (Input.IsActionJustPressed("ui_left")) dir = Vector2.Left;
        if (Input.IsActionJustPressed("ui_right")) dir = Vector2.Right;
        if (Input.IsActionJustPressed("ui_up")) dir = Vector2.Up;
        if (Input.IsActionJustPressed("ui_down")) dir = Vector2.Down;
        if (dir != Vector2.Zero) {
            Vector2I currentCell = TileLayer.LocalToMap(Position);
            Vector2I targetCell = currentCell + new Vector2I((int)dir.X, (int)dir.Y);
            if (!IsBlocked(targetCell)) {
                _targetPosition = TileLayer.MapToLocal(targetCell);
                _isMoving = true;
                _elapsedTime = 0f;
                Moved?.Invoke(targetCell);
                if (targetCell != _lastCell) {
                    _lastCell = targetCell;
                    PathRecorded?.Invoke(targetCell);
                }
                if (!BloodExistsAtPosition(Position)) {
                    Node2D blood = _bloodScene.Instantiate<Node2D>();
                    blood.Position = Position;
                    Node bloodsNode = GetParent().GetNode("Bloods");
                    if (bloodsNode != null) {
                        bloodsNode.AddChild(blood);
                    } else {
                        GetParent().AddChild(blood);
                    }
                }
            }
        }
    }

    private bool IsBlocked(Vector2I cell) {
        int sourceId = TileLayer.GetCellSourceId(cell);
        var tileData = TileLayer.GetCellTileData(cell);
        return sourceId != -1 && tileData != null && tileData.GetCollisionPolygonsCount(0) > 0;
    }

    private bool BloodExistsAtPosition(Vector2 position) {
        Node parent = GetParent();
        if (parent == null) return false;
        
        foreach (Node child in parent.GetChildren()) {
            if (child.Name == "Bloods" && child is Node2D bloodsNode) {
                foreach (Node bloodChild in bloodsNode.GetChildren()) {
                    if (bloodChild is Node2D blood && blood.Position.DistanceTo(position) < 1.0f) {
                        return true;
                    }
                }
            }
        }
        return false;
    }
}