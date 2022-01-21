using Godot;
using System;

// https://github.com/jocamar/Godot-Post-Process-Outlines


public class Camera : Godot.Camera
{
    [Export]
    private float _lerpSpeed = 3.0f;

    [Export] Vector3 _offset = Vector3.Zero;

    [Export] Vector3 _rearviewOffset = new Vector3(0, 0, 100);

    private Spatial _target;

    public override void _Ready()
    {
    }

    public void Initialize(Spatial target)
    {
        _target = target;
    }

    public override void _PhysicsProcess(float delta)
    {
        if (_target == null)
        {

            return;
        }


        Transform targetPos = _target.GlobalTransform.Translated(_offset);

        this.GlobalTransform = GlobalTransform.InterpolateWith(targetPos, _lerpSpeed * delta);

        if (Input.IsActionPressed("rearview"))
        {
            // Use the difference from target to offset (which is 100 in z unit from the target) to achieve a back view
            LookAt(_target.GlobalTransform.Translated(_rearviewOffset).origin, Vector3.Up);
        }
        else
        {
            // If rear view not enable, just look directly at target directly
            LookAt(_target.GlobalTransform.origin, Vector3.Up);
        }

    }
}
