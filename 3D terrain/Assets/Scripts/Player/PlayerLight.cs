using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerLight : MonoBehaviour
{
    public GameObject throwLight;
    public float throwSpeed = 50;
    private Transform playerCamera;
    private GameObject playerLight;
    private PlayerMovement playerMovement;

    void Start() {
        playerCamera = transform.Find("Main Camera");
        playerLight = playerCamera.Find("Spot Light").gameObject;
        playerMovement = GetComponent<PlayerMovement>();
    }

    public void ThrowLight(Vector3 velocity) {
        StickyLight light = Instantiate(throwLight, transform.position, Quaternion.identity).GetComponent<StickyLight>();
        light.Init(velocity, playerCamera.forward*throwSpeed);
    }

    public void ToggleLight() {
        playerLight.SetActive(!playerLight.activeInHierarchy);
    }
}
