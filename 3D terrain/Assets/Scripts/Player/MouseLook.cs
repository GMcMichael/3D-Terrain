using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public Vector2 lookRange = new Vector2(-90, 90);
    private Transform playerBody;
    private float xRot = 0;
    private bool mouseLocked = true;

    void Start() {
        playerBody = transform.parent;
        if(mouseLocked) Cursor.lockState = CursorLockMode.Locked;
    }
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.L)) {
            mouseLocked = !mouseLocked;
            if(mouseLocked) Cursor.lockState = CursorLockMode.Locked;
            else Cursor.lockState = CursorLockMode.None;
        }
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        
        xRot -= mouseY;
        xRot = Mathf.Clamp(xRot, lookRange.x, lookRange.y);

        transform.localRotation = Quaternion.Euler(xRot, 0, 0);
        playerBody.Rotate(Vector3.up*mouseX);
    }

    void OnValidate() {
        if(mouseSensitivity < 0) mouseSensitivity = 0;
    }
}
