using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RotorBlurManager : MonoBehaviour
{
    [Header("Alle RotorBlurUnits in der Szene")]
    public RotorBlurUnit[] units;

    [Header("Rotierende 3D-Objekte")]
    public Transform[] rotatingParts;

    private Slider slider_rpm;
    private Slider slider_sigma;
    private Slider slider_spreading;
    private Slider slider_texScale;

    private TextMeshProUGUI text_rpm;
    private TextMeshProUGUI text_sigma;
    private TextMeshProUGUI text_spreading;
    private TextMeshProUGUI text_texScale;

    void Awake()
    {
        slider_rpm = GameObject.Find("Slider_rpm")?.GetComponent<Slider>();
        slider_sigma = GameObject.Find("Slider_sigma")?.GetComponent<Slider>();
        slider_spreading = GameObject.Find("Slider_spreading")?.GetComponent<Slider>();
        slider_texScale = GameObject.Find("Slider_texScale")?.GetComponent<Slider>();

        text_rpm = GameObject.Find("Text_rpm (1)")?.GetComponent<TextMeshProUGUI>();
        text_sigma = GameObject.Find("Text_sigma (1)")?.GetComponent<TextMeshProUGUI>();
        text_spreading = GameObject.Find("Text_spreading (1)")?.GetComponent<TextMeshProUGUI>();
        text_texScale = GameObject.Find("Text_texScale (1)")?.GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        float rpm = slider_rpm != null ? slider_rpm.value : 150f;
        float sigma = slider_sigma != null ? slider_sigma.value : 0.35f;
        float spreading = slider_spreading != null ? slider_spreading.value : 40f;
        float texScale = slider_texScale != null ? slider_texScale.value : 1.0f;

        if (text_rpm != null) text_rpm.text = rpm.ToString("0");
        if (text_sigma != null) text_sigma.text = sigma.ToString("0.00");
        if (text_spreading != null) text_spreading.text = spreading.ToString("0");
        if (text_texScale != null) text_texScale.text = texScale.ToString("0.00");

        // 1. Physische Teile drehen
        float deltaAngle = -rpm * 6.0f * Time.deltaTime;
        if (rotatingParts != null)
        {
            foreach (var part in rotatingParts)
            {
                if (part != null)
                    part.Rotate(0f, deltaAngle, 0f, Space.Self);
            }
        }

        // 2. Blur-Parameter an alle RotorBlurUnits übergeben
        if (units != null)
        {
            foreach (var unit in units)
            {
                if (unit != null)
                {
                    unit.sigma = sigma;
                    unit.spreading = spreading;
                    unit.texScale = texScale;
                }
            }
        }


        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
}
