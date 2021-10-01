using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float baseSpeed = 12, sprintMultiplier = 2;
    public float gravity = -9.81f;
    public float jumpHeight = 3, jumpCooldown = 2;
    private CharacterController characterController;
    private Vector3 velocity;
    private float jumpVelocity;
    private bool ableToJump, isGrounded;
    private Transform groundChecker;
    public float groundDist = 0.4f;
    private LayerMask groundMask;
    private PlayerLight lightController;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        lightController = GetComponent<PlayerLight>();
        groundMask = LayerMask.GetMask("World");
        groundChecker = transform.Find("Ground Checker");
        jumpVelocity = Mathf.Sqrt(jumpHeight*-2*gravity);
        StartCoroutine("JumpCooldown");
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundChecker.position, groundDist, groundMask);
        if(isGrounded && velocity.y < 0) velocity.y = -2f;
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 movement = transform.right*x + transform.forward*z;

        characterController.Move(movement * baseSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1) * Time.deltaTime);

        if(Input.GetButton("Jump") && ableToJump && isGrounded) {
            velocity.y = jumpVelocity;
            StartCoroutine("JumpCooldown");
        }

        velocity.y += gravity * Time.deltaTime;

        characterController.Move(velocity * Time.deltaTime);
        
        if(Input.GetKeyDown(KeyCode.Mouse0)) lightController.ThrowLight(velocity);//Send actual velocity
        if(Input.GetKeyDown(KeyCode.Mouse1)) lightController.ToggleLight();
    }

    IEnumerator JumpCooldown() {
        ableToJump = false;
        yield return new WaitForSeconds(jumpCooldown);
        ableToJump = true;
    }

    void OnValidate() {
        if(baseSpeed < 0) baseSpeed = 0;
        if(sprintMultiplier < 1) sprintMultiplier = 1;
    }
}
