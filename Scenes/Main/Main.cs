using Godot;

public partial class Main : Control {
    public override void _Process(double delta) {
        if (Input.IsActionJustPressed("confirm")) {
            GameManager.LoadNextLevelScene();
        }
        if (Input.IsActionJustPressed("quit")) {
            GetTree().Quit();
        }
    }
    
    
}