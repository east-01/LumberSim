using System;
using System.Collections.Generic;
using System.Linq;
using EMullen.Core;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

/// The hit/split management class is responsible for taking in SingleHitData structs as hits and
///   stores them as MultiHitData structs to ultimately split a log at that point.
public partial class TreeLogGroup : NetworkBehaviour, IS3
{
    [SerializeField]
    private GameObject hitMarkerPrefab;
    [SerializeField]
    private float minLogLength = 0.2f;
    [SerializeField]
    private float hitPointMergeDistance = 0.5f;
    [SerializeField]
    private Gradient hitMarkerGradient;

    private readonly SyncList<MultiHitData> hitPoints = new();

    private Dictionary<MultiHitData, GameObject> hitMarkers = new();
    /// <summary>
    /// Set this flag if the tree should prune tiny logs on the next possible frame.
    /// </summary>
    public bool ShouldPrune = false;
    private float hitMarkerUpdateTimer;

    public void HitSplitUpdate() 
    {
        // Update hit markers every second
        if(hitMarkerUpdateTimer <= 0) {
            UpdateHitMarkers();  
            hitMarkerUpdateTimer = 0.15f;       
        } else {
            hitMarkerUpdateTimer -= Time.deltaTime;
        }
    }

    public void HitLog(SingleHitData hitPoint) 
    {
        // Not stoked with hard-coding this, see NetworkedAudioController description
        audioController.PlaySound($"log_hit_{UnityEngine.Random.Range(1, 7)}");

        if(!InstanceFinder.IsServerStarted) {
            ServerRpcHitLog(hitPoint);
            UpdateHitMarkers();
            return;
        }

        float clostestHPDistance = float.MaxValue;
        int closestHitIndex = -1;
        for(int i = 0; i < hitPoints.Count; i++) {
            MultiHitData testHitPoint = hitPoints[i];
            // Only count as hit if its on the same log
            if(!hitPoint.identifierPath.SequenceEqual(testHitPoint.identifierPath))
                continue;

            float testDist = Vector3.Distance(hitPoint.location, testHitPoint.location);
            if(testDist < clostestHPDistance) {
                clostestHPDistance = testDist;
                closestHitIndex = i;
            }
        }

        if(closestHitIndex == -1 || clostestHPDistance > hitPointMergeDistance) {
            MultiHitData newData = new(hitPoint.identifierPath, hitPoint.location, 0);
            hitPoints.Add(newData);
            closestHitIndex = hitPoints.Count - 1;
        }

        MultiHitData multiHitPoint = hitPoints[closestHitIndex];
        multiHitPoint.totalHitAmount += hitPoint.hitAmount;

        if(multiHitPoint.totalHitAmount > GetRequiredHitsToSplit(hitPoint.identifierPath)) {
            SplitLog(hitPoint);
            hitPoints.RemoveAt(closestHitIndex);
        } else {
            hitPoints[closestHitIndex] = multiHitPoint;
            hitPoints.Dirty(closestHitIndex);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void ServerRpcHitLog(SingleHitData hitPoint) => HitLog(hitPoint);

    public void SplitLog(SingleHitData hitPoint) 
    {
        int[] identifierPath = hitPoint.identifierPath;
        Vector3 hitGlobal = hitPoint.location;
        TreeLog hitLog = TreeOpGet(identifierPath);
        if(hitLog == null) {
            Debug.LogError($"Failed to resolve hit log from treelog ip: {string.Join(", ", identifierPath)}");
            return;
        }

        Vector3 endpointForward = GetFrontEndpoint(identifierPath);
        Vector3 endpointBackward = hitLog.transform.position;

        // Store original data points for future reference
        TreeLogData origHitLogData = hitLog.Data.Clone();
        float origLogLength = hitLog.Data.length;

        // ----- Part of the tree that's still attached -----
        // Calculate the length of the log still attached
        Vector3 hitPointVector = hitGlobal - endpointBackward;
        Vector3 logVector = endpointForward-endpointBackward;
        float attachedLength = Vector3.Dot(hitPointVector, logVector.normalized);

        // Create the data for the log that is still attached to the group
        TreeLogData newAttachedData = origHitLogData.Clone();
        newAttachedData.length = attachedLength;
        newAttachedData.children = new TreeLogData[0];
        TreeOpSet(identifierPath, newAttachedData);

        // ----- Part of the tree that's just been detached -----
        // Create the data for the new log group to be spawned
        float newLogLength = origLogLength-attachedLength;
        TreeLogData newLogData = origHitLogData.Clone();
        newLogData.length = newLogLength;

        // Spawn new log group
        TreeLogGroup newGroup = gameplayManager.SpawnLogObject(hitGlobal, transform.rotation, newLogData, hitPoint.owner);

        // ----- Cleanup -----
        ShouldPrune = true;
        newGroup.ShouldPrune = true;
    }

    public float GetRequiredHitsToSplit(int[] treeLogIP) 
    {
        TreeLogData targLogData = TreeOpGet(treeLogIP).Data;
        return targLogData.radius*2;
    }

    private void PruneTinyLogs() 
    {
        void CheckPrune(TreeLog log) {
            if(log.Data.length < minLogLength) {
                // Spawn children and continue prune with the newly spawned groups to ensure all
                //   branches are pruned
                foreach(TreeLog child in log.ChildBranches) {
                    TreeLogGroup newChild = gameplayManager.SpawnLogObject(child.transform.position, child.transform.rotation, child.Data, log.LogGroup.Owner);
                    newChild.ShouldPrune = true;
                }

                if(log == Root) {
                    Delete();
                } else {
                    TreeOpDelete(log.GetIdentifierPath());
                }
                return;
            }
            // Check children
            foreach(TreeLog child in log.ChildBranches) {
                CheckPrune(child);
            }
        }
        CheckPrune(Root);
    }

    public void UpdateHitMarkers() 
    {
        // ----- Synchronize local hit points with remote hit points -----
        HashSet<MultiHitData> synced = hitPoints.ToHashSet();
        HashSet<MultiHitData> local = hitMarkers.Keys.ToHashSet();

        HashSet<MultiHitData> onlyLocal = new(local);
        onlyLocal.ExceptWith(synced); // Removes elements present in `synced`
        foreach(MultiHitData onlyLocalData in onlyLocal) {
            Destroy(hitMarkers[onlyLocalData]);
            hitMarkers.Remove(onlyLocalData);
        }

        HashSet<MultiHitData> onlyRemote = new(synced);
        onlyRemote.ExceptWith(local); // Removes elements present in `local`
        foreach(MultiHitData onlyRemoteData in onlyRemote) {
            GameObject hitMarkerGO = Instantiate(hitMarkerPrefab, onlyRemoteData.location, Quaternion.identity, transform);
            hitMarkers.Add(onlyRemoteData, hitMarkerGO);
        }   

        // ----- Synchronize color -----
        hitMarkers.Keys.ToList().ForEach(hitPoint => {
            GameObject hitMarker = hitMarkers[hitPoint];
            float progress = hitPoint.totalHitAmount/GetRequiredHitsToSplit(hitPoint.identifierPath);
            Color progressColor = hitMarkerGradient.Evaluate(progress);
            progressColor.a = 0.4f;
            
            Renderer renderer = hitMarker.GetComponent<Renderer>();
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_Color", progressColor);  // Change to desired color
            renderer.SetPropertyBlock(propertyBlock);
        });
    }

    /// <summary>
    /// Data describing a player hitting the tree at a specific point
    /// </summary>
    [Serializable]
    public struct SingleHitData
    {
        public int[] identifierPath;
        public Vector3 location;
        public float hitAmount;
        public NetworkConnection owner;
        public SingleHitData(int[] identifierPath, Vector3 location, float hitAmount, NetworkConnection owner) 
        {
            this.identifierPath = identifierPath;
            this.location = location;
            this.hitAmount = hitAmount;
            this.owner = owner;
        }
    }

    /// <summary>
    /// Data describing a point on the tree where it has been hit at least once, stores the total
    ///   amount of hits.
    /// </summary>
    [Serializable]
    public struct MultiHitData 
    {
        public int[] identifierPath;
        public Vector3 location;
        public float totalHitAmount;
        public MultiHitData(int[] identifierPath, Vector3 location, float totalHitAmount) 
        {
            this.identifierPath = identifierPath;
            this.location = location;
            this.totalHitAmount = totalHitAmount;
        }
    }
}