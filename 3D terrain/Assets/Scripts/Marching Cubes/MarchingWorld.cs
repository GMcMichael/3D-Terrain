using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MarchingWorld : MonoBehaviour
{
    public enum WorldType {
        Noise,
        Terrain,
        Testing
    }
    public WorldType worldType;
    public bool generateAtStart;
    public int seed = 0, width = 1, height = 1, depth = 1;
    public float scale = 1, noiseFrequency = 1, noiseAmplitude = 1;
    public int noiseOctaves = 1;
    public float filterFrequency = 1;
    public bool randFilterCenter;
    public Vector3 filterCenter;
    public AnimationCurve filterCurve;
    public float warpFrequency = 1, warpAmplitude = 1, floorLevel = -13;
    public bool hasFloor = false;
    public readonly Vector2[] worlds = new Vector2[] //stores as Vector2(Frequency, Amplitude)
        { new Vector2(0.02f, 38)};
    public bool autoUpdate, centerWorld, encaseWorld, enclosureInvisable;
    public Material meshMaterial;
    public Marching marchingNoise, marchingTerrain, marchingTesting;
    private bool valuesChanged;
    private Transform[,,] world;
    private MeshRenderer[] encasingRenderers;
    public GameObject encasingObject;
    public NavMeshSurface navSurface;

    void Start() {//TODO: make world run the marching cubes algorithm and cut only put chunks where there is terrain
        if(generateAtStart) GenerateWorld();
        else WorldManaManager.CreateNoise();
    }

    public void GenerateWorld() {
        GenerateWorld(width, height, depth, scale, seed);
        WorldManaManager.CreateNoise();//TODO: Maybe add seed here
        navSurface.BuildNavMesh();
    }

    private void GenerateWorld(int _width, int _height, int _depth, float _scale, int _seed) {//TODO: Add seed on world here and apply seed to chunks, make world save and reload chunks with dictonary
        if(randFilterCenter) filterCenter = new Vector3(Random.Range(-100000, 100000), Random.Range(-100000, 100000), Random.Range(-100000, 100000));
        valuesChanged = false;
        DeleteWorld();
        world = new Transform[_width, _height, _depth];
        Marching marchingType = null;
        if(worldType == WorldType.Noise) marchingType = marchingNoise;
        else if(worldType == WorldType.Terrain) marchingType = marchingTerrain;
        else marchingType = marchingTesting;
        Vector3 ChunkDimensions = marchingType.GetDimensions();
        Vector3 offset = Vector3.zero;
        if(centerWorld) offset = Vector3.Scale(ChunkDimensions-Vector3.one, new Vector3(_width, _height, _depth))/2f;
        for (int x = 0; x < world.GetLength(0); x++)
        {
            for (int y = 0; y < world.GetLength(1); y++)
            {
                for (int z = 0; z < world.GetLength(2); z++)
                {
                    world[x,y,z] = Instantiate(marchingType.gameObject, new Vector3((x*ChunkDimensions.x)-x,(y*ChunkDimensions.y)-y,(z*ChunkDimensions.z)-z)-offset+transform.position, Quaternion.identity, transform).transform;
                    world[x,y,z].gameObject.SetActive(true);
                    Marching marching = world[x,y,z].GetComponent<Marching>();
                    marching.SetMaterial(meshMaterial);
                    marching.SetNoiseData(_seed, noiseFrequency, noiseAmplitude, noiseOctaves, filterFrequency, filterCenter, filterCurve, warpFrequency, warpAmplitude, floorLevel, hasFloor);
                    marching.DisplayMeshes();
                }
            }
        }
        //change transform scale to scale the world
        transform.localScale = Vector3.one *_scale;
        if(encaseWorld) AddEncasing(_width, _height, _depth, _scale, ChunkDimensions);
    }

    private void AddEncasing(int _width, int _height, int _depth, float _scale, Vector3 ChunkDimensions) {
        Vector3 Dimensions = Vector3.Scale(new Vector3(_width, _height, _depth), ChunkDimensions);
        int x = (int)(Dimensions.x*_scale)/2;
        int y = (int)((Dimensions.y-1)*_scale)/2;
        int z = (int)(Dimensions.z*_scale)/2;
        Vector3 offset = Vector3.zero;
        if(centerWorld) offset = new Vector3((Dimensions.x-_width)*_scale/2f, (Dimensions.y-_height)*_scale/2f, (Dimensions.z-_depth)*_scale/2f);
        encasingRenderers = new MeshRenderer[] {
            Instantiate(encasingObject, new Vector3(x,0,z)+transform.position-offset, Quaternion.Euler(0,0,0), transform).GetComponent<MeshRenderer>(),
            Instantiate(encasingObject, new Vector3(x,(Dimensions.y-_height)*_scale,z)+transform.position-offset, Quaternion.Euler(180,0,0), transform).GetComponent<MeshRenderer>(),
            Instantiate(encasingObject, new Vector3(0,y,z)+transform.position-offset, Quaternion.Euler(0,0,-90), transform).GetComponent<MeshRenderer>(),
            Instantiate(encasingObject, new Vector3((Dimensions.x-_width)*_scale,y,z)+transform.position-offset, Quaternion.Euler(0,0,90), transform).GetComponent<MeshRenderer>(),
            Instantiate(encasingObject, new Vector3(x,y,0)+transform.position-offset, Quaternion.Euler(90,0,0), transform).GetComponent<MeshRenderer>(),
            Instantiate(encasingObject, new Vector3(x,y,(Dimensions.z-_depth)*_scale)+transform.position-offset, Quaternion.Euler(-90,0,0), transform).GetComponent<MeshRenderer>()
        };
        for (int i = 0; i < 6; i++) {
            encasingRenderers[i].gameObject.layer = LayerMask.NameToLayer("World");
            if(enclosureInvisable) encasingRenderers[i].enabled = false;
        }
        encasingRenderers[0].transform.localScale = new Vector3(_width,1,_depth);
        encasingRenderers[1].transform.localScale = new Vector3(_width,1,_depth);
        encasingRenderers[2].transform.localScale = new Vector3(_height,1,_depth);
        encasingRenderers[3].transform.localScale = new Vector3(_height,1,_depth);
        encasingRenderers[4].transform.localScale = new Vector3(_width,1,_height);
        encasingRenderers[5].transform.localScale = new Vector3(_width,1,_height);
    }

    private void DeleteWorld() {
        transform.localScale = Vector3.one;
        for (int i = transform.childCount-1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
    }

    void OnValidate() {
        valuesChanged = true;
        if(width < 1) width = 1;
        if(height < 1) height = 1;
        if(depth < 1) depth = 1;
        if(scale < 1) scale = 1;
        if(noiseFrequency < 0) noiseFrequency = 0;
        if(noiseAmplitude < 0) noiseAmplitude = 0;
        if(noiseOctaves < 0) noiseOctaves = 0;
    }

    public bool ValuesChanged() {
        return valuesChanged;
    }
}
