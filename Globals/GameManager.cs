using Godot;
using System.Collections.Generic;

public partial class GameManager : Node {
    const int totalLevels = 3;
    private Dictionary<int, PackedScene> levels = new Dictionary<int, PackedScene>();
    private int currentLevel = 0;

    public static GameManager Instance { get; private set; }

    public override void _Ready() {
        Instance = this;
        PackedScene gameScene = GD.Load<PackedScene>("res://Scenes/Game/game.tscn");
        if (gameScene == null) {
            GD.PushError("Failed to load game scene: res://Scenes/Game/game.tscn");
            return;
        }

        levels.Add(1, gameScene);
    }

    private void SetNextLevel() {
        this.currentLevel++;
        if (this.currentLevel > totalLevels) {
            this.currentLevel = 1;
        }
    }

    public static void LoadNextLevelScene() {
        Instance.SetNextLevel();
        if (!Instance.levels.TryGetValue(Instance.currentLevel, out PackedScene scene) || scene == null) {
            GD.PushError($"Level {Instance.currentLevel} is not loaded.");
            return;
        }

        Instance.GetTree().ChangeSceneToPacked(scene);
    }
}
