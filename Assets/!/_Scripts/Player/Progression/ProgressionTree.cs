using System;
using System.Collections.Generic;
using System.Linq;
using EMullen.Core;
using EMullen.PlayerMgmt;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "DefaultProgressionTree", menuName = "Progression/ProgressionTree")]
public class ProgressionTree : ScriptableObject 
{
    public List<ProgressionPoint> progressionPoints;

    [Serializable]
    public struct ProgressionTreePoint 
    {
        public ProgressionPoint progressionPoint;
        public List<ProgressionPoint> predecessors;
    }

#region Cache
    /// <summary>
    /// Stores the version of progressionPoints that was used to generate the tree info.
    /// </summary>
    private List<ProgressionPoint> cachedProgressionPoints;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ClearAllProgressionTreeCaches()
    {
        foreach (var tree in Resources.FindObjectsOfTypeAll<ProgressionTree>())
        {
            tree.cachedProgressionPoints = null;
        }
    }

    private Dictionary<string, ProgressionPoint> pointByID;
    /// <summary>
    /// A dictionary containing the progression point id as a key and its respective 
    ///   ProgressionPoint.
    /// </summary>
    public Dictionary<string, ProgressionPoint> PointByID { get {
        if(CacheDirty())
            GenerateCache();
        return pointByID;
    } }

    private List<string> entries;
    /// <summary>
    /// Progression points with no predecessors
    /// </summary>
    public List<string> Entries { get {
        if(CacheDirty())
            GenerateCache();
        return entries;
    } }
    
    private Dictionary<string, List<string>> childList;
    /// <summary>
    /// A dictionary containing progression point id keys and a list of progression point id
    ///   children. Accessing ChildList[<id>] will give the children to this point.
    /// </summary>
    public Dictionary<string, List<string>> ChildList { get {
        if(CacheDirty())
            GenerateCache();
        return childList;
    } }

    private bool CacheDirty() => cachedProgressionPoints == null || !progressionPoints.SequenceEqual(cachedProgressionPoints);

    private void GenerateCache() 
    {
        cachedProgressionPoints = progressionPoints;

        entries = new();
        childList = new();
        pointByID = new();

        foreach(ProgressionPoint point in progressionPoints) {
            pointByID.Add(point.progressionID, point);

            if(point.predecessors.Count == 0) {
                // No predecessors, add to entries
                entries.Add(point.progressionID);
            } else {
                // Has predecessors, add current point to the child list of each predecessor.
                foreach(ProgressionPoint pred in point.predecessors) {
                    if(!childList.ContainsKey(pred.progressionID))
                        childList.Add(pred.progressionID, new());
                    
                   childList[pred.progressionID].Add(point.progressionID);
                }
            }
        }
    }
#endregion

#region Evaluation
    [Serializable]
    public struct EvaluationResults
    {
        public Dictionary<string, List<string>> nextSteps;
        public List<string> canUnlock;
        public List<string> visiblePoints;

        public EvaluationResults(Dictionary<string, List<string>> nextSteps, List<string> canUnlock) 
        {
            this.nextSteps = nextSteps;
            this.canUnlock = canUnlock;
            this.visiblePoints = new();
            visiblePoints.AddRange(canUnlock);
            visiblePoints.AddRange(nextSteps.Keys);
        }
    }

    public EvaluationResults Evaluate(PlayerData pd) 
    {
        GeneralPlayerData gpd = pd.GetData<GeneralPlayerData>();
        ProgressionData progression = pd.GetData<ProgressionData>();

        Dictionary<string, List<string>> nextSteps = new();
        List<string> canUnlock = new();

        void Recurse(string pointID) {
            BLog.Highlight($"Looking at {pointID} has unlocked: {progression.HasUnlocked(pointID)}");
            if(progression.HasUnlocked(pointID)) {

                // This progression point has been unlocked, continue recursion down this path of the tree.
                if(ChildList.ContainsKey(pointID))
                    ChildList[pointID].ForEach(child => Recurse(child));

            } else {

                // This progression has not been unlocked
                // Get the missing metrics
                List<string> missingMetrics = new();
                foreach(ProgressionMetric targetMetric in PointByID[pointID].targetMetrics.Select(tm => tm.metric)) {
                    string targMetricName = targetMetric.GetName();
                    bool hasCompletedMetric = progression.HasMetric(targMetricName) && progression.GetMetric(targetMetric.GetName()).Evaluate(targetMetric);
                    if(!hasCompletedMetric)
                        missingMetrics.Add(targMetricName);
                }

                // Check if balance is too low
                if(gpd.balance < pointByID[pointID].price) {
                    missingMetrics.Add("PRICE");
                }

                // Check if can be unlocked. If so add to can unlock, if not add to next steps with 
                //   missing metrics.
                if(missingMetrics.Count == 0) {
                    canUnlock.Add(pointID);
                } else {
                    nextSteps.Add(pointID, missingMetrics);
                }

            }
        }

        Entries.ForEach(entry => Recurse(entry));

        return new EvaluationResults(nextSteps, canUnlock);
    }

    public List<Tuple<string, ProgressionMetric.MetricType>> GetMissingMetrics(ProgressionData existing, List<string> visiblePoints) 
    {
        List<Tuple<string, ProgressionMetric.MetricType>> metrics = new();
        foreach(ProgressionPoint visiblePoint in visiblePoints.Select(progPointID => PointByID[progPointID])) {

            if(visiblePoint.targetMetrics == null || visiblePoint.targetMetrics.Count == 0)
                continue;

            foreach(ProgressionPoint.ProgressionMetricData targetMetricData in visiblePoint.targetMetrics) {
                ProgressionMetric targetMetric = targetMetricData.metric;

                if(!existing.HasMetric(targetMetric.GetName())) {
                    metrics.Add(new(targetMetric.GetName(), targetMetric.GetMetricType()));
                }
            }
        }

        return metrics;
    }

    /// <summary>
    /// Look through the 
    /// </summary>
    /// <param name="existing"></param>
    /// <returns></returns>
    public ProgressionData PopulateMissingMetrics(ProgressionData existing, List<string> visiblePoints) 
    {
        List<Tuple<string, ProgressionMetric.MetricType>> metrics = new();
        foreach(ProgressionPoint visiblePoint in visiblePoints.Select(progPointID => PointByID[progPointID])) {

            if(visiblePoint.targetMetrics == null || visiblePoint.targetMetrics.Count == 0)
                continue;

            foreach(ProgressionPoint.ProgressionMetricData targetMetricData in visiblePoint.targetMetrics) {
                ProgressionMetric targetMetric = targetMetricData.metric;

                if(!existing.HasMetric(targetMetric.GetName())) {
                    metrics.Add(new(targetMetric.GetName(), targetMetric.GetMetricType()));
                }
            }
        }

        return existing;
    }
#endregion    
}