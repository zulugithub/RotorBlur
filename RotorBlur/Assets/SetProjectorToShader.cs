using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
//using static Unity.Mathematics.math;
//using Unity.Mathematics;


public class SetProjectorToShader : MonoBehaviour
{
    private Camera hub_capture_camera;
    private Material bound_hub_material;
    private GameObject hub;
    private GameObject bound_hub;

    private Camera blade_capture_camera;
    private Material bound_blade_material;
    private GameObject blade;
    private GameObject bound_blade;

    private RenderTexture hub_targetColor;
    private RenderTexture hub_targetDepth;
    private RenderTexture blade_targetColor;

    private Matrix4x4 hub_bladePos;
    private Matrix4x4 blade_bladePos;



    private Slider slider_rpm;
    private Slider slider_sigma;
    private Slider slider_spreading;
    private Slider slider_texScale;
    private TextMeshProUGUI slider_rpm_value;
    private TextMeshProUGUI slider_sigma_value;
    private TextMeshProUGUI slider_spreading_value;
    private TextMeshProUGUI slider_texScale_value;

    private float ZoomAmount = 20;


    /*
    static void AddLayerAt(SerializedProperty layers, int index, string layerName, bool tryOtherIndex = true)
    {
        // Skip if a layer with the name already exists.
        for (int i = 0; i < layers.arraySize; ++i)
        {
            if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
            {
                Debug.Log("Skipping layer '" + layerName + "' because it already exists.");
                return;
            }
        }

        // Extend layers if necessary
        if (index >= layers.arraySize)
            layers.arraySize = index + 1;

        // set layer name at index
        var element = layers.GetArrayElementAtIndex(index);
        if (string.IsNullOrEmpty(element.stringValue))
        {
            element.stringValue = layerName;
            Debug.Log("Added layer '" + layerName + "' at index " + index + ".");
        }
        else
        {
            Debug.LogWarning("Could not add layer at index " + index + " because there already is another layer '" + element.stringValue + "'.");

            if (tryOtherIndex)
            {
                // Go up in layer indices and try to find an empty spot.
                for (int i = index + 1; i < 32; ++i)
                {
                    // Extend layers if necessary
                    if (i >= layers.arraySize)
                        layers.arraySize = i + 1;

                    element = layers.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(element.stringValue))
                    {
                        element.stringValue = layerName;
                        Debug.Log("Added layer '" + layerName + "' at index " + i + " instead of " + index + ".");
                        return;
                    }
                }

                Debug.LogError("Could not add layer " + layerName + " because there is no space left in the layers array.");
            }
        }
    }
    */

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


        //SerializedObject serializedObject = new SerializedObject(asset[0]);
        //SerializedProperty layers = serializedObject.FindProperty("layers");
        //AddLayerAt(layers, 8, "Bike");
        //LayerMask

        //gameObject.layer uses only integers, but we can turn a layer name into a layer integer using LayerMask.NameToLayer()
        //int hub_layer = LayerMask.NameToLayer("Hub");
        //GameObject.Find("hub").layer = hub_layer;
        //originalLayerMask |= (1 << layerToAdd);

        //string[] layers_ = Enumerable.Range(0, 31).Select(index => LayerMask.LayerToName(index)).Where(l => !string.IsNullOrEmpty(l)).ToArray();
        //Camera.main.cullingMask = Camera.main.cullingMask;
        //Debug.Log(Camera.main.cullingMask);



        ///////////////////////////////////////////////////////////////////////////////////////
        /// hub
        ///////////////////////////////////////////////////////////////////////////////////////
        hub = GameObject.Find("hub").transform.GetChild(0).gameObject; 
        bound_hub   = GameObject.Find("bound_hub").transform.GetChild(0).gameObject;
        bound_hub.GetComponent<Renderer>().material = Resources.Load<Material>("Bound_Hub");
        bound_hub_material = bound_hub.GetComponent<Renderer>().materials[0];

