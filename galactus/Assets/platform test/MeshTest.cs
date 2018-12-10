using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshTest : MonoBehaviour {

    private void OnValidate()
    {
        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
        if(mesh == null) {
            mesh = new Mesh();
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }
        mesh.Clear();
        Vector3[] verts = { Vector3.up, Vector3.down, Vector3.right, Vector3.forward };
        int[] tris = { 0,1,2,  1,2,3,  0,1,3,  0,2,3 };
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        Debug.Log("recalcled");
    }

}
