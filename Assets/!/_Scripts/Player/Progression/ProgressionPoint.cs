using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DefaultProgressionPoint", menuName = "Progression/ProgressionPoint")]
public class ProgressionPoint : ScriptableObject 
{
    public string progressionID;
    public string progressionDisplayName;
    public string progressionDescription;
    public float price;

    public List<ProgressionMetric> targetMetrics;
    public List<ProgressionPoint> predecessors;

    public bool CanUnlock(ProgressionData data) 
    {
        return true;
    }
}