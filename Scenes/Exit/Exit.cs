using Godot;

public partial class Exit : Area2D {
    [Export] public int targetLevelIndex;
    private Game _game;

    public override void _Ready() {
        BodyEntered += OnBodyEntered;
        _game = GetNode<Game>("/root/Game");
    }

    private async void OnBodyEntered(Node body) {
        if (body is Player) {
            AudioManager.Instance.PlaySound("exit");
            await _game.SwitchLevel(targetLevelIndex); 
        }
    }
}