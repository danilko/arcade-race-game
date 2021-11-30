using Godot;
using System;

public class Track : Spatial
{

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }

    private void _onDetectionZoneEntered(Node body)
    {
        if(body is KinematicVehicle)
        {
            ((KinematicVehicle)body).LapTimerStart();
        }
        else if(body.GetParent() is SpatialVehicle)
        {
            ((SpatialVehicle)body.GetParent()).LapTimerStart();
        }
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
