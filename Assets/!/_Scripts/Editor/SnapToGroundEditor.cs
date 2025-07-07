using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SnapToGround))]
public class SnapToGroundEditor : Editor
{
    private SnapToGround snapTarget;
    private Vector3 lastPosition;

    void OnEnable()
    {
        snapTarget = (SnapToGround)target;
        lastPosition = snapTarget.transform.position;
        EditorApplication.update += UpdateSnap;
    }

    void OnDisable()
    {
        EditorApplication.update -= UpdateSnap;
    }

    void UpdateSnap()
    {
        if (snapTarget == null) return;

        Transform tf = snapTarget.transform;
        Vector3 currentPosition = tf.position;

        int layerMask = 1 << LayerMask.NameToLayer("Terrain");

        // Only snap if position has changed
        if (currentPosition != lastPosition)
        {
            if (Physics.Raycast(currentPosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, Mathf.Infinity, layerMask))
            {
                Vector3 newPosition = new Vector3(currentPosition.x, hit.point.y + snapTarget.heightOffset, currentPosition.z);
                tf.position = newPosition;
                lastPosition = newPosition;
            }
        }
    }
}
