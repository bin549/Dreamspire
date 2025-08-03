using Godot;
using System.Threading.Tasks;

public partial class SceneTransition : CanvasLayer {
    private ColorRect _fadeRect;

    public override void _Ready() {
        _fadeRect = GetNode<ColorRect>("FadeRect");
    }

    public async Task FadeToBlack(float time = 1f) {
        var tween = GetTree().CreateTween();
        tween.TweenProperty(_fadeRect, "modulate:a", 1f, time);
        await ToSignal(tween, "finished");
    }

    public async Task FadeFromBlack(float time = 1f) {
        var tween = GetTree().CreateTween();
        tween.TweenProperty(_fadeRect, "modulate:a", 0f, time);
        await ToSignal(tween, "finished");
    }
}