using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class RotorBlurUnit : MonoBehaviour
{
    [Header("Ziele & Geometrie")]
    [Tooltip("Alle physischen 3D-Meshes dieser Rotor-Einheit (z.B. Hub oder alle Blätter eines Rotors inkl. Flapping)")]
    public GameObject sourceRoot;
    [Tooltip("Die Proxy-Hülle (z.B. default-Child von bound_hub oder bound_blade)")]
    public Renderer proxyRenderer;
    [Tooltip("Das Shader-Material (Custom/Bound_Hub_RotationBlurShader_URP)")]
    public Material blurMaterial;
    [Min(1)] public int bladeCount = 2;

    [Header("Capture-Einstellungen")]
    public int textureSize = 512;
    public bool controlClipping = true;
    public float clippingDivisor = 1.4f;
    public bool renderDepth = true;

    [Header("Blur-Parameter")]
    [Range(0f, 360f)] public float spreading = 40f;
    [Range(0.01f, 2f)] public float sigma = 0.35f;
    [Range(0.1f, 5f)] public float texScale = 1.0f;

    private static Camera sharedCaptureCam;
    private static readonly List<RotorBlurUnit> allUnits = new List<RotorBlurUnit>();
    private static int captureLayer = -1;

    private RenderTexture colorRT;
    private RenderTexture depthRT;
    private Material runtimeMaterial;
    private readonly List<Renderer> sourceRenderers = new List<Renderer>();

    void OnEnable()
    {
        if (sourceRoot == null || proxyRenderer == null || blurMaterial == null)
        {
            Debug.LogError($"[RotorBlurUnit] Fehlende Referenz auf {gameObject.name}!", this);
            return;
        }

        if (captureLayer == -1)
            captureLayer = LayerMask.NameToLayer("RotorCapture");

        if (captureLayer == -1)
        {
            Debug.LogError("[RotorBlurUnit] Layer 'RotorCapture' fehlt in den Project Settings.", this);
            return;
        }

        SetLayerRecursive(sourceRoot, captureLayer);

        sourceRenderers.Clear();
        sourceRoot.GetComponentsInChildren(true, sourceRenderers);
        SetRenderersVisible(false);

        colorRT = new RenderTexture(textureSize, textureSize, 24, RenderTextureFormat.ARGBHalf)
        {
            name = gameObject.name + "_ColorRT",
            filterMode = FilterMode.Bilinear,
            antiAliasing = 1
        };
        colorRT.Create();

        depthRT = new RenderTexture(textureSize, textureSize, 24, RenderTextureFormat.Depth)
        {
            name = gameObject.name + "_DepthRT",
            filterMode = FilterMode.Point,
            antiAliasing = 1
        };
        depthRT.Create();

        runtimeMaterial = new Material(blurMaterial);
        runtimeMaterial.name = blurMaterial.name + "_" + gameObject.name;
        proxyRenderer.sharedMaterial = runtimeMaterial;
        runtimeMaterial.SetTexture("_MainTex", colorRT);
        runtimeMaterial.SetTexture("_DepthTex", depthRT);

        EnsureSharedCamera();
        allUnits.Add(this);
    }

    void OnDisable()
    {
        allUnits.Remove(this);
        SetRenderersVisible(true);

        if (colorRT != null) { colorRT.Release(); Destroy(colorRT); }
        if (depthRT != null) { depthRT.Release(); Destroy(depthRT); }
        if (runtimeMaterial != null) Destroy(runtimeMaterial);

        if (allUnits.Count == 0 && sharedCaptureCam != null)
        {
            Destroy(sharedCaptureCam.gameObject);
            sharedCaptureCam = null;
        }
    }

    private static void EnsureSharedCamera()
    {
        if (sharedCaptureCam != null) return;

        var camGO = new GameObject("Shared_RotorCaptureCamera");
        camGO.hideFlags = HideFlags.HideAndDontSave;

        sharedCaptureCam = camGO.AddComponent<Camera>();
        sharedCaptureCam.enabled = false;
        sharedCaptureCam.clearFlags = CameraClearFlags.SolidColor;
        sharedCaptureCam.backgroundColor = new Color(0, 0, 0, 0);
        sharedCaptureCam.useOcclusionCulling = false;
        sharedCaptureCam.cullingMask = 1 << captureLayer;

        var urpData = camGO.AddComponent<UniversalAdditionalCameraData>();
        urpData.renderType = CameraRenderType.Base;
        urpData.renderPostProcessing = false;
        urpData.renderShadows = false;
        urpData.antialiasing = AntialiasingMode.None;
    }

    void LateUpdate()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null || sharedCaptureCam == null || proxyRenderer == null || runtimeMaterial == null) return;

        // 1. Use the main camera's viewpoint so projection follows its position and rotation.
        sharedCaptureCam.CopyFrom(mainCam);
        sharedCaptureCam.enabled = false;
        sharedCaptureCam.targetTexture = null;
        sharedCaptureCam.cullingMask = 1 << captureLayer;
        sharedCaptureCam.clearFlags = CameraClearFlags.SolidColor;
        sharedCaptureCam.backgroundColor = new Color(0, 0, 0, 0);
        sharedCaptureCam.useOcclusionCulling = false;
        sharedCaptureCam.rect = new Rect(0f, 0f, 1f, 1f);
        sharedCaptureCam.aspect = mainCam.aspect;
        sharedCaptureCam.transform.SetPositionAndRotation(
            mainCam.transform.position,
            mainCam.transform.rotation
        );

        Bounds captureBounds = proxyRenderer.bounds;
        for (int i = 0; i < sourceRenderers.Count; i++)
        {
            if (sourceRenderers[i] != null)
                captureBounds.Encapsulate(sourceRenderers[i].bounds);
        }

        Vector3 extents = captureBounds.extents;
        Transform captureTransform = sharedCaptureCam.transform;
        float halfDepth =
            Mathf.Abs(captureTransform.forward.x) * extents.x +
            Mathf.Abs(captureTransform.forward.y) * extents.y +
            Mathf.Abs(captureTransform.forward.z) * extents.z;

        float radius = Mathf.Max(extents.magnitude, 0.01f);
        float distance = captureTransform.InverseTransformPoint(captureBounds.center).z;

        if (controlClipping)
        {
            float clipRadius = Mathf.Max(
                halfDepth * 1.05f,
                radius / Mathf.Max(clippingDivisor, 0.01f)
            );
            sharedCaptureCam.nearClipPlane = Mathf.Max(0.01f, distance - clipRadius);
            sharedCaptureCam.farClipPlane = Mathf.Max(
                sharedCaptureCam.nearClipPlane + 0.01f,
                distance + clipRadius
            );
        }
        else
        {
            sharedCaptureCam.nearClipPlane = 0.01f;
            sharedCaptureCam.farClipPlane = 1000f;
        }

        // 2. Nur diesen Rotor aktivieren und in Color + Depth RTs rendern
        SetRenderersVisible(true);

        sharedCaptureCam.targetTexture = colorRT;
        sharedCaptureCam.Render();

        if (renderDepth)
        {
            sharedCaptureCam.targetTexture = depthRT;
            sharedCaptureCam.Render();
        }

        Matrix4x4 captureProjectionMatrix = sharedCaptureCam.projectionMatrix;
        sharedCaptureCam.targetTexture = null;
        SetRenderersVisible(false);

        // 3. Reprojektions-Matrix berechnen
        Matrix4x4 sampleMatrix = (
            GL.GetGPUProjectionMatrix(captureProjectionMatrix, false) *
            sharedCaptureCam.worldToCameraMatrix *
            proxyRenderer.transform.localToWorldMatrix
        ).transpose;

        for (int row = 0; row < 4; row++)
        {
            sampleMatrix[row, 0] = 0.5f * (sampleMatrix[row, 0] + sampleMatrix[row, 3]);
            sampleMatrix[row, 1] = 0.5f * (sampleMatrix[row, 1] + sampleMatrix[row, 3]);
        }

        runtimeMaterial.SetMatrix("_ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld", sampleMatrix);
        runtimeMaterial.SetMatrix("_ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld_inverse", sampleMatrix.inverse);
        runtimeMaterial.SetFloat("_spreading", spreading);
        runtimeMaterial.SetFloat("_sigma", sigma);
        runtimeMaterial.SetFloat("_texScale", texScale);
    }

    private void SetRenderersVisible(bool visible)
    {
        for (int i = 0; i < sourceRenderers.Count; i++)
        {
            if (sourceRenderers[i] != null)
                sourceRenderers[i].forceRenderingOff = !visible;
        }
    }

    private static void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
