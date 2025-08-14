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
    [Tooltip("Base speed for the flicker noise.")]
    public float flickerSpeed = 1f;
    [Tooltip("Per-instance random speed variation range (multiplier).")]
    public Vector2 perInstanceSpeedMultiplier = new Vector2(0.9f, 1.2f);

    [Header("Color Range")]
    [Tooltip("Color at the start of the range (yellow).")]
    public Color colorMin = new Color(1f, 0.92f, 0.016f);  // yellow
    [Tooltip("Color at the end of the range (orange).")]
    public Color colorMax = new Color(1f, 0.5f, 0f);       // orange

    [Header("Range Flicker")]
    [Tooltip("Lowest possible light range.")]
    public float minRange = 8f;
    [Tooltip("Highest possible light range.")]
    public float maxRange = 12f;

    [Header("Channel Independence")]
    [Tooltip("Use separate noise streams for intensity, color, and range (more organic).")]
    public bool useIndependentChannels = true;

    private Light _light;

    // Per-instance randomized data to de-sync candles
    float _speedMulIntensity, _speedMulColor, _speedMulRange;
    float _offIntensityX, _offIntensityY;
    float _offColorX, _offColorY;
    float _offRangeX, _offRangeY;

    void Awake()
    {
        _light = GetComponent<Light>();

        // Randomize offsets and slight speed variations per instance
        _speedMulIntensity = Random.Range(perInstanceSpeedMultiplier.x, perInstanceSpeedMultiplier.y);
        _speedMulColor     = Random.Range(perInstanceSpeedMultiplier.x, perInstanceSpeedMultiplier.y);
        _speedMulRange     = Random.Range(perInstanceSpeedMultiplier.x, perInstanceSpeedMultiplier.y);

        // Different 2D noise offsets to avoid synchronization
        _offIntensityX = Random.value * 1000f; _offIntensityY = Random.value * 1000f;
        _offColorX     = Random.value * 1000f; _offColorY     = Random.value * 1000f;
        _offRangeX     = Random.value * 1000f; _offRangeY     = Random.value * 1000f;
    }

    void Update()
    {
        float t = Time.time;

        float nIntensity, nColor, nRange;

        if (useIndependentChannels)
        {
            nIntensity = Mathf.PerlinNoise(_offIntensityX + t * flickerSpeed * _speedMulIntensity, _offIntensityY);
            nColor     = Mathf.PerlinNoise(_offColorX     + t * flickerSpeed * _speedMulColor,     _offColorY);
            nRange     = Mathf.PerlinNoise(_offRangeX     + t * flickerSpeed * _speedMulRange,     _offRangeY);
        }
        else
        {
            // Single shared noise stream but still desynced per instance via offsets
            float nx = _offIntensityX + t * flickerSpeed * _speedMulIntensity;
            float ny = _offIntensityY;
            float shared = Mathf.PerlinNoise(nx, ny);
            nIntensity = shared;
            nColor     = shared;
            nRange     = shared;
        }

        // Apply
        _light.intensity = Mathf.Lerp(minIntensity, maxIntensity, nIntensity);
        _light.color     = Color.Lerp(colorMin,    colorMax,    nColor);
        _light.range     = Mathf.Lerp(minRange,    maxRange,    nRange);
    }
}
