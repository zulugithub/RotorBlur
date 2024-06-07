using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TMPro;
using UnityEditor;
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
    private TextMeshProUGUI slider_rpm_value;
    private TextMeshProUGUI slider_sigma_value;
    private TextMeshProUGUI slider_spreading_value;

    private float ZoomAmount = 20;

    void Awake()
    {
        slider_rpm              = GameObject.Find("Slider_rpm").GetComponent<Slider>();
        slider_sigma            = GameObject.Find("Slider_sigma").GetComponent<Slider>();
        slider_spreading        = GameObject.Find("Slider_spreading").GetComponent<Slider>();
        slider_rpm_value        = GameObject.Find("Text_rpm (1)").GetComponent<TextMeshProUGUI>();
        slider_sigma_value      = GameObject.Find("Text_sigma (1)").GetComponent<TextMeshProUGUI>();
        slider_spreading_value  = GameObject.Find("Text_spreading (1)").GetComponent<TextMeshProUGUI>();

        FollowAndFocusOn(capture_camera, rotor, 0.65f);
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
        camera.fieldOfView = 2.0f * Mathf.Rad2Deg * Mathf.Atan((0.5f * bounds.extents.magnitude) / (distance * aspectRatio * spacingfactor));
        camera.transform.LookAt(focusedObject.transform);
    }

    void Update()
    {

        slider_rpm_value.text = slider_rpm.value.ToString("0");
        slider_sigma_value.text = slider_sigma.value.ToString("0.00");
        slider_spreading_value.text = slider_spreading.value.ToString("0");

        // demonstration: move camera 
        //Camera.main.transform.RotateAround(Camera.main.transform.position, -UnityEngine.Vector3.up, -Input.GetAxis("Mouse X") * 0.25f); 
        //Camera.main.transform.RotateAround(Camera.main.transform.position, transform.right, Input.GetAxis("Mouse Y") * 0.25f);
        Camera.main.fieldOfView = Mathf.Clamp(ZoomAmount += Input.GetAxis("Mouse ScrollWheel")*10, 5, 100);
        // demonstration: rotate rotor slowly to shwo effect from different angles
        rotor.transform.Rotate(0, -slider_rpm.value * 6.0f * Time.deltaTime  ,0 , Space.Self);

        ///////////////////////////////////////////////////////////////////////////////////////
        // capture camera must follow rotor and change FOV
        FollowAndFocusOn(capture_camera, cylinder, 0.65f);
        // pass matrix and parameter to shader
        cylinder_material.SetMatrix("_ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld", capture_camera.projectionMatrix * capture_camera.worldToCameraMatrix * cylinder.transform.worldToLocalMatrix.inverse);
        cylinder_material.SetFloat("_sigma_mod", 1.0f / (Mathf.Pow(slider_sigma.value, 2.0f) * 0.5f / 1.4427f)); //
        cylinder_material.SetFloat("_spreading", slider_spreading.value ); //
        ///////////////////////////////////////////////////////////////////////////////////////
      
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }
}
