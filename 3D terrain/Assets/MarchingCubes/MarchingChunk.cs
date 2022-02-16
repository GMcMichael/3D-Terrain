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

    public void SetUp(Vector3Int _coord, int _chunkSize, bool generateCollider, bool centerWorld = false, Vector3? numChunks = null) {//only need numChunks if centering world
        this.coord = _coord;
        this.generateCollider = generateCollider;
        this.chunkSize = _chunkSize;

        Vector3 position = _coord * _chunkSize;
        if(centerWorld) position -= (((Vector3)numChunks * chunkSize)/2);

        transform.position = position;

        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();

        //remove current mesh
        meshFilter.sharedMesh = null;
        meshCollider.sharedMesh = null;

        //turn collider on or off if I want collisions
        meshCollider.enabled = generateCollider;
    }

    public void UpdateMeshes(MeshData meshData, bool debug) {
        mesh = meshData.CreateMesh(debug);//pass true to output debug data

        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;

        //force collider update
        if(generateCollider) {
            meshCollider.enabled = false;
            meshCollider.enabled = true;
        }
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
            if(LogInfo) {
                Debug.Log("Verticies Count: " + verticies.Length);
                Debug.Log("Triangles Count: " + triangles.Length);
                for (int i = 0; i < verticies.Length; i+=3)
                {
                    Debug.Log("Triangle: " + verticies[i] + ", " + verticies[i+1] + ", " + verticies[i+2]);
                }
            }
            Mesh mesh = new Mesh();
            mesh.vertices = verticies;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
