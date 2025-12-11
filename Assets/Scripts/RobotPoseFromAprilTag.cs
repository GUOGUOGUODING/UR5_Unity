using UnityEngine;

public class RobotPoseFromAprilTag : MonoBehaviour
{
    [Header("Scene references")]
    public Camera hmdCamera;
    public Transform robotRoot;
    public AprilTagDetectorWrapper tagDetector;

    [Header("Marker pose relative to robot base (T_R_M)")]
    public Vector3 markerPosInRobot = new Vector3(0.15f, 0.0f, 0.0f);
    public Quaternion markerRotInRobot = Quaternion.identity;

    [Header("Registration Fine-tuning")]
    [Tooltip("Extra rotation (in degrees) applied in robot-base frame")]
    public Vector3 extraRobotRotationEuler = Vector3.zero;

    [Header("Smoothing")]
    public bool enableSmoothing = true;

    [Range(0.0f, 1.0f)]
    public float positionLerp = 0.2f;

    [Range(0.0f, 1.0f)]
    public float rotationLerp = 0.2f;

    [Header("Behavior when tag is lost")]
    [Tooltip("If true, keep the last valid pose when the tag is not detected.")]
    public bool holdLastPoseWhenLost = true;

    [Header("Debug")]
    public bool enableDebugLog = true;
    public int RobotError_code = 0;

    // Disable this for normal operation; only used for quick testing
    public bool followHeadTest = false;

    private bool hasValidPose;
    private Vector3 smoothedPos;
    private Quaternion smoothedRot;

    void Start()
    {
        // Validate scene references
        if (hmdCamera == null)
        {
            Debug.LogError("RobotPoseFromAprilTag: HMD camera is not assigned.");
            enabled = false;
            RobotError_code = 1;
            return;
        }

        if (robotRoot == null)
        {
            Debug.LogError("RobotPoseFromAprilTag: robotRoot is not assigned.");
            enabled = false;
            RobotError_code = 2;
            return;
        }

        if (tagDetector == null)
        {
            Debug.LogError("RobotPoseFromAprilTag: tagDetector is not assigned.");
            enabled = false;
            RobotError_code = 3;
            return;
        }

        // Initialize smoothing buffers with current robot pose
        smoothedPos = robotRoot.position;
        smoothedRot = robotRoot.rotation;
        hasValidPose = true;
    }

    void LateUpdate()
    {
        // === Case 1: Tag not detected ===
        if (!tagDetector.tagFound)
        {
            // If enabled, keep the last known valid robot pose
            // This prevents the robot from snapping back to the initial pose
            if (holdLastPoseWhenLost && hasValidPose)
            {
                ApplyRobotPose(smoothedPos, smoothedRot);
            }

            if (enableDebugLog)
            {
                Debug.Log("[RobotPose] Tag lost, holding last known pose.");
            }

            return;
        }

        // === Case 2: Test mode �� robot follows the HMD transform ===
        if (followHeadTest)
        {
            Vector3 pos = hmdCamera.transform.position + hmdCamera.transform.forward * 1.0f;
            Quaternion rot = Quaternion.LookRotation(hmdCamera.transform.forward, Vector3.up);
            ApplyRobotPose(pos, rot);
            return;
        }

        // === Case 3: Normal AprilTag �� Robot base alignment ===

        // 1. Build T_C_M: tag pose in camera coordinates
        Matrix4x4 T_C_M = Matrix4x4.TRS(
            tagDetector.markerPosCam,
            tagDetector.markerRotCam,
            Vector3.one
        );

        // 2. T_U_C: camera pose in world coordinates
        Matrix4x4 T_U_C = hmdCamera.transform.localToWorldMatrix;

        // 3. T_U_M: tag pose in world coordinates
        Matrix4x4 T_U_M = T_U_C * T_C_M;

        // 4. T_R_M: robot-base �� marker transform (known calibration)
        Matrix4x4 T_R_M = Matrix4x4.TRS(
            markerPosInRobot,
            markerRotInRobot,
            Vector3.one
        );

        // Compute marker �� robot transform
        Matrix4x4 T_M_R = T_R_M.inverse;

        // 5. T_U_R: world �� robot-base transform
        Matrix4x4 T_U_R = T_U_M * T_M_R;

        Vector3 targetPos = T_U_R.GetColumn(3);

        // Extra correction in robot-base frame
        Quaternion extraRot = Quaternion.Euler(extraRobotRotationEuler);

        Quaternion targetRot = T_U_R.rotation * extraRot;

        // 6. Apply smoothing if enabled
        if (!enableSmoothing || !hasValidPose)
        {
            smoothedPos = targetPos;
            smoothedRot = targetRot;
            hasValidPose = true;
        }
        else
        {
            smoothedPos = Vector3.Lerp(smoothedPos, targetPos, positionLerp);
            smoothedRot = Quaternion.Slerp(smoothedRot, targetRot, rotationLerp);
        }

        ApplyRobotPose(smoothedPos, smoothedRot);

        if (enableDebugLog)
        {
            Debug.Log(
                $"[RobotPose] tagFound=true robotPos={smoothedPos:F3} " +
                $"rotEuler={smoothedRot.eulerAngles:F1}"
            );
        }
    }

    /// <summary>
    /// Applies the given world pose to the robot root.
    /// Uses TeleportRoot if an ArticulationBody is present.
    /// </summary>
    private void ApplyRobotPose(Vector3 pos, Quaternion rot)
    {
        var ab = robotRoot.GetComponent<ArticulationBody>();
        if (ab != null)
        {
            // TeleportRoot is required for ArticulationBody hierarchy movement
            ab.TeleportRoot(pos, rot);
        }
        else
        {
            robotRoot.SetPositionAndRotation(pos, rot);
        }
    }
}
