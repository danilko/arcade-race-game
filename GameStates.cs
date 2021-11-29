using Godot;
using System;

public class GameStates : Node
{

    public int current_level = 0;

    public String[] levels = { "res://ui/TitleScreen.tscn", "res://GameWorld.tscn" };

    public String endResultScreen = "res://ui/EndGameScreen.tscn";

    public enum VehicleImplementation {
        KINEMATIC,
        RIGIDBODY_ARCADE
    }

    public VehicleImplementation CurrentVehicleImplementation {get; set;}

    private String messages;

    private int _totalLap = 3;

    public int GetTotalLaps()
    {
        return _totalLap;
    }

    public void setMessagesForNextScene(String inputMessages)
    {
        messages = inputMessages;
    }

    public String getMessgesForNextScene()
    {
        return messages;
    }

    public void EndGameScreen()
    {   
        // In menu, enable mouse
        Input.SetMouseMode(Input.MouseMode.Visible);
        GetTree().ChangeScene(endResultScreen);
    }

    public void restart()
    {
        current_level = 0;
        GetTree().ChangeScene(levels[current_level]);
    }

    public void EnterTitleScreen()
    {
        // In menu, enable mouse
        Input.SetMouseMode(Input.MouseMode.Visible);
        current_level = 0;
        GetTree().ChangeScene(levels[current_level]);
    }

    public void EnterGame()
    {
        current_level = 1;
        // In game, disable mouse
        Input.SetMouseMode(Input.MouseMode.Hidden);
        GetTree().ChangeScene(levels[current_level]);
    }
}
