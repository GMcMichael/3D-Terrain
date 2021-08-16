using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarchingCube : MonoBehaviour
{
    public int seed;
    public int xSize;
    public int ySize;
    public int zSize;
    public int resolution;
    public float border;

    public void RebuildMesh() {

    }

    private void GenerateMesh() {
        float[,,] vectors = GetVectorMap(seed, resolution, xSize, ySize, zSize);
        //step through the vectors array using 8 vectors at a time forming a cube
        Vector3[] cornerOffsets = new Vector3 {
            new Vector3(0,0,0),
            new Vector3(1,0,0),
            new Vector3(1,1,0),
            new Vector3(0,1,0),
            new Vector3(0,0,1),
            new Vector3(1,0,1),
            new Vector3(1,1,1),
            new Vector3(0,1,1)
        }
        Vector2[] edges = new Vector2 {
            new Vector2(0,1),
            new Vector2(1,2),
            new Vector2(2,3),
            new Vector2(3,0),
            new Vector2(4,5),
            new Vector2(5,6),
            new Vector2(6,7),
            new Vector2(7,4),
            new Vector2(0,4),
            new Vector2(1,5),
            new Vector2(3,7),
            new Vector2(2,6)
        }
        for(int x = 0; x < vectors.GetLength(0)-1; x++) {
            for(int y = 0; y < vectors.GetLength(1)-1; y++) {
                for(int z = 0; z < vectors.GetLength(2)-1; z++) {//0 and above is solid -- spacing should make every resolution span 1 unit eg(for resolution of 4, every 4 should span 1 unit)
                    int case = 0;//NEED TO GET THE LOOKUP TABLE THEN DECIDE HOW TO FIND CASE-----------------------------
                    for(int i = 0; i < cornerOffsets.length; i++) {
                        if(vectors[x+cornerOffsets[i].x][y+cornerOffsets[i].y][z+cornerOffsets[i].z] > border) {
                            //add vertex to case so I can use lookup table
                        }
                    }
                    
                }
            }
        }
        //if the value of the vector is below 0 it is solid? look at how it is done again
        //after checking all current vectors and getting the value, look it up from the table for the correct mesh creation
        //check how I made the icospheres in my mesh generation and add the vertices and triangles the same way to avoid dupe verticies
        //the vertices should be placed by lerping between the values to find where 0 should be

        //display the mesh by setting mesh vertices, triangles, and uvs
    }

    private float[,,] GetVectorMap(int seed, int resolution, int size) {//maybe call this ScalarMap?
        return GetVectorMap(seed, resolution, size, size, size);
    }

    //returns a 3d float array with values that should be in the range of [-1,1]
    private float[,,] GetVectorMap(int seed, int resolution, int xSize, int ySize, int zSize) {
        float[,,] vectors = new float[xSize*resolution, ySize*resolution, zSize*resolution];
        Noise noise = new Noise(seed);
        float offset = 1/resolution;
        for(int x = 0; x < xSize; x++) {
            for(int y = 0; y < ySize; y++) {
                for(int z = 0; z < zSize; z++) {
                    for(int r = 0; r < resolution; r++) {
                        vectors[x+r,y+r,z+r] = noise.Evaluate(new Vector3(x+(r*offset),y+(r*offset),z+(r*offset)));//might not work
                    }
                }
            }
        }
        return vectors;
    }

    OnValidation() {
        if(resolution < 1) resolution = 1;
        //if(border < -1) border = -1;
        //else if(border > 1) border = 1;
    }
}
