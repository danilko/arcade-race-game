using Godot;
using System;

public class GameWorld : Spatial
{

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    public void _onReady()
    {
        InitializeVehicle();
    }

    public void InitializeVehicle()
    {
        KinematicVehicle kinematicVehicle = (KinematicVehicle)((PackedScene)GD.Load("res://vehicles/KinematicVehicle.tscn")).Instance();
        kinematicVehicle.Transform = ((Position3D)GetNode("vehiclePosition")).Transform;
        this.AddChild(kinematicVehicle);

        Camera camera = ((Camera)GetNode("Camera"));
        camera.Initialize(kinematicVehicle);

        HUD hud = ((HUD)GetNode("HUD"));
        hud.Initialize(kinematicVehicle);

        kinematicVehicle.Initialize();
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
