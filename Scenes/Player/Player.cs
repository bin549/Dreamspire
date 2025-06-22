using Godot;
using System;

public partial class Player : CharacterBody2D
{
    [Export] public float Speed = 300f;

    public override void _PhysicsProcess(double delta)
    {
        Vector2 dir = Input.GetVector("ui_left","ui_right","ui_up","ui_down");
        Velocity = dir * Speed;
        MoveAndSlide();
    }
}