using UnityEngine;
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode
    {
        NoiseMap, // Draw a noise map
        ColorMap, // Draw a color map based on terrain types
        Mesh, // Draw a mesh based on the noise map
        FalloffMap
    }

    public DrawMode drawMode; // Current drawing mode

    public const int mapChunkSize = 239; // Size of the map chunk
    [Range(0, 6)]
    public int editorPreviewLOD = 1; // Level of detail for the map
    public float noiseScale = 20f; // Scale of the noise

    public int octaves = 4; // Number of octaves for the noise
    [Range(0, 1f)]
    public float persistance = 1f; // Amplitude reduction factor for each
    public float lacunarity = 2f; // Frequency increase factor for each octave

    public int seed; // Seed for random number generation
    public Vector2 offset; // Offset for the noise generation

    public float meshHeightMultiplier = 1f; // Height multiplier for the mesh

    public bool autoUpdate = true; // Automatically update the map when parameters change

    public TerrainType[] regions; // Array of terrain types
    public AnimationCurve meshHeightCurve; // Animation curve to control mesh height

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>(); // Queue to hold map data requests
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>(); // Queue to hold mesh data requests

    public bool useFalloff;

    float[,] falloffMap;

    public Noise.NormalizeMode normalizeMode;

    void Awake()
    {
        falloffMap = FallofGenerator.GenerateFalloffMap(mapChunkSize);
    }

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2.zero); // Generate the map data
        MapDisplay mapDisplay = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.ColorMap)
        {
            mapDisplay.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.ColorMap)
        {
            mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD), TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.FalloffMap)
        {
            mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(FallofGenerator.GenerateFalloffMap(mapChunkSize)));
        }
    }

    public void RequestMapData(Vector2 centre, System.Action<MapData> callback)
    {
        ThreadStart threadStart = delegate { MapDataThread(centre, callback); }; // Create a thread to generate map data
        new Thread(threadStart).Start(); // Start the thread
    }

    void MapDataThread(Vector2 centre, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(centre); // Generate the map data
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData)); // Enqueue the map data with the callback    
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate { MeshDataThread(mapData, lod, callback); }; // Create a thread to generate mesh data
        new Thread(threadStart).Start(); // Start the thread
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod); // Generate the mesh data
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData)); // Enqueue the mesh data with the callback
        }
    }

    void Update()
    {
        if (mapDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue(); // Dequeue the map data
                threadInfo.callback(threadInfo.parameter); // Invoke the callback with the generated map data
            }
        }

        if (meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue(); // Dequeue the mesh data
                threadInfo.callback(threadInfo.parameter); // Invoke the callback with the generated mesh data
            }
        }
    }
    private MapData GenerateMapData(Vector2 centre)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, noiseScale, seed, octaves, persistance, lacunarity, centre + offset, normalizeMode);

        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        for (int x = 0; x < mapChunkSize; x++)
        {
            for (int y = 0; y < mapChunkSize; y++)
            {
                if (useFalloff)
                {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                }
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    TerrainType region = regions[i];
                    // Check if the current height is within the range of the terrain type
                    if (currentHeight >= region.height)
                    {
                        colorMap[y * mapChunkSize + x] = region.color; // Assign the color based on terrain type
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        return new MapData(noiseMap, colorMap); // Return the generated map data

    }

    void OnValidate()
    {
        if (lacunarity < 1)
        {
            lacunarity = 1; // Ensure lacunarity is at least 1
        }
        if (octaves < 0)
        {
            octaves = 0; // Ensure octaves is at least 1
        }
        falloffMap = FallofGenerator.GenerateFalloffMap(mapChunkSize);
    }

    struct MapThreadInfo<T>
    {
        public Action<T> callback; // Callback to invoke with the generated map data
        public readonly T parameter; // Parameter to pass to the callback

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name; // Name of the terrain type
    public float height; // Height threshold for the terrain type
    public Color color; // Color of the terrain type
}


public struct MapData
{
    public readonly float[,] heightMap; // Noise map for the terrain
    public readonly Color[] colorMap; // Color map for the terrain

    public MapData(float[,] heightMap, Color[] colorMap)
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}