        var hub_cameraGameObject = new GameObject("hub_capture_camera"); // create camera GameObject
        hub_cameraGameObject.transform.parent = Camera.main.transform; // add camera as child to main camera
        hub_cameraGameObject.transform.localPosition = new Vector3(0, 0, 0);
        hub_capture_camera = hub_cameraGameObject.AddComponent<Camera>();
        hub_capture_camera.clearFlags = CameraClearFlags.SolidColor;
        hub_capture_camera.backgroundColor = new Color(0,0,0,0);
        hub_capture_camera.depth = -1;
        hub_capture_camera.orthographic = true;
        hub_capture_camera.orthographicSize = 1.2000000000000f; 
        hub_capture_camera.useOcclusionCulling = true;
        hub_capture_camera.cullingMask = LayerMask.GetMask("Hub");
        hub.layer = LayerMask.NameToLayer("Hub");

        int width = 256;
        hub_targetColor = new(width, width, 0, RenderTextureFormat.ARGBFloat);
        hub_targetColor.filterMode = FilterMode.Point;
        hub_targetColor.antiAliasing = 1;
        hub_targetDepth = new(width, width, 24, RenderTextureFormat.Depth);
        hub_capture_camera.SetTargetBuffers(hub_targetColor.colorBuffer, hub_targetDepth.depthBuffer);
        hub_capture_camera.depthTextureMode = DepthTextureMode.Depth;
        bound_hub_material.SetTexture("_MainTex", hub_targetColor);
        bound_hub_material.SetTexture("_DepthTex", hub_targetDepth);
        hub_bladePos = Matrix4x4.Rotate(Quaternion.Euler(0, 0, 0));
        ///////////////////////////////////////////////////////////////////////////////////////



        ///////////////////////////////////////////////////////////////////////////////////////
        /// blade
        ///////////////////////////////////////////////////////////////////////////////////////
        blade = GameObject.Find("blade").transform.GetChild(0).gameObject;
        bound_blade = GameObject.Find("bound_blade").transform.GetChild(0).gameObject;
        bound_blade.GetComponent<Renderer>().material = Resources.Load<Material>("Bound_Blade");
        bound_blade_material = bound_blade.GetComponent<Renderer>().materials[0];

        var blade_cameraGameObject = new GameObject("blade_capture_camera"); // create camera GameObject
        blade_cameraGameObject.transform.parent = Camera.main.transform; // add camera as child to main camera
        blade_cameraGameObject.transform.localPosition = new Vector3(0, 0, 0);
        blade_capture_camera = blade_cameraGameObject.AddComponent<Camera>();
        blade_capture_camera.clearFlags = CameraClearFlags.SolidColor;
        blade_capture_camera.backgroundColor = new Color(0, 0, 0, 0);
        blade_capture_camera.depth = -2;
        blade_capture_camera.orthographic = true;
        blade_capture_camera.orthographicSize = 10.00000000000f;
        blade_capture_camera.useOcclusionCulling = true;
        blade_capture_camera.cullingMask = LayerMask.GetMask("Blade");
        blade.layer = LayerMask.NameToLayer("Blade");

        width = 256*2;
        blade_targetColor = new(width, width, 0, RenderTextureFormat.ARGBFloat);
        blade_targetColor.filterMode = FilterMode.Point;
        blade_targetColor.antiAliasing = 1;
        blade_capture_camera.targetTexture = blade_targetColor;
        bound_blade_material.SetTexture("_MainTex", blade_targetColor);
        blade_bladePos = Matrix4x4.Rotate(Quaternion.Euler(0, 0, 0));
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
        /// hub
        ///////////////////////////////////////////////////////////////////////////////////////
        // capture camera must follow hub and change FOV
        FollowAndFocusOn(hub_capture_camera, bound_hub, 0.70f, true);
        // pass matrix and parameter to shader
        Matrix4x4 bound_hub_sampleMatrix = (GL.GetGPUProjectionMatrix(hub_capture_camera.projectionMatrix, false) * hub_capture_camera.worldToCameraMatrix  * bound_hub.transform.worldToLocalMatrix.inverse * hub_bladePos).transpose;

