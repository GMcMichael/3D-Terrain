using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    #region PublicVariables
    public float baseSpeed, sprintMultiplier;
    public float cameraSpeedX, cameraSpeedY;
    public float jumpVelocity, jumpCooldown, Gravity;
    public float minGroundDist;
    public float maxSlopeAngle = 100;
    public bool debug;
    public GameObject throwLight;
    public float throwSpeed = 1;
    #endregion

    #region PrivateVariables
    private Collider bodyCollider;
    private Transform Colliders, Graphics, Camera;
    private float cameraRotationY;
    private bool mouseLocked;
    private LayerMask worldMask;
    private float height = 0.5f;
    private float groundAngle;
    private float yVelocity;
    private bool jump, ableToJump, grounded;
    private Vector3 movement;
    private Rigidbody rb;
    private RigidbodyConstraints originalConstraints;
    private Transform groundChecker;
    private GameObject playerLight;
    #endregion
    
    public float vSmoothTime = 0.1f;
	public float airSmoothTime = 0.5f;
    Vector3 targetVelocity;
    Vector3 smoothVelocity;
    Vector3 smoothVRef;

    void Awake()
    {
        Colliders = transform.Find("Colliders");
        Graphics = transform.Find("Graphics");
        Camera = transform.Find("Main Camera");
        playerLight = Camera.Find("Spot Light").gameObject;
        worldMask = LayerMask.GetMask("World");
        bodyCollider = Colliders.Find("Capsule").GetComponent<Collider>();
        height = bodyCollider.bounds.extents.y;//may need to update if im changing collider size at any time
        rb = GetComponent<Rigidbody>();
        groundChecker = transform.Find("Ground Checker");
        StartCoroutine("JumpCooldown");
        originalConstraints = rb.constraints;
    }

    void Update()
    {
        grounded = IsGrounded();
        HandleInput();
        CalculateGroundAngle();
        ShowDebug();
    }

    void FixedUpdate() {//do physics calcs in fixed update
        //if(movement != Vector3.zero) Move();
        //Move();
        rb.MovePosition (rb.position + smoothVelocity * Time.fixedDeltaTime);
        if(!grounded) rb.AddForce(-Vector3.up * Gravity, ForceMode.Acceleration);
        else if(jump) {
            jump = false;
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(Vector3.up * jumpVelocity, ForceMode.VelocityChange);
        }
    }

    private void HandleInput() {
        if(Input.GetKeyDown(KeyCode.L)) {
            if(mouseLocked) Cursor.lockState = CursorLockMode.None;
            else Cursor.lockState = CursorLockMode.Locked;
            mouseLocked = !mouseLocked;
        }
        if(Input.GetKeyDown(KeyCode.Mouse0)) ThrowLight();
        if(Input.GetKeyDown(KeyCode.Mouse1)) ToggleLight();
        //value is in the range -1 to 1, use deltaTime to make it move (speed) meters per second instead of (speed) meters per frame
        //movement = new Vector3(Input.GetAxis("Vertical"), 0, Input.GetAxis("Horizontal")) * baseSpeed * Time.deltaTime;
        //if(Input.GetKey(KeyCode.LeftShift)) movement *= sprintMultiplier;
        //if(movement != Vector3.zero) Move();

        Vector3 input = new Vector3 (Input.GetAxisRaw ("Horizontal"), 0, Input.GetAxisRaw ("Vertical"));
		bool running = Input.GetKey (KeyCode.LeftShift);
		targetVelocity = transform.TransformDirection (input.normalized) * ((running) ? (baseSpeed*sprintMultiplier) : baseSpeed);
		smoothVelocity = Vector3.SmoothDamp (smoothVelocity, targetVelocity, ref smoothVRef, (grounded) ? vSmoothTime : airSmoothTime);

        //Get the mouse delta. This is not in the range -1 to 1
        Vector2 Rotation = new Vector2(-Input.GetAxis("Mouse Y") * cameraSpeedY, Input.GetAxis("Mouse X") * cameraSpeedX);
        if(Rotation != Vector2.zero) Rotate(Rotation);

        
        if(ableToJump && grounded && Input.GetKey(KeyCode.Space)){
            jump = true;
            StartCoroutine("JumpCooldown");
        }
    }

    private void ThrowLight() {
        PlayerLight light = Instantiate(throwLight, transform.position, Quaternion.identity).GetComponent<PlayerLight>();
        light.Init(smoothVelocity, Camera.forward*throwSpeed);
    }

    private void ToggleLight() {
        playerLight.SetActive(!playerLight.activeInHierarchy);
    }

    private void FreezeConstraints(bool freeze) {
        if(freeze) {
            rb.constraints = originalConstraints | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
        } else {
            rb.constraints = originalConstraints;
        }
    }

    IEnumerator JumpCooldown() {
        ableToJump = false;
        yield return new WaitForSeconds(jumpCooldown);
        ableToJump = true;
    }

    private void CheckHit() {
        RaycastHit hit;
        if(Physics.Raycast(Camera.position, Camera.forward, out hit, worldMask)) {
            Debug.Log("hit");
        }
    }

    private void Move() {
        if(groundAngle > maxSlopeAngle) {
            FreezeConstraints(false);
            return;
        }
        if(movement == Vector3.zero) {
            FreezeConstraints(true);
            return;
        }
        FreezeConstraints(false);
        rb.MovePosition(transform.position + (transform.forward * movement.x) + (transform.right * movement.z));
    }

    private void Rotate(Vector2 Rotation) {
        //rotate player if their character is pointed to far away from the camera
        transform.Rotate(0,Rotation.y,0);
        cameraRotationY += Rotation.x;
        //Clamp camera vertical rotation
        cameraRotationY = Mathf.Clamp(cameraRotationY, -60, 60);
        Camera.rotation = Quaternion.Euler(cameraRotationY, transform.rotation.eulerAngles.y, 0);
    }

    private void CalculateGroundAngle() {
        if(!grounded) {
            groundAngle = 90;
            return;
        }
        
        RaycastHit hitInfo;
        Physics.Raycast(transform.position, -Vector3.up, out hitInfo, height + minGroundDist, worldMask);
        groundAngle = Vector3.Angle(hitInfo.normal, transform.forward);
    }

    private void ShowDebug() {
        if(!debug) return;
    }

    public bool IsGrounded() {
        //check for ground in sphere around characters feet
        if(Physics.OverlapSphere(groundChecker.position, 0.4f, worldMask).Length > 0) {
            /*if(Vector3.Distance(transform.position, hitInfo.point) < height) {
                transform.position = Vector3.Lerp(transform.position, transform.position + Vector3.up * height, 5 * Time.deltaTime);
            }*/
            return true;
        }
        return false;
    }

    void OnValidate() {
        if(baseSpeed < 0) baseSpeed = 0;
        if(sprintMultiplier < 1) sprintMultiplier = 1;
        if(cameraSpeedX < 0) cameraSpeedX = 0;
        if(cameraSpeedY < 0) cameraSpeedY = 0;
    }
}
