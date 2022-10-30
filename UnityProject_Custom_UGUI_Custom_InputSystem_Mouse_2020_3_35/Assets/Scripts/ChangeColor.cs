// Using statements
using UnityEngine;

/// <summary>
/// Changes the color of the renderer
/// </summary>
public class ChangeColor : MonoBehaviour
{
    /// <summary>
    ///  Hue of color
    /// </summary>
    public float hue = 0f;

    /// <summary>
    /// Saturation of color
    /// </summary>
    public float saturation = 1f;

    /// <summary>
    /// Value of color
    /// </summary>
    public float value = 1f;

    /// <summary>
    /// The renderer of the object
    /// </summary>
    public Renderer objRenderer;

    // Update is called once per frame
    void Update()
    {
        // Control hue value based on hue
        if (hue < 1)        
            hue += 0.0005f;        
        else        
            hue = 0;
        
        // Adjust color
        objRenderer.sharedMaterial.color = Color.HSVToRGB(hue, saturation, value);
    }
}
