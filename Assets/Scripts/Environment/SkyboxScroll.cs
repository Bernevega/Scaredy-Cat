using UnityEngine;

public class SkyboxScroll : MonoBehaviour
{
    [Header("6-Sided Skybox")]
    [Tooltip("Horizontal texture scrolling speed.")]
    [SerializeField] private float scrollSpeedX = 0.01f;

    [Tooltip("Vertical texture scrolling speed.")]
    [SerializeField] private float scrollSpeedY = 0f;

    [Header("Cubemap Skybox")]
    [Tooltip("Rotation speed in degrees per second.")]
    [SerializeField] private float cubemapRotationSpeed = 1f;

    private Material skyboxMaterial;

    private Vector2 offset;
    private float rotation;

    private bool supportsSixSided;
    private bool supportsCubemapRotation;

    private readonly string[] textureProperties =
    {
        "_FrontTex",
        "_BackTex",
        "_LeftTex",
        "_RightTex",
        "_UpTex",
        "_DownTex"
    };

    private void Start()
    {
        if (RenderSettings.skybox == null)
            return;

        // Create a runtime copy so the original
        // skybox material asset is not modified.
        skyboxMaterial =
            new Material(RenderSettings.skybox);

        RenderSettings.skybox = skyboxMaterial;

        DetectSkyboxType();
    }

    private void DetectSkyboxType()
    {
        supportsSixSided = false;

        foreach (string property in textureProperties)
        {
            if (skyboxMaterial.HasProperty(property))
            {
                supportsSixSided = true;
                break;
            }
        }

        // Cubemap/Panoramic-style skyboxes commonly
        // expose a _Rotation property.
        supportsCubemapRotation =
            skyboxMaterial.HasProperty("_Rotation");

        if (supportsCubemapRotation)
        {
            rotation =
                skyboxMaterial.GetFloat("_Rotation");
        }
    }

    private void Update()
    {
        if (skyboxMaterial == null)
            return;

        // ---------------------------------------------
        // 6-SIDED SKYBOX
        // ---------------------------------------------

        if (supportsSixSided)
        {
            offset.x +=
                scrollSpeedX * Time.deltaTime;

            offset.y +=
                scrollSpeedY * Time.deltaTime;

            offset.x %= 1f;
            offset.y %= 1f;

            foreach (string property in textureProperties)
            {
                if (skyboxMaterial.HasProperty(property))
                {
                    skyboxMaterial.SetTextureOffset(
                        property,
                        offset
                    );
                }
            }
        }

        // ---------------------------------------------
        // CUBEMAP SKYBOX
        // ---------------------------------------------

        if (supportsCubemapRotation)
        {
            rotation +=
                cubemapRotationSpeed * Time.deltaTime;

            rotation %= 360f;

            skyboxMaterial.SetFloat(
                "_Rotation",
                rotation
            );
        }
    }
}