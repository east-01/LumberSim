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

    public Dictionary<int, Grabbable> grabbables = new();
    /// <summary>
    /// A list of grabbables that have yet to be stored in the grabbables dict. This can happen
    ///   when you try to register a grabbable before it is spawned (meaning network object id = 0).
    /// </summary>
    private List<Grabbable> unregisteredGrabbables = new();

    public LumberLobby Lobby;


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
        List<Grabbable> registeredGrabbables = new(); // List of grabbables that successfully registered
        foreach(Grabbable grabbable in unregisteredGrabbables) {
            if(grabbable.IsSpawned) {
                if(RegisterGrabbable(grabbable))
                    registeredGrabbables.Add(grabbable);
            }
        }
        if(registeredGrabbables.Count > 0)
            unregisteredGrabbables = unregisteredGrabbables.Except(registeredGrabbables).ToList();
    }

    // TODO: Below probably should go in its own TreeManager
    [SerializeField]
    private GameObject logObjectPrefab;
    [SerializeField]
    private GameObject choppableTreePrefab;

    public Grabbable SpawnGrabbable(SpawnGrabbaleArgs args) 
    {
        if(!InstanceFinder.IsServerStarted) 
            throw new InvalidOperationException("Can't spawn grabbable. Server isn't started.");

        Vector3 position = args.position;
        Quaternion rotation = args.rotation;
        GameObject grabbablePrefab = args.grabbablePrefab;
        NetworkConnection ownerConnection = args.ownerConnection;

        if(!grabbablePrefab.TryGetComponent(out Grabbable grabbableOnPrefab)) 
            throw new InvalidOperationException("Can't spawn prefab as Grabbable, it does not have Grabbable component.");

        GameObject grabbableObject = Instantiate(grabbablePrefab);
        grabbableObject.transform.SetPositionAndRotation(position, rotation);
        grabbableObject.GetComponent<Rigidbody>().velocity = args.initialVelocity;
        InstanceFinder.ServerManager.Spawn(grabbableObject, ownerConnection, gameObject.scene);

        Grabbable grabbable = grabbableObject.GetComponent<Grabbable>();
        RegisterGrabbable(grabbable);

        return grabbable;
    }
    
    [Serializable]
    public struct SpawnGrabbaleArgs 
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 initialVelocity;
        public GameObject grabbablePrefab;
        public NetworkConnection ownerConnection;

        public SpawnGrabbaleArgs(Vector3 position, Quaternion rotation, Vector3 initialVelocity, GameObject grabbablePrefab, NetworkConnection ownerConnection = null) 
        {
            this.position = position;
            this.rotation = rotation;
            this.initialVelocity = initialVelocity;
            this.grabbablePrefab = grabbablePrefab;
            this.ownerConnection = ownerConnection;
        }
    }

    /// <summary>
    /// Register a grabbable's network id in the grabbables dictionary. The NetworkObject must be
    ///   spawned for success.
    /// </summary>
    /// <param name="grabbable">The grabbable to register.</param>
    /// <returns>Success status, registering fails if the grabbable hasn't spawned yet.</returns>
    public bool RegisterGrabbable(Grabbable grabbable) 
    {
        if(!grabbable.IsSpawned) {
            if(!unregisteredGrabbables.Contains(grabbable))
                unregisteredGrabbables.Add(grabbable);
            return false;
        }

        int id = grabbable.NetworkObject.ObjectId;

        grabbables.Add(id, grabbable);
        return true;
    }

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

        if(logObject.TryGetComponent(out Grabbable grabbable)) {
            RegisterGrabbable(grabbable);
        } else {
            Debug.LogWarning("Failed to get Grabbable component on the TreeLogGroup prefab we're spawning.");
        }

        InstanceFinder.ServerManager.Spawn(logObject, ownerConnection, gameObject.scene);

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

    public Grabbable GetGrabbableFromNetworkID(int netID) {
        return grabbables[netID];
    }

}
