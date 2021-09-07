using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StickyLight : MonoBehaviour
{
    public int lifetime = 5, lightReductionWait = 4;
    public float radius = 0.1f, fallSpeed = 1, timeStep = 10;
    public LayerMask collisionMask;
    bool stuck;
    private Transform lightT;
    private Light realLight;
    private Vector3 velocity;

    void Awake() {
        lightT = transform.Find("Area Light");
        realLight = lightT.GetComponent<Light>();
    }

    public void Init(Vector3 inheritedVelocity, Vector3 throwVelocity) {
        velocity += inheritedVelocity + throwVelocity;
        StartCoroutine("Lifespan");//maybe only start after it sticks and destroy if it doesnt stick after a certain amount of time
    }

    void UpdateStuck() {//call it what it is stuck to is modified
        stuck = Physics.CheckSphere(transform.position, radius, collisionMask);
    }

    void Update() {
        if(!stuck) {
            velocity += Vector3.down*fallSpeed*Time.deltaTime;
            Ray ray = new Ray(transform.position, velocity.normalized);
            if(Physics.SphereCast(ray, radius, out RaycastHit hit, velocity.magnitude*Time.deltaTime, collisionMask)){
                transform.position = hit.point;
                lightT.position = hit.point + hit.normal*0.5f;
                velocity = Vector3.zero;
                stuck = true;
            } 
            else transform.position += velocity * Time.deltaTime;
        }
    }

    private IEnumerator Lifespan() {
        float lightReduction = realLight.intensity/timeStep;
        float waitTime = lifetime/timeStep;
        for (int i = 0; i < timeStep; i++)
        {
            //float realTime = i*waitTime;
            //if(realTime > lightReductionWait)
            realLight.intensity -= lightReduction;
            yield return new WaitForSeconds(waitTime);
        }
        Destroy(transform.gameObject);
    }

    void OnValidate() {
        if(radius < 0.1) radius = 0.1f;
        if(fallSpeed < 0.05) fallSpeed = 0.05f;
    }
}
