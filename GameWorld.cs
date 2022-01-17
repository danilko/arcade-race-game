using Godot;
using System;

public class GameWorld : Spatial
{
    private GameStates _gameStates;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    public void _onReady()
    {
        _gameStates = (GameStates)GetNode("/root/GAMESTATES");
        if(_gameStates.CurrentVehicleImplementation == GameStates.VehicleImplementation.KINEMATIC)
        {
            InitializeVehicle();
        }
        else
        {
            InitializeSpatialVehicle();
        }
    }

    public void InitializeVehicle()
    {
        KinematicVehicle kinematicVehicle = (KinematicVehicle)((PackedScene)GD.Load("res://vehicles/KinematicVehicle.tscn")).Instance();
        kinematicVehicle.Transform = ((Position3D)GetNode("track_f1/vehiclePosition")).Transform;
        this.AddChild(kinematicVehicle);

        Camera camera = ((Camera)GetNode("Camera"));
        camera.Initialize(kinematicVehicle);

        HUD hud = ((HUD)GetNode("HUD"));
        hud.Initialize(kinematicVehicle);

        kinematicVehicle.Initialize();
    }

    public void InitializeSpatialVehicle()
    {
        SpatialVehicle ghostSpatialVehicle = null;

        SpatialVehicle spatialVehicle = (SpatialVehicle)((PackedScene)GD.Load("res://vehicles/SpatialVehicle.tscn")).Instance();
        spatialVehicle.Transform = ((Position3D)GetNode("track_f1/vehiclePosition")).Transform;
        this.AddChild(spatialVehicle);

        Camera camera = ((Camera)GetNode("Camera"));
        camera.Initialize((Spatial)spatialVehicle.GetNode("vehicle"));

        HUD hud = ((HUD)GetNode("HUD"));
        hud.Initialize(spatialVehicle);

        spatialVehicle.Initialize(false);

        // If the game mode is ghost mode, create one more vehicle to host the ghost mode data
        if(_gameStates.GetGhostMode())
        {
            ghostSpatialVehicle = (SpatialVehicle)((PackedScene)GD.Load("res://vehicles/SpatialVehicle.tscn")).Instance();
            ghostSpatialVehicle.Transform = ((Position3D)GetNode("track_f1/vehiclePosition")).Transform;
            this.AddChild(ghostSpatialVehicle);

            // Set to ghost mode
            ghostSpatialVehicle.Initialize(true);
        }

        // Enable control on both (in future will use a timer state to trigger)
        if(ghostSpatialVehicle != null)
        {
            ghostSpatialVehicle.AllowControl();
        }

        spatialVehicle.AllowControl();
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
