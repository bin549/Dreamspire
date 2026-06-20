using Godot;

public partial class Main : Control {
    private bool isStarting;

    public override void _Input(InputEvent @event) {
        if (IsStartPressed(@event)) {
            StartGame();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed("quit")) {
            GetTree().Quit();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta) {
        if (Input.IsActionJustPressed("confirm")) {
            StartGame();
        }
        if (Input.IsActionJustPressed("quit")) {
            GetTree().Quit();
        }
    }

    private bool IsStartPressed(InputEvent @event) {
        if (@event.IsActionPressed("confirm")) return true;
        if (@event is not InputEventKey keyEvent) return false;
        if (!keyEvent.Pressed || keyEvent.Echo) return false;

        return keyEvent.Keycode == Key.E ||
               keyEvent.PhysicalKeycode == Key.E ||
               keyEvent.KeyLabel == Key.E;
    }

    private void StartGame() {
        if (this.isStarting) return;
        this.isStarting = true;
        GameManager.LoadNextLevelScene();
    }
}
