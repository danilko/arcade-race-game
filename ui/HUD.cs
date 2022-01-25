using Godot;
using System;
using System.Collections.Generic;

public class HUD : Control
{
    private Control _boosterControl;
    private TextureProgress _boosterProgress;
    private Label _boosterTimer;
    private Label _boosterBustLabel;
    private Label _boosterLimitLabel;
    private Label _boosterCount;
    private AnimationPlayer _animationPlayer;

    private Label _velocityLabel;

    private Label _lapTimerBestLap;
    private Label _lapTimerBest;
    private Label _lapTimerCurrentLap;
    private Label _lapTimerCurrent;
    private Label _lapTimerLapTemplate;
    private float _lapBestTimeCentiseconds;
    private VBoxContainer _lapContainer;
    private int _lapBestTime;

    private Label _transformMode;

    private List<float> _lapTimes;

    private GameStates _gameStates;

    private WindowDialog _windowDialog;

    private Label _startTimerCounter;
    private Timer _startTimerCounterFadeTimer;

    private Label _fps;

    private MiniMap _minimap;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _lapTimes = new List<float>();

        _gameStates = (GameStates)GetNode("/root/GAMESTATES");

        _boosterControl = (Control)GetNode("BoosterControl");
        _boosterProgress = (TextureProgress)_boosterControl.GetNode("BoosterProgress");
        _boosterTimer = (Label)_boosterControl.GetNode("BoosterTimer");
        _boosterBustLabel = (Label)_boosterControl.GetNode("BoosterBustLabel");
        _boosterLimitLabel = (Label)_boosterControl.GetNode("BoosterLimitLabel");

        _boosterCount = (Label)GetNode("BoosterCount");

        _velocityLabel = (Label)GetNode("Velocity");

        _lapTimerBestLap = (Label)GetNode("LapTimerBestLap");
        _lapTimerBest = (Label)GetNode("LapTimerBest");

        _lapTimerCurrent = (Label)GetNode("LapTimerCurrent");
        _lapTimerCurrentLap = (Label)GetNode("LapTimerCurrentLap");
        _lapBestTimeCentiseconds = -1.0f;
        _lapBestTime = 0;
        _lapContainer = (VBoxContainer)GetNode("LapsContainer");
        _lapTimerLapTemplate = (Label)GetNode("LapTimerLapTemplate");

        _animationPlayer = (AnimationPlayer)GetNode("AnimationPlayer");

        _transformMode = (Label)GetNode("TransformMode");

        _windowDialog = (WindowDialog)GetNode("SettingDialog");

        _startTimerCounter = (Label)GetNode("StartTimerCounter");
        _startTimerCounterFadeTimer = (Timer)GetNode("StartTimerCounterFadeTimer");

        _minimap = (MiniMap)GetNode("MiniMap");

