using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TreeEditor;
using EMullen.Core;
using System.Reflection;
using System;
using GameKit.Dependencies.Utilities;
using System.Linq;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using EMullen.SceneMgmt;

[RequireComponent(typeof(TreeLogGroup))]
public class ChoppableTree : NetworkBehaviour
{

    [SerializeField]
    private TreeGrowSettings defaultTreeSettings;

    private readonly SyncVar<float> age = new();
    public float Age => age.Value;
    private TreeGrowSettings settings;
    public TreeLogGroup LogGroup { get; private set; }

    public bool HasGrown => LogGroup != null;

    private void Awake() 
    {
        LogGroup = GetComponent<TreeLogGroup>();
    }

    private void Start() 
    {
        if(!InstanceFinder.IsServerStarted) {
            return;
        }

        Destroy(GetComponent<Rigidbody>());

        UnityEngine.Random.InitState(120);
        age.Value = 0;

        Grow(defaultTreeSettings);

        // TODO: Remove
        SetAge(1000000);
    }

    private void Update() 
    {
        // if(!InstanceFinder.IsServerStarted) {
        //     return;
        // }
    
        // if(Math.Floor(Time.time) != Math.Floor(Time.time - Time.deltaTime) && Math.Floor(Time.time) % 10 == 0) {
        //     SetAge(Time.time);
        // }
    }

    private void OnDrawGizmos() 
    {
        if(!HasGrown)
            return;

        bool drawEndpoints = true;
        if(drawEndpoints) {
            void DrawEndpoints(TreeLog branch) {
                if(branch == null) {
                    Debug.LogError("Can't DrawEndpoints branch is null.");
                    return;
                }

                // branch.DrawGizmos();
                branch.ChildBranches.ToList().ForEach(childBranch => DrawEndpoints(childBranch));
            }
            DrawEndpoints(LogGroup.Root);
        }
    }

    /// <summary>
    /// Spawns all of the TreeLogs
    /// </summary>
    public void Grow(TreeGrowSettings settings) 
    {
        TreeLogData GenerateData(int layerNum, int siblingIndex) 
        {
            int childBranchCount = 0;
            if(layerNum+1 < settings.maxNumBranches.Length) {
                Vector2 branchCountRange = settings.maxNumBranches[layerNum];
                childBranchCount = Mathf.FloorToInt(UnityEngine.Random.Range(branchCountRange.x, branchCountRange.y));
            }

            Vector2 branchLengthRange = settings.maxBranchLength[layerNum];
            float branchLength = UnityEngine.Random.Range(branchLengthRange.x, branchLengthRange.y);

            Vector3 angle = GenerateVector(Vector3.up, layerNum == 0 ? 15f : 80f);

            TreeLogData[] children = new TreeLogData[childBranchCount];
            for(int i = 0; i < children.Length; i++) {
                children[i] = GenerateData(layerNum+1, i);
            }

            return new(
                branchLength,
                1,
                angle,
                siblingIndex,
                children
            );;
        }

        TreeLogData rootData = GenerateData(0, 0);
        LogGroup.SetRootData(rootData);
    }

    public static Vector3 GenerateVector(Vector3 axis, float maxTiltAngle)
    {
        // Normalize the axis to ensure valid calculations
        axis = axis.normalized;

        // Generate a random tilt angle within the range [0, maxTiltAngle]
        float tiltAngle = UnityEngine.Random.Range(0f, maxTiltAngle);

        // Generate a random rotation angle around the axis
        float rotationAngle = UnityEngine.Random.Range(0f, 360f);

        // Create a rotation quaternion for the tilt
        Quaternion tiltRotation = Quaternion.AngleAxis(tiltAngle, UnityEngine.Random.onUnitSphere);

        // Apply the tilt to the axis
        Vector3 tiltedVector = tiltRotation * axis;

        // Create a rotation quaternion for the rotation around the axis
        Quaternion axisRotation = Quaternion.AngleAxis(rotationAngle, axis);

        // Apply the rotation around the axis
        Vector3 finalVector = axisRotation * tiltedVector;

        return finalVector;
    }

    public void SetAge(float age) 
    {
        // if(!HasGrown) {
        //     Debug.LogError("Can't set tree age, the tree hasn't grown");
        //     return;
        // }

        // BLog.Highlight($"Set age to {age}");
        // this.age.Value = age;
        
        // void UpdateAges(TreeLog branch) {
        //     if(branch == null) {
        //         Debug.LogError("Can't UpdateAges branch is null.");
        //         return;
        //     }
        //     int layerNum = branch.LayerNumber;

        //     branch.Grown = age >= layerNum*settings.timePerBranchSet;

        //     if(branch.Grown) {
        //         float progress = (age-layerNum*settings.timePerBranchSet)/settings.timePerBranchSet;
        //         float targLength = Mathf.Lerp(0, branch.MaxLength, progress); // Determine by interpolating to maxBranchLengths with progress variable

        //         if(branch.Length != targLength) {
        //             branch.Length = targLength;
        //         }
        //     }

        //     branch.ChildBranches.ToList().ForEach(childBranch => UpdateAges(childBranch));
        // }

        // UpdateAges(Root);
    }

    public static bool IsChoppableTree(TreeLogGroup treeLogGroup) 
    {
        GameObject obj = treeLogGroup.gameObject;
        return obj.GetComponent<ChoppableTree>() != null;
    }

}

[Serializable]
public struct TreeGrowSettings 
{
    public int branchLayers;
    public float timePerBranchSet;
    public Vector2[] maxBranchLength;
    /// <summary>
    /// An array of Vector2s where the x coordinate is the minimum number of branches and the y
    ///   coordinate is the maximum number of branches.
    /// </summary>
    public Vector2[] maxNumBranches;
}