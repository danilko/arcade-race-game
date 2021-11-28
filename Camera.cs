using Godot;
using System;

// https://github.com/jocamar/Godot-Post-Process-Outlines


public class Camera : Godot.Camera
{
    [Export]
    private float _lerpSpeed = 3.0f;

    [Export]
    NodePath _targetPath = null;
    [Export] Vector3 _offset = Vector3.Zero;

    private Spatial _target;

    public override void _Ready()
    {
        if (_targetPath != null)
        {
            _target = (Spatial)GetNode(_targetPath);
        }
    }


    public override void _PhysicsProcess(float delta)
    {
        if (_target == null)
        {
            
            return;
        }

        Transform targetPos = _target.GlobalTransform.Translated(_offset);

        this.GlobalTransform = GlobalTransform.InterpolateWith(targetPos, _lerpSpeed * delta);
        LookAt(_target.GlobalTransform.origin, Vector3.Up);
    }
}
