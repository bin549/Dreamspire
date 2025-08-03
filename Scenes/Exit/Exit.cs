using Godot;

public partial class Exit : Area2D {
    public override void _Ready() {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node body) { 
        if (body is Player player) {
            AudioManager.Instance.PlaySound("exit"); 
        }
    }
} 