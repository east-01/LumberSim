using System;
using System.Collections;
using System.Collections.Generic;
using EMullen.Core;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Managing.Scened;
using UnityEngine;

/// <summary>
/// The GrabbableSpawner class is responsible for spawning a grabbable at this transform's position 
///   once the server starts.
/// </summary>
public class ItemSpawner : GrabbableSpawner
{
    [SerializeField]
    private Item spawnAs;

    protected override Grabbable Spawn() {
        Grabbable grabbable = base.Spawn();
        if(!grabbable.TryGetComponent(out GrabbableItem grabbableItem)) {
            Debug.LogError($"Can't spawn item from grabbale prefab \"{grabbablePrefab}\" it does not have a GrabbableItem component on it!");
            return null;
        }

        grabbableItem.SetItem(spawnAs);
        return grabbable;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, 0.5f);       
    }
}
