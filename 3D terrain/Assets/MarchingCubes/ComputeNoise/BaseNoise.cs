using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseNoise : MonoBehaviour
{
    [SerializeField]
    protected ComputeShader computeNoise;

    protected int threadGroupSize = 8;

    public virtual ComputeBuffer Generate(ComputeBuffer pointsBuffer, int numPoints, int numPointsPerAxis, float spacing, Vector3 noiseOffset, Vector3 worldCoords, Vector3 worldSize) {
        computeNoise.SetBuffer(0, "points", pointsBuffer);
        computeNoise.SetInt("numPoints", numPoints);
        computeNoise.SetInt("numPointsPerAxis", numPointsPerAxis);
        computeNoise.SetFloat("spacing", spacing);
        computeNoise.SetVector("noiseOffset", noiseOffset);
        computeNoise.SetVector("worldCoords", worldCoords);
        computeNoise.SetVector("worldSize", worldSize);
        
        int noiseThreadsPerAxis = Mathf.CeilToInt(numPointsPerAxis / (float)threadGroupSize);
        computeNoise.Dispatch(0, noiseThreadsPerAxis, noiseThreadsPerAxis, noiseThreadsPerAxis);

        return pointsBuffer;
    }
}
