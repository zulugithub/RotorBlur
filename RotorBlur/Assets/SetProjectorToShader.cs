using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SetProjectorToShader : MonoBehaviour
{
    public Camera hub_capture_camera;
    public Material bound_hub_material;
    public GameObject hub;
    public GameObject bound_hub;

    public Camera blade_capture_camera;
    public Material bound_blade_material;
    public GameObject blade;
    public GameObject bound_blade;




    private Slider slider_rpm;
    private Slider slider_sigma;
    private Slider slider_spreading;
    private Slider slider_texScale;
    private TextMeshProUGUI slider_rpm_value;
    private TextMeshProUGUI slider_sigma_value;
    private TextMeshProUGUI slider_spreading_value;
    private TextMeshProUGUI slider_texScale_value;

    private float ZoomAmount = 20;
    public RenderTexture hub_targetColor;
    public RenderTexture hub_targetDepth;

    public RenderTexture blade_targetColor;

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
        hub_capture_camera.orthographic = true;
        hub_capture_camera.orthographicSize = 1.0f; 

        int width = 256;
        hub_targetColor = new(width, width, 0, RenderTextureFormat.ARGBFloat);
        hub_targetColor.filterMode = FilterMode.Point;
        hub_targetColor.antiAliasing = 1;
        hub_targetDepth = new(width, width, 24, RenderTextureFormat.Depth);
        hub_capture_camera.SetTargetBuffers(hub_targetColor.colorBuffer, hub_targetDepth.depthBuffer);
        hub_capture_camera.depthTextureMode = DepthTextureMode.Depth;
        bound_hub_material.SetTexture("_MainTex", hub_targetColor);
        bound_hub_material.SetTexture("_DepthTex", hub_targetDepth);
        ///////////////////////////////////////////////////////////////////////////////////////
        blade_capture_camera.orthographic = true;
        //blade_capture_camera.orthographicSize = 1.3f;
        width = 256*2;
        blade_targetColor = new(width, width, 0, RenderTextureFormat.ARGBFloat);
        blade_targetColor.filterMode = FilterMode.Point;
        blade_targetColor.antiAliasing = 1;
        blade_capture_camera.targetTexture = blade_targetColor;
        bound_blade_material.SetTexture("_MainTex", blade_targetColor);
        ///////////////////////////////////////////////////////////////////////////////////////

        for (int i = 0; i < 100; i++)
        { 
           // Instantiate(blade, transform.position, transform.rotation * Quaternion.Euler(new Vector3(0,i,0)), transform);
            Instantiate(blade, transform.position, transform.rotation * Quaternion.Euler(new Vector3(0,0,0)), hub.transform);
        }


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
        Bounds bounds = GetBoundsWithChildren(focusedObject); // Debug.Log(bounds.extents.magnitude);
        float aspectRatio = 1; // Screen.width / Screen.height;
        float distance = (camera.transform.position - focusedObject.transform.position).magnitude;
        //camera.fieldOfView = 2.0f * Mathf.Rad2Deg * Mathf.Atan((0.5f * bounds.extents.magnitude) / (distance * aspectRatio * spacingfactor));
        
        if (control_clipping == false)
        {
            // camera.transform.LookAt(bounds.center); ///////////////////////////////xXXXXXXXXXXxx
            camera.transform.LookAt(focusedObject.transform);
        }
        else
        {
            camera.transform.LookAt(focusedObject.transform);
            camera.nearClipPlane = distance - bounds.extents.magnitude / 1.4f;
            camera.farClipPlane = distance + bounds.extents.magnitude / 1.4f;
        }
    }

    void Update()
    {
        slider_rpm_value.text = slider_rpm.value.ToString("0");
        slider_sigma_value.text = slider_sigma.value.ToString("0.00");
        slider_spreading_value.text = slider_spreading.value.ToString("0");
        slider_texScale_value.text = slider_texScale.value.ToString("0.00");


        // demonstration: user moves camera 
        ZoomAmount += Input.GetAxis("Mouse ScrollWheel") * 10;
        ZoomAmount = Mathf.Clamp(ZoomAmount, 5, 100);
        Camera.main.fieldOfView = ZoomAmount;
        // demonstration: rotate hub slowly to shwo effect from different angles
        hub.transform.Rotate(0, -slider_rpm.value * 6.0f * Time.deltaTime, 0, Space.Self);
        blade.transform.Rotate(0, -slider_rpm.value * 6.0f * Time.deltaTime, 0, Space.Self);
        bound_blade.transform.Rotate(0, -slider_rpm.value * 6.0f * Time.deltaTime, 0, Space.Self);


        ///////////////////////////////////////////////////////////////////////////////////////
        // capture camera must follow hub and change FOV
        FollowAndFocusOn(hub_capture_camera, bound_hub, 0.70f, true);
        // pass matrix and parameter to shader
        Matrix4x4 bound_hub_sampleMatrix = (GL.GetGPUProjectionMatrix(hub_capture_camera.projectionMatrix, false) * hub_capture_camera.worldToCameraMatrix * bound_hub.transform.worldToLocalMatrix.inverse).transpose;
        
        float bound_hub_camera_size = hub_capture_camera.orthographicSize;

        bound_hub_sampleMatrix[3, 0] = 0.5f;
        bound_hub_sampleMatrix[3, 1] = 0.5f;
        bound_hub_sampleMatrix[3, 2] = 0.5f;
        bound_hub_sampleMatrix[0, 0] *= 0.5f/bound_hub_camera_size;
        bound_hub_sampleMatrix[1, 0] *= 0.5f/bound_hub_camera_size;
        bound_hub_sampleMatrix[2, 0] *= 0.5f/bound_hub_camera_size;
        bound_hub_sampleMatrix[0, 1] *= 0.5f/bound_hub_camera_size;
        bound_hub_sampleMatrix[1, 1] *= 0.5f/bound_hub_camera_size;
        bound_hub_sampleMatrix[2, 1] *= 0.5f/bound_hub_camera_size;
        bound_hub_sampleMatrix[0, 2] *= 0.5f/bound_hub_camera_size;
        bound_hub_sampleMatrix[1, 2] *= 0.5f/bound_hub_camera_size;
        bound_hub_sampleMatrix[2, 2] *= 0.5f/bound_hub_camera_size;
        bound_hub_material.SetMatrix("_ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld", bound_hub_sampleMatrix);
        bound_hub_material.SetMatrix("_ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld_inverse", bound_hub_sampleMatrix.inverse);
        bound_hub_material.SetFloat("_sigma", slider_sigma.value); //
        bound_hub_material.SetFloat("_spreading", slider_spreading.value); //
        bound_hub_material.SetFloat("_texScale", slider_texScale.value); //                                                                 
        ///////////////////////////////////////////////////////////////////////////////////////
        FollowAndFocusOn(blade_capture_camera, bound_blade, 0.30f, false);
        Matrix4x4 bound_blade_sampleMatrix = (GL.GetGPUProjectionMatrix(hub_capture_camera.projectionMatrix, false) * blade_capture_camera.worldToCameraMatrix * bound_blade.transform.worldToLocalMatrix.inverse).transpose;
       
        float bound_blade_camera_size = blade_capture_camera.orthographicSize;

        bound_blade_sampleMatrix[3, 0] = 0.5f;
        bound_blade_sampleMatrix[3, 1] = 0.5f;
        bound_blade_sampleMatrix[3, 2] = 0.5f;
        bound_blade_sampleMatrix[0, 0] *= 0.5f/bound_blade_camera_size;
        bound_blade_sampleMatrix[1, 0] *= 0.5f/bound_blade_camera_size;
        bound_blade_sampleMatrix[2, 0] *= 0.5f/bound_blade_camera_size;
        bound_blade_sampleMatrix[0, 1] *= 0.5f/bound_blade_camera_size;
        bound_blade_sampleMatrix[1, 1] *= 0.5f/bound_blade_camera_size;
        bound_blade_sampleMatrix[2, 1] *= 0.5f/bound_blade_camera_size;
        bound_blade_sampleMatrix[0, 2] *= 0.5f/bound_blade_camera_size;
        bound_blade_sampleMatrix[1, 2] *= 0.5f/bound_blade_camera_size;
        bound_blade_material.SetFloat("_sigma", slider_sigma.value); //
        bound_blade_material.SetFloat("_spreading", slider_spreading.value); //
        //bound_blade_material.SetFloat("_texScale", slider_texScale.value); //    
        bound_blade_material.SetMatrix("_ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld", bound_blade_sampleMatrix); ////////XXXXXXXXXXXXXXXXXxx

        ///////////////////////////////////////////////////////////////////////////////////////




        // Debug.Log( (GL.GetGPUProjectionMatrix(hub_capture_camera.projectionMatrix, false) * hub_capture_camera.worldToCameraMatrix * bound_hub.transform.worldToLocalMatrix.inverse).transpose);
        // Debug.Log(sampleMatrix);
        // Debug.Log(bound_hub.transform.worldToLocalMatrix.inverse);
        // Debug.Log(hub_capture_camera.worldToCameraMatrix);
        // Debug.Log(hub_capture_camera.projectionMatrix);
        // Debug.Log(GL.GetGPUProjectionMatrix(hub_capture_camera.projectionMatrix, false));
        // Debug.Log(GL.GetGPUProjectionMatrix(hub_capture_camera.projectionMatrix, true));
        // Debug.Log("------");

        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }
}
