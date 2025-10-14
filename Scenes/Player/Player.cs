using Godot;
using System;

public partial class Player : CharacterBody2D {
    [Export] public TileMapLayer tileLayer;
    [Export] public float MoveTime = 0.1f;
    private Vector2 _targetPosition;
    private bool _isMoving = false;
    private float _elapsedTime = 0f;
    public event Action<Vector2I> moved;
    public event Action<Vector2I> pathRecorded;
    private Vector2I _lastCell;
    private PackedScene _bloodScene;
    [Export] public bool isCombatPower = false;

    public override void _Ready() {
        Vector2 playerLocalToTile = this.tileLayer.ToLocal(GlobalPosition);
        this._lastCell = this.tileLayer.LocalToMap(playerLocalToTile);
        GlobalPosition = this.tileLayer.ToGlobal(this.tileLayer.MapToLocal(_lastCell));
        this._targetPosition = GlobalPosition;
		this._bloodScene = GD.Load<PackedScene>("res://Scenes/Blood/blood.tscn");
    }

    public override void _PhysicsProcess(double delta) {
        if (_isMoving) {
            _elapsedTime += (float)delta;
            GlobalPosition = GlobalPosition.Lerp(_targetPosition, _elapsedTime / MoveTime);
            if (_elapsedTime >= MoveTime) {
                GlobalPosition = _targetPosition;
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
			Vector2I currentCell = tileLayer.LocalToMap(tileLayer.ToLocal(GlobalPosition));
            Vector2I targetCell = currentCell + new Vector2I((int)dir.X, (int)dir.Y);
            if (!this.IsBlocked(targetCell)) {
				_targetPosition = tileLayer.ToGlobal(tileLayer.MapToLocal(targetCell));
                _isMoving = true;
                _elapsedTime = 0f;
                moved?.Invoke(targetCell);
                if (targetCell != _lastCell) {
                    _lastCell = targetCell;
                    pathRecorded?.Invoke(targetCell);
                }
				Vector2 spawnGlobal = tileLayer.ToGlobal(tileLayer.MapToLocal(currentCell));
				if (!this.BloodExistsAtPosition(spawnGlobal)) {
					Node2D blood = _bloodScene.Instantiate<Node2D>();
					Node2D bloodsNode = GetBloodsContainer();
					if (bloodsNode != null) {
						bloodsNode.AddChild(blood);
						blood.GlobalPosition = spawnGlobal;
						blood.ZIndex = 0;
						InitializeBloodSprite(blood);
					}
				}
            }
        }
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
        AudioManager.Instance.PlaySound("die");
        QueueFree();
    }
}