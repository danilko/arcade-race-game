using Godot;
using System;

public class SpatialVehicle : Spatial
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

    private TransformMode _transformMode;

    private RigidBody _rigidBody;
    private Spatial _vehicleModel;
    private RayCast _groundRay;

    [Export]
    // Where to place the car mesh relative to the sphere
    private Vector3 _sphereoffset = new Vector3(0.0f, -0.6f, 0.0f);
    // Engine power

    [Export]
    private float _acceleration = 100.0f;

    [Export]
    // Turn amount, in degrees
    private float _steering = 21.0f;

    [Export]
    // How quickly the car turns
    private float _turnSpeed = 5.0f;

    // Below this speed, the car doesn't turn
    [Export]
    float _turnStopLimit = 0.75f;

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

    // Variables for input values
    float _speedInput = 0.0f;
    float _rotateInput = 0.0f;

    Position3D cameraOrigin;

    private int _lapCounter;
    private float _lapTimeCounter;
    private Timer _lapTimer;

    private AnimationPlayer vehicleAnimationPlayer;

    public enum BoosterMode
    {
        OFF,
        ON,
        BUST
    }

    public override void _Ready()
    {
        _boostTimer = (Timer)GetNode("BoosterTimer");
        _boostParticles1 = (Particles)GetNode("vehicle/BoostParticles1");
        _boostParticles2 = (Particles)GetNode("vehicle/BoostParticles2");
        _boosterMode = BoosterMode.OFF;

        _lapTimer = (Timer)GetNode("LapTimer");
        _lapCounter = 0;
        _lapTimeCounter = 0.0f;

        _rigidBody = (RigidBody)GetNode("RigidBody");
        _vehicleModel = (Spatial)GetNode("vehicle");
        _groundRay = (RayCast)GetNode("vehicle/GroundRay");

        vehicleAnimationPlayer = (AnimationPlayer)GetNode("vehicle/AnimationPlayer");

        // Raycast to not collide with rigidBody
        _groundRay.AddException(_rigidBody);

        cameraOrigin = (Position3D)GetNode("Position3D");
    }

    public void Initialize()
    {
        _updateTransformMode(TransformMode.CIRCUIT);
    }

    private void _updateTransformMode(TransformMode transformMode)
    {
        _transformMode = transformMode;

        if (transformMode == TransformMode.CIRCUIT)
        {
            vehicleAnimationPlayer.Play("transform");
            _acceleration = 100.0f;
            _steering = 21.0f;
        }
        else
        {
            vehicleAnimationPlayer.Play("bustbooster");
            _acceleration = 150.0f;
            _steering = 21.0f;
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



    public void GetInput(float delta)
    {
        //Can't steer/accelerate when in the air
        if (!_groundRay.IsColliding())
        {
            return;
        }

        // Get accelerate/brake input
        _speedInput = 0.0f;
        _speedInput += Input.GetActionStrength("accelerate");
        _speedInput -= Input.GetActionStrength("brake");
        _speedInput *= _acceleration;

        // Get steering input
        _rotateInput = 0.0f;
        _rotateInput += Input.GetActionStrength("steer_left");
        _rotateInput -= Input.GetActionStrength("steer_right");
        _rotateInput *= Mathf.Deg2Rad(_steering);

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

        // rotate car mesh
        if (_rigidBody.LinearVelocity.Length() > _turnStopLimit)
        {
            var newBasis = _vehicleModel.GlobalTransform.basis.Rotated(_vehicleModel.GlobalTransform.basis.y, _rotateInput);

            Transform transform = _vehicleModel.GlobalTransform;
            transform.basis = _vehicleModel.GlobalTransform.basis.Slerp(newBasis, _turnSpeed * delta);
            _vehicleModel.Transform = transform;

            _vehicleModel.GlobalTransform = _vehicleModel.GlobalTransform.Orthonormalized();
        }


        float visualRotationFactor = 1.0f;
        if (_transformMode == TransformMode.CIRCUIT)
        {
            visualRotationFactor = 1.5f;
        }


        Vector3 rotation = ((MeshInstance)GetNode("vehicle/modelwheel1")).Rotation;

        // Not rotate forward during turn to simulate sliding
        if (_rotateInput != 0)
        {
            rotation.x = 0.0f;
            rotation.y = _rotateInput * visualRotationFactor;
        }
        else
        {
            rotation.x = rotation.x * visualRotationFactor;
            rotation.y = 0.0f;
        }

        ((MeshInstance)GetNode("vehicle/modelwheel1")).Rotation = rotation;
        ((MeshInstance)GetNode("vehicle/modelwheel11")).Rotation = rotation;

        EmitSignal(nameof(UpdateRotation), rotation.z);

        rotation = ((MeshInstance)GetNode("vehicle/modelwheel0")).Rotation;

        // Not rotate forward during turn to simulate sliding
        if (_rotateInput != 0)
        {
            rotation.x = 0.0f;
            rotation.y = _rotateInput * visualRotationFactor;
        }
        else
        {
            rotation.x = rotation.x * visualRotationFactor;
            rotation.y = 0.0f;
        }


        ((MeshInstance)GetNode("vehicle/modelwheel0")).Rotation = rotation;
        ((MeshInstance)GetNode("vehicle/modelwheel01")).Rotation = rotation;


        EmitSignal(nameof(UpdateSpeed), (int)(_rigidBody.LinearVelocity.Length()));
    }

    public override void _PhysicsProcess(float delta)
    {
        GetInput(delta);

        // Keep the car mesh aligned with the sphere
        Transform transform = _vehicleModel.Transform;

        transform.origin = _rigidBody.Transform.origin + _sphereoffset;
        _vehicleModel.Transform = transform;

        // Accelerate based on car's forward direction
        _rigidBody.AddCentralForce(-_vehicleModel.GlobalTransform.basis.z * _speedInput);

        cameraOrigin.GlobalTransform = _vehicleModel.GlobalTransform;

    }
}