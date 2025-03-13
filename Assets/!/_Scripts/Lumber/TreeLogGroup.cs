using System;
using System.Collections.Generic;
using EMullen.Core;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(NetworkedAudioController))]
public class TreeLogGroup : NetworkBehaviour, IS3
{
    private NetworkedAudioController audioController;

    [SerializeField]
    private float minLogLength = 0.2f;

    [SerializeField]
    private GameObject logPrefab;
    [SerializeField]
    private List<AudioClip> logHitAudios;

    private GameplayManager gameplayManager;

    private readonly SyncVar<TreeLogData> rootData = new();
    public TreeLogData RootData => rootData.Value;
    public TreeLog Root { get; private set; }
    public void SetRoot(TreeLog root) {
        Root = root;
    }

    /// <summary>
    /// Set this flag if the tree should prune tiny logs on the next possible frame.
    /// </summary>
    public bool ShouldPrune = false;

    #region Initializers
    private void Awake()
    {
        audioController = GetComponent<NetworkedAudioController>();
    }

    private void OnEnable() 
    {
        rootData.OnChange += TreeLogData_OnChange;
    }

    private void OnDisable() 
    {
        rootData.OnChange -= TreeLogData_OnChange;
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
#endregion

    public void Delete() 
    {
        Destroy(gameObject);
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

        if(ShouldPrune && gameplayManager != null) {
            PruneTinyLogs();
        }
    }

    /// <summary>
    /// The root TreeLogData has changed, update nodes
    /// </summary>
    public void DataUpdated() 
    {
        void EnsureDataMatches(TreeLogData data, TreeLog parent, TreeLog test, bool isRoot = false) {
            
            if(test == null) {
                Transform parentTransform = parent != null ? parent.transform : transform;

                GameObject logObject = Instantiate(logPrefab, parentTransform);
                test = logObject.GetComponent<TreeLog>();
                test.Initialize(data);
            }

            bool destroyChildren = test.ChildBranches.Length != data.children.Length;
            if(destroyChildren) {
                for(int childIdx = 0; childIdx < test.ChildBranches.Length; childIdx++) {
                    Destroy(test.ChildBranches[childIdx].gameObject);
                }
            }

            if(test.Data.length - data.length > 0.01 ||
               test.Data.radius - data.radius > 0.01 ||
               test.Data.angle != data.angle) {
                test.SetData(data, true);
            }

            for(int i = 0; i < data.children.Length; i++) {
                TreeLog[] childBranches = test.ChildBranches;
                TreeLog child = i < childBranches.Length ? childBranches[i] : null;
                if(destroyChildren)
                    child = null;
                EnsureDataMatches(data.children[i], test, child);
            }
        }

        EnsureDataMatches(rootData.Value, null, Root, true);
    }
    private void TreeLogData_OnChange(TreeLogData prev, TreeLogData next, bool asServer) => DataUpdated();

    public void HitLog(int[] identifierPath, Vector3 hitGlobal, NetworkConnection childOwner = null) 
    {
        // Not stoked with hard-coding this, see NetworkedAudioController description
        audioController.PlaySound($"log_hit_{UnityEngine.Random.Range(1, 7)}");

        if(!InstanceFinder.IsServerStarted) {
            ServerRpcHitLog(identifierPath, hitGlobal, LocalConnection);
            return;
        }

        // TODO: Logic for storing multiple hits

        SplitLog(identifierPath, hitGlobal, childOwner);
    }
    [ServerRpc(RequireOwnership = false)]
    private void ServerRpcHitLog(int[] treeLogIP, Vector3 hitGlobal, NetworkConnection childOwner = null) => HitLog(treeLogIP, hitGlobal, childOwner);

    public void SplitLog(int[] identifierPath, Vector3 hitGlobal, NetworkConnection childOwner = null) 
    {
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
        TreeLogGroup newGroup = gameplayManager.SpawnLogObject(hitGlobal, transform.rotation, newLogData, childOwner);

        // ----- Cleanup -----
        ShouldPrune = true;
        newGroup.ShouldPrune = true;
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

    public Vector3 GetFrontEndpoint(int[] identifierPath) 
    {
        Vector3 position = transform.position;
        Quaternion baseRotation = transform.rotation;
        void OffsetPositionBy(TreeLogData data) {
            Vector3 worldDirection = baseRotation * data.angle.normalized; // Rotate direction to world space
            position += worldDirection * data.length;
        }

        TreeLog curr = Root;
        OffsetPositionBy(curr.Data);
        for(int branchLayer = 0; branchLayer < identifierPath.Length; branchLayer++) {
            int siblingIdx = identifierPath[branchLayer];
            if(siblingIdx < 0 || siblingIdx >= curr.ChildBranches.Length)
                throw new InvalidOperationException($"Can't get TreeLog from identifierPath, the sibling idx \"{siblingIdx}\" is invalid for arr of len {curr.ChildBranches.Length}");
            
            curr = curr.ChildBranches[identifierPath[branchLayer]];
            OffsetPositionBy(curr.Data);
        }

        return position;
    }
    
#region Tree Operations
    /// <summary>
    /// Gets the TreeLog object at the identifier path on the tree.
    /// Uses a tree identifier path- See TreeLog#GetIdentifierPath() for details.
    /// </summary>
    public TreeLog TreeOpGet(int[] identifierPath) 
    {
        TreeLog curr = Root;
        for(int branchLayer = 0; branchLayer < identifierPath.Length; branchLayer++) {
            int siblingIdx = identifierPath[branchLayer];
            if(siblingIdx < 0 || siblingIdx >= curr.ChildBranches.Length)
                throw new InvalidOperationException($"Can't get TreeLog from identifierPath, the sibling idx \"{siblingIdx}\" is invalid for arr of len {curr.ChildBranches.Length}");
            curr = curr.ChildBranches[identifierPath[branchLayer]];
        }
        return curr;
    }

    /// <summary>
    /// Delete the TreeLogData at the specified identifier path.
    /// Uses a tree identifier path- See TreeLog#GetIdentifierPath() for details.
    /// </summary>
    /// <param name="treeLogIP"></param>
    public void TreeOpDelete(int[] treeLogIP) 
    {
        if(!InstanceFinder.IsServerStarted)
            throw new Exception("TreeOpDelete can't be executed on clients");
        if(treeLogIP.Length == 0) {
            Debug.LogError("Can't delete TreeLogData, the treeLogIP specified points to root!");
            return;
        }

        TreeLogData rootData = this.rootData.Value;
        // This loop gets us to the correct target data
        TreeLogData targData = rootData;
        for(int level = 0; level < treeLogIP.Length-1; level++) {
            int childIdx = treeLogIP[level];
            if(childIdx < 0 || childIdx >= targData.children.Length)
                throw new InvalidOperationException($"Can't get TreeLog from identifierPath, the sibling idx \"{childIdx}\" is invalid for arr of len {targData.children.Length}");
            targData = targData.children[childIdx];
        }

        // Delete the right child from the target data
        targData.RemoveChild(treeLogIP[^1]);

        this.rootData.Value = rootData;

        DataUpdated();
    }

    /// <summary>
    /// Set the TreeLogData at the specified identifier path.
    /// Uses a tree identifier path- See TreeLog#GetIdentifierPath() for details.
    /// </summary>
    /// <param name="treeLogIP">The identifier path of the data to set.</param>
    /// <param name="newData">The new data to set.</param>
    public void TreeOpSet(int[] treeLogIP, TreeLogData newData) 
    {
        if(!InstanceFinder.IsServerStarted)
            throw new Exception("TreeOpDelete can't be executed on clients");
        if(treeLogIP.Length > 0) {
            // This loop gets us to the correct target data
            TreeLogData targData = rootData.Value;
            for(int level = 0; level < treeLogIP.Length-1; level++) {
                int childIdx = treeLogIP[level];
                if(childIdx < 0 || childIdx >= targData.children.Length)
                    throw new InvalidOperationException($"Can't get TreeLog from identifierPath, the sibling idx \"{childIdx}\" is invalid for arr of len {targData.children.Length}");
                targData = targData.children[childIdx];
            }

            targData.children[treeLogIP[^1]] = newData;
        } else {
            rootData.Value = newData;
        }

        rootData.DirtyAll();

        DataUpdated();
    }
    /// <summary>
    /// Set the root TreeLogData for this TreeLogGroup.
    /// Shortcut for TreeOpSet([], rootData)
    /// </summary>
    /// <param name="rootData">The new data to set.</param>
    public void SetRootData(TreeLogData rootData) => TreeOpSet(new int[0], rootData);
    #endregion

    private void OnCollisionEnter(Collision collision)
    {
        float impactSpeed = collision.relativeVelocity.magnitude;
        // Ensure only forceful impacts make noise
        if(impactSpeed < 0.25f)
            return;

        audioController.PlaySound($"drop{UnityEngine.Random.Range(1, 7)}");
    }

}
