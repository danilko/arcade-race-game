using Godot;
using System;
using System.Collections.Generic;

public class GameStates : Node
{
    private int _currentLevel = 0;

    private Boolean _ghostMode = false;

    public String[] levels = { "res://ui/TitleScreen.tscn", "res://GameWorld.tscn" };

    public List<SpatialVehicle.VehicleState> current_memory = new List<SpatialVehicle.VehicleState>();

    public String endResultScreen = "res://ui/EndGameScreen.tscn";

    public enum VehicleImplementation
    {
        RIGIDBODY_ARCADE,
        KINEMATIC
    }

    public VehicleImplementation CurrentVehicleImplementation { get; set; }

    private String messages;

    private int _totalLap = 3;

    public void PushVehicleState(SpatialVehicle.VehicleState vehicleState)
    {
        current_memory.Add(vehicleState);
    }

    public Boolean GetGhostMode()
    {
        return _ghostMode;
    }

    public void SetGhostMode(Boolean input)
    {
        _ghostMode = input;
    }

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
        _currentLevel = 0;
        GetTree().ChangeScene(levels[_currentLevel]);
    }

    public void EnterTitleScreen()
    {
        // In menu, enable mouse
        Input.SetMouseMode(Input.MouseMode.Visible);

        _currentLevel = 0;

        current_memory.Clear();
        _ghostMode = false;

        GetTree().ChangeScene(levels[_currentLevel]);
    }

    public void EnterGame()
    {
        _currentLevel = 1;
        // In game, disable mouse
        Input.SetMouseMode(Input.MouseMode.Hidden);
        GetTree().ChangeScene(levels[_currentLevel]);
    }

    public Boolean CheckIfRecordExist()
    {

        File saveGame = new File();
        
        return saveGame.FileExists("user://record.save");
    }

    // Save the running game record
    public void SaveRecord()
    {
        File saveGame = new File();
        saveGame.Open("user://record.save", File.ModeFlags.Write);

        int counter = 0;

        foreach (SpatialVehicle.VehicleState vehicleState in current_memory)
        {

            // Store the save dictionary as a new line in the save file.
            saveGame.StoreLine(vehicleState.ToString());
            counter++;
        }

        saveGame.Close();
    }

    // Save the running game record
    public List<SpatialVehicle.VehicleState> LoadRecord()
    {
        List<SpatialVehicle.VehicleState> loaded_memory = new List<SpatialVehicle.VehicleState>();

        var saveGame = new File();
        saveGame.Open("user://record.save", File.ModeFlags.Read);
       int counter = 0;
        while (saveGame.GetPosition() < saveGame.GetLen())
        {
            // Loaded each line from the save file.
            String input = saveGame.GetLine();
            // Use contrustor to create out
            SpatialVehicle.VehicleState temp = new SpatialVehicle.VehicleState(input);

            loaded_memory.Add(temp);
                   counter++;
        }

        saveGame.Close();

        return loaded_memory;
    }
}
