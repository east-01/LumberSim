using System;
using EMullen.PlayerMgmt;

/// <summary>
/// The GeneralPlayerData class holds general player data like balance and axe level.
/// </summary>
public class GeneralPlayerData : PlayerDataClass
{
    public float balance;
    public int axeLevel;

    public GeneralPlayerData() {}

    public GeneralPlayerData(float balance, int axeLevel = 0) 
    {
        this.balance = balance;
        this.axeLevel = axeLevel;
    }
}