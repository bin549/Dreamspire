using Godot;
using System.Collections.Generic;

public partial class AudioManager : Node {
    public static AudioManager Instance { get; private set; }
    private AudioStreamPlayer2D player;
    private Dictionary<string, AudioStream> sounds = new();

    public override void _Ready() {
        Instance = this;
        this.player = GetNode<AudioStreamPlayer2D>("SfxPlayer");
        this.sounds["eat"] = GD.Load<AudioStream>("res://Assets/Sounds/eat.wav");
        this.sounds["kill"] = GD.Load<AudioStream>("res://Assets/Sounds/kill.wav");
        this.sounds["die"] = GD.Load<AudioStream>("res://Assets/Sounds/die.wav");
        this.sounds["exit"] = GD.Load<AudioStream>("res://Assets/Sounds/exit.wav");
    }

    public void PlaySound(string soundName) {
        if (this.sounds.TryGetValue(soundName, out var stream)) {
            this.player.Stream = stream;
            this.player.Play();
        } else {
            GD.PrintErr($"Sound '{soundName}' not found in AudioManager.");
        }
    }
}