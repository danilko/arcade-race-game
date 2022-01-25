using Godot;
using System;

public class CheckPoint : Area
{
    private int _checkPointIndex = 0;

    private Track _track;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    public void Initialize(Track track, int checkPointIndex)
    {
        _track = track;
        _checkPointIndex = checkPointIndex;
    }

    private void _onCheckPointEntered(Node body)
    {
        // Minimum implementation for KinematicVehicle
        if (body is KinematicVehicle)
        {
            ((KinematicVehicle)body).LapTimerStart();
            return;
        }

        SpatialVehicle vehicle = null;

        if (body.GetParent() is SpatialVehicle)
        {
            vehicle = ((SpatialVehicle)body.GetParent());
        }
        else
        {
            return;
        }

        // checkpoint index 0 is act as the finish line
        if (_checkPointIndex == 0)
        {
            vehicle.LapTimerStart();
        }

        // Update checkpoint if allowed
        // If the vehicle current checkpoint + 1 == current, the vehicle is moving correctly, update the checkpoint
        // Or if the vehicle current checkpoint + 1 >= num of checkpoints and current checkpoint is 0, this is the last checkpoint
        if(vehicle.GetCheckPointIndex() + 1 == _checkPointIndex || 
        (_checkPointIndex == 0 && vehicle.GetCheckPointIndex() + 1 >=  _track.GetCheckPoints().Count))
        {
            vehicle.SetCheckPointIndex(_checkPointIndex); 
        }
        else
        {
            // The vehicle is not in correct direction, notify it
            vehicle.NotifyInvalidCheckPoint();
        }
    }


    //  // Called every frame. 'delta' is the elapsed time since the previous frame.
    //  public override void _Process(float delta)
    //  {
    //      
    //  }
}
