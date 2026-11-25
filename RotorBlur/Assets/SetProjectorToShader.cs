using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class SetProjectorToShader : MonoBehaviour
{
    private Camera hub_capture_camera;
    private Camera hub_depth_camera;
    private Camera blade1_depth_camera;
    private Camera blade2_depth_camera;
    
    private Material bound_hub_material;
    private GameObject hub;
    private GameObject bound_hub;

    private Camera blade1_capture_camera;
    private Camera blade2_capture_camera;
    private Material bound_blade1_material;
    private Material bound_blade2_material;
    private GameObject blade1;
    private GameObject blade2;
    private GameObject bound_blade1;
    private GameObject bound_blade2;

    private RenderTexture hub_targetColor;
    private RenderTexture hub_targetDepth;
    private RenderTexture blade1_targetColor;
    private RenderTexture blade1_targetDepth;
    private RenderTexture blade2_targetColor;
    private RenderTexture blade2_targetDepth;

    private Matrix4x4 hub_bladePos;
    private Matrix4x4 blade1_bladePos;
    private Matrix4x4 blade2_bladePos;

    private Slider slider_rpm;
    private Slider slider_sigma;
    private Slider slider_spreading;
    private Slider slider_texScale;
    private TextMeshProUGUI slider_rpm_value;
    private TextMeshProUGUI slider_sigma_value;
    private TextMeshProUGUI slider_spreading_value;
    private TextMeshProUGUI slider_texScale_value;

    private float ZoomAmount = 20;

    void Awake()
    {
        slider_rpm = GameObject.Find("Slider_rpm").GetComponent<Slider>();
        slider_sigma = GameObject.Find("Slider_sigma").GetComponent<Slider>();
        slider_spreading = GameObject.Find("Slider_spreading").GetComponent<Slider>();
        slider_texScale = GameObject.Find("Slider_texScale").GetComponent<Slider>();
        slider_rpm_value = GameObject.Find("Text_rpm (1)").GetComponent<TextMeshProUGUI>();
        slider_sigma_value = GameObject.Find("Text_sigma (1)").GetComponent<TextMeshProUGUI>();
        slider_spreading_value = GameObject.Find("Text_spreading (1)").GetComponent<TextMeshProUGUI>();
        slider_texScale_value = GameObject.Find("Text_texScale (1)").GetComponent<TextMeshProUGUI>();

        ///////////////////////////////////////////////////////////////////////////////////////
        /// hub
        ///////////////////////////////////////////////////////////////////////////////////////
        hub = GameObject.Find("hub").transform.GetChild(0).gameObject;
        bound_hub = GameObject.Find("bound_hub").transform.GetChild(0).gameObject;
        bound_hub.GetComponent<Renderer>().material = Resources.Load<Material>("Bound_Hub");
        bound_hub_material = bound_hub.GetComponent<Renderer>().materials[0];

        int hubWidth = 256;
        hub_targetColor = new RenderTexture(hubWidth, hubWidth, 24, RenderTextureFormat.ARGBFloat);
        hub_targetColor.filterMode = FilterMode.Bilinear; // Trilinear/Bilinear wie im DCS Original
        hub_targetColor.antiAliasing = 1;
        hub_targetColor.Create();

        hub_targetDepth = new RenderTexture(hubWidth, hubWidth, 24, RenderTextureFormat.Depth);
        hub_targetDepth.filterMode = FilterMode.Point;
        hub_targetDepth.antiAliasing = 1;
        hub_targetDepth.Create();

        // Color Capture Camera
        hub_capture_camera = CreateCaptureCamera("hub_capture_camera", -2, 1.2f, "Hub", hub_targetColor);

        // Depth Capture Camera (URP-kompatibler Ersatz für SetTargetBuffers)
        hub_depth_camera = CreateCaptureCamera("hub_depth_camera", -3, 1.2f, "Hub", hub_targetDepth);

        bound_hub_material.SetTexture("_MainTex", hub_targetColor);
        bound_hub_material.SetTexture("_DepthTex", hub_targetDepth);
        hub_bladePos = Matrix4x4.identity;

        ///////////////////////////////////////////////////////////////////////////////////////
        /// blade1
        ///////////////////////////////////////////////////////////////////////////////////////
        blade1 = GameObject.Find("blade1").transform.GetChild(0).gameObject;
        bound_blade1 = GameObject.Find("bound_blade1").transform.GetChild(0).gameObject;
        bound_blade1.GetComponent<Renderer>().material = Resources.Load<Material>("Bound_Blade");
        bound_blade1_material = bound_blade1.GetComponent<Renderer>().materials[0];

        int bladeWidth = 256 * 10;
        blade1_targetColor = new RenderTexture(bladeWidth, bladeWidth, 24, RenderTextureFormat.ARGBFloat);
        blade1_targetColor.filterMode = FilterMode.Bilinear;
        blade1_targetColor.antiAliasing = 1;
        blade1_targetColor.Create();

        blade1_targetDepth = new RenderTexture(bladeWidth, bladeWidth, 24, RenderTextureFormat.Depth);
        blade1_targetDepth.filterMode = FilterMode.Point;
        blade1_targetDepth.antiAliasing = 1;
        blade1_targetDepth.Create();

        // Color Capture Camera
        blade1_capture_camera = CreateCaptureCamera("blade1_capture_camera", -2, 7.0f, "Blade1", blade1_targetColor);

        // Depth Capture Camera (URP-kompatibler Ersatz für SetTargetBuffers)
        blade1_depth_camera = CreateCaptureCamera("blade1_depth_camera", -2, 7.0f, "Blade1", blade1_targetDepth);
        
        bound_blade1_material.SetTexture("_MainTex", blade1_targetColor);
        bound_blade1_material.SetTexture("_DepthTex", blade1_targetDepth);
        blade1_bladePos = Matrix4x4.identity;

        ///////////////////////////////////////////////////////////////////////////////////////
        /// blade2
        ///////////////////////////////////////////////////////////////////////////////////////
        blade2 = GameObject.Find("blade2").transform.GetChild(0).gameObject;
        bound_blade2 = GameObject.Find("bound_blade2").transform.GetChild(0).gameObject;
        bound_blade2.GetComponent<Renderer>().material = Resources.Load<Material>("Bound_Blade");
        bound_blade2_material = bound_blade2.GetComponent<Renderer>().materials[0];

        //int bladeWidth = 256 * 10;
        blade2_targetColor = new RenderTexture(bladeWidth, bladeWidth, 24, RenderTextureFormat.ARGBFloat);
        blade2_targetColor.filterMode = FilterMode.Bilinear;
        blade2_targetColor.antiAliasing = 1;
        blade2_targetColor.Create();

        blade2_targetDepth = new RenderTexture(bladeWidth, bladeWidth, 24, RenderTextureFormat.Depth);
        blade2_targetDepth.filterMode = FilterMode.Point;
        blade2_targetDepth.antiAliasing = 1;
        blade2_targetDepth.Create();

        // Color Capture Camera
        blade2_capture_camera = CreateCaptureCamera("blade2_capture_camera", -2, 7.0f, "Blade2", blade2_targetColor);

        // Depth Capture Camera (URP-kompatibler Ersatz für SetTargetBuffers)
        blade2_depth_camera = CreateCaptureCamera("blade2_depth_camera", -2, 7f, "Blade2", blade2_targetDepth);
        
        bound_blade2_material.SetTexture("_MainTex", blade2_targetColor);
        bound_blade2_material.SetTexture("_DepthTex", blade2_targetDepth);
        blade2_bladePos = Matrix4x4.identity;
    }

    private Camera CreateCaptureCamera(string camName, float depth, float orthoSize, string layerName, RenderTexture targetRT)
    {
        var camGO = new GameObject(camName);
        camGO.transform.parent = Camera.main.transform;
        camGO.transform.localPosition = Vector3.zero;

        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.depth = depth;
        cam.orthographic = true;
        cam.orthographicSize = orthoSize;
        cam.useOcclusionCulling = true;
        cam.cullingMask = LayerMask.GetMask(layerName);
        cam.targetTexture = targetRT;

        // URP-spezifische Kamera-Konfiguration
        var urpCamData = camGO.AddComponent<UniversalAdditionalCameraData>();
        urpCamData.renderType = CameraRenderType.Base;
        urpCamData.renderPostProcessing = false;
        urpCamData.renderShadows = false;
        urpCamData.antialiasing = AntialiasingMode.None;
        urpCamData.requiresColorOption = CameraOverrideOption.Off;
        urpCamData.requiresDepthOption = CameraOverrideOption.Off;

        // Layer für das Objekt sicherstellen
        if (layerName == "Hub" && hub != null) hub.layer = LayerMask.NameToLayer(layerName);
        if (layerName == "Blade1" && blade1 != null) blade1.layer = LayerMask.NameToLayer(layerName);
        if (layerName == "Blade2" && blade2 != null) blade2.layer = LayerMask.NameToLayer(layerName);

        return cam;
    }

    public static Bounds GetBoundsWithChildren(GameObject gameObject)
    {
        Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers.Length > 0 ? renderers.FirstOrDefault().bounds : new Bounds();

        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i].enabled)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }
        return bounds;
    }

    public static void FollowAndFocusOn(Camera camera, GameObject focusedObject, float spacingfactor, bool control_clipping)
    {
        Bounds bounds = GetBoundsWithChildren(focusedObject);
        float distance = (camera.transform.position - focusedObject.transform.position).magnitude;

        camera.transform.LookAt(focusedObject.transform);
        if (control_clipping)
        {
            camera.nearClipPlane = Mathf.Max(0.01f, distance - bounds.extents.magnitude / 1.4f);
            camera.farClipPlane = distance + bounds.extents.magnitude / 1.4f;
        }
    }

    void LateUpdate()
    {
        slider_rpm_value.text = slider_rpm.value.ToString("0");
        slider_sigma_value.text = slider_sigma.value.ToString("0.00");
        slider_spreading_value.text = slider_spreading.value.ToString("0");
        slider_texScale_value.text = slider_texScale.value.ToString("0.00");

        ZoomAmount += Input.GetAxis("Mouse ScrollWheel") * 10;
        ZoomAmount = Mathf.Clamp(ZoomAmount, 5, 100);
        Camera.main.fieldOfView = ZoomAmount;

        hub.transform.Rotate(0, -slider_rpm.value * 6.0f * Time.deltaTime, 0, Space.Self);
        blade1.transform.Rotate(0, -slider_rpm.value * 6.0f * Time.deltaTime, 0, Space.Self);
        blade2.transform.Rotate(0, -slider_rpm.value * 6.0f * Time.deltaTime, 0, Space.Self);
        bound_blade1.transform.Rotate(0, -slider_rpm.value * 6.0f * Time.deltaTime, 0, Space.Self);
        bound_blade2.transform.Rotate(0, -slider_rpm.value * 6.0f * Time.deltaTime, 0, Space.Self);

        ///////////////////////////////////////////////////////////////////////////////////////
        /// hub
        ///////////////////////////////////////////////////////////////////////////////////////
        FollowAndFocusOn(hub_capture_camera, bound_hub, 0.70f, true);
        FollowAndFocusOn(hub_depth_camera, bound_hub, 0.70f, true);

        // WICHTIG FÜR URP: renderIntoTexture = true bei GL.GetGPUProjectionMatrix!
        Matrix4x4 bound_hub_sampleMatrix = (
            GL.GetGPUProjectionMatrix(hub_capture_camera.projectionMatrix, false) *
            hub_capture_camera.worldToCameraMatrix *
            bound_hub.transform.localToWorldMatrix *
            hub_bladePos
        ).transpose;

        float bound_hub_camera_size = 1f;

        bound_hub_sampleMatrix[3, 0] = 0.5f;
        bound_hub_sampleMatrix[3, 1] = 0.5f;
        bound_hub_sampleMatrix[0, 0] *= 0.5f / bound_hub_camera_size;
        bound_hub_sampleMatrix[2, 0] *= 0.5f / bound_hub_camera_size;
        bound_hub_sampleMatrix[0, 1] *= 0.5f / bound_hub_camera_size;
        bound_hub_sampleMatrix[1, 1] *= 0.5f / bound_hub_camera_size;
        bound_hub_sampleMatrix[2, 1] *= 0.5f / bound_hub_camera_size;

        bound_hub_material.SetMatrix("_ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld", bound_hub_sampleMatrix);
        bound_hub_material.SetMatrix("_ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld_inverse", bound_hub_sampleMatrix.inverse);
        bound_hub_material.SetFloat("_sigma", slider_sigma.value);
        bound_hub_material.SetFloat("_spreading", slider_spreading.value);
        bound_hub_material.SetFloat("_texScale", slider_texScale.value);

        ///////////////////////////////////////////////////////////////////////////////////////
        /// blade1
        ///////////////////////////////////////////////////////////////////////////////////////
        FollowAndFocusOn(blade1_capture_camera, bound_blade1, 0.70f, true);
        FollowAndFocusOn(blade1_depth_camera, bound_blade1, 0.70f, true);

        Matrix4x4 bound_blade1_sampleMatrix = (
            GL.GetGPUProjectionMatrix(blade1_capture_camera.projectionMatrix, false) * blade1_capture_camera.worldToCameraMatrix *
            bound_blade1.transform.localToWorldMatrix *
            blade1_bladePos
        ).transpose;

        float bound_blade1_camera_size = 1f; //blade_capture_camera.orthographicSize;

        bound_blade1_sampleMatrix[3, 0] = 0.5f;
        bound_blade1_sampleMatrix[3, 1] = 0.5f;
        bound_blade1_sampleMatrix[0, 0] *= 0.5f / bound_blade1_camera_size;
        bound_blade1_sampleMatrix[2, 0] *= 0.5f / bound_blade1_camera_size;
        bound_blade1_sampleMatrix[0, 1] *= 0.5f / bound_blade1_camera_size;
        bound_blade1_sampleMatrix[1, 1] *= 0.5f / bound_blade1_camera_size;
        bound_blade1_sampleMatrix[2, 1] *= 0.5f / bound_blade1_camera_size;

        bound_blade1_material.SetMatrix("_ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld", bound_blade1_sampleMatrix);
        bound_blade1_material.SetMatrix("_ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld_inverse", bound_blade1_sampleMatrix.inverse);
        bound_blade1_material.SetFloat("_sigma", slider_sigma.value);
        bound_blade1_material.SetFloat("_spreading", slider_spreading.value);
        bound_blade1_material.SetFloat("_texScale", slider_texScale.value); 

        ///////////////////////////////////////////////////////////////////////////////////////
        /// blade2
        ///////////////////////////////////////////////////////////////////////////////////////
        FollowAndFocusOn(blade2_capture_camera, bound_blade2, 0.70f, true);
        FollowAndFocusOn(blade2_depth_camera, bound_blade2, 0.70f, true);

        Matrix4x4 bound_blade2_sampleMatrix = (
            GL.GetGPUProjectionMatrix(blade2_capture_camera.projectionMatrix, false) * blade2_capture_camera.worldToCameraMatrix *
            bound_blade2.transform.localToWorldMatrix *
            blade2_bladePos
        ).transpose;

        float bound_blade2_camera_size = 1f; //blade2_capture_camera.orthographicSize;

        bound_blade2_sampleMatrix[3, 0] = 0.5f;
        bound_blade2_sampleMatrix[3, 1] = 0.5f;
        bound_blade2_sampleMatrix[0, 0] *= 0.5f / bound_blade2_camera_size;
        bound_blade2_sampleMatrix[2, 0] *= 0.5f / bound_blade2_camera_size;
        bound_blade2_sampleMatrix[0, 1] *= 0.5f / bound_blade2_camera_size;
        bound_blade2_sampleMatrix[1, 1] *= 0.5f / bound_blade2_camera_size;
        bound_blade2_sampleMatrix[2, 1] *= 0.5f / bound_blade2_camera_size;

        bound_blade2_material.SetMatrix("_ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld", bound_blade2_sampleMatrix);
        bound_blade2_material.SetMatrix("_ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld_inverse", bound_blade2_sampleMatrix.inverse);
        bound_blade2_material.SetFloat("_sigma", slider_sigma.value);
        bound_blade2_material.SetFloat("_spreading", slider_spreading.value);
        bound_blade2_material.SetFloat("_texScale", slider_texScale.value); 



        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
    void OnDestroy()
    {
        if (hub_targetColor != null) hub_targetColor.Release();
        if (hub_targetDepth != null) hub_targetDepth.Release();
        if (blade1_targetColor != null) blade1_targetColor.Release();
        if (blade1_targetDepth != null) blade1_targetDepth.Release();
        if (blade2_targetColor != null) blade2_targetColor.Release();
        if (blade2_targetDepth != null) blade2_targetDepth.Release();
    }
}
