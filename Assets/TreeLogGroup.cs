using System;
using EMullen.Core;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Component.Transforming;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

[RequireComponent(typeof(NetworkTransform))]
public class TreeLogGroup : NetworkBehaviour, IS3
{

    [SerializeField]
    private GameObject logPrefab;

    private GameplayManager gameplayManager;

    private readonly SyncVar<TreeLogData> rootData = new();
    public TreeLogData RootData => rootData.Value;
    public TreeLog Root { get {
        return GetComponentInChildren<TreeLog>();
    } }

#region Initializers
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

    /// <summary>
    /// The root TreeLogData has changed, update nodes
    /// </summary>
    public void DataUpdated() 
    {
        void EnsureDataMatches(TreeLogData data, TreeLog parent, TreeLog test) {
            
            if(test == null) {
                Transform parentTransform = parent != null ? parent.transform : transform;

                GameObject logObject = Instantiate(logPrefab, parentTransform);
                test = logObject.GetComponent<TreeLog>();
                test.Initialize(data);
            }

            if(test.ChildBranches.Length != data.children.Length) {
                for(int childIdx = 0; childIdx < test.ChildBranches.Length; childIdx++) {
                    Destroy(test.ChildBranches[childIdx].gameObject);
                }
            }

            if(test.Data.length - data.length > 0.01 ||
               test.Data.radius - data.radius > 0.01 ||
               test.Data.angle != data.angle) {
                test.SetData(data);
            }

            for(int i = 0; i < data.children.Length; i++) {
                TreeLog[] childBranches = test.ChildBranches;
                TreeLog child = i < childBranches.Length ? childBranches[i] : null;
                EnsureDataMatches(data.children[i], test, child);
            }
        }

        EnsureDataMatches(rootData.Value, null, Root);
    }

    public void SetRootData(TreeLogData rootData) {
        if(!InstanceFinder.IsServerStarted) {
            Debug.LogError("Can't SetRootData, server isn't started.");
            return;
        }
        this.rootData.Value = rootData;
        DataUpdated();
        BLog.Highlight("Updated root data w len " + rootData.length);
    }

    /// <summary>
    /// See TreeLog#GetIdentifierPath() for details, we follow each sibling index until the target
    ///   is found.
    /// </summary>
    public TreeLog GetTreeLog(int[] identifierPath) 
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

    public void HitLog(int[] identifierPath, Vector3 hitGlobal) 
    {
        if(!InstanceFinder.IsServerStarted) {
            ServerRpcHitLog(identifierPath, hitGlobal);
            return;
        }

        TreeLog hitLog = GetTreeLog(identifierPath);
        if(hitLog == null) {
            Debug.LogError($"Failed to resolve hit log from treelog ip: {string.Join(", ", identifierPath)}");
            return;
        }

        TreeLogData origHitLogData = hitLog.Data.Clone();

        Vector3 endpointForward = GetFrontEndpoint(identifierPath);
        Vector3 endpointBackward = hitLog.transform.position;

        Debug.DrawLine(hitGlobal, endpointBackward, Color.red, 60);
        Debug.DrawLine(endpointForward, endpointBackward, Color.yellow, 60);

        // Store log length for finding the new logs length
        float origLogLength = hitLog.Data.length;

        // Calculate the length of the log still attached
        Vector3 hitPointVector = hitGlobal - endpointBackward;
        Vector3 logVector = endpointForward-endpointBackward;
        float attachedLength = Vector3.Dot(hitPointVector, logVector.normalized);

        // Create the data for the log that is still attached to the group
        TreeLogData newAttachedData = origHitLogData.Clone();
        newAttachedData.length = attachedLength;
        newAttachedData.children = new TreeLogData[0];

        // Insert newly created data into the original groups data tree
        TreeLogData focusData = RootData;
        if(identifierPath.Length > 0) {
            for(int branchLayer = 0; branchLayer < identifierPath.Length; branchLayer++) {
                if(branchLayer < identifierPath.Length - 1) {
                    // For all branches except the one we're targeting, iterate through the tree
                    focusData = focusData.children[identifierPath[branchLayer]];
                } else {
                    // For the parent of the branch we're targeting, set the child to the new attached data
                    focusData.children[identifierPath[branchLayer]] = newAttachedData;
                }
            }
            DataUpdated();
        } else {
            SetRootData(newAttachedData);
        }

        // Create the data for the new log group to be spawned
        float newLogLength = origLogLength-attachedLength;
        TreeLogData newLogData = origHitLogData.Clone();
        newLogData.length = newLogLength;

        // Spawn new log group
        gameplayManager.SpawnLogObject(hitGlobal, transform.rotation, newLogData);
    }
    [ServerRpc(RequireOwnership = false)]
    private void ServerRpcHitLog(int[] treeLogIP, Vector3 hitGlobal) => HitLog(treeLogIP, hitGlobal);

    private void TreeLogData_OnChange(TreeLogData prev, TreeLogData next, bool asServer)
    {
        DataUpdated();
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

}
