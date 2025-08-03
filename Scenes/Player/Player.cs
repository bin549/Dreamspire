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

    public override void _Ready() {
        _targetPosition = Position;
        _lastCell = TileLayer.LocalToMap(Position);
        Position = TileLayer.MapToLocal(_lastCell);
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
            _targetPosition = TileLayer.MapToLocal(targetCell);
            _isMoving = true;
            _elapsedTime = 0f;
            Moved?.Invoke(targetCell);
            if (targetCell != _lastCell) {
                _lastCell = targetCell;
                PathRecorded?.Invoke(targetCell);
            }
        }
    }
}