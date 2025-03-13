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

    // TODO: Move to TreeSpawner
    [SerializeField]
    private TreeBranchLayerSettings[] defaultTreeSettings;

    private readonly SyncVar<float> age = new();
    public float Age => age.Value;
    private TreeBranchLayerSettings[] settings;
    public TreeLogGroup LogGroup { get; private set; }

    public bool HasGrown => LogGroup != null;

    private void Awake() 
    {
        LogGroup = GetComponent<TreeLogGroup>();
    }

    private void Start() 
    {
        Destroy(GetComponent<Rigidbody>());

        if(!InstanceFinder.IsServerStarted) {
            return;
        }

        // UnityEngine.Random.InitState(120);
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
    public void Grow(TreeBranchLayerSettings[] settings) 
    {
        TreeLogData GenerateData(float parentRadius, int layerNum, int siblingIndex) 
        {
            TreeBranchLayerSettings layerSettings = settings[layerNum];

            int childBranchCount = 0;
            Vector2 numBranchesRange = layerSettings.childCountRange;
            childBranchCount = Mathf.FloorToInt(UnityEngine.Random.Range(numBranchesRange.x, numBranchesRange.y));

            Vector2 branchLengthRange = layerSettings.lengthRange;
            float branchLength = UnityEngine.Random.Range(branchLengthRange.x, branchLengthRange.y);

            Vector2 branchRadiusRange = layerSettings.radiusRange;
            float branchRadius = UnityEngine.Random.Range(branchRadiusRange.x, branchRadiusRange.y);
            if(layerNum > 0) {
                // The branch radius values for branches other than the trunk are scale values for the parent branch radius
                branchRadius *= parentRadius;
            }

            Vector3 angle = GenerateVector(Vector3.up, layerNum == 0 ? 25f : 110f);

            TreeLogData[] children = new TreeLogData[0];
            if(layerNum < settings.Length) {
                children = new TreeLogData[childBranchCount];
                for(int i = 0; i < children.Length; i++) {
                    children[i] = GenerateData(branchRadius, layerNum+1, i);
                }
            }

            return new(
                branchLength,
                branchRadius,
                angle,
                siblingIndex,
                children
            );
        }

        TreeLogData rootData = GenerateData(-1, 0, 0);
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
public struct TreeBranchLayerSettings 
{
    public float timeToGrow;
    public Vector2 lengthRange;
    [Header("If root layer, values reflect actual units of length. If non-root layer, values reflect units of scale in terms of the parent branch.")]
    public Vector2 radiusRange;
    public Vector2 childCountRange;
}