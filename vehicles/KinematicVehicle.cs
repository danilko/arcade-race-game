using Godot;
using System;

public class KinematicVehicle : KinematicBody
{

    [Signal]
    public delegate void UpdateRotation();

    [Signal]
    public delegate void UpdateDrifting();

    [Signal]
    public delegate void UpdateBoosterTimer();

    [Signal]
    public delegate void UpdateSpeed();

    [Signal]
    public delegate void UpdateLapTimer();

    [Signal]
    public delegate void UpdateTransformMode();

    [Signal]
    public delegate void CompleteLapTimer();

    [Signal]
    public delegate void UpdateBoosterCount();

    public enum TransformMode
    {
        CIRCUIT,
        AERO
    }

    public enum BoosterMode
    {
        OFF,
        ON,
        BUST
    }

    private TransformMode _transformMode;

    [Export]
    private float _gravity = -20.0f;

    // distance between front/rear axles
    [Export]
    private float _wheelBase = 10f;

    // front wheel max turning angle (deg)
    [Export]
    private float _steeringLimit = 5.0f;

    [Export]
    private float _enginePower = 2000.0f;

    [Export]
    private float _braking = -0.1f;
    [Export]
    private float _friction = -10.0f;
    [Export]
    private float _drag = -2.0f;
    [Export]
    private float _maxSpeedReverse = 100.0f;
    [Export]
    float _slipSpeed = 10.0f;
    [Export]
    private float _tractionSlow = 0.75f;
    [Export]
    private float _tractionFast = 0.02f;

    [Export]
    private float _boostTime = 8.0f;
    [Export]
    private float _bustBoostTime = 3.0f;
    [Export]
    private float _boosterCount = 6;

    private float _boostRemainTime;
    private Timer _boostTimer;
    private Particles _boostParticles2;
    private Particles _boostParticles1;
    private BoosterMode _boosterMode;

    // Car state properties
    // current acceleration
    private Vector3 _acceleration = Vector3.Zero;
    // current velocity
    private Vector3 _velocity = Vector3.Zero;
    // current wheel angle
    private float _steerAngle = 0.0f;
    bool _drifting = false;

    private int _lapCounter;
    private float _lapTimeCounter;
    private Timer _lapTimer;

    private AnimationPlayer vehicleAnimationPlayer;

    public override void _Ready()
    {
        _boostTimer = (Timer)GetNode("BoosterTimer");
        _boostParticles1 = (Particles)GetNode("BoostParticles1");
        _boostParticles2 = (Particles)GetNode("BoostParticles2");
        _boosterMode = BoosterMode.OFF;

        _lapTimer = (Timer)GetNode("LapTimer");
        _lapCounter = 0;
        _lapTimeCounter = 0.0f;

        vehicleAnimationPlayer = (AnimationPlayer)GetNode("vehicle/AnimationPlayer");

    }

    public void Initialize()
    {
        _updateTransformMode(TransformMode.CIRCUIT);

        EmitSignal(nameof(UpdateBoosterCount), _boosterCount);
    }

    private void _updateTransformMode(TransformMode transformMode)
    {
        _transformMode = transformMode;

        if (transformMode == TransformMode.CIRCUIT)
        {
            vehicleAnimationPlayer.Play("transform");
            
            _steeringLimit = 10.0f;
            _enginePower = 1000.0f;
        }
        else
        {
            vehicleAnimationPlayer.Play("bustbooster");
            _steeringLimit = 1.0f;
            _enginePower = 2000.0f;
        }

        EmitSignal(nameof(UpdateTransformMode), _transformMode);
    }

    public void LapTimerStart()
    {
        // Stop the timer first, as it is complete lap
        if (_lapTimeCounter != 0.0f)
        {
            LapTimerStop();
        }

        _lapCounter++;
        _lapTimeCounter = 0.0f;
        _lapTimer.Start();
    }
    public void LapTimerStop()
    {
        _lapTimer.Stop();
        EmitSignal(nameof(CompleteLapTimer), _lapTimeCounter, _lapCounter);
    }

    private void _lapTimerTimeout()
    {
        _lapTimeCounter++;
        EmitSignal(nameof(UpdateLapTimer), _lapTimeCounter, _lapCounter);
        _lapTimer.Start();
    }

    private void _stopBooster()
    {
        _boostRemainTime = 0.0f;
        _updateBoostRemainTime();
    }

    private void _startBooster()
    {
        // Only can enable booster if have remain
        if(_boosterCount <= 0)
        {
            return;
        }

        _boosterCount--;
        EmitSignal(nameof(UpdateBoosterCount), _boosterCount);

        _boosterMode = BoosterMode.ON;
        _boostRemainTime = _boostTime;

        _boostParticles1.Emitting = true;
        _boostParticles2.Emitting = true;

        EmitSignal(nameof(UpdateBoosterTimer), _boostRemainTime, _bustBoostTime, _boostTime, _boosterMode);

        _boostTimer.Start();
    }

    private void _startBustBooster()
    {
        _boosterMode = BoosterMode.BUST;
        _boostRemainTime = _bustBoostTime;

        _boostParticles1.Emitting = true;
        _boostParticles2.Emitting = true;

        EmitSignal(nameof(UpdateBoosterTimer), _boostRemainTime, _bustBoostTime, _boostTime, _boosterMode);

        _boostTimer.Start();
    }

