using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour {

    [Range(2,256)]
    public int resolution = 10;
    public enum FaceRenderMask { top, bottom, left, right, front, back, all }
    [ContextMenuItem("Fold or Unfold Cube", "ToggleFoldCubeMesh")]
    public FaceRenderMask faceRenderMask = FaceRenderMask.all;

    public ShapeSettings shapeSettings;
    public ColorSettings colorSettings;

    [HideInInspector]
    public bool shapeSettingsFoldout = true, colorSettingsFoldout = true, meshUnfolded = false;
    public bool autoUpdate = true;

    ShapeGenerator shapeGenerator = new ShapeGenerator();
    ColorGenerator colorGenerator = new ColorGenerator();

    [SerializeField, HideInInspector]
    MeshFilter[] meshFilters;
    TerrainFace[] terrainFaces;

    void Initialize() {
        shapeGenerator.UpdateSettings(shapeSettings);
        colorGenerator.UpdateSettings(colorSettings);
        const int countFacesOnCube = 6;
        if (meshFilters == null || meshFilters.Length == 0)
        {
            meshFilters = new MeshFilter[countFacesOnCube];
        }
        terrainFaces = new TerrainFace[countFacesOnCube];
        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

        for (int i = 0; i < countFacesOnCube; ++i)
        {
            if (meshFilters[i] == null)
            {
                GameObject meshObj = new GameObject(((FaceRenderMask)i).ToString());
                meshObj.transform.parent = transform;

                meshObj.AddComponent<MeshRenderer>();
                meshFilters[i] = meshObj.AddComponent<MeshFilter>();
                meshFilters[i].sharedMesh = new Mesh();
                meshFilters[i].mesh = meshFilters[i].sharedMesh;
            }
            meshFilters[i].GetComponent<MeshRenderer>().sharedMaterial = colorSettings.planetMaterial;

            terrainFaces[i] = new TerrainFace(shapeGenerator, meshFilters[i].sharedMesh, resolution, directions[i]);
            bool renderFace = faceRenderMask == FaceRenderMask.all || (int)faceRenderMask == i;
            meshFilters[i].gameObject.SetActive(renderFace);
        }
    }

    private struct FOffset
    {
        public Vector3 offset;
        public Vector3 eulerRot;
        public FOffset(Vector3 offset, Vector3 eulerRot) { this.offset = offset; this.eulerRot = eulerRot; }
    }
    static FOffset[] fOffsets = { 
        new FOffset(Vector3.zero,  Vector3.zero), // top
        new FOffset(Vector3.back*2,Vector3.right*180), // bottom
        new FOffset(Vector3.left,  Vector3.forward*-90), // left
        new FOffset(Vector3.right, Vector3.forward*90), // right
        new FOffset(Vector3.forward,Vector3.right*-90), // front
        new FOffset(Vector3.back,  Vector3.right*90), // back
    };
    public void UnfoldCubeMesh() {
        float rad = shapeSettings.planetRadius;
        for (int i = 0; i < meshFilters.Length; ++i) {
            meshFilters[i].gameObject.transform.localPosition = fOffsets[i].offset * rad * 1.5f;
            meshFilters[i].gameObject.transform.localEulerAngles = fOffsets[i].eulerRot;
        }
    }
    public void RefoldCubeMesh() {
        for (int i = 0; i < meshFilters.Length; ++i) {
            meshFilters[i].gameObject.transform.localPosition = Vector3.zero;
            meshFilters[i].gameObject.transform.localEulerAngles = Vector3.zero;
        }
    }
    public void ToggleFoldCubeMesh() {
        if(!meshUnfolded) {
            UnfoldCubeMesh();
        } else {
            RefoldCubeMesh();
        }
        meshUnfolded = !meshUnfolded;
    }

    public void GeneratePlanet()
    {
        Initialize();
        GenerateMesh();
        GenerateColors();
    }

    public void OnShapeSettingsUpdated() {
        if (autoUpdate)
        {
            Initialize();
            GenerateMesh();
        }
    }

    public void OnColorSettingsUpdated() {
        if (autoUpdate)
        {
            Initialize();
            GenerateColors();
        }
    }

    void GenerateMesh() {
        for (int i = 0; i < meshFilters.Length; ++i) {
            if(meshFilters[i].gameObject.activeSelf) {
                terrainFaces[i].ConstructMesh();
            }
        }
        colorGenerator.UpdateElevation(shapeGenerator.elevationMinMax);
    }

    void GenerateColors() {
        colorGenerator.UpdateColors();
        for (int i = 0; i < meshFilters.Length; ++i)
        {
            if (meshFilters[i].gameObject.activeSelf)
            {
                terrainFaces[i].UpdateUVs(colorGenerator);
            }
        }
    }
}
