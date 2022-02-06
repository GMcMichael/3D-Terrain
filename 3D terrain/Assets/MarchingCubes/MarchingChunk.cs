using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class MarchingChunk : MonoBehaviour
{
    //chunk coords, not world coords
    public Vector3Int coord;

    [HideInInspector]
    public Mesh mesh;

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    MeshCollider meshCollider;
    bool generateCollider;
    int chunkSize;

    public void DestoryOrDisable() {
        if(Application.isPlaying) {
            mesh.Clear();
            gameObject.SetActive(false);
        } else {
            DestroyImmediate(gameObject, false);
        }
    }

    public Vector3 ChunkWorldCoords() {
        return transform.position;
    }

    public void SetUp(Vector3Int _coord, int _chunkSize, bool generateCollider) {
        this.coord = _coord;
        this.generateCollider = generateCollider;
        this.chunkSize = _chunkSize;

        transform.position = _coord * chunkSize;

        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();

        //turn collider on or off if I want collisions
        meshCollider.enabled = generateCollider;

        mesh = meshFilter.sharedMesh;
        if(mesh == null) {
            mesh = new Mesh();
            //mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;//do I need this?
            meshFilter.sharedMesh = mesh;
        }

        if(generateCollider) {
            if(meshCollider.sharedMesh == null) meshCollider.sharedMesh = mesh;
            //force collider update
            meshCollider.enabled = false;
            meshCollider.enabled = true;
        }
    }

    public void UpdateMeshes(List<MeshData> meshes) {
        Debug.Log(meshes.Count);
        mesh = meshes[0].CreateMesh(true);
    }

    public struct MeshData {
        private Vector3[] verticies;
        private int[] triangles;
        public MeshData(Vector3[] verticies, int[] triangles) {
            this.verticies = verticies;
            this.triangles = triangles;
        }

        public MeshData(List<Vector3> verticies, List<int> triangles) {
            this.verticies = verticies.ToArray();
            this.triangles = triangles.ToArray();
        }

        public Mesh CreateMesh(bool LogInfo = false) {
            if(LogInfo)
                Debug.Log("Verticies Count: " + verticies.Length);
            Mesh mesh = new Mesh();
            mesh.vertices = verticies;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
