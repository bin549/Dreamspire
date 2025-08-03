using Godot;
using System.Threading.Tasks;

public partial class Game : Node2D {
    [Export] public float[] LevelXPositions;
    [Export] public Vector2[] LevelZooms;
    [Export] public int CurrentLevelIndex = 0;
    [Export] private SceneTransition _transition;
    [Export] private float transitionTime = 0.5f;
    [Export] private Camera2D camera;

    public override async void _Ready() {
        RenderingServer.SetDefaultClearColor(Colors.Black);
        if (_transition == null) {
            GD.PrintErr("⚠ _transition 未设置，请在 Inspector 绑定 SceneTransition 节点！");
            return;
        }
        if (LevelXPositions == null || LevelXPositions.Length == 0) {
            GD.PrintErr("⚠ LevelXPositions 未设置，请在 Inspector 填写每个关卡的X坐标！");
            return;
        }
        if (LevelZooms == null || LevelZooms.Length != LevelXPositions.Length)
        {
            GD.PrintErr("⚠ LevelZooms 未设置或数量与 LevelXPositions 不一致！");
            return;
        }
        if (camera == null) {
            GD.PrintErr("⚠ camera 未设置，请在 Inspector 绑定 Camera2D 节点！");
            return;
        }
        camera.Offset = new Vector2(LevelXPositions[CurrentLevelIndex], camera.Offset.Y);
        await _transition.FadeFromBlack(transitionTime);
    }

    public async Task SwitchLevel(int newIndex) {
        if (newIndex < 0 || newIndex >= LevelXPositions.Length) return;
        if (newIndex == CurrentLevelIndex) return;
        await _transition.FadeToBlack(transitionTime);
        CurrentLevelIndex = newIndex;
        camera.Offset = new Vector2(LevelXPositions[CurrentLevelIndex], camera.Offset.Y);
        camera.Zoom = LevelZooms[CurrentLevelIndex];
        await _transition.FadeFromBlack(transitionTime);
    }
}