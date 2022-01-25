using Godot;
using System;
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

    private Track _track;

    private HUD _hud;

    public class VehiclePosition
    {
        public float Length = 0;

        public int CheckPointsCount = 0;

        public String Name;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _startTimer = (Timer)GetNode("StartTimer");
        // Need to add 1 as that additional 1 will not even show up due to how UI logic is written
        _startTimerCounter = _initialStartTimerTime + 1;
        vehicles = new List<SpatialVehicle>();
        _gameStates = (GameStates)GetNode("/root/GAMESTATES");
        _track = (Track)GetNode("track_f1");
    }

    public void _onReady()
    {
        _track.Initialize();

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

    public Spatial GetTrack()
    {
        return _track;
    }

    public List<SpatialVehicle> GetVehciles()
    {
        return vehicles;
    }

    public void InitializeSpatialVehicle()
    {
        SpatialVehicle spatialVehicle = (SpatialVehicle)((PackedScene)GD.Load("res://vehicles/SpatialVehicle.tscn")).Instance();
        spatialVehicle.Transform = ((Position3D)GetNode("track_f1/vehiclePosition")).Transform;
        this.AddChild(spatialVehicle);

        Camera camera = ((Camera)GetNode("Camera"));
        camera.Initialize((Spatial)spatialVehicle.GetNode("vehicle"));

        _hud = ((HUD)GetNode("HUD"));
        _hud.Initialize(this, spatialVehicle);

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

        // Initialize minimap after all vehicles are loaded, need to clean up so may able to load at same time as HUD instead of separate logic
        _hud.GetMiniMap().Iniitialize(this);

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

    private void _checkPosition()
    {
        List<VehiclePosition> positions = new List<VehiclePosition>();

        foreach (SpatialVehicle vehicle in vehicles)
        {
            VehiclePosition vehiclePosition = new VehiclePosition();
            vehiclePosition.Name = vehicle.Name;

            int currentCheckPoint = vehicle.GetCheckPointIndex();

            if (vehicle.GetCheckPointIndex() == -1)
            {
                currentCheckPoint = 0;
            }

            int nextCheckPoint = vehicle.GetCheckPointIndex() + 1;

            if (nextCheckPoint >= _track.GetCheckPoints().Count)
            {
                nextCheckPoint = 0;
            }

            vehiclePosition.CheckPointsCount = (vehicle.GetLaps() * _track.GetCheckPoints().Count) + currentCheckPoint;

            vehiclePosition.Length = vehicle.GetVehicleGlobalTransform().origin.DistanceTo(_track.GetCheckPoints()[nextCheckPoint].GlobalTransform.origin);

            positions.Add(vehiclePosition);
        }

        // If a checkpoints are less than b checkpoints, it means a travel less than b, so need to do 1, as a is sort after b
        // Else if a/b checkpoint are same or greater
        // If a is greater than b checkpoint count, return -1 as a is indeed before b
        // If checkpoint is same, then a.length > b.length (greater length means further away from next checkpoint), a will be after b, as it is further, so return 1, otherwise return -1
        positions.Sort((a, b) => ((a.CheckPointsCount < b.CheckPointsCount)?1:((a.CheckPointsCount > b.CheckPointsCount)?-1:((a.Length > b.Length)?1:-1))));
        _hud.UpdateVehiclePositions(positions);

    }

    //  // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        _checkPosition();
    }
}
