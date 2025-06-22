using Godot;
using System.Collections.Generic;

public partial class GameManager : Node {
    const int TotalLevels = 3;

    private Dictionary<int, PackedScene> _levels = new Dictionary<int, PackedScene>();
    
    public static GameManager Instance { get; private set; }
    
    public override void _Ready() {
        Instance = this;
        _levels.Add(
            0,
            GD.Load<PackedScene>($"res://Scenes/LevelBase/Level.tscn")
        );
    }
}
