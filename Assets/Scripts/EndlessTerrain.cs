using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    const float scale = 1f;
    const float viewerMoveThresholdForChunkUpdate = 25f; // Threshold for updating terrain chunks based on viewer movement
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate; // Squared threshold for performance optimization

    public LODInfo[] detailLevels; // Array of detail levels for terrain rendering

    public static float maxViewDistance; // Maximum view distance for the terrain
    public Transform viewer; // Reference to the viewer's transform
    public Material mapMaterial; // Material to be used for the terrain chunks

    public static Vector2 viewerPosition; // Current position of the viewer
    public static Vector2 lastViewerPosition; // Last recorded position of the viewer, used to determine if the viewer has moved significantly

    int chunckSize; // Size of each terrain chunk
    int chunksVisibleInViewDistance; // Number of chunks visible in the current view distance
    static MapGenerator mapGenerator; // Reference to the MapGenerator instance

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>(); // Dictionary to hold the terrain chunks
    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>(); // List to keep track of the terrain chunks visible in the last update

    void Start()
    {
        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold; // Set the maximum view distance based on the last detail level
        mapGenerator = FindObjectOfType<MapGenerator>(); // Find the MapGenerator instance in the scene
        chunckSize = MapGenerator.mapChunkSize - 1; // Set chunk size based on map chunk size
        chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / chunckSize);
        UpdateVisibleChuncks();
    }

    void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / scale; // Update the viewer's position
        if ((lastViewerPosition - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            lastViewerPosition = viewerPosition; // Update the last viewer position if the viewer has moved significantly
            UpdateVisibleChuncks(); // Update the terrain chunks based on the new viewer position
        }
    }

    void UpdateVisibleChuncks()
    {
        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false); // Set all previously visible chunks to invisible
        }
        terrainChunksVisibleLastUpdate.Clear(); // Clear the list of visible chunks from the last update

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunckSize); // Current chunk coordinate in X
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunckSize); // Current chunk coordinate in Z

        for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                // Check if the chunk is already being generated or displayed
                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk(); // Update the existing terrain chunk
                }
                else
                {
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunckSize, detailLevels, transform, mapMaterial)); // Add new terrain chunk to the dictionary
                }
            }
        }
    }

    public class TerrainChunk
    {
        Vector2 position; // Position of the terrain chunk
        GameObject meshObject;

        Bounds bounds;
        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        LODInfo[] detailLevels;
        LODMesh[] lodMeshes; // Array of LOD meshes for different detail levels
        MapData mapData;
        bool mapDataReceived; // Flag to check if map data has been received
        int previousLODIndex = -1; // Previous LOD index to avoid unnecessary updates

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            this.detailLevels = detailLevels; // Set the detail levels for the terrain chunk
            position = coord * size; // Set the position based on the coordinate and size
            bounds = new Bounds(position, Vector2.one * size); // Create a bounds object for the terrain chunk
            Vector3 positionV3 = new Vector3(position.x, 0, position.y); // Convert to Vector3 for Unity
            meshObject = new GameObject("Terrain Chunk "); // Create a new GameObject for the terrain chunk
            meshRenderer = meshObject.AddComponent<MeshRenderer>(); // Add a MeshRenderer component to the GameObject
            meshRenderer.sharedMaterial = material; // Assign the material to the MeshRenderer
            meshFilter = meshObject.AddComponent<MeshFilter>(); // Add a MeshFilter component to the

            meshObject.transform.position = positionV3 * scale; // Set the position of the mesh object
            meshObject.transform.parent = parent; // Set the parent of the mesh object
            meshObject.transform.localScale = Vector3.one * scale;
            mapGenerator.RequestMapData(position, OnMapDataReceived);

            lodMeshes = new LODMesh[detailLevels.Length]; // Initialize the LOD meshes array
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk); // Create a new LODMesh for each detail level
            }

            SetVisible(false); // Initially set the terrain chunk to be invisible
        }

        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData; // Store the received map data
            mapDataReceived = true; // Set the flag indicating that map data has been received
            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize); // Generate a texture from the color map
            meshRenderer.material.mainTexture = texture; // Assign the texture to the material
            UpdateTerrainChunk(); // Update the terrain chunk with the received map data
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            meshFilter.mesh = meshData.CreateMesh(); // Create and assign the mesh to the MeshFilter  

        }

        public void UpdateTerrainChunk()
        {
            if (!mapDataReceived)
            {
                return; // If map data has not been received, do not update the terrain chunk
            }
            float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition)); // Calculate the squared distance from the viewer to the terrain chunk
            bool visible = viewerDistanceFromNearestEdge <= maxViewDistance; // Check if the terrain chunk is within the view distance

            if (visible)
            {
                int lodIndex = 0; // Initialize the LOD index
                for (int i = 0; i < detailLevels.Length - 1; i++)
                {
                    if (viewerDistanceFromNearestEdge > detailLevels[i].visibleDistanceThreshold)
                    {
                        lodIndex = i + 1; // Increment the LOD index if the viewer is beyond the current detail level's threshold
                    }
                    else
                    {
                        break; // Break if the viewer is within the current detail level's threshold
                    }
                    if (lodIndex != previousLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex]; // Get the LOD mesh for the current LOD index
                        if (lodMesh.hasMesh)
                        {
                            previousLODIndex = lodIndex; // Update the previous LOD index
                            meshFilter.mesh = lodMesh.mesh; // If the mesh is ready, assign it to the MeshFilter
                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(mapData); // If the mesh has not been requested, request it
                        }
                    }
                }
                terrainChunksVisibleLastUpdate.Add(this);
            }
            SetVisible(visible); // Set the visibility of the terrain chunk based on the distance
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible); // Set the visibility of the terrain chunk's mesh object
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf; // Return whether the terrain chunk's mesh object is active (visible)
        }
    }

    class LODMesh
    {
        public Mesh mesh; // The mesh for the LOD
        public bool hasRequestedMesh; // Flag to check if the mesh has been requested
        public bool hasMesh; // Flag to check if the mesh is ready
        public int lod;
        public System.Action updateCallback; // Callback to be invoked when the mesh is ready

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.updateCallback = updateCallback; // Set the callback to be invoked when the mesh is ready
            this.lod = lod; // Set the level of detail
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh(); // Create the mesh from the received mesh data
            hasMesh = true; // Set the flag indicating that the mesh is ready
            updateCallback(); // Invoke the callback with this LODMesh instance
        }

        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true; // Set the flag indicating that the mesh has been requested
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived); // Request the mesh data
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod; // Level of detail
        public float visibleDistanceThreshold; // Distance threshold for visibility

        public LODInfo(int lod, float visibleDistanceThreshold)
        {
            this.lod = lod; // Set the level of detail
            this.visibleDistanceThreshold = visibleDistanceThreshold; // Set the visibility distance threshold
        }
    }
}

