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

/// The data management class is responsible for all TreeLogData objects in a group.
public partial class TreeLogGroup : NetworkBehaviour, IS3
{
    private readonly SyncVar<TreeLogData> rootData = new();
    public TreeLogData RootData => rootData.Value;
    public TreeLog Root => GetComponentInChildren<TreeLog>();

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
}