using Godot;
using System;
using System.Collections.Generic;

public class Track : Spatial
{
    private List<CheckPoint> _checkpoints;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _checkpoints = new List<CheckPoint>();
    }

    public void Initialize()
    {
        int index = 0;
        foreach(CheckPoint checkpoint in GetNode("CheckPoints").GetChildren())
        {
            checkpoint.Initialize(this, index);
            _checkpoints.Add(checkpoint);
            index++;
        }
    }

    public List<CheckPoint>  GetCheckPoints()
    {
        return _checkpoints;
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