        float bound_hub_camera_size = 1;// hub_capture_camera.orthographicSize;

        // https://forum.unity.com/threads/projecting-texture-from-a-camera-on-objects.628189/#post-4307893	 ComputeNonStereoScreenPos()
        // "By default the projection matrix produces a position that's in a -w to w range on the x and y, but
        //  for sampling a texture you want it in 0.0 to 1.0 range, so you need to scale and offset the matrix
        //  to produce values in that range"	
        //m4x4 =   [ *f  *f   0   0
        //           *f  *f   0   0
        //           *f  *f   0   0   
        //            f   f   0   0] f = 0.5  (this matrix is transposed!)
        bound_hub_sampleMatrix[3, 0]  = 0.5f; // column major order
        bound_hub_sampleMatrix[3, 1]  = 0.5f;
        bound_hub_sampleMatrix[0, 0] *= 0.5f/bound_hub_camera_size;
        bound_hub_sampleMatrix[2, 0] *= 0.5f/bound_hub_camera_size;
        bound_hub_sampleMatrix[0, 1] *= 0.5f/bound_hub_camera_size;
        bound_hub_sampleMatrix[1, 1] *= 0.5f/bound_hub_camera_size;
        bound_hub_sampleMatrix[2, 1] *= 0.5f/bound_hub_camera_size;

        bound_hub_material.SetMatrix("_ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld", bound_hub_sampleMatrix);
        bound_hub_material.SetMatrix("_ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld_inverse", bound_hub_sampleMatrix.inverse);
        bound_hub_material.SetFloat("_sigma", slider_sigma.value); //
        bound_hub_material.SetFloat("_spreading", slider_spreading.value); //
        bound_hub_material.SetFloat("_texScale", slider_texScale.value); //                                                                 
        ///////////////////////////////////////////////////////////////////////////////////////
    
        ///////////////////////////////////////////////////////////////////////////////////////
        /// blade
        ///////////////////////////////////////////////////////////////////////////////////////
        FollowAndFocusOn(blade_capture_camera, bound_blade, 0.30f, false);
        Matrix4x4 bound_blade_sampleMatrix = (GL.GetGPUProjectionMatrix(hub_capture_camera.projectionMatrix, false) * blade_capture_camera.worldToCameraMatrix * bound_blade.transform.worldToLocalMatrix.inverse * blade_bladePos ).transpose;
       
        float bound_blade_camera_size = blade_capture_camera.orthographicSize;

        // https://forum.unity.com/threads/projecting-texture-from-a-camera-on-objects.628189/#post-4307893	 ComputeNonStereoScreenPos()	
        // "By default the projection matrix produces a position that's in a -w to w range on the x and y, but
        //  for sampling a texture you want it in 0.0 to 1.0 range, so you need to scale and offset the matrix
        //  to produce values in that range"	
        //m4x4 =   [ *f  *f   0   0
        //           *f  *f   0   0
        //           *f  *f   0   0   
        //            f   f   0   0] f = 0.5  (this matrix is transposed!)
        bound_blade_sampleMatrix[3, 0]  = 0.5f; // column major order
        bound_blade_sampleMatrix[3, 1]  = 0.5f;
        bound_blade_sampleMatrix[0, 0] *= 0.5f / bound_blade_camera_size;
        bound_blade_sampleMatrix[2, 0] *= 0.5f / bound_blade_camera_size;
        bound_blade_sampleMatrix[0, 1] *= 0.5f / bound_blade_camera_size;
        bound_blade_sampleMatrix[1, 1] *= 0.5f / bound_blade_camera_size;
        bound_blade_sampleMatrix[2, 1] *= 0.5f / bound_blade_camera_size;

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
