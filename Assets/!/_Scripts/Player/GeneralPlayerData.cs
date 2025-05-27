using System;
using EMullen.Core;
using EMullen.Core.PlayerMgmt;

/// <summary>
/// The GeneralPlayerData class holds general player data like balance and axe level.
/// </summary>
public class GeneralPlayerData : PlayerDatabaseDataClass
{
    public string _uid;
    public override string UID => _uid;
    public float balance;
    public int axeLevel;

    public GeneralPlayerData() {}

    public GeneralPlayerData(string uid, float balance, int axeLevel = 0) 
    {
        this._uid = uid;
        this.balance = balance;
        this.axeLevel = axeLevel;
    }

    public static GeneralPlayerData CreateDefault(string uid) => new(uid, 20);
}