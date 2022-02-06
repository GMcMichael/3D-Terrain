using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SebastianMarching : MonoBehaviour
{
    public ComputeShader marching;
    public GameObject chunkObject;
    public bool fixedMapSize;
    public Vector3Int numChunks = Vector3Int.one;

    public int seed;
    public float surface = 0, noiseFrequency = 1, noiseAmplitude = 1;
    public Vector3 noiseOffset = Vector3.zero;

    [Range (2, 100)]
    public int numPointsPerAxis = 30;
    public int chunkSize = 10;
    List<MarchingChunk> chunks;
    Dictionary<Vector3Int, MarchingChunk> existingChunks;

    private int threadGroupSize = 8;

    ComputeBuffer triangleBuffer;
    ComputeBuffer pointsBuffer;
    ComputeBuffer triCountBuffer;
    Noise noise;

    int indexFromCoord(int x, int y, int z) {
        return z * numPointsPerAxis * numPointsPerAxis + y * numPointsPerAxis + x;
    }

    public void GenerateFixedMap() {
        InitChunks();
        UpdateAllChunks();
    }

    private void InitChunks() {
        chunks = new List<MarchingChunk>();
        List<MarchingChunk> oldChunks = new List<MarchingChunk>(FindObjectsOfType<MarchingChunk> ());

        //go through all coords and create chunk if it doesnt exist
        for (int x = 0; x < numChunks.x; x++)
        {
            for (int y = 0; y < numChunks.y; y++)
            {
                for (int z = 0; z < numChunks.z; z++)
                {
                    Vector3Int coord = new Vector3Int(x, y, z);
                    bool chunkExists = false;

                    //if chunk exists, add to list
                    for (int i = 0; i < oldChunks.Count; i++)
                    {
                        if(oldChunks[i].coord == coord) {
                            chunks.Add(oldChunks[i]);
                            oldChunks.RemoveAt(i);
                            chunkExists = true;
                            break;
                        }
                    }

                    //create chunk
                    if(!chunkExists) {
                        MarchingChunk newChunk = CreateChunk(coord);
                        chunks.Add(newChunk);
                    }

                    chunks[chunks.Count - 1].SetUp(coord, chunkSize, true);
                }
            }
        }

        //Delete unused chunks
        for (int i = 0; i < oldChunks.Count; i++)
        {
            oldChunks[i].DestoryOrDisable();
        }
    }

    MarchingChunk CreateChunk(Vector3Int coord) {
        GameObject chunkObj = Instantiate(chunkObject, (coord*chunkSize), Quaternion.identity, transform);
        MarchingChunk chunk = chunkObj.GetComponent<MarchingChunk>();
        chunk.SetUp(coord, chunkSize, true);
        return chunk;
    }

    private void UpdateChunkMesh(MarchingChunk chunk) {
        int numVoxelsPerAxis = numPointsPerAxis - 1;
        int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / (float)threadGroupSize);
        float pointSpacing = chunkSize / (numPointsPerAxis - 1);

        int numPoints = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
        int maxTriangleCount = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis * 5;

        if(triangleBuffer != null) {
            triangleBuffer.Release();
            pointsBuffer.Release();
            triCountBuffer.Release();
        }

        triangleBuffer = new ComputeBuffer (maxTriangleCount, sizeof (float) * 3 * 3, ComputeBufferType.Append);
        pointsBuffer = new ComputeBuffer (numPoints, sizeof (float) * 4);
        triCountBuffer = new ComputeBuffer (1, sizeof (int), ComputeBufferType.Raw);

        Vector3Int coord = chunk.coord;
        Vector3 worldCoords = chunk.ChunkWorldCoords();

        Vector3 worldBounds = new Vector3(numChunks.x, numChunks.y, numChunks.z) * chunkSize;

        //generate noise
        Vector4[] noiseMap = GenerateNoise(numPoints, worldCoords);

        pointsBuffer.SetData(noiseMap);

        triangleBuffer.SetCounterValue(0);
        marching.SetBuffer(0, "points", pointsBuffer);
        marching.SetBuffer(0, "triangles", triangleBuffer);
        marching.SetInt("numPointsPerAxis", numPointsPerAxis);
        marching.SetFloat("surface", surface);

        marching.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        //Get number of triangles in triangle buffer
        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] triCountArray = {0};
        triCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];

        //get triangle data from shader
        Triangle[] tris = new Triangle[numTris];
        triangleBuffer.GetData(tris, 0, 0, numTris);

        Debug.Log("NumTris: " + numTris);

        /*List<Vector3> verticies = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < numTris; i++) {
            for (int j = 0; j < 3; j++) {
                //meshTriangles[i * 3 + j] = i * 3 + j;
                //vertices[i * 3 + j] = tris[i][j];
                verticies.Add(tris[i][j]);
                triangles.Add(verticies.Count - 1);
            }
        }*/

        var verticies = new Vector3[numTris * 3];
        var triangles = new int[numTris * 3];

        for (int i = 0; i < numTris; i++) {
            for (int j = 2; j >= 0; j--) {
                triangles[i * 3 + j] = i * 3 + j;
                verticies[i * 3 + j] = tris[i][(3-(j+1))];
            }
        }

        //Create mesh from triangles
        MarchingChunk.MeshData meshData = new MarchingChunk.MeshData(verticies, triangles);

        List<MarchingChunk.MeshData> meshes = new List<MarchingChunk.MeshData>();
        meshes.Add(meshData);

        chunk.UpdateMeshes(meshes);
    }

    Vector4[] GenerateNoise(int numPoints, Vector3 worldCoords) {
        if(noise == null) noise = new Noise(seed);
        Vector4[] noiseMap = new Vector4[numPoints];

        float spacing = chunkSize / (numPointsPerAxis - 1);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    //maybe need to add world coords
                    Vector3 pos = worldCoords + (new Vector3(x,y,z) * spacing);//(spacing - chunkSize/2);
                    int index = indexFromCoord(x,y,z);
                    noiseMap[index] = new Vector4(pos.x, pos.y, pos.z, Evaluate(pos));
                }
            }
        }

        return noiseMap;
    }

    float Evaluate(Vector3 pos) {
        float value = 0;
        float radius = 10;
        int resolution = 2;

        //---- SPHERE ----
        value = radius - pos.magnitude;

        //--- NOISE ----
        float _x = pos.x/(resolution-1);
        float _y = pos.y/(resolution-1);
        float _z = pos.z/(resolution-1);
        value = noise.Evaluate((new Vector3(_x,_y,_z)) * noiseFrequency) * noiseAmplitude;

        return value;
    }

    private void UpdateAllChunks() {
        foreach (MarchingChunk chunk in chunks) {
            UpdateChunkMesh(chunk);
        }
    }

    struct Triangle {
        #pragma warning disable 649 // disable unassigned variable warning
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;

        public Vector3 this [int i] {
            get {
                switch (i) {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    default:
                        return c;
                }
            }
        }
    }
}
