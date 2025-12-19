using Godot;
using System.Collections.Generic;

public partial class GameManager : Node {
    const int totalLevels = 3;
    private Dictionary<int, PackedScene> levels = new Dictionary<int, PackedScene>();
    private int currentLevel = 0;

    public static GameManager Instance { get; private set; }

    public override void _Ready() {
        Instance = this;
        levels.Add(
            1,
            GD.Load<PackedScene>($"res://Scenes/Game/game.tscn")
        );
    }

    private void SetNextLevel() {
        this.currentLevel++;
        if (this.currentLevel > totalLevels) {
            this.currentLevel = 1;
        }
    }

    public static void LoadNextLevelScene() {
        Instance.SetNextLevel();
        Instance.GetTree().ChangeSceneToPacked(Instance.levels[Instance.currentLevel]);
    }
}