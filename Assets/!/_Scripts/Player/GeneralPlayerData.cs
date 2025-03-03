using System;
using EMullen.PlayerMgmt;

/// <summary>
/// The GeneralPlayerData class holds general player data like balance.
/// </summary>
public class GeneralPlayerData : PlayerDataClass
{
    public float balance;

    public GeneralPlayerData() {}

    public GeneralPlayerData(float balance) 
    {
        this.balance = balance;
    }
}