        _fps = (Label)GetNode("FPS"); 
    }

    public MiniMap GetMiniMap()
    {
        return _minimap;
    }

    public void Initialize(KinematicVehicle vehicle)
    {
        vehicle.Connect(nameof(KinematicVehicle.CompleteLapTimer), this, nameof(_updateBestLapTimerDisplay));
        vehicle.Connect(nameof(KinematicVehicle.UpdateBoosterTimer), this, nameof(_updateBoosterTImerDisplay));
        vehicle.Connect(nameof(KinematicVehicle.UpdateLapTimer), this, nameof(_updateLapTimerDisplay));
        vehicle.Connect(nameof(KinematicVehicle.UpdateBoosterCount), this, nameof(_updateBoosterCountDisplay));
        vehicle.Connect(nameof(KinematicVehicle.UpdateSpeed), this, nameof(_updateVelocityDisplay));
        vehicle.Connect(nameof(KinematicVehicle.UpdateTransformMode), this, nameof(_updateTransformModeDisplay));
    }

    public void Initialize(GameWorld gameWorld, SpatialVehicle vehicle)
    {
        vehicle.Connect(nameof(SpatialVehicle.CompleteLapTimer), this, nameof(_updateBestLapTimerDisplay));
        vehicle.Connect(nameof(SpatialVehicle.UpdateBoosterTimer), this, nameof(_updateBoosterTImerDisplay));
        vehicle.Connect(nameof(SpatialVehicle.UpdateLapTimer), this, nameof(_updateLapTimerDisplay));
        vehicle.Connect(nameof(SpatialVehicle.UpdateBoosterCount), this, nameof(_updateBoosterCountDisplay));
        vehicle.Connect(nameof(SpatialVehicle.UpdateSpeed), this, nameof(_updateVelocityDisplay));
        vehicle.Connect(nameof(SpatialVehicle.UpdateTransformMode), this, nameof(_updateTransformModeDisplay));

        // Update the start timer signal with HUD
        gameWorld.Connect(nameof(GameWorld.StartTimerChange), this, nameof(_showStartTimerCountDown));
    }

    public void _showStartTimerCountDown(int counter)
    {
        String resultText = "" + counter;
        
        if(counter == 0)
        {
            // Set text to GO when counter reaches 0
            resultText = "GO";
        }

        _startTimerCounter.Text = resultText;
        _startTimerCounter.Visible = true;
        _startTimerCounterFadeTimer.Start();
    }

    public void _onStartTimerCounterFade()
    {
        _startTimerCounter.Visible = false;
    }

    private void _updateTransformModeDisplay(KinematicVehicle.TransformMode transformMode)
    {
        // Workaround as the vehicle called this during READY state
        if (_transformMode == null)
        {
            _transformMode = (Label)GetNode("TransformMode");
        }

        _transformMode.Text = "" + transformMode;
    }

    private void _onExitGameSession()
    {
        _windowDialog.Hide();
        _gameStates.EnterTitleScreen();
    }

    private void _updateLapTimerDisplay(float centiseconds, int lap)
    {
        _updateLapTimerDisplay(centiseconds, lap, _lapTimerCurrent, _lapTimerCurrentLap);
    }

    private void _updateLapTimerDisplay(float centiseconds, int lap, Label timer, Label lapLabel)
    {
        timer.Text = ConvertCentiSeconds(centiseconds);
        lapLabel.Text = "LAP " + FormatTwoDigits(lap);
    }

    private void _updateNewLap(float centiseconds, int lap)
    {
        // Push this new time to recorded lap times
        _lapTimes.Add(centiseconds);

        Label newLap = (Label)_lapTimerLapTemplate.Duplicate();
        _lapContainer.AddChild(newLap);

        newLap.Text = ConvertCentiSeconds(centiseconds) + " LAP " + FormatTwoDigits(lap);
        newLap.Visible = true;
    }

    private void _updateBestLapTimerDisplay(float centiseconds, int lap)
    {
        _updateNewLap(centiseconds, lap);

        // If this is the first lap, use it as best time
        if (_lapBestTimeCentiseconds == -1.0f || centiseconds < _lapBestTimeCentiseconds)
        {
            _lapBestTime = lap;
            _lapBestTimeCentiseconds = centiseconds;
            _updateLapTimerDisplay(_lapBestTimeCentiseconds, lap, _lapTimerBest, _lapTimerBestLap);
        }

        // TODO Consider move this logic to game world level
        // Right now will end game when current completed lap == total lap
        if (lap == _gameStates.GetTotalLaps())
        {
            _transitionToEndGame();
        }
    }

    private void _transitionToEndGame()
    {
        String message = _lapBestTime + ";" + _lapBestTimeCentiseconds + ";";

        message = message + _lapTimes.Count + ";";

        for (int index = 0; index < _lapTimes.Count; index++)
        {
            message = message + (index + 1) + ";" + _lapTimes[index] + ";";
        }

        _gameStates.setMessagesForNextScene(message);
        _gameStates.EndGameScreen();
    }

    public static String ConvertCentiSeconds(float centiseconds)
    {
        int hour = (int)(centiseconds / 360000);

        centiseconds = centiseconds - (hour * 360000);

        int minutes = (int)(centiseconds / 6000);

        centiseconds = centiseconds - (minutes * 6000);

        int seconds = (int)(centiseconds / 100);

        centiseconds = centiseconds - (seconds * 100);

        return FormatTwoDigits(hour) + ":" + FormatTwoDigits(minutes) + ":" + FormatTwoDigits(seconds) + "." + FormatTwoDigits((int)centiseconds);
    }

    public static String FormatTwoDigits(int input)
    {
        if (input < 10)
        {
            return "0" + input;
        }
        else
        {
            return "" + input;
        }
    }

    private void _updateBoosterCountDisplay(int boosterCount)
    {
        if (_boosterCount == null)
        {
            _boosterCount = (Label)GetNode("BoosterCount");
        }
        _boosterCount.Text = " " + FormatTwoDigits(boosterCount);
    }

    private void _updateVelocityDisplay(float speed)
    {
        _velocityLabel.Text = "" + speed;
    }

    private void _updateBoosterTImerDisplay(float timer, float bust, float total, KinematicVehicle.BoosterMode boosterMode)
    {
        if (timer <= -1)
        {
            _boosterControl.Hide();
            _boosterBustLabel.Hide();
            _boosterLimitLabel.Hide();
            _animationPlayer.Stop();
            return;
        }

        _boosterProgress.MaxValue = total;
        _boosterProgress.Value = timer;
        _boosterTimer.Text = timer + "";

        // Transition over to booster mode
        if (_boosterControl.Visible == false)
        {
            _boosterControl.Show();
        }

        // Enable limit label
        if (timer <= bust && boosterMode == KinematicVehicle.BoosterMode.ON)
        {
            _boosterLimitLabel.Show();
            _animationPlayer.Play("BustLimitAnimation");
        }

        if (boosterMode == KinematicVehicle.BoosterMode.BUST)
        {
            _boosterProgress.MaxValue = bust;

            // Transition over to bust booster mode
            if (_boosterBustLabel.Visible == false)
            {
                _animationPlayer.Stop();
                _boosterLimitLabel.Hide();
                _boosterBustLabel.Show();
                _animationPlayer.Play("BustModeAnimation");
            }
        }
    }

    private void _onSettingDialogHide()
    {
        // Menu close, disable mouse
        Input.SetMouseMode(Input.MouseMode.Hidden);
    }

    public override void _Process(float delta)
    {
        if (Input.IsActionJustReleased("ui_setting"))
        {
            if (!_windowDialog.Visible)
            {
                // In menu, enable mouse
                Input.SetMouseMode(Input.MouseMode.Visible);

                _windowDialog.PopupCentered();
            }
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        _fps.Text = "FPS: " + 1/delta;
    }
}
