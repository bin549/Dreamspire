using Godot;
using System.Collections.Generic;

public partial class GameManager : Node {
    const int TotalLevels = 3;

    private Dictionary<int, PackedScene> _levels = new Dictionary<int, PackedScene>();
    
    int _currentLevel = 0;

    public static GameManager Instance { get; private set; }
    
    public override void _Ready() {
        Instance = this;
        _levels.Add(
            1,
            GD.Load<PackedScene>($"res://Scenes/Level/Level1.tscn")
        );
    }
    
    private void SetNextLevel() {
        _currentLevel++;
        if (_currentLevel > TotalLevels) {
            _currentLevel = 1;
        }
    }
    
    public static void LoadNextLevelScene() {
        Instance.SetNextLevel();
        Instance.GetTree().ChangeSceneToPacked(Instance._levels[Instance._currentLevel]);
    }
}
