using Godot;
using System.Collections.Generic;

public partial class AudioManager : Node {
    public static AudioManager Instance { get; private set; }

    private AudioStreamPlayer2D _player;

    private Dictionary<string, AudioStream> _sounds = new();

    public override void _Ready() {
        Instance = this;
        _player = GetNode<AudioStreamPlayer2D>("SfxPlayer");
        _sounds["eat"] = GD.Load<AudioStream>("res://Assets/Sounds/eat.wav");
        _sounds["kill"] = GD.Load<AudioStream>("res://Assets/Sounds/kill.wav");
        _sounds["die"] = GD.Load<AudioStream>("res://Assets/Sounds/die.wav");
        _sounds["exit"] = GD.Load<AudioStream>("res://Assets/Sounds/exit.wav");
    }

    public void PlaySound(string soundName) {
        if (_sounds.TryGetValue(soundName, out var stream)) {
            _player.Stream = stream;
            _player.Play();
        } else {
            GD.PrintErr($"Sound '{soundName}' not found in AudioManager.");
        }
    }
}