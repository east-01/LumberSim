using System;
using System.Collections;
using System.Collections.Generic;
using EMullen.Core;
using FishNet;
using FishNet.Component.Transforming;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

[RequireComponent(typeof(NetworkTransform))]
public class TreeLogGroup : NetworkBehaviour
{

    [SerializeField]
    private GameObject logPrefab;

    private readonly SyncVar<TreeLogData> rootData = new();
    public TreeLogData RootData => rootData.Value;
    public TreeLog Root { get {
        return GetComponentInChildren<TreeLog>();
    } }

    private void OnEnable() 
    {
        rootData.OnChange += TreeLogData_OnChange;
    }

    private void OnDisable() 
    {
        rootData.OnChange -= TreeLogData_OnChange;
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

    private void TreeLogData_OnChange(TreeLogData prev, TreeLogData next, bool asServer)
    {
        DataUpdated();
    }
}
