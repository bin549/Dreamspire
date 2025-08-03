using Godot;

public partial class Food : Area2D {
    public override void _Ready() {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node body) { 
        if (body is Player player) {
            player.CombatPower += 1;
            AudioManager.Instance.PlaySound("eat"); 
            QueueFree(); 
        }
    }
}