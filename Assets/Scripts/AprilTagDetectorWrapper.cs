using UnityEngine;
using AprilTag;  // jp.keijiro.apriltag

public class AprilTagDetectorWrapper : MonoBehaviour
{
    [Header("Camera image input")]
    [Tooltip("Texture2D updated every frame with camera image (set by PcaCameraFeed).")]
    public Texture2D cameraTexture;

    [Header("Tag / camera parameters")]
    public float horizontalFov = 60f;
    public float tagSize = 0.12f;
    public int targetTagId = 0;
    public float decimation = 2.0f;

    [Header("Output: pose of tag in camera frame (T_C_M)")]
    public bool tagFound;
    public Vector3 markerPosCam;
    public Quaternion markerRotCam;

    [Header("Debug")]
    public bool enableDebugLog = true;

    [Header("Error code")]
    // 0 = OK
    // 1 = cameraTexture is null
    // 2 = detector not initialized
    // 3 = pixelBuffer not allocated
    // 4 = detector init failed (exception)
    // 5 = invalid texture size (0x0)
    public int April_error_code = 0;

    private TagDetector detector;
    private Color32[] pixelBuffer;
    private int texWidth;
    private int texHeight;

    void Update()
    {
        // No texture yet -> just wait
        if (cameraTexture == null)
        {
            tagFound = false;
            April_error_code = 1; // cameraTexture null
            return;
        }

        // If detector not created yet, or texture size changed -> (re)initialize
        if (detector == null ||
            cameraTexture.width != texWidth ||
            cameraTexture.height != texHeight)
        {
            InitDetector();
        }

        if (detector == null)
        {
            tagFound = false;
            April_error_code = 2; // detector not initialized
            return;
        }

        if (pixelBuffer == null)
        {
            tagFound = false;
            April_error_code = 3; // pixelBuffer not allocated
            return;
        }

        // Everything OK
        April_error_code = 0;

        // Copy pixels from Texture2D to CPU buffer
        //cameraTexture.GetPixels32(pixelBuffer);
        pixelBuffer = cameraTexture.GetPixels32();


        // Run AprilTag detection
        detector.ProcessImage(pixelBuffer, horizontalFov, tagSize);

        tagFound = false;

        foreach (var tag in detector.DetectedTags)
        {
            if (tag.ID == targetTagId)
            {
                tagFound = true;

                markerPosCam = tag.Position;
                markerRotCam = tag.Rotation;

                if (enableDebugLog)
                {
                    Debug.Log(
                        $"[AprilTag] ID={tag.ID} " +
                        $"pos_C_M={markerPosCam:F3} " +
                        $"rot_C_M_euler={markerRotCam.eulerAngles:F1}"
                    );
                }

                break;
            }
        }
    }

    private void InitDetector()
    {
        // Clean up old detector
        if (detector != null)
        {
            detector.Dispose();
            detector = null;
        }

        if (cameraTexture == null)
        {
            April_error_code = 1;
            return;
        }

        texWidth = cameraTexture.width;
        texHeight = cameraTexture.height;

        if (texWidth <= 0 || texHeight <= 0)
        {
            April_error_code = 5; // invalid size
            if (enableDebugLog)
            {
                Debug.LogWarning($"[AprilTag] Invalid texture size {texWidth}x{texHeight}");
            }
            return;
        }

        pixelBuffer = new Color32[texWidth * texHeight];

        try
        {
            detector = new TagDetector(texWidth, texHeight, (int)decimation);

            if (enableDebugLog)
            {
                Debug.Log($"[AprilTag] Init detector with {texWidth}x{texHeight}, decimation={decimation}");
            }
        }
        catch (System.Exception e)
        {
            detector = null;
            April_error_code = 4; // init failed
            Debug.LogError($"[AprilTag] Failed to create TagDetector: {e.GetType().Name} - {e.Message}");
        }
    }

    void OnDestroy()
    {
        if (detector != null)
        {
            detector.Dispose();
            detector = null;
        }
    }
}
