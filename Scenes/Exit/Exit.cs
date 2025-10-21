using Godot;

public partial class Exit : Area2D {
    [Export] public int targetLevelIndex;
    private Game _game;

    public override void _Ready() {
        BodyEntered += OnBodyEntered;
        this._game = GetNode<Game>("/root/Game");
    }

    private async void OnBodyEntered(Node body) {
        if (body is Player) {
            AudioManager.Instance.PlaySound("exit");
            int lastIndex = this._game.levelXPositions.Length - 1;
            bool isFinalLevelExit = this._game.currentLevelIndex == lastIndex;
            if (isFinalLevelExit) {
                await this._game.PlayFinalCameraAnimation();
                return;
            }
            await this._game.SwitchLevel(targetLevelIndex);
        }
    }
}
