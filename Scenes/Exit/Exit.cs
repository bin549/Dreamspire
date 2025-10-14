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
            int lastIndex = _game.levelXPositions.Length - 1;
            bool isFinalLevelExit = _game.currentLevelIndex == lastIndex;
            if (isFinalLevelExit) {
                await _game.PlayFinalCameraAnimation();
                return;
            }
            await _game.SwitchLevel(targetLevelIndex); 
        }
    }
}