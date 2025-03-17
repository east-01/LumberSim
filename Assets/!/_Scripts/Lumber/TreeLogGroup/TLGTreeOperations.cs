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

/// The tree operations class is responsible for basic tree operations with the TreeLog/TreeLogDatas.
/// It speaks in terms of identifier paths- see TreeLog#GetIdentifierPath() for details.
public partial class TreeLogGroup : NetworkBehaviour, IS3
{
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

}