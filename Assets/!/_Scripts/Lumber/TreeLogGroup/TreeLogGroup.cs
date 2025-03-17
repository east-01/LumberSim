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

/// <summary>
/// The TreeLogGroup is a class that handles everything for lumber groups. It's a partial class
///   with multiple subsections: data management, hit/split management, and tree operations. See
///   each classes .cs file for more detailed descriptions.
/// </summary>
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(NetworkedAudioController))]
public partial class TreeLogGroup : NetworkBehaviour, IS3
{
    [SerializeField]
    private GameObject logPrefab;
    [SerializeField]
    private List<AudioClip> logHitAudios;

    private GameplayManager gameplayManager;
    private NetworkedAudioController audioController;

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

        HitSplitUpdate();

        if(ShouldPrune && gameplayManager != null) {
            PruneTinyLogs();
        }

        Root.gameObject.name = "TreeLog (Root)";
    }

    private void OnCollisionEnter(Collision collision)
    {
        float impactSpeed = collision.relativeVelocity.magnitude;
        // Ensure only forceful impacts make noise
        if(impactSpeed < 0.25f)
            return;

        audioController.PlaySound($"drop{UnityEngine.Random.Range(1, 7)}");
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
