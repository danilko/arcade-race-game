using Godot;
using System;

public class EndGameScreen : Control
{
    private GameStates _gameStates;

    public override void _Ready()
    {
        _gameStates = (GameStates)GetNode("/root/GAMESTATES");
        _computeResult();
    }

    public void _computeResult()
    {
        String messages = _gameStates.getMessgesForNextScene();
        _gameStates.setMessagesForNextScene("");

        int parseIndex = 0;

        Label label = (Label)GetNode("Timer/LapTimerBest");
        int lap = int.Parse(messages.Split(";")[parseIndex]);
        parseIndex++;
        float time = float.Parse(messages.Split(";")[parseIndex]);
        parseIndex++;
        _updateLapTimerDisplay(time, lap, label);

        Label timerTemplate = (Label)GetNode("Timer/LapTimerLapTemplate");

        int totalLaps = int.Parse(messages.Split(";")[parseIndex]);
        parseIndex++;

        VBoxContainer container = (VBoxContainer)GetNode("Timer/LapsContainer");
        
        float totalTime = 0.0f;

        for (int index = 0; index < totalLaps; index++)
        {
            lap = int.Parse(messages.Split(";")[parseIndex]);
            parseIndex++;
            time = float.Parse(messages.Split(";")[parseIndex]);
            parseIndex++;
            totalTime += time;

            label = (Label)timerTemplate.Duplicate();
            container.AddChild(label);
            _updateLapTimerDisplay(time, lap, label);
            label.Visible = true;
        }

        label = (Label)GetNode("Timer/LapTimerTotal");
        label.Text = HUD.ConvertCentiSeconds(totalTime);
    }

    private void _onSaveRecord()
    {
        _gameStates.SaveRecord();
    }

    private void _updateLapTimerDisplay(float centiseconds, int lap, Label timer)
    {
        timer.Text = HUD.ConvertCentiSeconds(centiseconds) + " LAP " + HUD.FormatTwoDigits(lap);
    }

    public void _onNext()
    {
        _gameStates.EnterTitleScreen();
    }
}
