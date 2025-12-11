using Meta.XR;
using Meta.XR.MRUtilityKit; // Namespace for PassthroughCameraAccess (MRUK)
using UnityEngine;

public class PcaCameraFeed : MonoBehaviour
{
    [Header("PCA component")]
    public PassthroughCameraAccess pca;          // MRUK component
    [Header("AprilTag detector target")]
    public AprilTagDetectorWrapper tagDetector;  // Your existing AprilTag wrapper

    [Header("Downscale factor (1 = full res, 2 = half, etc.)")]
    public int downscale = 2;

    [Header("Debug")]
    public bool enableDebugLog = true;    
    private bool _loggedResolution = false;
    public int Pca_error_code = 0;
    // GPU side
    private RenderTexture _rt;

    // CPU side
    private Texture2D _cpuTexture;

    void Start()
    {
        if (pca == null)
        {
            pca = GetComponent<PassthroughCameraAccess>();
        }

        if (pca == null)
        {
            Debug.LogError("PcaCameraFeed: PassthroughCameraAccess is not assigned.");
            enabled = false;
            Pca_error_code = 1;
            return;
        }

        if (tagDetector == null)
        {
            Debug.LogError("PcaCameraFeed: AprilTagDetectorWrapper is not assigned.");
            enabled = false;
            Pca_error_code = 2;
            return;
        }
    }

    void Update()
    {
        // Get the current camera texture from PCA
        Texture pcaTexture = pca.GetTexture();
        if (pcaTexture == null)
        {
            Pca_error_code = 3;
            // This usually means permission not granted yet or PCA not initialized
            if (enableDebugLog)
            {
                Debug.Log("[PCA] pcaTexture is null (no permission or not ready yet).");
            }
            return;
        }

        int srcWidth = pcaTexture.width;
        int srcHeight = pcaTexture.height;

        int width = Mathf.Max(1, srcWidth / downscale);
        int height = Mathf.Max(1, srcHeight / downscale);

        if (!_loggedResolution && enableDebugLog)
        {
            Debug.Log($"[PCA] Raw={srcWidth}x{srcHeight}, Downscaled={width}x{height}");
            _loggedResolution = true;
        }

        // Lazy init RenderTexture and CPU Texture2D if null or size changed
        if (_rt == null || _rt.width != width || _rt.height != height)
        {
            if (_rt != null) _rt.Release();

            _rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            _rt.Create();

            _cpuTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            // Update AprilTag detector input texture
            tagDetector.cameraTexture = _cpuTexture;
        }

        // Copy PCA texture to our RT (GPU ¡ú GPU)
        Graphics.Blit(pcaTexture, _rt);

        // Read back to CPU Texture2D
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = _rt;
        _cpuTexture.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
        _cpuTexture.Apply();
        RenderTexture.active = prev;

        // Now tagDetector.cameraTexture already points to _cpuTexture,
        // and its Update() will call detector.ProcessImage() using this data.
    }

    void OnDestroy()
    {
        if (_rt != null)
        {
            _rt.Release();
            _rt = null;
        }
    }
}
