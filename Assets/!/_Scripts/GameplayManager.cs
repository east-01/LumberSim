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

/// <summary>
/// The GameplayManager class is a networked scene singleton that controls general gameplay actions.
/// It mainly controlls TreeLogGroups at the moment, but that should be split to its own manager soon.
/// </summary>
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
    }

    private void OnDisable() 
    {
        if(SceneSingleton.ContainsKey(gameObject.scene.GetSceneLookupData()))
            SceneSingleton.Remove(gameObject.scene.GetSceneLookupData());
    }

    private void Update() 
    {

    }

    // TODO: Below probably should go in its own TreeManager
    [SerializeField]
    private GameObject logObjectPrefab;
    [SerializeField]
    private GameObject choppableTreePrefab;

    /// <summary>
    /// Spawn a TreeLogGroup game object.
    /// </summary>
    /// <param name="position">The position to spawn the TreeLogGroup at.</param>
    /// <param name="rotation">The rotation to spawn the TreeLogGroup with.</param>
    /// <param name="rootData">The root data to spawn the TreeLogGroup with.</param>
    /// <param name="ownerConnection">The owning connection of this TreeLogGroup. Mandatory to 
    ///    track sale payouts.</param>
    /// <returns>The spawned TreeLogGroup</returns>
    /// <exception cref="InvalidOperationException">The server isn't started.</exception>
    public TreeLogGroup SpawnLogObject(Vector3 position, Quaternion rotation, TreeLogData rootData, NetworkConnection ownerConnection = null) 
    {
        if(!InstanceFinder.IsServerStarted) 
            throw new InvalidOperationException("Can't spawn log object, server isn't started.");
    
        GameObject logObject = Instantiate(logObjectPrefab);

        TreeLogGroup log = logObject.GetComponent<TreeLogGroup>();
        logObject.transform.SetPositionAndRotation(position, rotation);
        log.SetRootData(rootData);

        InstanceFinder.ServerManager.Spawn(logObject, ownerConnection, gameObject.scene);

        treeLogGroups.Add(log.NetworkObject.ObjectId, log);

        return log;
    }

    /// <summary>
    /// Spawn the ChoppableTree prefab at the Vector3 position.
    /// </summary>
    /// <returns>The spawned ChoppableTree.</returns>
    /// <exception cref="InvalidOperationException">The server isn't started.</exception>
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
