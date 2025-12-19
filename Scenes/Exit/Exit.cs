using Godot;

public partial class Exit : Area2D {
    [Export] public int targetLevelIndex;
    private Game game;

    public override void _Ready() {
        BodyEntered += OnBodyEntered;
        this.game = GetNode<Game>("/root/Game");
    }

    private async void OnBodyEntered(Node body) {
        if (body is Player) {
            AudioManager.Instance.PlaySound("exit");
            int lastIndex = this.game.levelXPositions.Length - 1;
            if (this.game.currentLevelIndex == lastIndex) {
                await this.game.PlayFinalCameraAnimation();
                return;
            }
            await this.game.SwitchLevel(targetLevelIndex);
        }
    }
}
