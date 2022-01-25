using Godot;
using System;
using System.Collections.Generic;

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

    private GameStates _gameStates;

    public enum TransformMode
    {
        CIRCUIT,
        AERO,
        AERO_BUST
    }

    private TransformMode _transformMode;

    private RigidBody _rigidBody;
    private Spatial _vehicleModel;
    private RayCast _groundRay;

    [Export]
    // Where to place the car mesh relative to the sphere
    private Vector3 _sphereoffset = new Vector3(0.0f, -1.75f, 0.0f);
    // Engine power

    [Export]
    private float _acceleration = 100.0f;

    [Export]
    // Turn amount, in degrees
    private float _steering = 21.0f;

    [Export]
    // How quickly the car turns
    private float _turnSpeed = 3.0f;

    // Below this speed, the car doesn't turn
    [Export]
    float _turnStopLimit = 0.75f;

    [Export]
    private float _boostTime = 5.0f;
    [Export]
    private float _bustBoostTime = 2.0f;
    [Export]
    private float _boosterCount = 6;
    [Export]
    private float _bodyTilt = 175.0f;

    [Export]
    private float _boosterAccelerationFactor = 1.3f;
    [Export]
    private float _busterboosterAccelerationFactor = 1.5f;
    
    private float _boostRemainTime;
    private Timer _boostTimer;
    private Particles _boostParticles2;
    private Particles _boostParticles1;

    private Particles _bustBoostParticles2;
    private Particles _bustBoostParticles1;
    private BoosterMode _boosterMode;

    private Position3D _boosterParticlePosition1;
    private Position3D _boosterParticlePosition2;
    private Position3D _boosterBustParticlePosition1;
    private Position3D _boosterBustParticlePosition2;

    // Variables for input values
    float _speedInput = 0.0f;
    float _rotateInput = 0.0f;

    private int _lapCounter;
    private float _lapTimeCounter;
    private Timer _lapTimer;

    private int _currentCheckPointIndex;

    private Boolean _allowControl;

    private List<VehicleState> _inMemoryVehicleState;

    private AnimationPlayer vehicleAnimationPlayer;

    // Used to control the backlight/rim-backlight
    private SpatialMaterial _backlight;
    private SpatialMaterial _rimBacklight;

    private Color _BacklightOnColor = new Color(1f, 0f, 0f, 1f);
    private Color _BacklightOffColor = new Color(0f, 0f, 0f, 1f);
    public class KeyInput
    {
        public Boolean Booster { get; set; }
        public Boolean BurstBooster { get; set; }
        public Boolean Transform { get; set; }
        public float SpeedInput { get; set; }
        public float RotateInput { get; set; }

        public KeyInput()
        {
            Booster = false;
            BurstBooster = false;
            Transform = false;
            SpeedInput = 0.0f;
            RotateInput = 0.0f;
        }

        public KeyInput(String input)
        {
            int index = 0;

            Booster = Boolean.Parse(input.Split(',')[index]);
            index++;

            BurstBooster = Boolean.Parse(input.Split(',')[index]);
            index++;

            Transform = Boolean.Parse(input.Split(',')[index]);
            index++;

            SpeedInput = float.Parse(input.Split(',')[index]);
            index++;

            RotateInput = float.Parse(input.Split(',')[index]);
            index++;
        }

        public override String ToString()
        {
            return Booster + "," + BurstBooster + "," + Transform + "," + SpeedInput + "," + RotateInput;
        }
    }

    public class VehicleState
    {
        public Vector3 RigidBodyOrigin { get; set; }
        public Vector3 RigidBodyLinearVelocity { get; set; }
        public KeyInput KeyInput { get; set; }

        public VehicleState()
        {
            RigidBodyOrigin = Vector3.Zero;
            RigidBodyLinearVelocity = Vector3.Zero;
            KeyInput = new KeyInput();
        }

        public VehicleState(String input)
        {
            KeyInput = new KeyInput(input);

            // This is base on KeyInput's index usage, may need better way to handle in future
            int index = 5;

            float x = float.Parse(input.Split(',')[index]);
            index++;

            float y = float.Parse(input.Split(',')[index]);
            index++;

            float z = float.Parse(input.Split(',')[index]);
            index++;

            RigidBodyOrigin = new Vector3(x, y, z);

            x = float.Parse(input.Split(',')[index]);
            index++;

            y = float.Parse(input.Split(',')[index]);
            index++;

            z = float.Parse(input.Split(',')[index]);
            index++;

            RigidBodyLinearVelocity = new Vector3(x, y, z);
        }

        public override String ToString()
        {
            return KeyInput.ToString() + "," + RigidBodyOrigin.x + "," + RigidBodyOrigin.y + "," + RigidBodyOrigin.z + "," + RigidBodyLinearVelocity.x + "," + RigidBodyLinearVelocity.y + "," + RigidBodyLinearVelocity.z;
        }
    }


    public enum BoosterMode
    {
        OFF,
        ON,
        BUST
    }

    public override void _Ready()
    {
        _inMemoryVehicleState = null;

        // Default vehicle will not allow to be controlled, until game is ready
        _allowControl = false;

        _gameStates = (GameStates)GetNode("/root/GAMESTATES");

        _boostTimer = (Timer)GetNode("BoosterTimer");
        _boostParticles1 = (Particles)GetNode("vehicle/BoostParticles1");
        _boostParticles2 = (Particles)GetNode("vehicle/BoostParticles2");
        _bustBoostParticles2 = (Particles)GetNode("vehicle/BustBoostParticles1");
        _bustBoostParticles1 = (Particles)GetNode("vehicle/BustBoostParticles2");
        _boosterParticlePosition1 = (Position3D)GetNode("vehicle/BoostParticles1Position");
        _boosterParticlePosition2 = (Position3D)GetNode("vehicle/BoostParticles2Position");
        _boosterBustParticlePosition1 = (Position3D)GetNode("vehicle/BoostBustParticles1Position");
        _boosterBustParticlePosition2 = (Position3D)GetNode("vehicle/BoostBustParticles2Position");

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
    }

    public void AllowControl()
    {
        _allowControl = true;
    }

    public int GetLaps()
    {
        return _lapCounter;
    }


    public void SetCheckPointIndex(int checkpointIndex)
    {
        _currentCheckPointIndex = checkpointIndex;
    }

    public int GetCheckPointIndex()
    {
        return _currentCheckPointIndex;
    }

    public void NotifyInvalidCheckPoint()
    {

    }

    public void Initialize(Boolean isGhostMode)
    {
        // Initial check point is -1 to indicate a not exist index
        _currentCheckPointIndex = -1;

        // Need to duplicate material, otherwise will cause the material to be reset for all vehicle
        // For chasis
        // Get this origin mash material to only apply to this current instanced mesh to not impact others
        int materialIndex = 1;
        _backlight = (SpatialMaterial)((MeshInstance)_vehicleModel.GetNode("armcar/Skeleton/modelvehicle")).Mesh.SurfaceGetMaterial(materialIndex).Duplicate();
        // Set this current mesh (not the origin origin) to only apply to this current instanced mesh to not impact others
        ((MeshInstance)_vehicleModel.GetNode("armcar/Skeleton/modelvehicle")).SetSurfaceMaterial(materialIndex, _backlight);

        // For wheels backlight
        materialIndex = 2;
        _rimBacklight = (SpatialMaterial)((MeshInstance)_vehicleModel.GetNode("modelwheel1")).Mesh.SurfaceGetMaterial(materialIndex).Duplicate();
        ((MeshInstance)_vehicleModel.GetNode("modelwheel1")).SetSurfaceMaterial(materialIndex, _rimBacklight);
        ((MeshInstance)_vehicleModel.GetNode("modelwheel11")).SetSurfaceMaterial(materialIndex, _rimBacklight);
        ((MeshInstance)_vehicleModel.GetNode("modelwheel0")).SetSurfaceMaterial(materialIndex, _rimBacklight);
        ((MeshInstance)_vehicleModel.GetNode("modelwheel01")).SetSurfaceMaterial(materialIndex, _rimBacklight);
        ((MeshInstance)_vehicleModel.GetNode("modelwheel3")).SetSurfaceMaterial(materialIndex, _rimBacklight);
        ((MeshInstance)_vehicleModel.GetNode("modelwheel2")).SetSurfaceMaterial(materialIndex, _rimBacklight);

        // Enable emission as this will be used as backlight
        _backlight.EmissionEnabled = true;
        _rimBacklight.EmissionEnabled = true;

        if (isGhostMode)
        {
            _inMemoryVehicleState = _gameStates.LoadRecord();
            // Disable the vehicle collision between ghost and real vehicle
            _rigidBody.SetCollisionLayerBit(1, false);

            // Set the transparency
            // Need to duplicate material, otherwise will cause the material to be reset for all vehicle
            // For chasis
            // Get this origin mash material to only apply to this current instanced mesh to not impact others
            materialIndex = 0;
            SpatialMaterial material = (SpatialMaterial)((MeshInstance)_vehicleModel.GetNode("armcar/Skeleton/modelvehicle")).Mesh.SurfaceGetMaterial(materialIndex).Duplicate();
            material.FlagsTransparent = true;
            // Set the transparency
            Color color = material.AlbedoColor;
            color.a = 0.2f;
            material.AlbedoColor = color;
            // Set this current mesh (not the origin origin) to only apply to this current instanced mesh to not impact others
            ((MeshInstance)_vehicleModel.GetNode("armcar/Skeleton/modelvehicle")).SetSurfaceMaterial(materialIndex, material);

            // For wheels
            // Need to do for both rim and tire
            for (int index = 0; index < 2; index++)
            {
                materialIndex = index;
                material = (SpatialMaterial)((MeshInstance)_vehicleModel.GetNode("modelwheel1")).Mesh.SurfaceGetMaterial(materialIndex).Duplicate();
                material.FlagsTransparent = true;
                // Set the transparency
                color = material.AlbedoColor;
                color.a = 0.2f;
                material.AlbedoColor = color;

                ((MeshInstance)_vehicleModel.GetNode("modelwheel1")).SetSurfaceMaterial(materialIndex, material);
                ((MeshInstance)_vehicleModel.GetNode("modelwheel11")).SetSurfaceMaterial(materialIndex, material);
                ((MeshInstance)_vehicleModel.GetNode("modelwheel0")).SetSurfaceMaterial(materialIndex, material);
                ((MeshInstance)_vehicleModel.GetNode("modelwheel01")).SetSurfaceMaterial(materialIndex, material);
                ((MeshInstance)_vehicleModel.GetNode("modelwheel3")).SetSurfaceMaterial(materialIndex, material);
                ((MeshInstance)_vehicleModel.GetNode("modelwheel2")).SetSurfaceMaterial(materialIndex, material);
            }

            // For backlight material also 
            color = _backlight.AlbedoColor;
            color.a = 0.2f;
            _backlight.AlbedoColor = color;

            color = _rimBacklight.AlbedoColor;
            color.a = 0.2f;
            _rimBacklight.AlbedoColor = color;
        }

        _updateTransformMode(TransformMode.CIRCUIT, TransformMode.CIRCUIT);
        EmitSignal(nameof(UpdateBoosterCount), _boosterCount);
    }

    private void _updateTransformMode(TransformMode currentTransformMode, TransformMode previousTransformMode)
    {
        _transformMode = currentTransformMode;

        if (currentTransformMode == TransformMode.CIRCUIT)
        {
            // Reverse the animation
            vehicleAnimationPlayer.Play("transform", -1, -2f, true);
            _acceleration = 7000.0f;
            _steering = 21.0f;
        }
        else if (currentTransformMode == TransformMode.AERO)
        {
            if (previousTransformMode == TransformMode.CIRCUIT)
            {
                // Play the animation
                vehicleAnimationPlayer.Play("transform", -1, 2f, false);
                _acceleration = 7350.0f;
                _steering = 1.0f;
            }
            else if (previousTransformMode == TransformMode.AERO_BUST)
            {
                // Play the reverse animation
                vehicleAnimationPlayer.Play("bustbooster", -1, -2f, true);
                _acceleration = 7350.0f;
                _steering = 1.0f;
            }
        }

        else if (currentTransformMode == TransformMode.AERO_BUST)
        {
            // Play the animation
            vehicleAnimationPlayer.Play("bustbooster", -1, 2f, false);
            _acceleration = 7350.0f;
            _steering = 1.0f;
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
        if (_boosterCount <= 0)
        {
            return;
        }

        _boosterCount--;
        EmitSignal(nameof(UpdateBoosterCount), _boosterCount);

        _boosterMode = BoosterMode.ON;
        _boostRemainTime = _boostTime;

        _boostParticles1.Transform = _boosterParticlePosition1.Transform;
        _boostParticles2.Transform = _boosterParticlePosition2.Transform;

        _boostParticles1.Emitting = true;
        _boostParticles2.Emitting = true;

        EmitSignal(nameof(UpdateBoosterTimer), _boostRemainTime, _bustBoostTime, _boostTime, _boosterMode);

        _boostTimer.Start();
    }

    private void _startBustBooster()
    {
        // Force to set into areo mode
        _updateTransformMode(TransformMode.AERO_BUST, _transformMode);

        _boosterMode = BoosterMode.BUST;
        _boostRemainTime = _bustBoostTime;

        _boostParticles1.Transform = _boosterBustParticlePosition1.Transform;
        _boostParticles2.Transform = _boosterBustParticlePosition2.Transform;

        _boostParticles1.Emitting = true;
        _boostParticles2.Emitting = true;

        _bustBoostParticles1.Emitting = true;
        _bustBoostParticles2.Emitting = true;

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
            _boostTimer.Stop();

            // Stop booster
            _boosterMode = BoosterMode.OFF;
            _boostParticles1.Emitting = false;
            _boostParticles2.Emitting = false;
            _bustBoostParticles1.Emitting = false;
            _bustBoostParticles2.Emitting = false;

            // Force to set into areo mode
            _updateTransformMode(TransformMode.AERO, _transformMode);
        }
    }

    private Transform _alignWithY(Transform transform, Vector3 newY)
    {
        transform.basis.y = newY;
        transform.basis.x = -transform.basis.z.Cross(newY);
        transform.basis = transform.basis.Orthonormalized();
        return transform;
    }

    protected virtual KeyInput GetInput()
    {
        KeyInput currentKeyInput = new KeyInput();

        if (Input.IsActionJustPressed("booster"))
        {
            currentKeyInput.Booster = true;
        }

        if (Input.IsActionJustPressed("bustbooster"))
        {
            currentKeyInput.BurstBooster = true;
        }

        if (Input.IsActionJustPressed("transform"))
        {
            currentKeyInput.Transform = true;
        }

        // Get accelerate/brake input
        currentKeyInput.SpeedInput = 0.0f;
        currentKeyInput.SpeedInput += Input.GetActionStrength("accelerate");
        currentKeyInput.SpeedInput -= Input.GetActionStrength("brake");

        // Get steering input
        currentKeyInput.RotateInput = 0.0f;
        currentKeyInput.RotateInput += Input.GetActionStrength("steer_left");
        currentKeyInput.RotateInput -= Input.GetActionStrength("steer_right");

        return currentKeyInput;
    }

    private void _applyInput(float delta, KeyInput currentKeyInput)
    {
        //Can't steer/accelerate when in the air
        if (!_groundRay.IsColliding())
        {
            return;
        }

        float boosterAcceleration = 1.0f;

        if (_boosterMode == BoosterMode.ON)
        {
            boosterAcceleration = _boosterAccelerationFactor;
        }

        if (_boosterMode == BoosterMode.BUST)
        {
            boosterAcceleration = _busterboosterAccelerationFactor;
        }

        // Get accelerate/brake input
        _speedInput = currentKeyInput.SpeedInput;
        _speedInput *= _acceleration * boosterAcceleration;

        Boolean backlightOn = false;
        if (_speedInput < 0)
        {
            backlightOn = true;
        }

        // Set the backlight on
        if (backlightOn)
        {
            // Set to high energy
            _backlight.EmissionEnergy = 16;
            _rimBacklight.EmissionEnergy = 16;
            // Set to red
            _backlight.Emission = _BacklightOnColor;
            _rimBacklight.Emission = _BacklightOnColor;

        }
        else
        {
            // Set to high energy
            _backlight.EmissionEnergy = 1;
            _rimBacklight.EmissionEnergy = 1;
            // Set to red
            _backlight.Emission = _BacklightOffColor;
            _rimBacklight.Emission = _BacklightOffColor;
        }


        // Get steering input
        _rotateInput = currentKeyInput.RotateInput;
        _rotateInput *= Mathf.Deg2Rad(_steering);

        // Only can modify booster in aero mode
        if (currentKeyInput.Booster)
        {
            if (_transformMode == TransformMode.AERO && _boosterMode == BoosterMode.OFF)
            {
                _startBooster();
            }
            else if (_boosterMode != BoosterMode.OFF)
            {
                _stopBooster();
            }
        }

        if (currentKeyInput.BurstBooster && _boosterMode == BoosterMode.ON && _boostRemainTime <= _bustBoostTime)
        {
            _startBustBooster();
        }

        // Only can transform if not in booster mode
        if (currentKeyInput.Transform && _boosterMode == BoosterMode.OFF)
        {
            TransformMode previousTransformMode = _transformMode;

            if (_transformMode == TransformMode.CIRCUIT)
            {
                _transformMode = TransformMode.AERO;
            }
            else if (_transformMode == TransformMode.AERO)
            {
                _transformMode = TransformMode.CIRCUIT;
            }

            _updateTransformMode(_transformMode, previousTransformMode);
        }

        // smoke?
        float directionValue = _rigidBody.LinearVelocity.Normalized().Dot(-_vehicleModel.Transform.basis.z);
        bool particleEmit = false;
        if (_rigidBody.LinearVelocity.Length() > _turnStopLimit && directionValue < 0.9)
        {
            particleEmit = true;
        }

        ((Particles)_vehicleModel.GetNode("Particles0")).Emitting = particleEmit;
        ((Particles)_vehicleModel.GetNode("Particles01")).Emitting = particleEmit;
        ((Particles)_vehicleModel.GetNode("Particles1")).Emitting = particleEmit;
        ((Particles)_vehicleModel.GetNode("Particles11")).Emitting = particleEmit;
        ((Particles)_vehicleModel.GetNode("Particles2")).Emitting = particleEmit;
        ((Particles)_vehicleModel.GetNode("Particles3")).Emitting = particleEmit;

        // If it is sliding but not backward, enable rim light only
        if (particleEmit && !backlightOn)
        {
            // Set to high energy
            _rimBacklight.EmissionEnergy = 16;
            // Set to red
            _rimBacklight.Emission = _BacklightOnColor;
        }

        // rotate car mesh
        if (_rigidBody.LinearVelocity.Length() > _turnStopLimit)
        {
            var newBasis = _vehicleModel.GlobalTransform.basis.Rotated(_vehicleModel.GlobalTransform.basis.y, _rotateInput);

            Transform transform = _vehicleModel.GlobalTransform;
            transform.basis = _vehicleModel.GlobalTransform.basis.Slerp(newBasis, _turnSpeed * delta);
            _vehicleModel.Transform = transform;

            _vehicleModel.GlobalTransform = _vehicleModel.GlobalTransform.Orthonormalized();

            // tilt body for effect
            float tilt = -_rotateInput * 0.25f * _rigidBody.LinearVelocity.Length() / _bodyTilt;
            Vector3 vehcileTiltRotation = _vehicleModel.Rotation;
            vehcileTiltRotation.z = Mathf.Lerp(_vehicleModel.Rotation.z, tilt, 10 * delta);
            _vehicleModel.Rotation = vehcileTiltRotation;
        }

        // Tilt the car with slope
        Vector3 normal = _groundRay.GetCollisionNormal();
        Transform vehicleTransform = _alignWithY(_vehicleModel.GlobalTransform, normal.Normalized());
        _vehicleModel.GlobalTransform = _vehicleModel.GlobalTransform.InterpolateWith(vehicleTransform, 10 * delta);

        Vector3 rotation = ((MeshInstance)GetNode("vehicle/modelwheel1")).Rotation;

        // Not rotate forward during turn to simulate sliding
        if (_rotateInput != 0)
        {
            rotation.x = 0.0f;
            rotation.y = _rotateInput;
        }
        else
        {
            rotation.x = rotation.x * 2.0f;
            rotation.y = 0.0f;
        }

        ((MeshInstance)GetNode("vehicle/modelwheel1")).Rotation = rotation;
        ((MeshInstance)GetNode("vehicle/modelwheel11")).Rotation = rotation;

        rotation = ((MeshInstance)GetNode("vehicle/modelwheel0")).Rotation;

        // Not rotate forward during turn to simulate sliding
        if (_rotateInput != 0)
        {
            rotation.x = 0.0f;
            rotation.y = _rotateInput;
        }
        else
        {
            rotation.x = rotation.x * 2.0f;
            rotation.y = 0.0f;
        }


        ((MeshInstance)GetNode("vehicle/modelwheel0")).Rotation = rotation;
        ((MeshInstance)GetNode("vehicle/modelwheel01")).Rotation = rotation;
    }

    // Use for other system to get vheicle transform
    // Need to use GlobalTransform, otherwise will only get relative origin to spatial
    public Transform GetVehicleGlobalTransform()
    {
        return _vehicleModel.GlobalTransform;
    }

    public override void _PhysicsProcess(float delta)
    {
        if (_allowControl)
        {
            KeyInput keyInput = null;

            if (_inMemoryVehicleState != null)
            {
                keyInput = null;

                if (_inMemoryVehicleState.Count > 0)
                {
                    VehicleState vehicleState = _inMemoryVehicleState[0];
                    _inMemoryVehicleState.RemoveAt(0);

                    // Apply the initial state, so this frame will be setup to be same as the state when recorded happen
                    keyInput = vehicleState.KeyInput;
                    _rigidBody.LinearVelocity = vehicleState.RigidBodyLinearVelocity;
                    Transform tempTransform = _rigidBody.GlobalTransform;
                    tempTransform.origin = vehicleState.RigidBodyOrigin;
                    _rigidBody.GlobalTransform = tempTransform;
                }
                // brake
                else
                {
                    keyInput = new KeyInput();
                }
            }
            else
            {
                // Normal user control vehicle
                // Get user input
                keyInput = GetInput();

                // Push the vehicle state (rigidbody origin/velocity)
                VehicleState vehicleState = new VehicleState();
                vehicleState.KeyInput = keyInput;
                vehicleState.RigidBodyLinearVelocity = _rigidBody.LinearVelocity;
                vehicleState.RigidBodyOrigin = _rigidBody.GlobalTransform.origin;

                // Push the state to indicate current frame
                _gameStates.PushVehicleState(vehicleState);
            }

            // Apply the user input to vehicle
            // If this is ghost mode vehicle, the rigidbody velocity/origin is being set to be same as recorded frame
            _applyInput(delta, keyInput);
        }

        // Keep the car mesh aligned with the sphere
        Transform transform = _vehicleModel.Transform;

        transform.origin = _rigidBody.Transform.origin + _sphereoffset;
        _vehicleModel.Transform = transform;

        // Accelerate based on car's forward direction
        _rigidBody.AddCentralForce(-_vehicleModel.GlobalTransform.basis.z * _speedInput);

        // Apply the speed
        EmitSignal(nameof(UpdateSpeed), (int)(_rigidBody.LinearVelocity.Length()));
    }
}
