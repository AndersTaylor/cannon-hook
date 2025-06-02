using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float runSpeed = 10f;
    public float maxAccel = 10f;

    public float friction = 5f;
    
    public KeyCode jumpKey = KeyCode.Space;
    public float jumpPower = 2f;

    public Camera playerCamera;
    public float fov = 60f;
    public bool cameraCanMove = true;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 50f;

    private Rigidbody rb;
    private float targetSpeed;
    private bool isGrounded = false;
    private bool isWalking = false;
    private bool isJumpQueued = false;

    private float yaw;
    private float pitch;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        playerCamera.fieldOfView = fov;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        #region Camera
        // Control camera movement
        if(cameraCanMove)
        {
            yaw = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= mouseSensitivity * Input.GetAxis("Mouse Y");
            

            // Clamp pitch between lookAngle
            pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

            transform.localEulerAngles = new Vector3(0, yaw, 0);
            playerCamera.transform.localEulerAngles = new Vector3(pitch, 0, 0);
        }
        #endregion
        
        CheckGround();
        
        //AT Jump
        if (Input.GetKeyDown(jumpKey))
        {
            isJumpQueued = true;
        }
        if (Input.GetKeyUp(jumpKey))
        {
            isJumpQueued = false;
        }
        if (isJumpQueued && isGrounded)
        {
            Jump();
            isJumpQueued = false;
        }
    }
    
    private void FixedUpdate()
    {
        // Calculate how fast we should be moving
        Vector3 targetVelocity = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        // Checks if player is walking and isGrounded
        if (targetVelocity.x != 0 || targetVelocity.z != 0 && isGrounded)
        {
            isWalking = true;
        }
        else
        {
            isWalking = false;
        }

        Vector3 velocity = rb.linearVelocity;
        Debug.Log(Accelerate(Vector3.Normalize(targetVelocity), velocity, runSpeed, maxAccel));
        rb.AddForce(Accelerate(Vector3.Normalize(targetVelocity), velocity, runSpeed, maxAccel), ForceMode.Force);
    }

    private void Jump()
    {
        // Adds force to the player rigidbody to jump
        if (isGrounded)
        {
            rb.AddForce(0f, jumpPower, 0f, ForceMode.Impulse);
            isGrounded = false;
        }
    }
    
    private void CheckGround()
    {
        Vector3 origin = new Vector3(transform.position.x, transform.position.y - (transform.localScale.y * .5f), transform.position.z);
        Vector3 direction = transform.TransformDirection(Vector3.down);
        float distance = .75f;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, distance))
        {
            Debug.DrawRay(origin, direction * distance, Color.red);
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }
    
    // accelDir: normalized direction that the player has requested to move (taking into account the movement keys and look direction)
    // prevVelocity: The current velocity of the player, before any additional calculations
    // accelerate: The server-defined player acceleration value
    // max_velocity: The server-defined maximum player velocity (this is not strictly adhered to due to strafejumping)
    private Vector3 Accelerate(Vector3 accelDir, Vector3 prevVelocity, float accelerate, float max_velocity)
    {
        float projVel = Vector3.Dot(prevVelocity, accelDir); // Vector projection of Current velocity onto accelDir.
        float accelVel = accelerate * Time.fixedDeltaTime; // Accelerated velocity in direction of movment

        // If necessary, truncate the accelerated velocity so the vector projection does not exceed max_velocity
        if(projVel + accelVel > max_velocity)
            accelVel = max_velocity - projVel;

        return prevVelocity + accelDir * accelVel;
    }
    
    private Vector3 MoveGround(Vector3 accelDir, Vector3 prevVelocity)
    {
        // Apply Friction
        float speed = prevVelocity.magnitude;
        if (speed != 0) // To avoid divide by zero errors
        {
            float drop = speed * friction * Time.fixedDeltaTime;
            prevVelocity *= Mathf.Max(speed - drop, 0) / speed; // Scale the velocity based on friction.
        }

        // ground_accelerate and max_velocity_ground are server-defined movement variables
        return Accelerate(accelDir, prevVelocity, runSpeed, maxAccel);
    }

    private Vector3 MoveAir(Vector3 accelDir, Vector3 prevVelocity)
    {
        // air_accelerate and max_velocity_air are server-defined movement variables
        return Accelerate(accelDir, prevVelocity, runSpeed, maxAccel);
    }
    
}
