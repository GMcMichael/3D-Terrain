using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarchingManager : MonoBehaviour
{
    /*public enum NoiseType {
        Noise,
        NoiseStretch,
        Sphere,
        Terrain,
        Testing
    }*/
    public ComputeShader marching;
    public GameObject chunkObject;
    public bool DebugInfo = false;
    //public NoiseType noiseType = NoiseType.Terrain;
    public int seed;
    public float surface = 0, noiseFrequency = 1, noiseAmplitude = 1;
    public int noiseOctaves = 3;
    public bool hasFloor = false;
    public float floorLevel = -13;
    public float radius = 1;
    public bool fixedMap;
    public Vector3Int worldSize = Vector3Int.one;


    ComputeBuffer noiseBuffer, vertsBuffer, triangleBuffer, triCountBuffer, edgeIndex;

    public int octaves = 1;
    private const int numThreads = 8;//CHANGE IF COMPUTE SHADER CHANGES

    public int viewDistance = 100;
    //this is actual size of chunks, chunks are squares
    public int chunkSize = 10;

    public Transform playerCamera;
    private List<MarchingChunk> chunks = new List<MarchingChunk>();
    private Dictionary<Vector3Int, MarchingChunk> currChunks;
    private Noise noise;

    public int resolution = 10;

    void SetUp() {
        noise = new Noise(seed);
    }

    void Update() {
        CreateVisibleChunks();
    }

    public void CreateFixedMap() {
        if(chunks == null) chunks = new List<MarchingChunk>();

        //destroy any existing chunks
        foreach (MarchingChunk chunk in chunks)
        {
            try {
                DestroyImmediate(chunk.gameObject, false);
            } catch {
                Debug.Log("Failed to delete chunks. Reattempting");
                foreach (MarchingChunk chunkObj in new List<MarchingChunk> (FindObjectsOfType<MarchingChunk>()))
                {
                    DestroyImmediate(chunkObj.gameObject, false);
                }
                break;
            }
        }

        chunks.Clear();

        // Go through all coords and create a chunk there
        for (int x = 0; x < worldSize.x; x++) {
            for (int y = 0; y < worldSize.y; y++) {
                for (int z = 0; z < worldSize.z; z++) {
                    Vector3Int coord = new Vector3Int (x, y, z);
                    // Create new chunk
                    chunks.Add(CreateChunk(coord));
                }
            }
        }

        foreach (MarchingChunk chunk in chunks)
        {
            UpdateChunk(chunk);
        }
    }

    MarchingChunk CreateChunk(Vector3Int coord) {
        Transform chunkObj = Instantiate(chunkObject, (coord * chunkSize), Quaternion.identity).transform;
        chunkObj.parent = transform;
        MarchingChunk chunk = chunkObj.GetComponent<MarchingChunk>();
        chunk.SetUp(coord, chunkSize, true);

        return chunk;
    }

    void CreateVisibleChunks() {//could maybe turn into compute shader if needed
        //get camera coords
        Vector3 camPos = playerCamera.position;
        Vector3 camChunk = camPos / chunkSize;
        //cameraCoord is in chunk coords not world coords
        Vector3Int cameraCoord = new Vector3Int(Mathf.RoundToInt(camChunk.x), Mathf.RoundToInt(camChunk.y), Mathf.RoundToInt(camChunk.z));
        
        //check maxChunks I could see by doing viewDistance / chunkSize
        int maxChunks = Mathf.CeilToInt(viewDistance / chunkSize);
        //square view distance for faster calculations
        float sqrViewDistance = viewDistance * viewDistance;

        //go through existing chunks and recycle/remove if outside viewDistance+bufferDist
        for (int i = chunks.Count-1; i >= 0; i--)//have to go backwards to not break loop while removing
        {
            MarchingChunk chunk = chunks[i];
            Vector3 worldCoord = chunk.ChunkWorldCoords();
            Vector3 cameraOffset = camPos - worldCoord;
            Vector3 absOff = new Vector3(Mathf.Abs(cameraOffset.x), Mathf.Abs(cameraOffset.y), Mathf.Abs(cameraOffset.z));
            float sqrDist = new Vector3(Mathf.Max(absOff.x, 0), Mathf.Max(absOff.y, 0), Mathf.Max(absOff.z, 0)).sqrMagnitude;//dont know if .max is needed
            if(sqrDist > sqrViewDistance) {
                DestroyChunk(i);
            }
        }

        //loop though all possible positions for new chunks (eg: maxChunks = 10, goes from (-10, -10, -10) to (10, 10, 10) around the camera position)
        for (int x = -maxChunks; x <= maxChunks; x++)
        {
            for (int y = -maxChunks; y <= maxChunks; y++)
            {
                for (int z = -maxChunks; z <= maxChunks; z++)
                {
                    Vector3Int coord = new Vector3Int(x, y, z) + cameraCoord;

                    if(currChunks.ContainsKey(coord)) continue;

                    Vector3 worldPos = coord*chunkSize;
                    Vector3 cameraOffset = camPos - worldPos;
                    Vector3 absOff = new Vector3(Mathf.Abs(cameraOffset.x), Mathf.Abs(cameraOffset.y), Mathf.Abs(cameraOffset.z));
                    float sqrDist = new Vector3(Mathf.Max(absOff.x, 0), Mathf.Max(absOff.y, 0), Mathf.Max(absOff.z, 0)).sqrMagnitude;//dont know if .max is needed
                    
                    if(sqrDist <= sqrViewDistance) {
                        //Chunk is within viewDistance so create it
                        MarchingChunk chunk = CreateChunk(coord);
                        currChunks.Add(coord, chunk);
                        chunks.Add(chunk);
                        UpdateChunk(chunk);
                    }
                }
            }
        }
    }

    void UpdateChunk(MarchingChunk chunk) {
        int voxelsPerAxis = chunkSize - 1;//try removing the -1 maybe
        int threads = Mathf.CeilToInt(voxelsPerAxis / (float)numThreads);
        int bufferSize = chunkSize * chunkSize * chunkSize * 3 * 5;//max number of verts (maybe need to do -1 on width, depth, and height)

        Vector3 worldCoord = chunk.ChunkWorldCoords();

        //generate noise for the chunk
        Vector4[] noiseMap = GenerateNoise(worldCoord);

        //create noise buffer and add the noise data
        noiseBuffer = new ComputeBuffer(chunkSize * chunkSize * chunkSize, sizeof(float)*4);
        noiseBuffer.SetData(noiseMap);

        //holds the verts generated by the marching cubes
        vertsBuffer = new ComputeBuffer(bufferSize, sizeof(float)*3);

        //populate vertsBuffer with -1 so when I read it later I can stop the cube when I reach -1
        float[] empty = new float[bufferSize*3];
        for (int i = 0; i < bufferSize*3; i++)
            empty[i] = -1.0f;

        vertsBuffer.SetData(empty);

        //hold triangles
        triangleBuffer = new ComputeBuffer((bufferSize/5), sizeof(float)*3*3, ComputeBufferType.Append);
        triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        edgeIndex = new ComputeBuffer(1, sizeof(int));

        //Set up marching compute shader and run it
        triangleBuffer.SetCounterValue(0);
        marching.SetInt("width", chunkSize);
        marching.SetInt("height", chunkSize);
        marching.SetInt("depth", chunkSize);
        marching.SetInt("octaves", octaves);
        marching.SetFloat("surface", surface);
        marching.SetBuffer(0, "noise", noiseBuffer);
        marching.SetBuffer(0, "verts", vertsBuffer);
        marching.SetBuffer(0, "triangles", triangleBuffer);
        marching.SetBuffer(0, "edgeIndex", edgeIndex);
        if(DebugInfo) Debug.Log("Threads: " + threads);
        marching.Dispatch(0, threads, threads, threads);

        //turn buffers into mesh data and send to chunk
        //chunk.UpdateMeshes(ReadMeshData(bufferSize));

        int[] edgeIndexes = new int[1];
        edgeIndex.GetData(edgeIndexes);

        Debug.Log("Edge Index: " + edgeIndexes[0]);

        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] triCountArray = {0};
        triCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];

        chunk.UpdateMeshes(ReadMeshTriangleData(numTris));

        //remove buffers because I got the data from them
        noiseBuffer.Dispose();
        vertsBuffer.Dispose();

        triangleBuffer.Dispose();
        triCountBuffer.Dispose();
    }

    public List<MarchingChunk.MeshData> ReadMeshTriangleData(int numTris) {
        Triangle[] tris = new Triangle[numTris];
        triangleBuffer.GetData(tris, 0, 0, numTris);

        List<Vector3> verticies = new List<Vector3>();
        List<int> triangles = new List<int>();

        List<MarchingChunk.MeshData> meshes = new List<MarchingChunk.MeshData>();

        //extract triangles and verticies from the buffer
        int index = 0;
        int maxTris = 65000/3;
        for (int i = 0; i < numTris; i++)
        {

            for (int j = 2; j >= 0; j--)
            {
                verticies.Add(tris[i][j]);
                triangles.Add(index++);
            }

            
            if(index >= maxTris) {
                meshes.Add(new MarchingChunk.MeshData(verticies, triangles));
                if(DebugInfo) Debug.Log("1: " + verticies.Count + ", " + triangles.Count);
                index = 0;
                verticies.Clear();
                triangles.Clear();
            }
        }

        if(verticies.Count != 0) {
            meshes.Add(new MarchingChunk.MeshData(verticies, triangles));
            if(DebugInfo) Debug.Log("2: " + verticies.Count + ", " + triangles.Count);
            verticies.Clear();
            triangles.Clear();
        }

        return meshes;
    }

    public List<MarchingChunk.MeshData> ReadMeshData(int bufferSize) {//Check data here, no mesh data is being sent---------------------
        Vector3[] computeVerticies = new Vector3[bufferSize];
        
        vertsBuffer.GetData(computeVerticies);

        List<Vector3> verticies = new List<Vector3>();
        List<int> triangles = new List<int>();
        //maybe do normals somehow too

        List<MarchingChunk.MeshData> meshes = new List<MarchingChunk.MeshData>();

        //extract triangles and verticies from the buffer
        int testCount = 0;
        int index = 0;
        int maxTris = 65000/3;
        Vector3 negOne = new Vector3(-1, -1, -1);
        for (int i = 0; i < bufferSize; i++)
        {
            /*Debug.Log("Compute: " + computeVerticies[i]);
            Debug.Log("NegOne: " + negOne);
            Debug.Log("Not Equal: " + (computeVerticies[i] != negOne));*/

            //if generated vert, value will not be -1
            if(computeVerticies[i]  != negOne) {
                //Debug.Log("Added");
                verticies.Add(computeVerticies[i]);
                triangles.Add(index++);
            } else {
                testCount++;
            }

            
            if(index >= maxTris) {
                meshes.Add(new MarchingChunk.MeshData(verticies, triangles));
                if(DebugInfo) Debug.Log("1: " + verticies.Count + ", " + triangles.Count);
                index = 0;
                verticies.Clear();
                triangles.Clear();
            }
        }

        if(DebugInfo) Debug.Log("Buffer Size: " + bufferSize + ", Test Count: " + testCount);

        if(verticies.Count != 0) {
            meshes.Add(new MarchingChunk.MeshData(verticies, triangles));
            if(DebugInfo) Debug.Log("2: " + verticies.Count + ", " + triangles.Count);
            verticies.Clear();
            triangles.Clear();
        }

        return meshes;
    }

    Vector4[] GenerateNoise(Vector3 coord) {
        if(noise == null) noise = new Noise(seed);
        //generate the 3d noise and put into a 1d array
        Vector4[] noiseMap = new Vector4[chunkSize*chunkSize*chunkSize];

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {//index: x + width * (y + depth * z)
                    //int index = x + chunkSize * (y + chunkSize * z);
                    int index = indexFromCoord(x,y,z);
                    Vector3 noiseCoord = coord + (new Vector3(x,y,z)/chunkSize);
                    noiseMap[index] = new Vector4(noiseCoord.x, noiseCoord.y, noiseCoord.z, Evaluate(noiseCoord));
                    //Debug.Log("Coord: " + noiseCoord);
                    //Debug.Log(noiseMap[x + chunkSize * (y + chunkSize * z)]);
                }
            }
        }

        return noiseMap;
    }

    int indexFromCoord(int x, int y, int z) {
    return z * chunkSize * chunkSize + y * chunkSize + x;
}

    private float Evaluate(Vector3 point) {
        float value = 0;
        float _x, _y, _z;

        //----  TERRAIN ----
        /*value = -point.y+1;
        float nAmp = noiseAmplitude;
        float nFre = noiseFrequency;
        //Random.InitState((int)(point.x+point.y+point.z));
        for (int i = 0; i < noiseOctaves; i++)
        {
            value += noise.Evaluate(point*nFre)*nAmp;
            nAmp /= 2;
            nFre *= 1.95f;//GetFrequencyMultiplier();//pretty sure this is causing the "spikes" in the terrain
        }
        if(hasFloor) value += Mathf.Clamp01((floorLevel - point.y)*3)*40;*/

        //----  SPHERES ----
        point -= worldSize/2;
        value = radius-Mathf.Sqrt(Mathf.Pow(point.x,2)+Mathf.Pow(point.y,2)+Mathf.Pow(point.z,2));

        //--- NOISE ----
        /*_x = point.x/(resolution-1);
        _y = point.y/(resolution-1);
        _z = point.z/(resolution-1);
        value = noise.Evaluate((new Vector3(_x,_y,_z)) * noiseFrequency);*/

        return value;


        /*switch(noiseType) {
            case NoiseType.Noise:
                _x = point.x/(resolution-1);
                _y = point.y/(resolution-1);
                _z = point.z/(resolution-1);
                value = noise.Evaluate((new Vector3(_x,_y,_z)+noiseCenter) * noiseFrequency);
            break;
            case NoiseType.NoiseStretch:
                _x = point.x / (dimensions.x - 1.0f);
                _y = point.y / (dimensions.y - 1.0f);
                _z = point.z / (dimensions.z - 1.0f);
                value = noise.Evaluate(new Vector3(_x,_y,_z) * noiseFrequency + noiseCenter);
            break;
            case NoiseType.Sphere:
                point -= dimensions/2;
                value = radius-Mathf.Sqrt(Mathf.Pow(point.x,2)+Mathf.Pow(point.y,2)+Mathf.Pow(point.z,2));
            break;
            case NoiseType.Terrain://TODO: try and use trilinear interpolation when sampling the lowest octave or two (using full floating-point percision?).
                value = -point.y+1;
                float nAmp = noiseAmplitude;
                float nFre = noiseFrequency;
                //Random.InitState((int)(point.x+point.y+point.z));
                for (int i = 0; i < noiseOctaves; i++)
                {
                    value += noise.Evaluate(point*nFre)*nAmp;
                    nAmp /= 2;
                    nFre *= 1.95f;//GetFrequencyMultiplier();//pretty sure this is causing the "spikes" in the terrain
                }
                if(hasFloor) value += Mathf.Clamp01((floorLevel - point.y)*3)*40;
                //keep adding noise to value
            break;
            case NoiseType.Testing:
                //Get filter noise to multiply the heightmap noise by
                float filterFre = filterFrequency;
                float filterWeight = noise.Evaluate((filterCenter+point)*filterFre);
                //filterWeight is from -1 to 1 so I +1 then /2 to get from (-1 to 1) to (0 to 1)
                filterWeight++;
                filterWeight /= 2;
                //maybe could multiply by an amplitude here to make it larger if needed
                //Set value to flat plane (-point.y+1)
                value = -point.y+1;
                //get heightmap noise
                float heightValue = 0;
                float heightAmp = noiseAmplitude;
                float heightFre = noiseFrequency;
                for (int i = 0; i < noiseOctaves; i++)
                {
                    heightValue += noise.Evaluate(point*heightFre)*heightAmp;
                    heightAmp /= 2;
                    heightFre *= 1.95f;
                }
                //multiply heightmap noise by filter
                heightValue *= filterCurve.Evaluate(filterWeight);
                //add filtered heightmap to value
                value += heightValue;
            break;
        }

        return value;*/
    }

    void DestroyChunk(int index) {
        MarchingChunk chunk = chunks[index];
        chunks.RemoveAt(index);
        currChunks.Remove(chunk.coord);
        Destroy(chunk.gameObject);
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