    private void _updateBoostRemainTime()
    {
        _boostRemainTime--;
        EmitSignal(nameof(UpdateBoosterTimer), _boostRemainTime, _bustBoostTime, _boostTime, _boosterMode);

        // Continue toward count down
        if (_boostRemainTime > -1)
        {
            _boostTimer.Start();
        }
        else
        {
            // Stop booster
            _boosterMode = BoosterMode.OFF;
            _boostParticles1.Emitting = false;
            _boostParticles2.Emitting = false;
        }
    }

    private void _getInput(float delta)
    {
        float turn = Input.GetActionStrength("steer_left");
        turn -= Input.GetActionStrength("steer_right");

        _steerAngle = turn * Mathf.Deg2Rad(_steeringLimit);


        _acceleration = Vector3.Zero;

        if (Input.IsActionPressed("accelerate"))
        {
            _acceleration = -Transform.basis.z * _enginePower;

            if (_boosterMode == BoosterMode.ON)
            {
                _acceleration = _acceleration * 1.5f;
            }

            if (_boosterMode == BoosterMode.BUST)
            {
                _acceleration = _acceleration * 3f;
            }
        }

        if (Input.IsActionPressed("brake"))
        {
            _acceleration = -Transform.basis.z * _braking;
        }

        // Only can modify booster in aero mode
        if (Input.IsActionJustReleased("booster") && _transformMode == TransformMode.AERO)
        {
            if (_boosterMode == BoosterMode.OFF)
            {
                _startBooster();
            }
            else if (_boosterMode == BoosterMode.ON && _boostRemainTime <= _bustBoostTime)
            {
                _startBustBooster();
            }
            else
            {
                _stopBooster();
            }
        }

        // Only can transform if not in booster mode
        if (Input.IsActionJustReleased("transform") && _boosterMode == BoosterMode.OFF)
        {
            if (_transformMode == TransformMode.CIRCUIT)
            {
                _transformMode = TransformMode.AERO;
            }
            else
            {
                _transformMode = TransformMode.CIRCUIT;
            }

            _updateTransformMode(_transformMode);
        }

        float visualRotationFactor = 2.0f;
        if (_transformMode == TransformMode.CIRCUIT)
        {
            visualRotationFactor = 3.0f;
        }


        Vector3 rotation = ((MeshInstance)GetNode("vehicle/modelwheel1")).Rotation;

        // Not rotate forward during turn to simulate sliding
        if (turn != 0)
        {
            rotation.x = 0.0f;
            rotation.z = _steerAngle * visualRotationFactor;
        }
        else
        {
            rotation.x = rotation.x * visualRotationFactor;
            rotation.z = 0.0f;
        }

        ((MeshInstance)GetNode("vehicle/modelwheel1")).Rotation = rotation;
        ((MeshInstance)GetNode("vehicle/modelwheel11")).Rotation = rotation;

        EmitSignal(nameof(UpdateRotation), rotation.z);

        rotation = ((MeshInstance)GetNode("vehicle/modelwheel0")).Rotation;

        // Not rotate forward during turn to simulate sliding
        if (turn != 0)
        {
            rotation.x = 0.0f;

            rotation.z = _steerAngle * visualRotationFactor;
        }
        else
        {
            rotation.x = rotation.x * visualRotationFactor;
            rotation.z = 0.0f;
        }


        ((MeshInstance)GetNode("vehicle/modelwheel0")).Rotation = rotation;
        ((MeshInstance)GetNode("vehicle/modelwheel01")).Rotation = rotation;


    }
    private void _applyFriction(float delta)
    {
        if (_velocity.Length() < 0.2 && _acceleration.Length() == 0)
        {
            _velocity.x = 0.0f;
            _velocity.z = 0.0f;
        }

        Vector3 frictionForce = _velocity * _friction * delta;
        Vector3 dragForce = _velocity * _velocity.Length() * _drag * delta;
        _acceleration += dragForce + frictionForce;
    }
    private void _calculateSteering(float delta)
    {
        Vector3 rearWheel = Transform.origin + Transform.basis.z * _wheelBase / 2.0f;
        Vector3 frontWheel = Transform.origin - Transform.basis.z * _wheelBase / 2.0f;
        rearWheel += _velocity * delta;
        frontWheel += _velocity.Rotated(Transform.basis.y, _steerAngle) * delta;
        Vector3 newHeading = rearWheel.DirectionTo(frontWheel);

        if (!_drifting && _velocity.Length() > _slipSpeed)
        {
            _drifting = true;
        }

        if (_drifting && _velocity.Length() < _slipSpeed && _steerAngle == 0)
        {
            _drifting = false;
        }

        float traction = _drifting ? _tractionFast : _tractionSlow;


        float d = newHeading.Dot(_velocity.Normalized());
        if (d > 0)
        {
            _velocity.x = Mathf.Lerp(_velocity.x, newHeading.x * _velocity.Length(), traction);
            _velocity.y = Mathf.Lerp(_velocity.y, newHeading.y * _velocity.Length(), traction);
            _velocity.z = Mathf.Lerp(_velocity.z, newHeading.z * _velocity.Length(), traction);
        }

        if (d < 0)
        {
            _velocity = -newHeading * Mathf.Min(_velocity.Length(), _maxSpeedReverse);
        }

        LookAt(Transform.origin + newHeading, Transform.basis.y);
    }

    public override void _PhysicsProcess(float delta)
    {
        if (IsOnFloor())
        {
            _getInput(delta);
            _applyFriction(delta);
            _calculateSteering(delta);
        }

        _acceleration.y = _gravity;
        _velocity += _acceleration * delta;

        EmitSignal(nameof(UpdateSpeed), (int)_velocity.Length());
        _velocity = MoveAndSlideWithSnap(_velocity, -Transform.basis.y, Vector3.Up, true);

    }
}
