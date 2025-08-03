using Godot;
using System.Threading.Tasks;

public partial class Game : Node2D {
    [Export] public Node2D[] Levels;
    [Export] public int CurrentLevelIndex = 0;
    [Export] private SceneTransition _transition;
    [Export] private float transitionTime = 0.5f;

    public override async void _Ready() {
        RenderingServer.SetDefaultClearColor(Colors.Black);
        if (_transition == null) {
            GD.PrintErr("⚠ _transition 未设置，请在 Inspector 绑定 SceneTransition 节点！");
            return;
        }
        if (Levels == null || Levels.Length == 0) {
            GD.PrintErr("⚠ Levels 未设置，请在 Inspector 添加关卡节点！");
            return;
        }
        ShowLevel(CurrentLevelIndex);
        await _transition.FadeFromBlack(transitionTime);
    }

    public async Task SwitchLevel(int newIndex) {
        if (newIndex < 0 || newIndex >= Levels.Length) return;
        if (newIndex == CurrentLevelIndex) return;
        await _transition.FadeToBlack(transitionTime);
        Levels[CurrentLevelIndex].Visible = false;
        CurrentLevelIndex = newIndex;
        Levels[CurrentLevelIndex].Visible = true;
        await _transition.FadeFromBlack(transitionTime);
    }

    private void ShowLevel(int index) {
        for (int i = 0; i < Levels.Length; i++)
            Levels[i].Visible = (i == index);
    }
}