using UnityEngine;

public static class Noise
{
    public enum NormalizeMode { Local, Global }

    public static float[,] GenerateNoiseMap(int width, int height, float scale, int seed, int octaves, float persistance, float lacunarity, Vector2 offset, NormalizeMode normalizeMode)
    {
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        float maxPossibleHeight = 0;
        float frequency = 1f;
        float amplitude = 1f;

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        if (scale <= 0)
        {
            scale = 0.0001f; // Prevent division by zero or negative scale
        }
        float[,] noiseMap = new float[width, height];

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        float halfWidth = width / 2f;
        float halfHeight = height / 2f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {

                frequency = 1f;
                amplitude = 1f;
                float noiseHeight = 0f;
                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                    // Using Perlin noise to generate a value between 0 and 1
                    float perlinvalue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;// Scale to range [-1, 1];
                    noiseHeight += perlinvalue * amplitude;
                    amplitude *= persistance; // Decrease amplitude for next octave
                    frequency *= lacunarity; // Increase frequency for next octave
                }
                if (noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight; // Track the maximum noise height
                }
                else if (noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight; // Track the minimum noise height
                }
                noiseMap[x, y] = noiseHeight;
            }
        }
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (normalizeMode == NormalizeMode.Local)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]); // Normalize the noise values to [0, 1]    
                }
                else
                {
                    float normalizedHeigth = (noiseMap[x, y] + 1) / (2 * maxPossibleHeight / 1.5f);
                    noiseMap[x, y] = normalizedHeigth;
                }

            }
        }

        return noiseMap;
    }
}
