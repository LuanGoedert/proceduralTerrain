using System;
using UnityEngine;
using UnityEngine.UIElements;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heighMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail)
    {
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys); // Create a new AnimationCurve to avoid modifying the original
        int width = heighMap.GetLength(0);
        int height = heighMap.GetLength(1);
        float topleftX = (width - 1) / -2f; // Top-left corner z coordinate
        float topleftz = (height - 1) / 2f; // Top-left corner X coordinate

        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2; // Level of detail for mesh simplification
        int verticesPerLine = (width - 1) / meshSimplificationIncrement + 1; // Number of vertices per line based on level of detail

        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);

        int vertexIndex = 0;

        for (int y = 0; y < height; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < width; x += meshSimplificationIncrement)
            {
                float rawHeight = Mathf.Clamp01(heighMap[x, y]);
                float evaluatedHeight = heightCurve.Evaluate(rawHeight) * heightMultiplier;
                meshData.vertices[vertexIndex] = new Vector3(
                    topleftX + x,
                   evaluatedHeight,
                    topleftz - y
                );
                meshData.uv[vertexIndex] = new Vector2((float)x / (width - 1), (float)y / (height - 1));
                vertexIndex++;
            }
        }

        for (int y = 0; y < verticesPerLine - 1; y++)
        {
            for (int x = 0; x < verticesPerLine - 1; x++)
            {
                int current = y * verticesPerLine + x;
                int nextRow = (y + 1) * verticesPerLine + x;

                meshData.AddTriangle(current, nextRow + 1, nextRow);
                meshData.AddTriangle(nextRow + 1, current, current + 1);
            }
        }

        return meshData;
    }

    internal static MeshData GenerateTerrainMesh(object heightMap, float meshHeightMultiplier, AnimationCurve meshHeightCurve, int levelOfDetail)
    {
        throw new NotImplementedException();
    }
}

public class MeshData
{
    public Vector3[] vertices; // Array of vertices for the mesh
    public int[] triangles; // Array of triangle indices for the mesh
    public Vector2[] uv; // Array of UV coordinates for texture mapping

    public int triangleIndex;


    public MeshData(int meshWidth, int meshHeight)
    {
        vertices = new Vector3[meshWidth * meshHeight];
        uv = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6]; // 2 triangles per square, 6 indices per triangle
    }

    public void AddTriangle(int a, int b, int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;
        triangleIndex += 3; // Move to the next set of triangle indices
    }

    Vector3 CalculateNormals()
    {
        throw new NotImplementedException();
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals(); // Recalculate normals for lighting
        return mesh; // Return the generated mesh
    }
}
