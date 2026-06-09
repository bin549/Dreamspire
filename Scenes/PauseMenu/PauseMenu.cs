using Godot;

public partial class PauseMenu : CanvasLayer {
    private Control root;
    private CheckButton fullscreenToggle;
    private HSlider volumeSlider;
    private Label volumeValueLabel;
    private Button resumeButton;
    private int masterBusIndex;
    private bool isRefreshing;

    public override void _Ready() {
        ProcessMode = ProcessModeEnum.Always;
        this.root = GetNode<Control>("Root");
        this.fullscreenToggle = GetNode<CheckButton>("Root/PanelContainer/MarginContainer/VBoxContainer/FullscreenRow/FullscreenToggle");
        this.volumeSlider = GetNode<HSlider>("Root/PanelContainer/MarginContainer/VBoxContainer/VolumeRow/VolumeSlider");
        this.volumeValueLabel = GetNode<Label>("Root/PanelContainer/MarginContainer/VBoxContainer/VolumeRow/VolumeValue");
        this.resumeButton = GetNode<Button>("Root/PanelContainer/MarginContainer/VBoxContainer/ResumeButton");
        Button quitButton = GetNode<Button>("Root/PanelContainer/MarginContainer/VBoxContainer/QuitButton");
        this.masterBusIndex = AudioServer.GetBusIndex("Master");
        this.root.Visible = false;
        RefreshControls();
        this.resumeButton.Pressed += () => SetPaused(false);
        this.fullscreenToggle.Toggled += OnFullscreenToggled;
        this.volumeSlider.ValueChanged += OnVolumeChanged;
        quitButton.Pressed += QuitGame;
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (@event.IsActionPressed("quit")) {
            SetPaused(!this.root.Visible);
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _ExitTree() {
        if (GetTree() != null && this.root != null && this.root.Visible) {
            GetTree().Paused = false;
        }
    }

    private void SetPaused(bool paused) {
        this.root.Visible = paused;
        GetTree().Paused = paused;
        if (paused) {
            RefreshControls();
            this.resumeButton.GrabFocus();
        }
    }

    private void RefreshControls() {
        this.isRefreshing = true;
        this.fullscreenToggle.ButtonPressed = IsFullscreen();
        float linearVolume = Mathf.DbToLinear(AudioServer.GetBusVolumeDb(this.masterBusIndex));
        this.volumeSlider.Value = Mathf.RoundToInt(Mathf.Clamp(linearVolume, 0f, 1f) * 100f);
        UpdateVolumeLabel(this.volumeSlider.Value);
        this.isRefreshing = false;
    }

    private void OnFullscreenToggled(bool enabled) {
        if (this.isRefreshing) return;
        DisplayServer.WindowSetMode(enabled
            ? DisplayServer.WindowMode.ExclusiveFullscreen
            : DisplayServer.WindowMode.Windowed);
    }

    private void OnVolumeChanged(double value) {
        float linearVolume = Mathf.Clamp((float)value / 100f, 0f, 1f);
        float dbVolume = linearVolume <= 0f ? -80f : Mathf.LinearToDb(linearVolume);
        AudioServer.SetBusVolumeDb(this.masterBusIndex, dbVolume);
        UpdateVolumeLabel(value);
    }

    private void UpdateVolumeLabel(double value) {
        this.volumeValueLabel.Text = $"{Mathf.RoundToInt((float)value)}%";
    }

    private bool IsFullscreen() {
        DisplayServer.WindowMode mode = DisplayServer.WindowGetMode();
        return mode == DisplayServer.WindowMode.Fullscreen ||
               mode == DisplayServer.WindowMode.ExclusiveFullscreen;
    }

    private void QuitGame() {
        GetTree().Paused = false;
        GetTree().Quit();
    }
}
