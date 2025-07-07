using System;
using System.Collections.Generic;
using EMullen.Core.PlayerMgmt;
using Unity.VisualScripting;

public class ProgressionData : PlayerDatabaseDataClass
{
    public string _uid;
    public override string UID => _uid;

    /// <summary>
    /// A list of unlocked progression point ids
    /// </summary>
    public List<string> unlockedPoints;
    public List<ProgressionMetric> metrics;
    
    public ProgressionData() {}
    public ProgressionData(string uid) 
    {
        this._uid = uid;
        this.unlockedPoints = new();
        this.metrics = new();
    }

    public static ProgressionData CreateDefault(string uid) => new(uid);

    public bool HasUnlocked(string progressionPointID) => unlockedPoints.Contains(progressionPointID);

    public ProgressionMetric GetMetric(string name) 
    {
        foreach(ProgressionMetric metric in metrics) {
            if(metric.GetName() == name)
                return metric;
        }

        return null;
    }

    public bool HasMetric(string name) 
    {
        foreach(ProgressionMetric metric in metrics) {
            if(metric.GetName() == name)
                return true;
        }
        return false;
    }

    public void AddMetric(ProgressionMetric metric) 
    {
        if(HasMetric(metric.GetName()))
            throw new InvalidOperationException($"Can't add new metric \"{metric.GetName()}\" the metric already exists.");
        
        metrics.Add(metric);
    }
}