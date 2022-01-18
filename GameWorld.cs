using Godot;
using System.Collections.Generic;

public class GameWorld : Spatial
{
    [Signal]
    public delegate void StartTimerChange();

    private GameStates _gameStates;

    private List<SpatialVehicle> vehicles;

    private int _startTimerCounter;
    // Count down before vehicle can be controlled

    private int _initialStartTimerTime = 5;

    private Timer _startTimer;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _startTimer = (Timer)GetNode("StartTimer");
        // Need to add 1 as that additional 1 will not even show up due to how UI logic is written
        _startTimerCounter = _initialStartTimerTime + 1;
        vehicles = new List<SpatialVehicle>();
        _gameStates = (GameStates)GetNode("/root/GAMESTATES");
    }

    public void _onReady()
    {
        if (_gameStates.CurrentVehicleImplementation == GameStates.VehicleImplementation.KINEMATIC)
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
        SpatialVehicle spatialVehicle = (SpatialVehicle)((PackedScene)GD.Load("res://vehicles/SpatialVehicle.tscn")).Instance();
        spatialVehicle.Transform = ((Position3D)GetNode("track_f1/vehiclePosition")).Transform;
        this.AddChild(spatialVehicle);

        Camera camera = ((Camera)GetNode("Camera"));
        camera.Initialize((Spatial)spatialVehicle.GetNode("vehicle"));

        HUD hud = ((HUD)GetNode("HUD"));
        hud.Initialize(this, spatialVehicle);

        spatialVehicle.Initialize(false);
        vehicles.Add(spatialVehicle);

        // If the game mode is ghost mode, create one more vehicle to host the ghost mode data
        if (_gameStates.GetGhostMode())
        {
            spatialVehicle = (SpatialVehicle)((PackedScene)GD.Load("res://vehicles/SpatialVehicle.tscn")).Instance();
            spatialVehicle.Transform = ((Position3D)GetNode("track_f1/vehiclePosition")).Transform;
            this.AddChild(spatialVehicle);

            // Set to ghost mode
            spatialVehicle.Initialize(true);
            vehicles.Add(spatialVehicle);
        }

        // Start count down timer
        _startTimer.Start();
    }

    private void _startTimerTimeout()
    {
        _startTimerCounter--;
        EmitSignal(nameof(StartTimerChange), _startTimerCounter);

        // Reach 0, no longer
        if (_startTimerCounter == 0)
        {
            foreach (SpatialVehicle vehicle in vehicles)
            {
                vehicle.AllowControl();
            }
        }
        else
        {
            _startTimer.Start();
        }

    }

    //  // Called every frame. 'delta' is the elapsed time since the previous frame.
    //  public override void _Process(float delta)
    //  {
    //      
    //  }
}
