using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SetProjectorToShader : MonoBehaviour
{
    public Camera capture_camera;
    public Material cylinder_material;
    public GameObject rotor;
    public GameObject cylinder;

    private Slider slider_rpm;
    private Slider slider_sigma;
    private Slider slider_spreading;
    private Slider slider_texScale;
    private TextMeshProUGUI slider_rpm_value;
    private TextMeshProUGUI slider_sigma_value;
    private TextMeshProUGUI slider_spreading_value;
    private TextMeshProUGUI slider_texScale_value;

    private float ZoomAmount = 20;
    public RenderTexture targetColor;
    public RenderTexture targetDepth;

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
        capture_camera.orthographic = true;
        capture_camera.orthographicSize = 1.3f; // 1.5f

        int width = 256;
        targetColor = new(width, width, 0, RenderTextureFormat.ARGBFloat);
        targetColor.filterMode = FilterMode.Point;
        targetColor.antiAliasing = 1;
        targetDepth = new(width, width, 24, RenderTextureFormat.Depth);
        capture_camera.SetTargetBuffers(targetColor.colorBuffer, targetDepth.depthBuffer);
        capture_camera.depthTextureMode = DepthTextureMode.Depth;
        cylinder_material.SetTexture("_MainTex", targetColor);
        cylinder_material.SetTexture("_DepthTex", targetDepth);
        ///////////////////////////////////////////////////////////////////////////////////////
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
    public static void FollowAndFocusOn(Camera camera, GameObject focusedObject, float spacingfactor)
    {
        Bounds bounds = GetBoundsWithChildren(focusedObject); // Debug.Log(bounds.extents.magnitude);
        float aspectRatio = 1; // Screen.width / Screen.height;
        float distance = (camera.transform.position - focusedObject.transform.position).magnitude;
        //camera.fieldOfView = 2.0f * Mathf.Rad2Deg * Mathf.Atan((0.5f * bounds.extents.magnitude) / (distance * aspectRatio * spacingfactor));
        camera.transform.LookAt(focusedObject.transform);

        camera.nearClipPlane = distance - bounds.extents.magnitude / 1.4f;
        camera.farClipPlane = distance + bounds.extents.magnitude / 1.4f;
    }

    void Update()
    {
        slider_rpm_value.text = slider_rpm.value.ToString("0");
        slider_sigma_value.text = slider_sigma.value.ToString("0.00");
        slider_spreading_value.text = slider_spreading.value.ToString("0");
        slider_texScale_value.text = slider_texScale.value.ToString("0.00");


        // demonstration: move camera 
        ZoomAmount += Input.GetAxis("Mouse ScrollWheel") * 10;
        ZoomAmount = Mathf.Clamp(ZoomAmount, 5, 100);
        Camera.main.fieldOfView = ZoomAmount;
        // demonstration: rotate rotor slowly to shwo effect from different angles
        rotor.transform.Rotate(0, -slider_rpm.value * 6.0f * Time.deltaTime, 0, Space.Self);


        ///////////////////////////////////////////////////////////////////////////////////////
        // capture camera must follow rotor and change FOV
        FollowAndFocusOn(capture_camera, cylinder, 0.70f);
        // pass matrix and parameter to shader
        Matrix4x4 sampleMatrix = (GL.GetGPUProjectionMatrix(capture_camera.projectionMatrix, false) * capture_camera.worldToCameraMatrix * cylinder.transform.worldToLocalMatrix.inverse).transpose;

        sampleMatrix[3, 0] = 0.5f;
        sampleMatrix[3, 1] = 0.5f;
        sampleMatrix[3, 2] = 0.5f;
        sampleMatrix[0, 0] *= 0.5f;
        sampleMatrix[1, 0] *= 0.5f;
        sampleMatrix[2, 0] *= 0.5f;
        sampleMatrix[0, 1] *= 0.5f;
        sampleMatrix[1, 1] *= 0.5f;
        sampleMatrix[2, 1] *= 0.5f;
        sampleMatrix[0, 2] *= 0.5f;
        sampleMatrix[1, 2] *= 0.5f;
        sampleMatrix[2, 2] *= 0.5f;

        cylinder_material.SetMatrix("_ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld", sampleMatrix);
        cylinder_material.SetMatrix("_ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld_inverse", sampleMatrix.inverse);
        cylinder_material.SetFloat("_sigma", slider_sigma.value); //
        cylinder_material.SetFloat("_spreading", slider_spreading.value); //
        cylinder_material.SetFloat("_texScale", slider_texScale.value); //                                                                 
        ///////////////////////////////////////////////////////////////////////////////////////


        // Debug.Log( (GL.GetGPUProjectionMatrix(capture_camera.projectionMatrix, false) * capture_camera.worldToCameraMatrix * cylinder.transform.worldToLocalMatrix.inverse).transpose);
        // Debug.Log(sampleMatrix);
        // Debug.Log(cylinder.transform.worldToLocalMatrix.inverse);
        // Debug.Log(capture_camera.worldToCameraMatrix);
        // Debug.Log(capture_camera.projectionMatrix);
        // Debug.Log(GL.GetGPUProjectionMatrix(capture_camera.projectionMatrix, false));
        // Debug.Log(GL.GetGPUProjectionMatrix(capture_camera.projectionMatrix, true));
        // Debug.Log("------");

        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }
}
