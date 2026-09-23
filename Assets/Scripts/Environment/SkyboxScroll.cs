using UnityEngine;

public class SkyboxScroll : MonoBehaviour
{
    [Header("Scroll Settings")]
    [SerializeField] private float scrollSpeedX = 0.01f;
    [SerializeField] private float scrollSpeedY = 0f;

    private Material skyboxMaterial;
    private Vector2 offset;

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

        // Create a copy so the original material asset isn't modified.
        skyboxMaterial = new Material(RenderSettings.skybox);
        RenderSettings.skybox = skyboxMaterial;
    }

    private void Update()
    {
        if (skyboxMaterial == null)
            return;

        offset.x += scrollSpeedX * Time.deltaTime;
        offset.y += scrollSpeedY * Time.deltaTime;

        // Keep values small.
        offset.x %= 1f;
        offset.y %= 1f;

        foreach (string property in textureProperties)
        {
            if (skyboxMaterial.HasProperty(property))
            {
                skyboxMaterial.SetTextureOffset(property, offset);
            }
        }
    }
}