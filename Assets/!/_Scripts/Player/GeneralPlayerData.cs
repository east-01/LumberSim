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
    public float maxCarryWeight = 60;

    public GeneralPlayerData() {}

    public GeneralPlayerData(string uid, float balance) 
    {
        this._uid = uid;
        this.balance = balance;
    }

    public static GeneralPlayerData CreateDefault(string uid) => new(uid, 20);
}