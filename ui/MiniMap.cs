using Godot;
using System;
using System.Collections.Generic;

public class MiniMap : MarginContainer
{
    private Sprite _vechicleMarker;
    private TextureRect _map;

    private Vector2 _mapCorner;
    private float _mapScale;
    private Vector2 _mapLength;

    private List<SpatialVehicle> _vehicles;

    private Dictionary<String, Sprite> _vechicleMarkers;

    private GameWorld _gameWorld;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _map = (TextureRect)GetNode("Map");
        _vechicleMarker = (Sprite)GetNode("VehicleMarkerTemplate");
        _gameWorld = null;
        _vehicles = null;

        _vechicleMarkers = new Dictionary<String, Sprite>();
    }

    public void Iniitialize(GameWorld gameWorld)
    {
        _gameWorld = gameWorld;

        _vehicles = _gameWorld.GetVehciles();

        // Initialize vheicle sprites
        foreach(SpatialVehicle vehicle in _vehicles)
        {
            Sprite vechicleMarker = (Sprite)_vechicleMarker.Duplicate();
            vechicleMarker.Name = vehicle.Name + "_marker";
            vechicleMarker.SelfModulate = new Color(1, 0, 0, 1);
            vechicleMarker.Show();

            _map.AddChild(vechicleMarker);

            // Add marker to dictionary
            _vechicleMarkers.Add(vehicle.Name, vechicleMarker);
        }

        // Get the corner in global position
        // Get the ground object
        MeshInstance ground = (MeshInstance)_gameWorld.GetTrack().GetNode("grounds");
        Vector3 origin = ground.GlobalTransform.origin;
        AABB aabb = ground.GetAabb().Abs();

        // Basically:
        // Origin is the position in current 3D
        // aabb position is the beginning corner (at least for x and z)
        // aabb position * aabb scale will get the beginning corner of the scaled mesh
        // origin of the world to get the top corner in the world
        _mapCorner = new Vector2(origin.x + (aabb.Position.x * ground.GlobalTransform.basis.Scale.x), origin.z + (aabb.Position.z * ground.GlobalTransform.basis.Scale.z));

        // Need to times the base aabb by the scale of the actual mesh
        _mapLength = new Vector2(aabb.Size.x * ground.GlobalTransform.basis.Scale.x, aabb.Size.z * ground.GlobalTransform.basis.Scale.z);
    }

    private void _updateMap()
    {
        if (_mapLength == Vector2.Zero)
        {
            return;
        }

        // Calculate the scale
        float mapScaleX = _map.GetRect().Size.x / _mapLength.x;
        float mapScaleY = _map.GetRect().Size.y / _mapLength.y;

        foreach (SpatialVehicle vehicle in _vehicles)
        {
            if (vehicle == null || !IsInstanceValid(vehicle))
            {
                return;
            }

            
            Sprite vehicleMarker = _vechicleMarkers[vehicle.Name]; 

            if (vehicleMarker != null && IsInstanceValid(vehicleMarker))
            {
                //GD.Print("angle" + vehicle.GetVehicleGlobalTransform().basis.GetEuler().y);
                vehicleMarker.GlobalRotation = -vehicle.GetVehicleGlobalTransform().basis.GetEuler().y;

                // In UI, the x is horizontal, y is vertical
                // Map to 3D, need to be z axis, and x axis
                Vector2 markerPosition = new Vector2(((vehicle.GetVehicleGlobalTransform().origin.x - _mapCorner.x) * mapScaleX), 
                ((vehicle.GetVehicleGlobalTransform().origin.z - _mapCorner.y) * mapScaleY));

                // Update marker
                vehicleMarker.Position = markerPosition;
            }
        }
    }

    public override void _Process(float delta)
    {
        _updateMap();
    }
}
