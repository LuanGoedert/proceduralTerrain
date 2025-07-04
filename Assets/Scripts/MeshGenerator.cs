using System;
using UnityEngine;
using UnityEngine.UIElements;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail)
    {
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys); // Create a new AnimationCurve to avoid modifying the original
        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2; // Level of detail for mesh simplification
        int borderedSize = heightMap.GetLength(0);

        int meshSize = borderedSize - 2 * meshSimplificationIncrement;
        int meshSizeUnsimplified = borderedSize - 2;

        float topleftX = (meshSizeUnsimplified - 1) / -2f; // Top-left corner z coordinate
        float topleftz = (meshSizeUnsimplified - 1) / 2f; // Top-left corner X coordinate


        int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1; // Number of vertices per line based on level of detail

        MeshData meshData = new MeshData(verticesPerLine);

        int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;

        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement)
            {
                bool isBorderVertex = (y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1);
                if (isBorderVertex)
                {
                    vertexIndicesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else
                {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement)
            {
                int vertexIndex = vertexIndicesMap[x, y];
                Vector2 percent = new Vector2((float)(x - meshSimplificationIncrement) / (float)meshSize, (float)(y - meshSimplificationIncrement) / (float)meshSize);
                float height = heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;

                Vector3 vertexPosition = new Vector3(topleftX + percent.x * meshSizeUnsimplified, height, topleftz - percent.y * meshSizeUnsimplified);
                meshData.AddVertex(vertexPosition, percent, vertexIndex);

                if (x < borderedSize - 1 && y < borderedSize - 1)
                {
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + meshSimplificationIncrement, y];
                    int c = vertexIndicesMap[x, y + meshSimplificationIncrement];
                    int d = vertexIndicesMap[x + meshSimplificationIncrement, y + meshSimplificationIncrement];

                    meshData.AddTriangle(a, d, c);
                    meshData.AddTriangle(d, a, b);
                }
                vertexIndex++;
            }
        }

        // for (int y = 0; y < verticesPerLine - 1; y++)
        // {
        //     for (int x = 0; x < verticesPerLine - 1; x++)
        //     {
        //         int current = y * verticesPerLine + x;
        //         int nextRow = (y + 1) * verticesPerLine + x;

        //         meshData.AddTriangle(current, nextRow + 1, nextRow);
        //         meshData.AddTriangle(nextRow + 1, current, current + 1);
        //     }
        // }

        return meshData;
    }
}

public class MeshData
{
    Vector3[] vertices; // Array of vertices for the mesh
    int[] triangles; // Array of triangle indices for the mesh
    Vector2[] uvs; // Array of UV coordinates for texture mapping

    public int triangleIndex;

    Vector3[] borderVertices;
    int[] borderTriangles;

    int borderTriangleIndex;

    public MeshData(int verticePerLine)
    {
        vertices = new Vector3[verticePerLine * verticePerLine];
        uvs = new Vector2[verticePerLine * verticePerLine];
        triangles = new int[(verticePerLine - 1) * (verticePerLine - 1) * 6]; // 2 triangles per square, 6 indices per triangle

        borderVertices = new Vector3[verticePerLine * 4 + 4];
        borderTriangles = new int[24 * verticePerLine];
    }

    public void AddVertex(Vector3 vertextPosition, Vector2 uv, int vertexIndex)
    {
        if (vertexIndex < 0)
        {
            borderVertices[-vertexIndex - 1] = vertextPosition;

        }
        else
        {
            vertices[vertexIndex] = vertextPosition;
            this.uvs[vertexIndex] = uv;
        }
    }

    public void AddTriangle(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;
            borderTriangleIndex += 3; // Move to the next set of triangle indices
        }
        else
        {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3; // Move to the next set of triangle indices    
        }

    }

    Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        int triangleCount = triangles.Length / 3;
        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndecise(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        int borderTriangleCount = borderTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTriangleIndex];
            int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
            int vertexIndexC = borderTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndecise(vertexIndexA, vertexIndexB, vertexIndexC);
            if (vertexIndexA >= 0)
            {
                vertexNormals[vertexIndexA] += triangleNormal;
            }
            if (vertexIndexB >= 0)
            {
                vertexNormals[vertexIndexB] += triangleNormal;
            }
            if (vertexIndexC >= 0)
            {
                vertexNormals[vertexIndexC] += triangleNormal;
            }
        }
        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }
        return vertexNormals;
    }

    Vector3 SurfaceNormalFromIndecise(int indexA, int indexB, int indexC)
    {
        Vector3 pointA = (indexA < 0) ? borderVertices[-indexA - 1] : vertices[indexA];
        Vector3 pointB = (indexB < 0) ? borderVertices[-indexB - 1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? borderVertices[-indexC - 1] : vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals = CalculateNormals();
        return mesh; // Return the generated mesh
    }
}
