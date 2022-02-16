using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarchingManager : MonoBehaviour
{
    public enum NoiseType {
        Plane,
        Noise,
        Sphere,
        Terrain,
        Testing
    }

    public ComputeShader marching;
    private SphereNoise sphereNoise;
    private PlaneNoise planeNoise;
    private TerrainNoise terrainNoise;
    private TestingNoise testingNoise;
    private Noise3D noise3D;
    public GameObject chunkObject;
    public bool debugInfo;
    [HideInInspector]
    public bool settingsChanged;
    
    [Header("World Settings")]
    #region World Settings
    public Vector3Int numChunks = Vector3Int.one;
    public int chunkSize = 10;
    [Range (2, 100)]
    public int numPointsPerAxis = 30;
    public bool autoUpdate, centerWorld = true, fixedMapSize;
    #endregion

    [Header("Noise Settings")]
    #region Noise Settings
    public NoiseType noiseType;
    public int seed;
    public float surface = 0;
    public int noiseOctaves = 1;
    public float noiseFrequency = 1, noiseAmplitude = 1, radius = 10, floorLevel = -13;
    public bool hasFloor = false;
    public Vector3 noiseOffset = Vector3.zero;
    Noise noise;
    #endregion

    List<MarchingChunk> chunks;
    Dictionary<Vector3Int, MarchingChunk> existingChunks;

    #region ComputeShaders
    private int threadGroupSize = 8;
    ComputeBuffer triangleBuffer;
    ComputeBuffer pointsBuffer;
    ComputeBuffer triCountBuffer;
    ComputeBuffer debugBuffer;
    ComputeBuffer debugCountBuffer;
    #endregion

    int indexFromCoord(int x, int y, int z) {
        return z * numPointsPerAxis * numPointsPerAxis + y * numPointsPerAxis + x;
    }

    public void Run() {
        settingsChanged = false;
        noise = new Noise(seed);
        sphereNoise = GetComponent<SphereNoise>();
        planeNoise = GetComponent<PlaneNoise>();
        terrainNoise = GetComponent<TerrainNoise>();
        testingNoise = GetComponent<TestingNoise>();
        noise3D = GetComponent<Noise3D>();
        if(fixedMapSize) GenerateFixedMap();
        else Debug.Log("Not implemented");
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
                        MarchingChunk newChunk = CreateChunk(coord, centerWorld);
                        chunks.Add(newChunk);
                    }

                    chunks[chunks.Count - 1].SetUp(coord, chunkSize, true, centerWorld, numChunks);
                }
            }
        }

        //Delete unused chunks
        for (int i = 0; i < oldChunks.Count; i++)
        {
            oldChunks[i].DestoryOrDisable();
        }
    }

    MarchingChunk CreateChunk(Vector3Int coord, bool centerChunks = false) {
        Vector3 position = coord*chunkSize;
        if(fixedMapSize && centerChunks) position = (coord*chunkSize)-((numChunks * chunkSize)/2);
        GameObject chunkObj = Instantiate(chunkObject, Vector3.zero, Quaternion.identity, transform);
        chunkObj.name = "Chunk " + coord;
        MarchingChunk chunk = chunkObj.GetComponent<MarchingChunk>();
        chunk.SetUp(coord, chunkSize, true, centerChunks, numChunks);
        return chunk;
    }

    private void UpdateChunkMesh(MarchingChunk chunk) {
        int numVoxelsPerAxis = numPointsPerAxis - 1;
        int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / (float)threadGroupSize);
        float pointSpacing = chunkSize / (numPointsPerAxis - 1);

        int numPoints = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
        int maxTriangleCount = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis * 5;

        ReleaseBuffers();

        //later, create buffers once and just replace the data
        triangleBuffer = new ComputeBuffer (maxTriangleCount, sizeof (float) * 3 * 3, ComputeBufferType.Append);
        pointsBuffer = new ComputeBuffer (numPoints, sizeof (float) * 4);
        triCountBuffer = new ComputeBuffer (1, sizeof (int), ComputeBufferType.IndirectArguments);

        //DEBUGGING
        if(debugInfo) {
            int maxDebugCount = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
            debugBuffer = new ComputeBuffer (maxDebugCount, sizeof (int) * 3, ComputeBufferType.Append);
            debugCountBuffer = new ComputeBuffer (1, sizeof (int), ComputeBufferType.IndirectArguments);
        }

        //Vector3Int coord = chunk.coord;
        Vector3 worldCoords = chunk.ChunkWorldCoords();

        Vector3 worldSize = numChunks * chunkSize;

        float spacing = (float)chunkSize / (numPointsPerAxis - 1);

        //generate noise
        /*Vector4[] noiseMap = GenerateNoise(numPoints, worldCoords, worldSize, spacing);

        pointsBuffer.SetData(noiseMap);*/

        //generate noise with compute shader
        pointsBuffer = GenerateNoise(pointsBuffer, numPoints, numPointsPerAxis, spacing, noiseOffset, worldCoords, worldSize);

        if(debugInfo) {
            Vector4[] noiseMap = new Vector4[numPoints];

            pointsBuffer.GetData(noiseMap);

            for (int i = 0; i < noiseMap.Length; i++)
            {
                Debug.Log(i + ": " + noiseMap[i].w);
            }
        }


        if(debugInfo) {
            marching.SetBuffer(0, "debuging", debugBuffer);
            debugBuffer.SetCounterValue(0);
        }

        triangleBuffer.SetCounterValue(0);
        marching.SetBuffer(0, "points", pointsBuffer);
        marching.SetBuffer(0, "triangles", triangleBuffer);
        marching.SetInt("numPointsPerAxis", numPointsPerAxis);
        marching.SetFloat("surface", surface);

        if(debugInfo) Debug.Log("Thread Groups: " + numThreadsPerAxis);
        marching.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        //get debug data
        if(debugInfo) {
            ComputeBuffer.CopyCount(debugBuffer, debugCountBuffer, 0);
            int[] debugCountArray = {0};
            debugCountBuffer.GetData(debugCountArray);
            int debugNum = debugCountArray[0];

            Vector3Int[] debugData = new Vector3Int[debugNum];
            debugBuffer.GetData(debugData);

            Debug.Log("Debug Count: " + debugNum);

            foreach (Vector3Int id in debugData)
            {
                Debug.Log("id: " + id);
            }
        }

        //Get number of triangles in triangle buffer
        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] triCountArray = new int[1] {0};
        triCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];

        if(numTris != 0) {
            //get triangle data from shader
            Triangle[] tris = new Triangle[numTris];
            triangleBuffer.GetData(tris);

            if(debugInfo) {
                Debug.Log("NumTris: " + numTris);

                /*for (int i = 0; i < numTris; i++)
                {
                    Debug.Log("Triangle " + i + ": " + tris[i][0] + ", " + tris[i][1] + ", " + tris[i][2]);
                }

                Debug.Log("BREAK\nBREAK\nBREAK\nBREAK\nBREAK");*/
            }

            List<Vector3> verticies = new List<Vector3>();
            List<int> triangles = new List<int>();

            for (int i = 0; i < numTris; i++) {
                for (int j = 2; j >= 0; j--) {
                    Vector3 vertex = tris[i][(3-(j+1))];
                    verticies.Add(vertex);
                    triangles.Add(verticies.Count-1);
                }
            }

            //Create mesh from triangles
            MarchingChunk.MeshData meshData = new MarchingChunk.MeshData(verticies, triangles);

            chunk.UpdateMeshes(meshData, debugInfo);
        }
        
        ReleaseBuffers();
    }

    ComputeBuffer GenerateNoise(ComputeBuffer _pointsBuffer, int _numPoints, int _numPointsPerAxis, float _spacing, Vector3 _noiseOffset, Vector3 _worldCoords, Vector3 _worldSize) {
        ComputeBuffer buffer = null;
        switch(noiseType) {
            default:
            case MarchingManager.NoiseType.Plane:
                buffer = planeNoise.Generate(_pointsBuffer, _numPoints, _numPointsPerAxis, _spacing, _noiseOffset, _worldCoords, _worldSize);
                break;
            case MarchingManager.NoiseType.Sphere:
                sphereNoise.radius = radius;
                buffer = sphereNoise.Generate(_pointsBuffer, _numPoints, _numPointsPerAxis, _spacing, _noiseOffset, _worldCoords, _worldSize);
                break;
            case MarchingManager.NoiseType.Terrain:
                terrainNoise.SetValues(hasFloor, floorLevel, noiseFrequency, noiseAmplitude, noiseOctaves);
                buffer = terrainNoise.Generate(_pointsBuffer, _numPoints, _numPointsPerAxis, _spacing, _noiseOffset, _worldCoords, _worldSize);
                break;
            case MarchingManager.NoiseType.Testing:
                testingNoise.testNum = radius;
                buffer = testingNoise.Generate(_pointsBuffer, _numPoints, _numPointsPerAxis, _spacing, _noiseOffset, _worldCoords, _worldSize);
                break;
            case MarchingManager.NoiseType.Noise:
                noise3D.SetValues(noiseFrequency, noiseAmplitude, noiseOctaves);
                buffer = noise3D.Generate(_pointsBuffer, _numPoints, _numPointsPerAxis, _spacing, _noiseOffset, _worldCoords, _worldSize);
                break;
        }

        return buffer;
    }

    void ReleaseBuffers() {
        if(triangleBuffer != null) {
            triangleBuffer.Release();
            triangleBuffer = null;
        }
        if(pointsBuffer != null) {
            pointsBuffer.Release();
            pointsBuffer = null;
        }
        if(triCountBuffer != null) {
            triCountBuffer.Release();
            triCountBuffer = null;
        }

        //DEBBUGING
        if(debugBuffer != null) {
            debugBuffer.Release();
            debugBuffer = null;
        }
        if(debugCountBuffer != null) {
            debugCountBuffer.Release();
            debugCountBuffer = null;
        }
    }

    Vector4[] GenerateNoise(int numPoints, Vector3 worldCoords, Vector3 worldSize, float spacing) {
        if(noise == null) noise = new Noise(seed);
        Vector4[] noiseMap = new Vector4[numPoints];

        if(debugInfo) Debug.Log("NumPoints: " + numPoints + ", Spacing: " + spacing);

        for (int x = 0; x < numPointsPerAxis; x++)
        {
            for (int y = 0; y < numPointsPerAxis; y++)
            {
                for (int z = 0; z < numPointsPerAxis; z++)
                {
                    Vector3 pos = worldCoords + noiseOffset + (new Vector3(x,y,z) * spacing) - (worldSize/2);
                    int index = indexFromCoord(x,y,z);
                    noiseMap[index] = new Vector4(x*spacing, y*spacing, z*spacing, Evaluate(pos));
                }
            }
        }

        return noiseMap;
    }

    float Evaluate(Vector3 pos) {
        float value = 0;

        switch(noiseType) {
            case NoiseType.Plane:
                value = -pos.y+1;
                break;
            case NoiseType.Sphere:
                //value = radius - pos.magnitude;
                pos += (numChunks*chunkSize)/2;
                value = radius-Mathf.Sqrt(Mathf.Pow(pos.x,2)+Mathf.Pow(pos.y,2)+Mathf.Pow(pos.z,2));
                break;
            case NoiseType.Noise:
                value = noise.Evaluate(pos * noiseFrequency) * noiseAmplitude;
                break;
            case NoiseType.Terrain:
                value = -pos.y+1;
                float nAmp = noiseAmplitude;
                float nFre = noiseFrequency;
                for (int i = 0; i < noiseOctaves; i++)
                {
                    value += noise.Evaluate(pos*nFre)*nAmp;
                    nAmp /= 2;
                    nFre *= 1.95f;
                }
                if(hasFloor) value += Mathf.Clamp01((floorLevel - pos.y)*3)*40;
                break;
            case NoiseType.Testing:
                value = -pos.y+1;
                break;
        }

        return value;
    }

    private void UpdateAllChunks() {
        foreach (MarchingChunk chunk in chunks) {
            UpdateChunkMesh(chunk);
        }
    }

    void OnValidate() {
        settingsChanged = true;
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
