using Godot;
using System;

public class TitleScreen : Control
{
    private GameStates _gameStates;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _gameStates = (GameStates)GetNode("/root/GAMESTATES");
    }

    public void _onChangeVehicleImplementation(bool enable)
    {
        if(enable)
        {
            _gameStates.CurrentVehicleImplementation = GameStates.VehicleImplementation.RIGIDBODY_ARCADE;
        }
        else
        {
            _gameStates.CurrentVehicleImplementation = GameStates.VehicleImplementation.RIGIDBODY_ARCADE;
        }
    }

    public void _onNewGame()
    {
        _gameStates.EnterGame();
    }

    public void _onSettings()
    {
        WindowDialog windowDialog = (WindowDialog)GetNode("SettingDialog");
        windowDialog.PopupCentered();
    }


    public void _onExit()
    {
        GetTree().Quit();
    }
}
