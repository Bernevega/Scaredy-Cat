// LightFlicker.cs
using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    [Header("Intensity Range")]
    [Tooltip("Lowest possible light intensity.")]
    public float minIntensity = 0.5f;
    [Tooltip("Highest possible light intensity.")]
    public float maxIntensity = 1.5f;

    [Header("Flicker Speed")]
    [Tooltip("How fast the flicker noise evolves.")]
    public float flickerSpeed = 1f;

    [Header("Color Range")]
    [Tooltip("Color at the start of the range (yellow).")]
    public Color colorMin = new Color(1f, 0.92f, 0.016f);  // yellow
    [Tooltip("Color at the end of the range (orange).")]
    public Color colorMax = new Color(1f, 0.5f, 0f);        // orange

    private Light _light;

    void Awake()
    {
        _light = GetComponent<Light>();
    }

    void Update()
    {
        // Generate a smooth noise value 0→1
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);

        // Smoothly lerp intensity and color
        _light.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
        _light.color     = Color.Lerp(colorMin,    colorMax,    noise);
    }
}
