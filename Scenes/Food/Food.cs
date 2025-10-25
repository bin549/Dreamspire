using Godot;

public partial class Food : Area2D {
    public override void _Ready() {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node body) { 
        if (body is Player player) {
            player.isCombatPower = true;
            AudioManager.Instance.PlaySound("eat"); 
            Game game = GetNodeOrNull<Game>("/root/Game");
            string scenePath = (this.SceneFilePath != null && this.SceneFilePath != "") ? this.SceneFilePath : "res://Scenes/Food/food.tscn";
            game?.RegisterFoodPicked(scenePath, GlobalPosition);
            QueueFree(); 
        }
    }
}   