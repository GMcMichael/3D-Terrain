using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldManaManager : MonoBehaviour
{
    private static Noise noise;

    public static void CreateNoise() {
        CreateNoise(Random.Range(-100000, 100000));
    }
    
    public static void CreateNoise(int seed) {
        noise = new Noise(seed);
    }

    public static float CheckMana(Vector3 point) {//TODO: modify the value to fit within my games needs, maybe make only original values above 0.8 or something to make areas more rare
        float manaAmount = noise.Evaluate(point*0.002f);
        manaAmount = Mathf.Abs(manaAmount)*50;
        return manaAmount;
    }
}
