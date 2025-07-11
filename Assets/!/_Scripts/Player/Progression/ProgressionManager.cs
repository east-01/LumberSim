using System;
using System.Collections.Generic;
using System.Linq;
using EMullen.Core;
using EMullen.PlayerMgmt;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

/// <summary>
/// The ProgressionManager sits on the player and holds the progression tree and evaluation results 
///   for this specific player.
/// </summary>
public class ProgressionManager : NetworkBehaviour 
{
    [SerializeField]
    private ProgressionTree progressionTree;
    public ProgressionTree ProgressionTree => progressionTree;
    [SerializeField]
    private ItemAssignments itemAssignments;

    public readonly SyncVar<ProgressionTree.EvaluationResults> ProgressionResults = new();
    private Player player;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void OnEnable()
    {
        PlayerDataRegistry.Instance.PlayerDataUpdatedEvent += PlayerDataRegistry_PlayerDataUpdated;        
    }

    private void OnDisable()
    {
        PlayerDataRegistry.Instance.PlayerDataUpdatedEvent -= PlayerDataRegistry_PlayerDataUpdated;
    }

    private void PlayerDataRegistry_PlayerDataUpdated(PlayerData playerData, PlayerDataClass newData)
    {
        List<Type> whitelistedTypes = new() { typeof(ProgressionData), typeof(GeneralPlayerData) };
        if(playerData.GetUID() == player.uid.Value && whitelistedTypes.Contains(newData.GetType()))
            UpdateProgressionResults();
    }

    private void Update()
    {

    }

    public void UpdateProgressionResults() 
    {
        if(!InstanceFinder.IsServerStarted) {
            ServerRPCUpdateProgressionResults();
            return;
        }

        if(progressionTree == null) 
            throw new InvalidOperationException("Can't update progression results, there is no progression tree.");

        ProgressionResults.Value = progressionTree.Evaluate(player.PlayerData);

        ProgressionData progression = player.PlayerData.GetData<ProgressionData>();
        List<Tuple<string, ProgressionMetric.MetricType>> missingMetrics = progressionTree.GetMissingMetrics(progression, ProgressionResults.Value.visiblePoints);
        if(missingMetrics.Count > 0) {
            missingMetrics.ForEach(metric => {
                progression.AddMetric(new(metric.Item1, metric.Item2, ProgressionMetric.GetDefaultValue(metric.Item2)));
            });
            player.PlayerData.SetData(progression);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void ServerRPCUpdateProgressionResults() => UpdateProgressionResults();

    public void UnlockProgressionPoint(string progressionPointID) 
    {
        if(!InstanceFinder.IsServerStarted) {
            ServerRPCUnlockProgressionPoint(LocalConnection, progressionPointID);
            return;
        }

        UpdateProgressionResults();

        if(!ProgressionResults.Value.canUnlock.Contains(progressionPointID))
            throw new InvalidOperationException($"Player tried to unlock progression point \"{progressionPointID}\" when they can't.");

        PlayerData pd = player.PlayerData;
        GeneralPlayerData gpd = pd.GetData<GeneralPlayerData>();
        gpd.balance -= progressionTree.PointByID[progressionPointID].price;
        player.PlayerData.SetData(gpd);

        ProgressionData progression = pd.GetData<ProgressionData>();
        progression.unlockedPoints.Add(progressionPointID);
        pd.SetData(progression);

        player.NetworkedAudioController.PlaySound("purchased");

        if(InstanceFinder.IsServerStarted)
            player.GameplayManager.UpdateAllSelectables();
    }
    [ServerRpc(RequireOwnership = false)]
    private void ServerRPCUnlockProgressionPoint(NetworkConnection unlocker, string progressionPointID) 
    {
        UnlockProgressionPoint(progressionPointID);
        TargetRPCProgressionPointUnlocked(unlocker);
    }
    [TargetRpc]
    private void TargetRPCProgressionPointUnlocked(NetworkConnection unlocker) 
    {
        player.GameplayManager.UpdateAllSelectables();
    }

    public bool CanUseItem(Item item) 
    {
        ProgressionData progression = player.PlayerData.GetData<ProgressionData>();
        List<ProgressionPoint> requiredPoints = itemAssignments.Get(item).requiredProgressionPoints;
        if(requiredPoints.Count == 0)
            return true;

        return requiredPoints.Exists(pp => progression.HasUnlocked(pp.progressionID));
    }

    public List<ProgressionPoint> ItemRequiredProgressionPoints(Item item) 
    {
        ProgressionData progression = player.PlayerData.GetData<ProgressionData>();
        List<ProgressionPoint> requiredPoints = itemAssignments.Get(item).requiredProgressionPoints;
        return requiredPoints.Where(pp => !progression.HasUnlocked(pp.progressionID)).ToList();
    }

}