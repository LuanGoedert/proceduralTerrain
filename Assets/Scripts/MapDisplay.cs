using System;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public Renderer textureRenderer; // Reference to the Renderer component to display the texture
    public MeshFilter meshFilter; // Reference to the MeshFilter component to display the mesh
    public MeshRenderer meshRenderer; // Reference to the MeshRenderer component to display the mesh  

    public void DrawTexture(Texture2D texture)
    {        // Assign the texture to the renderer
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height); // Scale the renderer to match the texture size
    }

    public void DrawMesh(MeshData meshData, Texture2D texture2D)
    {
        meshFilter.sharedMesh = meshData.CreateMesh(); // Create and assign the mesh to the MeshFilter
        meshRenderer.sharedMaterial.mainTexture = texture2D; // Assign the texture to the Mesh
    }
}
