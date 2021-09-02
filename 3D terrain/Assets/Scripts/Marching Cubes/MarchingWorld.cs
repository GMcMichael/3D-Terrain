using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarchingWorld : MonoBehaviour
{
    public int width, height, depth;
    public float scale;
    public bool autoUpdate, centerWorld, encaseWorld, enclosureInvisable;
    public Marching MarchingChunk;
    private bool valuesChanged;
    private Transform[,,] world;
    private MeshRenderer[] encasingRenderers;
    public GameObject encasingObject;

    void Start() {
        GenerateWorld();
    }

    public void GenerateWorld() {
        GenerateWorld(width, height, depth, scale);
    }

    private void GenerateWorld(int _width, int _height, int _depth, float _scale) {
        valuesChanged = false;
        DeleteWorld();
        world = new Transform[_width, _height, _depth];
        Vector3 ChunkDimensions = MarchingChunk.GetDimensions();
        Vector3 offset = Vector3.zero;
        if(centerWorld) offset = Vector3.Scale(ChunkDimensions-Vector3.one, new Vector3(_width, _height, _depth))/2f; //new Vector3(((ChunkDimensions.x-1)*(_width-1))/2f, ((ChunkDimensions.y-1)*(_height-1))/2f, ((ChunkDimensions.z-1)*(_depth-1))/2f);
        for (int x = 0; x < world.GetLength(0); x++)
        {
            for (int y = 0; y < world.GetLength(1); y++)
            {
                for (int z = 0; z < world.GetLength(2); z++)
                {
                    world[x,y,z] = Instantiate(MarchingChunk.gameObject, new Vector3((x*ChunkDimensions.x)-x,(y*ChunkDimensions.y)-y,(z*ChunkDimensions.z)-z)-offset+transform.position, Quaternion.identity, transform).transform;
                    Marching marching = world[x,y,z].GetComponent<Marching>();
                    marching.DisplayMeshes();
                    marching.SetPartOfWorld(true);
                }
            }
        }
        //change transform scale to scale the world
        transform.localScale = Vector3.one *_scale;
        if(encaseWorld) AddEncasing(_width, _height, _depth, _scale);
    }

    private void AddEncasing(int _width, int _height, int _depth, float _scale) {
        Vector3 Dimensions = Vector3.Scale(new Vector3(_width, _height, _depth), MarchingChunk.GetDimensions());
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
    }

    public bool ValuesChanged() {
        return valuesChanged;
    }
}
