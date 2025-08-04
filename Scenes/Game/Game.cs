using Godot;
using System.Threading.Tasks;

public partial class Game : Node2D {
    [Export] public Level[] levels;
    [Export] public float[] levelXPositions;
    [Export] public Vector2[] levelZooms;
    [Export] public int currentLevelIndex = 0;
    [Export] private SceneTransition _transition;
    [Export] private float transitionTime = 0.5f;
    [Export] private Camera2D camera;
    [Export] private Player player;

    public override async void _Ready() {
        RenderingServer.SetDefaultClearColor(Colors.Black);
        if (_transition == null) {
            GD.PrintErr("⚠ _transition 未设置，请在 Inspector 绑定 SceneTransition 节点！");
            return;
        }
        if (levelXPositions == null || levelXPositions.Length == 0) {
            GD.PrintErr("⚠ levelXPositions 未设置，请在 Inspector 填写每个关卡的X坐标！");
            return;
        }
        if (levelZooms == null || levelZooms.Length != levelXPositions.Length)
        {
            GD.PrintErr("⚠ levelZooms 未设置或数量与 levelXPositions 不一致！");
            return;
        }
        if (camera == null) {
            GD.PrintErr("⚠ camera 未设置，请在 Inspector 绑定 Camera2D 节点！");
            return;
        }
        camera.Offset = new Vector2(levelXPositions[currentLevelIndex], camera.Offset.Y);
        this.player.tileLayer = this.levels[currentLevelIndex].tileMapLayer;
        Vector2 markerGlobalPos = this.levels[currentLevelIndex].marker2D.GlobalPosition;
        this.player.GlobalPosition = new Vector2(markerGlobalPos.X, markerGlobalPos.Y);
        await _transition.FadeFromBlack(transitionTime);
    }

    public async Task SwitchLevel(int newIndex) {
        if (newIndex < 0 || newIndex >= levelXPositions.Length) return;
        if (newIndex == currentLevelIndex) return;
        await _transition.FadeToBlack(transitionTime);
        this.currentLevelIndex = newIndex;
        this.camera.Offset = new Vector2(levelXPositions[currentLevelIndex], camera.Offset.Y);
        this.camera.Zoom = levelZooms[currentLevelIndex];
        this.player.tileLayer = this.levels[currentLevelIndex].tileMapLayer;
        Vector2 markerGlobalPos = this.levels[currentLevelIndex].marker2D.GlobalPosition;
        this.player.GlobalPosition = new Vector2(markerGlobalPos.X, markerGlobalPos.Y);
        await _transition.FadeFromBlack(transitionTime);
    }
}