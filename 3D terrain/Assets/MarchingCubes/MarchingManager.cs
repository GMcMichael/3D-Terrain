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

    [Header("Player Settings")]
    public Transform playerCamera;
    public Vector2Int viewDistance = new Vector2Int(5, 2);//Horizontal viewDistance, Vertical viewDistance
    
    [Header("World Settings")]
    #region World Settings
    public Vector3Int numChunks = Vector3Int.one;
    public int chunkSize = 10;
    public int chunkGenMax = 10;//Change to allow for lower framerate if needed. Maybe instead lower mesh quality at high speeds exponentally to a min quality
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
    #endregion

    //List<MarchingChunk> chunks;
    Dictionary<Vector3Int, MarchingChunk> chunks;
    Queue<MarchingChunk> chunkGenQueue;

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

    void Awake() {
        Init();
    }

    void Update() {
        if(!fixedMapSize) {
            CreateVisibleChunks();//Update the terrain as player moves
            GenerateChunkQueue();
        }

        if(settingsChanged) UpdateAllChunks();
    }

    public void Init() {
        sphereNoise = GetComponent<SphereNoise>();
        planeNoise = GetComponent<PlaneNoise>();
        terrainNoise = GetComponent<TerrainNoise>();
        testingNoise = GetComponent<TestingNoise>();
        noise3D = GetComponent<Noise3D>();

        chunks = new Dictionary<Vector3Int, MarchingChunk>();
        chunkGenQueue = new Queue<MarchingChunk>();
    }

    private async void GenerateChunkQueue() {
        if(chunkGenQueue.Count > 0)
            for (int i = 0; i < Mathf.Min(chunkGenMax, chunkGenQueue.Count); i++)
            {
                UpdateChunkMesh(chunkGenQueue.Dequeue());
            }
        await System.Threading.Tasks.Task.Yield();
    }

    private void CreateVisibleChunks() {
        if(chunks == null) Init();
        //set all chunks into list of old chunks
        Dictionary<Vector3Int, MarchingChunk> oldChunks = new Dictionary<Vector3Int, MarchingChunk>(chunks);

        //Get chunk coord of player
        Vector3 pCC = playerCamera.position / chunkSize;
        Vector3Int playerChunkCoord = new Vector3Int(Mathf.RoundToInt(pCC.x), Mathf.RoundToInt(pCC.y), Mathf.RoundToInt(pCC.z));

        //go from player chunk coord -viewDistance to player chunk coord +viewDistance (viewDistance is (Horizonal Range, Vertical Range) )
        for (int x = -viewDistance.x; x <= viewDistance.x; x++)
        {
            for (int y = -viewDistance.y; y <= viewDistance.y; y++)
            {
                for (int z = -viewDistance.x; z <= viewDistance.x; z++)
                {
                    //Get current chunk coord
                    Vector3Int chunkCoord = new Vector3Int(x,y,z) + playerChunkCoord;

                    //check if chunk is in old chunks, if it is add it to chunks dictonary. If it isnt, make the chunk and add to dictonary
                    MarchingChunk chunk;

                    if(oldChunks.TryGetValue(chunkCoord, out chunk)) {
                        //chunk exists, remove from old dictonary
                        oldChunks.Remove(chunkCoord);
                        continue;
                    } else {
                        //chunk doesnt exist, create chunk and add to dictonary
                        chunk = CreateChunk(chunkCoord, centerWorld);
                        chunks.Add(chunkCoord, chunk);
                    }
                }
            }
        }
        
        //remove all old chunks from current dictonary
        foreach (Vector3Int chunkCoord in oldChunks.Keys)
        {
            chunks.Remove(chunkCoord);
            oldChunks[chunkCoord].DestoryOrDisable();
        }
    }

    public void GenerateFixedMap() {
        Init();
        InitChunks();
    }

    private void InitChunks() {
        if(chunks == null) Init();
        Dictionary<Vector3Int, MarchingChunk> oldChunks = new Dictionary<Vector3Int, MarchingChunk>();
        foreach (MarchingChunk chunk in FindObjectsOfType<MarchingChunk>())
        {
            oldChunks.Add(chunk.coord, chunk);
        }

        //go through all coords and create chunk if it doesnt exist
        for (int x = 0; x < numChunks.x; x++)
        {
            for (int y = 0; y < numChunks.y; y++)
            {
                for (int z = 0; z < numChunks.z; z++)
                {
                    Vector3Int coord = new Vector3Int(x, y, z);

                    MarchingChunk chunk;

                    if(oldChunks.TryGetValue(coord, out chunk)) {
                        //chunks exists, remove from old dictonary
                        oldChunks.Remove(coord);
                    } else {
                        //chunk doesnt exist, create chunk
                        chunk = CreateChunk(coord, centerWorld);
                    }

                    //Setup chunk and add to dictonary
                    chunks.Add(coord, chunk);
                }
            }
        }

        //Delete unused chunks
        foreach (MarchingChunk value in oldChunks.Values)
        {
            value.DestoryOrDisable();
        }
    }

    MarchingChunk CreateChunk(Vector3Int coord, bool centerChunks = false) {
        Vector3 position = coord*chunkSize;
        if(fixedMapSize && centerChunks) position = (coord*chunkSize)-((numChunks * chunkSize)/2);
        GameObject chunkObj = Instantiate(chunkObject, Vector3.zero, Quaternion.identity, transform);
        chunkObj.name = "Chunk " + coord;
        MarchingChunk chunk = chunkObj.GetComponent<MarchingChunk>();
        if(fixedMapSize && centerChunks) chunk.SetUp(coord, chunkSize, true, centerChunks, numChunks);
        else chunk.SetUp(coord, chunkSize, true);
        if(Application.isPlaying) chunkGenQueue.Enqueue(chunk);
        else UpdateChunkMesh(chunk);
        return chunk;
    }

    private void UpdateChunkMesh(MarchingChunk chunk) {//Called async some places so need to check if chunks still exists
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

        if(chunk == null) return;
        //Vector3Int coord = chunk.coord;
        Vector3 worldCoords = chunk.ChunkWorldCoords();

        Vector3 worldSize = numChunks * chunkSize;

        float spacing = (float)chunkSize / (numPointsPerAxis - 1);

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

        if(chunk == null) return;

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

        if(chunk == null) return;

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

            if(chunk == null) return;
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

    public void UpdateAllChunks() {
        settingsChanged = false;
        foreach (MarchingChunk chunk in chunks.Values) {
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
