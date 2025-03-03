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
public class LogSellPoint : NetworkBehaviour
{
    [SerializeField]
    private float pricePerFt = 3f;
    [SerializeField]
    private float radiusMultiplier = 1f;

    void OnTriggerEnter(Collider other)
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
        if(!InstanceFinder.IsServerStarted) {
            ServerRpcSellLog(logGroupNetworkObject);
            return;
        }

        NetworkConnection owner = logGroupNetworkObject.Owner;
        if(!owner.IsValid) {
            Debug.LogWarning("Sold log to invalid owner!");
            return;
        }

        BLog.Highlight("Owner's client id: " + owner.ClientId);

        float saleValue = logGroupNetworkObject.GetComponent<TreeLogGroup>().EvaluateLumber(pricePerFt, radiusMultiplier);
        BLog.Highlight("Sale value: " + saleValue);

        // Assumes connected to lobby bootstrapped
        List<PlayerData> connectionsPlayerDatas = PlayerDataRegistry.Instance.GetAllData().ToList().Where(pd => pd.GetData<NetworkIdentifierData>().clientID == owner.ClientId).ToList();
        BLog.Highlight("Got " + connectionsPlayerDatas.Count + " connection datas.");
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
