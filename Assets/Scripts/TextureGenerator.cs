using UnityEngine;

public static class TextureGenerator
{
    public static Texture2D TextureFromColorMap(Color[] colorMap, int width, int height)
    {
        // Create a new texture with the specified width and height
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point; // Set filter mode to Point for pixelated look
        texture.wrapMode = TextureWrapMode.Clamp; // Prevent texture wrapping

        // Set the pixels of the texture using the color map
        texture.SetPixels(colorMap);
        texture.Apply(); // Apply changes to the texture

        return texture; // Return the generated texture
    }

    public static Texture2D TextureFromHeightMap(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        Color[] colorMap = new Color[width * height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float value = heightMap[x, y];
                colorMap[y * width + x] = new Color(value, value, value); // Grayscale color based on height value
            }
        }
        return TextureFromColorMap(colorMap, width, height); // Create texture from the color map
    }

}
