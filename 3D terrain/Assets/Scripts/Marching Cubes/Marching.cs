using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Marching : MonoBehaviour
{
    #region Enums
    public enum PositionSpace {
        Local,
        World
    }
    public enum VertexMerging {
        Merge,
        None
    }
    public enum WindingOrder
    {
        Clockwise,
        Counter_Clockwise
    }
    public enum NoiseType {
        Noise,
        NoiseStretch,
        Sphere,
        Terrain,
        Testing
    }
    public PositionSpace positionSpace;
    public VertexMerging vertexMerging;
    public WindingOrder windingOrder;
    public NoiseType noiseType;
    #endregion

    #region PrivateVariables
    private int[] windingOrderTable;
    private float minValue;
    private float maxValue;
    private float[] cube;
    private Vector3[] edgeVerticies;
    private List<Vector3> verticies;
    private List<int> triangles;
    private Dictionary<string, int> existingVerticies;
    private MeshRenderer[] encasingRenderers;
    private bool valuesChanged;
    #endregion
    
    #region PublicVariables
    public float surface;
    public bool randSeed, randCenter, randFilterCenter, encloseNoise, enclosureInvisable, centerMesh, autoUpdate, LogInfo;
    public GameObject encasingObject;
    public int seed, width = 10, height = 10, depth = 10, resolution = 10;//make width, depth, and height a vector3 so its easier to pass
    public float scale = 1, noiseFrequency = 1, noiseAmplitude = 1, filterFrequency = 1;
    public AnimationCurve filterCurve;
    public int noiseOctaves = 3;
    public float warpFrequency = 1, warpAmplitude = 1, floorLevel = -13;
    public bool hasFloor = false;
    public Vector3 noiseCenter, filterCenter;
    public float radius;
    public Material meshMaterial;
    #endregion

    public void SetUp() {
        if(randSeed) seed = Random.Range(0, 100000);
        Random.InitState(seed);
        if(randCenter) noiseCenter = new Vector3(Random.Range(-100000, 100000),Random.Range(-100000, 100000),Random.Range(-100000, 100000));
        if(randFilterCenter) filterCenter = new Vector3(Random.Range(-100000, 100000), Random.Range(-100000, 100000), Random.Range(-100000, 100000));
        minValue = float.MaxValue;
        maxValue = float.MinValue;
        cube = new float[8];
        edgeVerticies = new Vector3[12];
        verticies = new List<Vector3>();
        triangles = new List<int>();
        if(windingOrder == WindingOrder.Clockwise)
            windingOrderTable = new int[] {0, 1, 2};
        else
            windingOrderTable = new int[] {2, 1, 0};
    }

    public void DisplayMeshes() {
        DisplayMeshes(GenerateMesh(seed, surface, width, height, depth), width, height, depth, scale);
    }

    public void DisplayMeshes(List<MeshData> meshes, int _width, int _height, int _depth, float _scale) {
        valuesChanged = false;
        for (int i = transform.childCount-1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
        if(LogInfo) Debug.Log("Displaying " + meshes.Count +" Meshes");
        Vector3 offset = Vector3.zero;
        if(centerMesh) offset = new Vector3(width/2f, height/2f, depth/2f);
        foreach (MeshData meshData in meshes)
        {
            GameObject mesh = new GameObject("Mesh");
            mesh.transform.position += transform.position-offset;
            mesh.transform.parent = transform;
            mesh.transform.localScale = Vector3.one*_scale;//*(1/frequency);
            mesh.layer = LayerMask.NameToLayer("World");
            Mesh newMesh = meshData.CreateMesh(LogInfo);
            MeshFilter meshFilter = mesh.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = mesh.AddComponent<MeshRenderer>();
            MeshCollider meshCollider = mesh.AddComponent<MeshCollider>();
            meshRenderer.material = meshMaterial;
            meshFilter.mesh = newMesh;
            meshCollider.sharedMesh = newMesh;
        }
        if(encloseNoise) AddBarriers(_width, _height, _depth, _scale);
    }

    private void AddBarriers(int _width, int _height, int _depth, float _scale) {
        int x = (int)(_width*_scale)/2;
        int y = (int)((_height-1)*_scale)/2;
        int z = (int)(_depth*_scale)/2;
        Vector3 offset = Vector3.zero;
        if(centerMesh) offset = new Vector3(_width/2f,_height/2f,_depth/2f);
        encasingRenderers = new MeshRenderer[] {
            Instantiate(encasingObject, new Vector3(x,0,z)+transform.position-offset, Quaternion.Euler(0,0,0), transform).GetComponent<MeshRenderer>(),
            Instantiate(encasingObject, new Vector3(x,(_height-1)*_scale,z)+transform.position-offset, Quaternion.Euler(180,0,0), transform).GetComponent<MeshRenderer>(),
            Instantiate(encasingObject, new Vector3(0,y,z)+transform.position-offset, Quaternion.Euler(0,0,-90), transform).GetComponent<MeshRenderer>(),
            Instantiate(encasingObject, new Vector3((_width-1)*_scale,y,z)+transform.position-offset, Quaternion.Euler(0,0,90), transform).GetComponent<MeshRenderer>(),
            Instantiate(encasingObject, new Vector3(x,y,0)+transform.position-offset, Quaternion.Euler(90,0,0), transform).GetComponent<MeshRenderer>(),
            Instantiate(encasingObject, new Vector3(x,y,(_depth-1)*_scale)+transform.position-offset, Quaternion.Euler(-90,0,0), transform).GetComponent<MeshRenderer>()
        };
        for (int i = 0; i < 6; i++) {
            encasingRenderers[i].gameObject.layer = LayerMask.NameToLayer("World");
            if(enclosureInvisable) encasingRenderers[i].enabled = false;
        }
        encasingRenderers[0].transform.localScale = new Vector3((_width*_scale)/10,1,(_depth*_scale)/10);
        encasingRenderers[1].transform.localScale = new Vector3((_width*_scale)/10,1,(_depth*_scale)/10);
        encasingRenderers[2].transform.localScale = new Vector3((_height*_scale)/10,1,(_depth*_scale)/10);
        encasingRenderers[3].transform.localScale = new Vector3((_height*_scale)/10,1,(_depth*_scale)/10);
        encasingRenderers[4].transform.localScale = new Vector3((_width*_scale)/10,1,(_height*_scale)/10);
        encasingRenderers[5].transform.localScale = new Vector3((_width*_scale)/10,1,(_height*_scale)/10);
    }

    public List<MeshData> GenerateMesh(int _seed, float _surface, int _width, int _height, int _depth) {
        /*_width *= (int)frequency;
        _height *= (int)frequency;
        _depth *= (int)frequency;*/
        SetUp();
        float[,,] noiseMap = Get3DNoise(_seed, _width, _height, _depth);
        existingVerticies = new Dictionary<string, int>();//Key: Vector position as string, Value: index in vertex array
        for (int x = 0; x < _width-1; x++)
        {
            for (int y = 0; y < _height-1; y++)
            {
                for (int z = 0; z < _depth-1; z++)
                {
                    //create cube from 8 verticies
                    for (int i = 0; i < 8; i++)
                    {
                        cube[i] = noiseMap[x+(int)CornerOffsets[i].x,y+(int)CornerOffsets[i].y,z+(int)CornerOffsets[i].z];
                    }
                    //march
                    March(x,y,z);
                }
            }
        }
        //split the meshes
        //must be divisible by 3
        int maxVerticiesPerMesh = 30000;
        if(maxVerticiesPerMesh%3 != 0) maxVerticiesPerMesh -= maxVerticiesPerMesh%3;
        int numMeshes = triangles.Count / maxVerticiesPerMesh + 1;//use triangles instead of verticies incase of removing duplicate verticies
        List<MeshData> meshes = new List<MeshData>();
        for (int i = 0; i < numMeshes; i++)
        {
            List<Vector3> splitVerticies = new List<Vector3>();
            List<int> splitTriangles = new List<int>();
            List<Vector2> splitUvs = new List<Vector2>();
            existingVerticies = new Dictionary<string, int>();

            for (int j = 0; j < maxVerticiesPerMesh; j++)
            {
                int index = i*maxVerticiesPerMesh+j;
                if(index < triangles.Count) {
                    if(vertexMerging == VertexMerging.Merge) {
                        if(existingVerticies.TryGetValue(GetHashCode(verticies[triangles[index]]), out int existingVertex)) {
                            //allready exists, add existing vertex to triangle array
                            splitTriangles.Add(existingVertex);
                        } else {
                            //doesn't exist, add to vertex list and existingVerticies
                            splitVerticies.Add(verticies[triangles[index]]);
                            //splitUvs.Add(uvs[triangles[index]]);//can probably generate uvs right here
                            Vector3 vertex = verticies[triangles[index]];
                            splitUvs.Add(new Vector2(vertex.x/_width, vertex.z/_depth));
                            existingVerticies.Add(GetHashCode(verticies[triangles[index]]), splitVerticies.Count-1);
                            splitTriangles.Add(splitVerticies.Count-1);
                        }
                    } else {
                        splitVerticies.Add(verticies[triangles[index]]);
                        splitTriangles.Add(j);
                    }
                } else break;
            }

            if(splitVerticies.Count == 0) break;

            meshes.Add(new MeshData(splitVerticies, splitTriangles, splitUvs));
        }
        return meshes;
    }

    public void March(float x, float y, float z) {
        int edgeFlagIndex = 0;
        //Find the vertices inside and outside the mesh
        for (int i = 0; i < 8; i++)
            if(cube[i] <= surface) edgeFlagIndex |= 1<<i;
        
        //Get the edges intersected by the mesh
        int edgeFlag = EdgeFlags[edgeFlagIndex];
        if(edgeFlag == 0) return;

        //Find the intersection between the surface and edge
        for (int i = 0; i < 12; i++)
        {
            //if intersection
            if((edgeFlag & (1<<i)) != 0) {
                float offset = ((cube[(int)EdgeConnections[i].y]-cube[(int)EdgeConnections[i].x]) == 0f) ? surface : (surface-cube[(int)EdgeConnections[i].x])/(cube[(int)EdgeConnections[i].y]-cube[(int)EdgeConnections[i].x]);
            
                edgeVerticies[i].x = x + (CornerOffsets[(int)EdgeConnections[i].x].x + offset * EdgeDirection[i].x);
                edgeVerticies[i].y = y + (CornerOffsets[(int)EdgeConnections[i].x].y + offset * EdgeDirection[i].y);
                edgeVerticies[i].z = z + (CornerOffsets[(int)EdgeConnections[i].x].z + offset * EdgeDirection[i].z);
                if(!existingVerticies.TryGetValue(GetHashCode(edgeVerticies[i]), out int existingVertex)) {
                    //doesn't exist, add to vertex list and existingVerticies
                    verticies.Add(edgeVerticies[i]);
                    existingVerticies.Add(GetHashCode(edgeVerticies[i]), verticies.Count-1);
                }
            }
        }

        //Create the triangles found, can only be 5 per cube
        for (int i = 0; i < 5; i++)
        {
            if(TriangleTable[edgeFlagIndex, 3*i] < 0) break;
            //int index = verticies.Count;
            
            if(windingOrder == WindingOrder.Clockwise) {
                for (int j = 0; j < 3; j++)
                {
                    int vertex = TriangleTable[edgeFlagIndex, 3*i+j];
                    existingVerticies.TryGetValue(GetHashCode(edgeVerticies[vertex]), out int VertexIndex);
                    triangles.Add(VertexIndex);//index + windingOrderTable[j]);
                }
            } else {
                for (int j = 2; j >= 0; j--)
                {
                    int vertex = TriangleTable[edgeFlagIndex, 3*i+j];
                    existingVerticies.TryGetValue(GetHashCode(edgeVerticies[vertex]), out int VertexIndex);
                    triangles.Add(VertexIndex);
                }
            }
        }
    }

    private string GetHashCode(Vector3 vertex) {
        string hash = "" + vertex.x + "," + vertex.y + "," + vertex.z;
        return hash;
    }

    //returns a 3d float array with values that should be in the range of [-1,1]
    private float[,,] Get3DNoise(int _seed, int _width, int _height, int _depth) {
        float[,,] noiseMap = new float[_width, _height, _depth];
        Noise noise = new Noise(_seed);
        minValue = float.MaxValue;
        maxValue = float.MinValue;
        for(int x = 0; x < _width; x++) {
            for(int y = 0; y < _height; y++) {
                for(int z = 0; z < _depth; z++) {
                    float value = Evaluate(noise, GetPosition(new Vector3(x, y, z)), new Vector3(_width, _height, _depth));
                    if(value > maxValue) maxValue = value;
                    if(value < minValue) minValue = value;
                    noiseMap[x,y,z] = value;
                }
            }
        }
        return noiseMap;
    }

    private Vector3 GetPosition(Vector3 point) {
        if(positionSpace == PositionSpace.World)
            return transform.TransformPoint(point);
        else
            return point;
    }

    private float Evaluate(Noise noise, Vector3 point, Vector3 dimensions) {
        float value = 0;
        float _x, _y, _z;
        switch(noiseType) {
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
        return value;
    }

    private Vector3 GetWarp(Vector3 point, Noise noise) {
        //This causes terraces
        /*point *= warpFrequency;
        float x = noise.Evaluate(new Vector3(point.x, 0, 0));
        float y = noise.Evaluate(new Vector3(0, point.y, 0));
        float z = noise.Evaluate(new Vector3(0, 0, point.z));
        Vector3 test = new Vector3(x,y,z)*warpAmplitude;
        return test;*/
        
        float warp = noise.Evaluate(point*warpFrequency)*warpAmplitude;
        return Vector3.one*warp;
    }

    private float GetFrequencyMultiplier() {
        float num = 0;
        while(num == 0) num = Random.Range(-0.05f, 0.05f);
        return num+2;
    }

    void OnValidate() {
        valuesChanged = true;
        if(seed < 0) seed = 0;
        if(scale < 1) scale = 1;
        if(width < 10) width = 10;
        if(height < 10) height = 10;
        if(depth < 10) depth = 10;
        if(resolution < 1) resolution = 1;
        if(noiseFrequency < 0) noiseFrequency = 0;
        if(noiseAmplitude < 0) noiseAmplitude = 0;
        if(noiseOctaves < 0) noiseOctaves = 0;
    }

    public void SetNoiseData(int _seed, float _frequency, float _amplitude, int _octaves, float _filterFrequency, Vector3 _filterCenter, AnimationCurve _filterCurve, float _warpFrequency, float _warpAmplitude, float _floorLevel, bool _hasFloor) {
        seed = _seed;
        noiseFrequency = _frequency;
        noiseAmplitude = _amplitude;
        noiseOctaves = _octaves;
        filterFrequency = _filterFrequency;
        filterCenter = _filterCenter;
        filterCurve = _filterCurve;
        warpFrequency = _warpFrequency;
        warpAmplitude =_warpAmplitude;
        floorLevel = _floorLevel;
        hasFloor = _hasFloor;
        OnValidate();
    }

    public void SetMaterial(Material _meshMaterial) {
        meshMaterial = _meshMaterial;
    }

    public bool ValuesChanged() {
        return valuesChanged;
    }

    public Vector3 GetDimensions() {
        return new Vector3(width*scale, height*scale, depth*scale);
    }

    public struct MeshData {
        private Vector3[] verticies;
        private int[] triangles;
        private Vector2[] uvs;
        public MeshData(Vector3[] verticies, int[] triangles, Vector2[] uvs) {
            this.verticies = verticies;
            this.triangles = triangles;
            this.uvs = uvs;
        }

        public MeshData(List<Vector3> verticies, List<int> triangles, List<Vector2> uvs) {
            this.verticies = verticies.ToArray();
            this.triangles = triangles.ToArray();
            this.uvs = uvs.ToArray();
        }

        public Mesh CreateMesh(bool LogInfo = false) {
            if(LogInfo)
                Debug.Log("Verticies Count: " + verticies.Length);
            Mesh mesh = new Mesh();
            mesh.vertices = verticies;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }

#region tables
private static readonly Vector3[] CornerOffsets = new Vector3[] {
    new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(1,1,0), new Vector3(0,1,0),
    new Vector3(0,0,1), new Vector3(1,0,1), new Vector3(1,1,1),  new Vector3(0,1,1)
};

private static readonly Vector2[] EdgeConnections = new Vector2[] {
    new Vector2(0,1), new Vector2(1,2), new Vector2(2,3), new Vector2(3,0),
    new Vector2(4,5), new Vector2(5,6), new Vector2(6,7), new Vector2(7,4),
    new Vector2(0,4), new Vector2(1,5), new Vector2(2,6), new Vector2(3,7)
};

private static readonly Vector3[] EdgeDirection = new Vector3[]
{
    new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(-1, 0, 0), new Vector3(0, -1, 0),
    new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(-1, 0, 0), new Vector3(0, -1, 0),
    new Vector3(0, 0, 1), new Vector3(0, 0, 1), new Vector3( 0, 0, 1), new Vector3(0,  0, 1)
};

private static readonly int[] EdgeFlags = new int[]
{
    0x000, 0x109, 0x203, 0x30a, 0x406, 0x50f, 0x605, 0x70c, 0x80c, 0x905, 0xa0f, 0xb06, 0xc0a, 0xd03, 0xe09, 0xf00, 
    0x190, 0x099, 0x393, 0x29a, 0x596, 0x49f, 0x795, 0x69c, 0x99c, 0x895, 0xb9f, 0xa96, 0xd9a, 0xc93, 0xf99, 0xe90, 
    0x230, 0x339, 0x033, 0x13a, 0x636, 0x73f, 0x435, 0x53c, 0xa3c, 0xb35, 0x83f, 0x936, 0xe3a, 0xf33, 0xc39, 0xd30, 
    0x3a0, 0x2a9, 0x1a3, 0x0aa, 0x7a6, 0x6af, 0x5a5, 0x4ac, 0xbac, 0xaa5, 0x9af, 0x8a6, 0xfaa, 0xea3, 0xda9, 0xca0, 
    0x460, 0x569, 0x663, 0x76a, 0x066, 0x16f, 0x265, 0x36c, 0xc6c, 0xd65, 0xe6f, 0xf66, 0x86a, 0x963, 0xa69, 0xb60, 
    0x5f0, 0x4f9, 0x7f3, 0x6fa, 0x1f6, 0x0ff, 0x3f5, 0x2fc, 0xdfc, 0xcf5, 0xfff, 0xef6, 0x9fa, 0x8f3, 0xbf9, 0xaf0, 
    0x650, 0x759, 0x453, 0x55a, 0x256, 0x35f, 0x055, 0x15c, 0xe5c, 0xf55, 0xc5f, 0xd56, 0xa5a, 0xb53, 0x859, 0x950, 
    0x7c0, 0x6c9, 0x5c3, 0x4ca, 0x3c6, 0x2cf, 0x1c5, 0x0cc, 0xfcc, 0xec5, 0xdcf, 0xcc6, 0xbca, 0xac3, 0x9c9, 0x8c0, 
    0x8c0, 0x9c9, 0xac3, 0xbca, 0xcc6, 0xdcf, 0xec5, 0xfcc, 0x0cc, 0x1c5, 0x2cf, 0x3c6, 0x4ca, 0x5c3, 0x6c9, 0x7c0, 
    0x950, 0x859, 0xb53, 0xa5a, 0xd56, 0xc5f, 0xf55, 0xe5c, 0x15c, 0x055, 0x35f, 0x256, 0x55a, 0x453, 0x759, 0x650, 
    0xaf0, 0xbf9, 0x8f3, 0x9fa, 0xef6, 0xfff, 0xcf5, 0xdfc, 0x2fc, 0x3f5, 0x0ff, 0x1f6, 0x6fa, 0x7f3, 0x4f9, 0x5f0, 
    0xb60, 0xa69, 0x963, 0x86a, 0xf66, 0xe6f, 0xd65, 0xc6c, 0x36c, 0x265, 0x16f, 0x066, 0x76a, 0x663, 0x569, 0x460, 
    0xca0, 0xda9, 0xea3, 0xfaa, 0x8a6, 0x9af, 0xaa5, 0xbac, 0x4ac, 0x5a5, 0x6af, 0x7a6, 0x0aa, 0x1a3, 0x2a9, 0x3a0, 
    0xd30, 0xc39, 0xf33, 0xe3a, 0x936, 0x83f, 0xb35, 0xa3c, 0x53c, 0x435, 0x73f, 0x636, 0x13a, 0x033, 0x339, 0x230, 
    0xe90, 0xf99, 0xc93, 0xd9a, 0xa96, 0xb9f, 0x895, 0x99c, 0x69c, 0x795, 0x49f, 0x596, 0x29a, 0x393, 0x099, 0x190, 
    0xf00, 0xe09, 0xd03, 0xc0a, 0xb06, 0xa0f, 0x905, 0x80c, 0x70c, 0x605, 0x50f, 0x406, 0x30a, 0x203, 0x109, 0x000
};

private static readonly int[,] TriangleTable =   {
    {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 1, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 8, 3, 9, 8, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 3, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {9, 2, 10, 0, 2, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {2, 8, 3, 2, 10, 8, 10, 9, 8, -1, -1, -1, -1, -1, -1, -1},
    {3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 11, 2, 8, 11, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 9, 0, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 11, 2, 1, 9, 11, 9, 8, 11, -1, -1, -1, -1, -1, -1, -1},
    {3, 10, 1, 11, 10, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 10, 1, 0, 8, 10, 8, 11, 10, -1, -1, -1, -1, -1, -1, -1},
    {3, 9, 0, 3, 11, 9, 11, 10, 9, -1, -1, -1, -1, -1, -1, -1},
    {9, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 3, 0, 7, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 1, 9, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 1, 9, 4, 7, 1, 7, 3, 1, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 10, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {3, 4, 7, 3, 0, 4, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1},
    {9, 2, 10, 9, 0, 2, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1},
    {2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4, -1, -1, -1, -1},
    {8, 4, 7, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {11, 4, 7, 11, 2, 4, 2, 0, 4, -1, -1, -1, -1, -1, -1, -1},
    {9, 0, 1, 8, 4, 7, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1},
    {4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1, -1, -1, -1, -1},
    {3, 10, 1, 3, 11, 10, 7, 8, 4, -1, -1, -1, -1, -1, -1, -1},
    {1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4, -1, -1, -1, -1},
    {4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3, -1, -1, -1, -1},
    {4, 7, 11, 4, 11, 9, 9, 11, 10, -1, -1, -1, -1, -1, -1, -1},
    {9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {9, 5, 4, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 5, 4, 1, 5, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {8, 5, 4, 8, 3, 5, 3, 1, 5, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 10, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {3, 0, 8, 1, 2, 10, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1},
    {5, 2, 10, 5, 4, 2, 4, 0, 2, -1, -1, -1, -1, -1, -1, -1},
    {2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8, -1, -1, -1, -1},
    {9, 5, 4, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 11, 2, 0, 8, 11, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1},
    {0, 5, 4, 0, 1, 5, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1},
    {2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5, -1, -1, -1, -1},
    {10, 3, 11, 10, 1, 3, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1},
    {4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10, -1, -1, -1, -1},
    {5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3, -1, -1, -1, -1},
    {5, 4, 8, 5, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1},
    {9, 7, 8, 5, 7, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {9, 3, 0, 9, 5, 3, 5, 7, 3, -1, -1, -1, -1, -1, -1, -1},
    {0, 7, 8, 0, 1, 7, 1, 5, 7, -1, -1, -1, -1, -1, -1, -1},
    {1, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {9, 7, 8, 9, 5, 7, 10, 1, 2, -1, -1, -1, -1, -1, -1, -1},
    {10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3, -1, -1, -1, -1},
    {8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2, -1, -1, -1, -1},
    {2, 10, 5, 2, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1},
    {7, 9, 5, 7, 8, 9, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1},
    {9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11, -1, -1, -1, -1},
    {2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7, -1, -1, -1, -1},
    {11, 2, 1, 11, 1, 7, 7, 1, 5, -1, -1, -1, -1, -1, -1, -1},
    {9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11, -1, -1, -1, -1},
    {5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0, -1},
    {11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0, -1},
    {11, 10, 5, 7, 11, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 3, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {9, 0, 1, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 8, 3, 1, 9, 8, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1},
    {1, 6, 5, 2, 6, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 6, 5, 1, 2, 6, 3, 0, 8, -1, -1, -1, -1, -1, -1, -1},
    {9, 6, 5, 9, 0, 6, 0, 2, 6, -1, -1, -1, -1, -1, -1, -1},
    {5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8, -1, -1, -1, -1},
    {2, 3, 11, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {11, 0, 8, 11, 2, 0, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1},
    {0, 1, 9, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1},
    {5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11, -1, -1, -1, -1},
    {6, 3, 11, 6, 5, 3, 5, 1, 3, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6, -1, -1, -1, -1},
    {3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9, -1, -1, -1, -1},
    {6, 5, 9, 6, 9, 11, 11, 9, 8, -1, -1, -1, -1, -1, -1, -1},
    {5, 10, 6, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 3, 0, 4, 7, 3, 6, 5, 10, -1, -1, -1, -1, -1, -1, -1},
    {1, 9, 0, 5, 10, 6, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1},
    {10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4, -1, -1, -1, -1},
    {6, 1, 2, 6, 5, 1, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7, -1, -1, -1, -1},
    {8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6, -1, -1, -1, -1},
    {7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9, -1},
    {3, 11, 2, 7, 8, 4, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1},
    {5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11, -1, -1, -1, -1},
    {0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1},
    {9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6, -1},
    {8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6, -1, -1, -1, -1},
    {5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11, -1},
    {0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7, -1},
    {6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9, -1, -1, -1, -1},
    {10, 4, 9, 6, 4, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 10, 6, 4, 9, 10, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1},
    {10, 0, 1, 10, 6, 0, 6, 4, 0, -1, -1, -1, -1, -1, -1, -1},
    {8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10, -1, -1, -1, -1},
    {1, 4, 9, 1, 2, 4, 2, 6, 4, -1, -1, -1, -1, -1, -1, -1},
    {3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4, -1, -1, -1, -1},
    {0, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {8, 3, 2, 8, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1},
    {10, 4, 9, 10, 6, 4, 11, 2, 3, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6, -1, -1, -1, -1},
    {3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10, -1, -1, -1, -1},
    {6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1, -1},
    {9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3, -1, -1, -1, -1},
    {8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1, -1},
    {3, 11, 6, 3, 6, 0, 0, 6, 4, -1, -1, -1, -1, -1, -1, -1},
    {6, 4, 8, 11, 6, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {7, 10, 6, 7, 8, 10, 8, 9, 10, -1, -1, -1, -1, -1, -1, -1},
    {0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10, -1, -1, -1, -1},
    {10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0, -1, -1, -1, -1},
    {10, 6, 7, 10, 7, 1, 1, 7, 3, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7, -1, -1, -1, -1},
    {2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9, -1},
    {7, 8, 0, 7, 0, 6, 6, 0, 2, -1, -1, -1, -1, -1, -1, -1},
    {7, 3, 2, 6, 7, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7, -1, -1, -1, -1},
    {2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7, -1},
    {1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11, -1},
    {11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1, -1, -1, -1, -1},
    {8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6, -1},
    {0, 9, 1, 11, 6, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0, -1, -1, -1, -1},
    {7, 11, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {3, 0, 8, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 1, 9, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {8, 1, 9, 8, 3, 1, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1},
    {10, 1, 2, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 10, 3, 0, 8, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1},
    {2, 9, 0, 2, 10, 9, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1},
    {6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8, -1, -1, -1, -1},
    {7, 2, 3, 6, 2, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {7, 0, 8, 7, 6, 0, 6, 2, 0, -1, -1, -1, -1, -1, -1, -1},
    {2, 7, 6, 2, 3, 7, 0, 1, 9, -1, -1, -1, -1, -1, -1, -1},
    {1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6, -1, -1, -1, -1},
    {10, 7, 6, 10, 1, 7, 1, 3, 7, -1, -1, -1, -1, -1, -1, -1},
    {10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8, -1, -1, -1, -1},
    {0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7, -1, -1, -1, -1},
    {7, 6, 10, 7, 10, 8, 8, 10, 9, -1, -1, -1, -1, -1, -1, -1},
    {6, 8, 4, 11, 8, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {3, 6, 11, 3, 0, 6, 0, 4, 6, -1, -1, -1, -1, -1, -1, -1},
    {8, 6, 11, 8, 4, 6, 9, 0, 1, -1, -1, -1, -1, -1, -1, -1},
    {9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6, -1, -1, -1, -1},
    {6, 8, 4, 6, 11, 8, 2, 10, 1, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6, -1, -1, -1, -1},
    {4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9, -1, -1, -1, -1},
    {10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3, -1},
    {8, 2, 3, 8, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1},
    {0, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8, -1, -1, -1, -1},
    {1, 9, 4, 1, 4, 2, 2, 4, 6, -1, -1, -1, -1, -1, -1, -1},
    {8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1, -1, -1, -1, -1},
    {10, 1, 0, 10, 0, 6, 6, 0, 4, -1, -1, -1, -1, -1, -1, -1},
    {4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3, -1},
    {10, 9, 4, 6, 10, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 9, 5, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 3, 4, 9, 5, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1},
    {5, 0, 1, 5, 4, 0, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1},
    {11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5, -1, -1, -1, -1},
    {9, 5, 4, 10, 1, 2, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1},
    {6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5, -1, -1, -1, -1},
    {7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2, -1, -1, -1, -1},
    {3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6, -1},
    {7, 2, 3, 7, 6, 2, 5, 4, 9, -1, -1, -1, -1, -1, -1, -1},
    {9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7, -1, -1, -1, -1},
    {3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0, -1, -1, -1, -1},
    {6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8, -1},
    {9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7, -1, -1, -1, -1},
    {1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4, -1},
    {4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10, -1},
    {7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10, -1, -1, -1, -1},
    {6, 9, 5, 6, 11, 9, 11, 8, 9, -1, -1, -1, -1, -1, -1, -1},
    {3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5, -1, -1, -1, -1},
    {0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11, -1, -1, -1, -1},
    {6, 11, 3, 6, 3, 5, 5, 3, 1, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6, -1, -1, -1, -1},
    {0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10, -1},
    {11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5, -1},
    {6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3, -1, -1, -1, -1},
    {5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2, -1, -1, -1, -1},
    {9, 5, 6, 9, 6, 0, 0, 6, 2, -1, -1, -1, -1, -1, -1, -1},
    {1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8, -1},
    {1, 5, 6, 2, 1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6, -1},
    {10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0, -1, -1, -1, -1},
    {0, 3, 8, 5, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {10, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {11, 5, 10, 7, 5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {11, 5, 10, 11, 7, 5, 8, 3, 0, -1, -1, -1, -1, -1, -1, -1},
    {5, 11, 7, 5, 10, 11, 1, 9, 0, -1, -1, -1, -1, -1, -1, -1},
    {10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1, -1, -1, -1, -1},
    {11, 1, 2, 11, 7, 1, 7, 5, 1, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11, -1, -1, -1, -1},
    {9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7, -1, -1, -1, -1},
    {7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2, -1},
    {2, 5, 10, 2, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1},
    {8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5, -1, -1, -1, -1},
    {9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2, -1, -1, -1, -1},
    {9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2, -1},
    {1, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 7, 0, 7, 1, 1, 7, 5, -1, -1, -1, -1, -1, -1, -1},
    {9, 0, 3, 9, 3, 5, 5, 3, 7, -1, -1, -1, -1, -1, -1, -1},
    {9, 8, 7, 5, 9, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {5, 8, 4, 5, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1},
    {5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0, -1, -1, -1, -1},
    {0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5, -1, -1, -1, -1},
    {10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4, -1},
    {2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8, -1, -1, -1, -1},
    {0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11, -1},
    {0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5, -1},
    {9, 4, 5, 2, 11, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4, -1, -1, -1, -1},
    {5, 10, 2, 5, 2, 4, 4, 2, 0, -1, -1, -1, -1, -1, -1, -1},
    {3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9, -1},
    {5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2, -1, -1, -1, -1},
    {8, 4, 5, 8, 5, 3, 3, 5, 1, -1, -1, -1, -1, -1, -1, -1},
    {0, 4, 5, 1, 0, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5, -1, -1, -1, -1},
    {9, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 11, 7, 4, 9, 11, 9, 10, 11, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11, -1, -1, -1, -1},
    {1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11, -1, -1, -1, -1},
    {3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4, -1},
    {4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2, -1, -1, -1, -1},
    {9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3, -1},
    {11, 7, 4, 11, 4, 2, 2, 4, 0, -1, -1, -1, -1, -1, -1, -1},
    {11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4, -1, -1, -1, -1},
    {2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9, -1, -1, -1, -1},
    {9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7, -1},
    {3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10, -1},
    {1, 10, 2, 8, 7, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 9, 1, 4, 1, 7, 7, 1, 3, -1, -1, -1, -1, -1, -1, -1},
    {4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1, -1, -1, -1, -1},
    {4, 0, 3, 7, 4, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 8, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {9, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {3, 0, 9, 3, 9, 11, 11, 9, 10, -1, -1, -1, -1, -1, -1, -1},
    {0, 1, 10, 0, 10, 8, 8, 10, 11, -1, -1, -1, -1, -1, -1, -1},
    {3, 1, 10, 11, 3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 11, 1, 11, 9, 9, 11, 8, -1, -1, -1, -1, -1, -1, -1},
    {3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9, -1, -1, -1, -1},
    {0, 2, 11, 8, 0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {3, 2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {2, 3, 8, 2, 8, 10, 10, 8, 9, -1, -1, -1, -1, -1, -1, -1},
    {9, 10, 2, 0, 9, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8, -1, -1, -1, -1},
    {1, 10, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 3, 8, 9, 1, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 9, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 3, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}
};
#endregion

}
