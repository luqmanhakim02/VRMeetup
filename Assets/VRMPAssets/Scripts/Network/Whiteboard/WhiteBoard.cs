using Unity.Netcode;
using UnityEngine;

public class WhiteBoard : NetworkBehaviour
{
    public Texture2D texture;
    public Vector2 textureSize = new Vector2(2048, 2048);

    private Renderer _renderer;

    void Start()
    {
        _renderer = GetComponent<Renderer>();  // Get the renderer of the whiteboard
        texture = new Texture2D((int)textureSize.x, (int)textureSize.y);
        _renderer.material.mainTexture = texture;  // Assign the texture to the material's main texture

    }

    // Method to sample the background color from the whiteboard's texture (including effects, lighting, and shaders)
    public Color SampleBackgroundColor()
    {
        if (texture != null)
        {
            // Sample a pixel from the whiteboard texture to get its visual background color
            return texture.GetPixel(0, 0);  // We are assuming the background color is at (0,0)
        }
        return Color.white; // Return white if the texture is missing or something goes wrong
    }

    // Method to clear the whiteboard's texture
    public void ClearBoard()
    {
        Color[] clearColor = new Color[texture.width * texture.height];
        Color backgroundColor = SampleBackgroundColor();  // Use the sampled background color

        // Set all pixels to the background color
        for (int i = 0; i < clearColor.Length; i++)
        {
            clearColor[i] = backgroundColor;
        }

        // Apply the background color to the whole texture
        texture.SetPixels(clearColor);
        texture.Apply();  // Apply the changes to the texture
    }
}
