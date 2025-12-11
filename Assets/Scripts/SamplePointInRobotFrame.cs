using UnityEngine;

// -------------------------------------------------------------
// SamplePointInRobotFrame (OVR version)
// - Uses Oculus OVRInput to detect trigger (no Input System)
// - Creates only ONE marker and moves it instead of spawning many
// - Enforces workspace limit (0.85 m from robot root)
// - If outside workspace: do NOT move marker or update value
// -------------------------------------------------------------

public class SamplePointInRobotFrame : MonoBehaviour
{
    [Header("Scene References")]
    public Transform robotRoot;              // Robot base frame
    public Transform controllerTransform;    // VR controller transform

    [Header("Point on Controller")]
    public Vector3 localPointOnController = Vector3.zero; // Offset on controller model

    [Header("Debug Input")]
    public KeyCode debugKey = KeyCode.T;     // Keyboard backup trigger

    [Header("Marker")]
    public GameObject markerPrefab;          // Prefab for marker (optional)
    public Vector3 markerScale = new Vector3(0.02f, 0.02f, 0.02f);
    public bool parentMarkerUnderRobot = true;

    [Header("Output")]
    public Vector3 lastSampleInRobot;        // Final sampled point in robot coordinates

    public bool enableDebugLog = true;

    private GameObject markerInstance = null;   // Single marker instance
    [Header("Safety")]
    public float workspaceLimit = 0.85f; // 85 cm workspace radius


    void Update()
    {
        if (robotRoot == null || controllerTransform == null)
            return;

        // ---------------------------
        // 1) Trigger Detection (OVR)
        // ---------------------------
        bool triggerPressed = false;

        // Right controller index trigger
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            triggerPressed = true;

        // Optional debug key
        if (Input.GetKeyDown(debugKey))
            triggerPressed = true;

        if (!triggerPressed)
            return;

        // -------------------------------------------
        // 2) Convert controller local point → world
        // -------------------------------------------
        Vector3 pointWorld = controllerTransform.TransformPoint(localPointOnController);

        // -------------------------------------------
        // 3) Convert world → robot local coordinates
        // -------------------------------------------
        Vector3 pointRobot = robotRoot.InverseTransformPoint(pointWorld);

        // -------------------------------
        // 4) Workspace boundary check
        // -------------------------------
        float distance = pointRobot.magnitude; // distance in meters from robot base

        if (distance > workspaceLimit)
        {
            Debug.LogWarning("[SamplePointInRobotFrame] Exceed working space: " 
                             + distance.ToString("F3") + " m");
            return; // Do NOT update marker or lastSampleInRobot
        }

        // Valid sample → update output variable
        lastSampleInRobot = pointRobot;

        if (enableDebugLog)
            Debug.Log($"[SamplePointInRobotFrame] Sampled point = {pointRobot:F4}");

        // -------------------------------
        // 5) Update (or create) single marker
        // -------------------------------
        UpdateMarker(pointWorld, pointRobot);
    }


    private void UpdateMarker(Vector3 pointWorld, Vector3 pointRobot)
    {
        // Create marker one time
        if (markerInstance == null)
        {
            if (markerPrefab != null)
                markerInstance = Instantiate(markerPrefab);
            else
            {
                markerInstance = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                // Remove collider to avoid physics interaction
                var col = markerInstance.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }

            markerInstance.transform.localScale = markerScale;

            if (parentMarkerUnderRobot && robotRoot != null)
                markerInstance.transform.SetParent(robotRoot, false);
        }

        // Update marker position
        if (parentMarkerUnderRobot)
        {
            markerInstance.transform.localPosition = pointRobot;
            markerInstance.transform.localRotation = Quaternion.identity;
        }
        else
        {
            markerInstance.transform.position = pointWorld;
            markerInstance.transform.rotation = Quaternion.identity;
        }
    }
}
