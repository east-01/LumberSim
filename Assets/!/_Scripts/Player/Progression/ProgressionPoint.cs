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

    public List<ProgressionMetricData> targetMetrics;
    public List<ProgressionPoint> predecessors;

    [Serializable]
    public struct ProgressionMetricData {
        public ProgressionMetric metric;
        public string displayName;
    }
}