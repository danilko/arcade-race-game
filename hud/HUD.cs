using Godot;
using System;

public class HUD : Control
{
    private Label rotationLabel;
    private Control _boosterControl;
    private TextureProgress _boosterProgress;
    private Label _boosterTimer;
    private Label _boosterBustLabel;
    private Label _boosterLimitLabel;

    private AnimationPlayer _animationPlayer;

    private Label _velocityLabel;

    private Label _lapTimerBestLap;
    private Label _lapTimerBest;
    private Label _lapTimerCurrentLap;
    private Label _lapTimerCurrent;
    private float _lapBestTimeCentiseconds;

    private Label _transformMode;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        rotationLabel = (Label)GetNode("Rotation");
        _boosterControl = (Control)GetNode("BoosterControl");
        _boosterProgress = (TextureProgress)_boosterControl.GetNode("BoosterProgress");
        _boosterTimer = (Label)_boosterControl.GetNode("BoosterTimer");
        _boosterBustLabel = (Label)_boosterControl.GetNode("BoosterBustLabel");
        _boosterLimitLabel = (Label)_boosterControl.GetNode("BoosterLimitLabel");

        _velocityLabel = (Label)GetNode("Velocity");

        _lapTimerBestLap = (Label)GetNode("LapTimerBestLap");
        _lapTimerBest = (Label)GetNode("LapTimerBest");

        _lapTimerCurrent = (Label)GetNode("LapTimerCurrent");
        _lapTimerCurrentLap = (Label)GetNode("LapTimerCurrentLap");
        _lapBestTimeCentiseconds = -1.0f;

        _animationPlayer = (AnimationPlayer)GetNode("AnimationPlayer");

        _transformMode = (Label)GetNode("TransformMode");
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

    private void _updateLapTimerDisplay(float centiseconds, int lap)
    {
        _updateLapTimerDisplay(centiseconds, lap, _lapTimerCurrent, _lapTimerCurrentLap);
    }

    private void _updateLapTimerDisplay(float centiseconds, int lap, Label timer, Label lapLabel)
    {
        int hour = (int)(centiseconds / 360000);

        centiseconds = centiseconds - (hour * 360000);

        int minutes = (int)(centiseconds / 6000);

        centiseconds = centiseconds - (minutes * 6000);

        int seconds = (int)(centiseconds / 100);

        centiseconds = centiseconds - (seconds * 100);

        timer.Text = formatTwoDigits(hour) + ":" + formatTwoDigits(minutes) + ":" + formatTwoDigits(seconds) + "." + formatTwoDigits((int)centiseconds);
        lapLabel.Text = "LAP " + formatTwoDigits(lap);
    }

    private void _updateBestLapTimerDisplay(float centiseconds, int lap)
    {
        // If this is the first lap, use it as best time
        if (_lapBestTimeCentiseconds == -1.0f || centiseconds < _lapBestTimeCentiseconds)
        {
            _lapBestTimeCentiseconds = centiseconds;

            _updateLapTimerDisplay(_lapBestTimeCentiseconds, lap, _lapTimerBest, _lapTimerBestLap);
        }
    }

    private String formatTwoDigits(int input)
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

    private void _updateRotationDisplay(float rotation)
    {
        rotationLabel.Text = "Rotation: " + rotation;
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
}
