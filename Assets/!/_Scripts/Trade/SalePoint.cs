using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EMullen.Core;
using EMullen.PlayerMgmt;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;

/// <summary>
/// The LogSellPoint can go on any collider (make collider isTrigger=true) to make it a sell point
///   for lumber. You can change the value of the lumber.
/// </summary>
[RequireComponent(typeof(NetworkedAudioController))]
public class SalePoint : NetworkBehaviour, IS3
{
    private GameplayManager gameplayManager;
    private NetworkedAudioController audioController;

    private void Awake()
    {
        audioController = GetComponent<NetworkedAudioController>();
    }

    public void SingletonRegistered(Type type, object singleton)
    {
        if(type != typeof(GameplayManager))
            return;

        gameplayManager = singleton as GameplayManager;
    }

    public void SingletonDeregistered(Type type, object singleton)
    {
        if(type != typeof(GameplayManager))
            return;

    }

    private void Update()
    {
        // Safely subscribe to the GameplayManager singleton
        if(gameObject.scene.name == "GameplayScene") {
            SceneLookupData lookupData = gameObject.scene.GetSceneLookupData();

            if(!SceneSingletons.IsSubscribed(this, lookupData, typeof(GameplayManager))) {
                SceneSingletons.SubscribeToSingleton(this, lookupData, typeof(GameplayManager));
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {    
        NetworkObject nob = other.GetComponentInParent<NetworkObject>();

        if(nob == null) {
            Debug.LogError($"Failed to get NetworkObject on gameObject named \"{other.gameObject.name}\"");
            return;
        }

        IMarketEvaluator eval = IMarketEvaluator.FindEvaluator(nob.gameObject);
        BLog.Highlight($"Checking for eval: {eval}");
        if(eval == null)
            return;

        SellObject(nob);
    }

    /// <summary>
    /// Sell a NetworkObject. Runs on the server via an auto ServerRpc.
    /// </summary>
    /// <param name="nob">The NetworkObject to sell.</param>
    private void SellObject(NetworkObject nob) 
    {
        if(nob == null) {
            Debug.LogError("Can't sell log the log group network object is null!");
            return;
        }

        audioController.PlaySound("chaching");

        if(!InstanceFinder.IsServerStarted) {
            ServerRpcSellLog(nob);
            return;
        }

        NetworkConnection owner = nob.Owner;
        if(!owner.IsValid) {
            Debug.LogWarning("Sold log to invalid owner!");
            return;
        }

        IMarketEvaluator eval = IMarketEvaluator.FindEvaluator(nob.gameObject);
        if(eval == null)
            throw new InvalidOperationException("Tried to sell a network object that doesn't have an IMarketEvaluator on it.");

        float saleValue = eval.EvaluateSalePrice(gameplayManager.GlobalMarket);

        if(saleValue == -1)
            return;

        // Assumes connected to lobby bootstrapped
        List<PlayerData> connectionsPlayerDatas = PlayerDataRegistry.Instance.GetAllData().ToList().Where(pd => pd.GetData<NetworkIdentifierData>().clientID == owner.ClientId).ToList();
        connectionsPlayerDatas.ForEach(pd => {
            GeneralPlayerData gpd = pd.GetData<GeneralPlayerData>();
            gpd.balance += saleValue;
            pd.SetData(gpd);
        });

        InstanceFinder.ServerManager.Despawn(nob);
    }
    [ServerRpc(RequireOwnership = false)]
    private void ServerRpcSellLog(NetworkObject logGroupNetworkObject) => SellObject(logGroupNetworkObject);
}
