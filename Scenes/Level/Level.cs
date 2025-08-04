using Godot;
using System;
 
public partial class Level : Node2D {
    public TileMapLayer tileMapLayer;
    public Marker2D marker2D;
    
    public override async void _Ready() {
        this.tileMapLayer = GetNode<TileMapLayer>("TileMapLayer");
        this.marker2D = GetNode<Marker2D>("Entry");
    }


}