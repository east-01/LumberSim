using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EMullen.Core;
using EMullen.PlayerMgmt;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(LumberEvaluator))]
[RequireComponent(typeof(NetworkedAudioController))]
public class LogSellPoint : NetworkBehaviour
{
    [SerializeField]
    private float pricePerFt = 3f;
    [SerializeField]
    private float radiusMultiplier = 1f;

    private NetworkedAudioController audioController;

    private void Awake()
    {
        audioController = GetComponent<NetworkedAudioController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!other.CompareTag("TreeLog"))
            return;

        NetworkObject nob = other.GetComponentInParent<NetworkObject>();

        if(nob == null) {
            Debug.LogError($"Failed to get NetworkObject on gameObject named \"{other.gameObject.name}\"");
            return;
        }

        SellLog(nob);
        // Destroy(other.GetComponentInParent<TreeLogGroup>().gameObject);
    }

    private void SellLog(NetworkObject logGroupNetworkObject) 
    {
        if(logGroupNetworkObject == null) {
            Debug.LogError("Can't sell log the log group network object is null!");
            return;
        }

        audioController.PlaySound("chaching");

        if(!InstanceFinder.IsServerStarted) {
            ServerRpcSellLog(logGroupNetworkObject);
            return;
        }

        NetworkConnection owner = logGroupNetworkObject.Owner;
        if(!owner.IsValid) {
            Debug.LogWarning("Sold log to invalid owner!");
            return;
        }

        float saleValue = logGroupNetworkObject.GetComponent<TreeLogGroup>().EvaluateLumber(pricePerFt, radiusMultiplier);

        // Assumes connected to lobby bootstrapped
        List<PlayerData> connectionsPlayerDatas = PlayerDataRegistry.Instance.GetAllData().ToList().Where(pd => pd.GetData<NetworkIdentifierData>().clientID == owner.ClientId).ToList();
        connectionsPlayerDatas.ForEach(pd => {
            GeneralPlayerData gpd = pd.GetData<GeneralPlayerData>();
            gpd.balance += saleValue;
            pd.SetData(gpd);
        });

        InstanceFinder.ServerManager.Despawn(logGroupNetworkObject);
    }
    [ServerRpc(RequireOwnership = false)]
    private void ServerRpcSellLog(NetworkObject logGroupNetworkObject) => SellLog(logGroupNetworkObject);
}
