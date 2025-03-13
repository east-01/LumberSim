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
using FishNet.Transporting;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayManager : NetworkBehaviour
{

    public static Dictionary<SceneLookupData, GameplayManager> SceneSingleton = new();

    public Scene GameplayScene => gameObject.scene;

    public Dictionary<int, TreeLogGroup> treeLogGroups = new();

    private void Start() 
    {
        SceneSingletons.Register(this);
    }

    private void OnEnable() 
    {
        if(SceneSingleton.ContainsKey(gameObject.scene.GetSceneLookupData())) {
            Debug.LogError("GameplayManager Instance already exists, destroying.");
            return;
        }

        SceneSingleton.Add(gameObject.scene.GetSceneLookupData(), this);

        // if(!InstanceFinder.IsServerStarted) {
        //     Debug.LogWarning("Gameplay manager is enabled without server being active, disabling.");
        //     gameObject.SetActive(false);
        //     return;
        // }

        if(InstanceFinder.IsClientStarted)
            InstanceFinder.ClientManager.RegisterBroadcast<ClientNetworkedScene>(OnClientNetworkedScene);
    }

    private void OnDisable() 
    {
        if(SceneSingleton.ContainsKey(gameObject.scene.GetSceneLookupData()))
            SceneSingleton.Remove(gameObject.scene.GetSceneLookupData());
            
        if(InstanceFinder.IsClientStarted)
            InstanceFinder.ClientManager.UnregisterBroadcast<ClientNetworkedScene>(OnClientNetworkedScene);
    }

    private void Update() 
    {

    }

    private void OnClientNetworkedScene(ClientNetworkedScene msg, Channel channel)
    {
        if(msg.scene.Name != "GameplayScene")
            return;

        // GameplayScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(msg.scene.Name);
    }

    // TODO: Below probably should go in its own TreeManager
    [SerializeField]
    private GameObject logObjectPrefab;
    [SerializeField]
    private GameObject choppableTreePrefab;

    public TreeLogGroup SpawnLogObject(Vector3 position, Quaternion rotation, TreeLogData rootData, NetworkConnection ownerConnection = null) 
    {
        if(!InstanceFinder.IsServerStarted) 
            throw new InvalidOperationException("Can't spawn log object, server isn't started.");
    
        GameObject logObject = Instantiate(logObjectPrefab);

        TreeLogGroup log = logObject.GetComponent<TreeLogGroup>();
        logObject.transform.SetPositionAndRotation(position, rotation);
        log.SetRootData(rootData);

        InstanceFinder.ServerManager.Spawn(logObject, ownerConnection, gameObject.scene);

        // logObject.transform.position = position;

        treeLogGroups.Add(log.NetworkObject.ObjectId, log);

        return log;
    }

    public ChoppableTree SpawnTree(Vector3 position) 
    {
        if(!InstanceFinder.IsServerStarted)
            throw new InvalidOperationException("Can't spawn tree, server isn't started");
        
        GameObject treeObject = Instantiate(choppableTreePrefab);

        InstanceFinder.ServerManager.Spawn(treeObject, null, gameObject.scene);

        treeObject.transform.position = position;

        return treeObject.GetComponent<ChoppableTree>();
    }

    public TreeLogGroup GetLogGroupFromNetworkID(int netID) {
        return treeLogGroups[netID];
    }

}
