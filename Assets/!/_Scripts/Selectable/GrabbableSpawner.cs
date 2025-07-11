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
public class GrabbableSpawner : MonoBehaviour, IS3
{
    /// <summary>
    /// Has sent the warning for destroying self if on client instance.
    /// </summary>
    private static bool hasWarned = false;

    private GameplayManager gameplayManager;

    [SerializeField]
    protected GameObject grabbablePrefab;

    private float startTime;

    void Start()
    {
        if(!InstanceFinder.IsServerStarted) {
            if(!hasWarned) {
                Debug.LogWarning("Server isn't started. Destroying GrabbableSpawner self");
                hasWarned = true;
            }
            Destroy(gameObject);
            return;
        }

        SceneLookupData lookupData = gameObject.scene.GetSceneLookupData();

        if(!SceneSingletons.IsSubscribed(this, lookupData, typeof(GameplayManager))) {
            SceneSingletons.SubscribeToSingleton(this, lookupData, typeof(GameplayManager));
        }

        startTime = Time.time;
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

    void Update()
    {
        // Spawn the tree if the gameplay manager exists and it hasn't already been spawned.
        if(gameplayManager != null && (Time.time - startTime) > 0.3f) {
            Spawn();
            gameObject.SetActive(false);
        }
    }

    protected virtual Grabbable Spawn() 
    {
        GameplayManager.SpawnGrabbaleArgs args = new(transform.position, Quaternion.identity, Vector3.zero, grabbablePrefab);
        return gameplayManager.SpawnGrabbable(args);       
    }
}
