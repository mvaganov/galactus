using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainFace
{

    ShapeGenerator shapeGenerator;
    Mesh mesh;
    int resolution;
    Vector3 localUp, axisA, axisB;


    public TerrainFace(ShapeGenerator shapeGenerator, Mesh mesh, int resolution, Vector3 localUp)
    {
        this.shapeGenerator = shapeGenerator;
        this.mesh = mesh; this.resolution = resolution; this.localUp = localUp;
        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);
    }

    public void ConstructMesh()
    {
        Vector3[] vertices = new Vector3[resolution * resolution];
        int[] triangles = new int[(int)Mathf.Pow(resolution - 1, 2) * 6];
        int triIndex = 0;
        Vector2[] uv = mesh.uv;
        int originalVertCount = mesh.vertexCount;

        for (int row = 0; row < resolution; ++row)
        {
            for (int col = 0; col < resolution; ++col)
            {
                int i = col + row * resolution;
                Vector2 percent = new Vector2(col, row) / (resolution - 1);
                Vector3 pointOnUnitCube = localUp +
                    (percent.x - 0.5f) * 2 * axisA +
                    (percent.y - 0.5f) * 2 * axisB;
                Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;
                vertices[i] = shapeGenerator.CalculatePointOnPlanet(pointOnUnitSphere);//pointOnUnitCube;
                //Debug.Log(i + " " + pointOnUnitCube);
                if (col != resolution - 1 && row != resolution - 1)
                {
                    triangles[triIndex + 0] = i;
                    triangles[triIndex + 1] = i + resolution + 1;
                    triangles[triIndex + 2] = i + resolution;
                    triangles[triIndex + 3] = i;
                    triangles[triIndex + 4] = i + 1;
                    triangles[triIndex + 5] = i + resolution + 1;
                    triIndex += 6;
                }
            }
        }
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        if (originalVertCount == vertices.Length)
        {
            mesh.uv = uv;
        }
    }

    public void UpdateUVs(ColorGenerator colorGenerator)
    {
        Vector2[] uv = new Vector2[(int)Mathf.Pow(resolution, 2)];
        for (int row = 0; row < resolution; ++row)
        {
            for (int col = 0; col < resolution; ++col)
            {
                int i = col + row * resolution;
                Vector2 percent = new Vector2(col, row) / (resolution - 1);
                Vector3 pointOnUnitCube = localUp +
                    (percent.x - 0.5f) * 2 * axisA +
                    (percent.y - 0.5f) * 2 * axisB;
                Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;

                uv[i] = new Vector2(colorGenerator.BiomePercentFromPoint(pointOnUnitSphere),0);
            }
        }
        mesh.uv = uv;
    }
}