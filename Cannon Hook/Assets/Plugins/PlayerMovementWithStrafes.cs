using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerMovementWithStrafes : MonoBehaviour
{
	public CharacterController controller;
	public Transform groundCheck;
	public LayerMask groundMask;

	private float wishspeed2;
	private float gravity = -20f;
	float wishspeed;

	public float checkGroundedDistance = 0.4f;
	public float moveSpeed = 7.0f;  // Ground move speed
	public float runAcceleration = 14f;   // Ground accel
	public float runDeacceleration = 10f;   // Deacceleration that occurs when running on the ground
	public float airAcceleration = 2.0f;  // Air accel
	public float airDeacceleration = 2.0f;    // Deacceleration experienced when opposite strafing
	public float airControl = 0.3f;  // How precise air control is
	public float sideStrafeAcceleration = 50f;   // How fast acceleration occurs to get up to sideStrafeSpeed when side strafing
	public float sideStrafeSpeed = 1f;    // What the max speed to generate when side strafing
	public float jumpSpeed = 8.0f;
	public float friction = 6f;
	
	private float playerFriction = 0f;
	private float playerTopVelocity = 0;
	private float addspeed;
	private float accelspeed;
	private float currentspeed;
	private float zspeed;
	private float speed;
	private float dot;
	private float k;
	private float accel;
	private float newspeed;
	private float control;
	private float drop;

	private bool jumpQueue = false;
	private bool wishJump = false;
	private bool isGrounded;

    //UI
	private Vector3 lastPos;
	private Vector3 moved;
	[SerializeField] private Vector3 playerVel;
	private float modulasSpeed;
	private float zVelocity;
	private float xVelocity;
	//End UI
	
	private Vector3 playerVelocity;
	private Vector3 wishDir;
	private Vector3 vec;
	
	public Camera playerCamera;
	public float fov = 60f;
	public float mouseSensitivity = 2f;
	public float maxLookAngle = 50f;
	
	private Transform player;
	private Vector3 udp;
	private Vector3 moveDirectionNorm;
	
	[HideInInspector] public bool cameraCanMove = true;
	private float yaw;
	private float pitch;


    private void Start()
    {
        //This is for UI, feel free to remove the Start() function.
        player = transform;
		lastPos = player.position;
		
		playerCamera.fieldOfView = fov;
		Cursor.lockState = CursorLockMode.Locked;
	}

    // Update is called once per frame
    void Update()
    {
		#region UI //UI, Feel free to remove the region.

		moved = player.position - lastPos;
		lastPos = player.position;
		playerVel = moved / Time.fixedDeltaTime;

		zVelocity = Mathf.Abs(playerVel.z);
		xVelocity = Mathf.Abs(playerVel.x);


		modulasSpeed = Mathf.Sqrt(playerVel.z * playerVel.z + playerVel.x * playerVel.x);

		#endregion

		isGrounded = Physics.CheckSphere(groundCheck.position, checkGroundedDistance, groundMask);

		QueueJump();

		/* Movement, here's the important part */
		if (controller.isGrounded)
			GroundMove();
		else if (!controller.isGrounded)
			AirMove();

		// Move the controller
		controller.Move(playerVelocity * Time.deltaTime);

		// Calculate top velocity
		udp = playerVelocity;
		udp.y = 0;
		if (udp.magnitude > playerTopVelocity)
			playerTopVelocity = udp.magnitude;
		
		#region Camera
		// Control camera movement
		yaw = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * mouseSensitivity;
		pitch -= mouseSensitivity * Input.GetAxis("Mouse Y");
        

		// Clamp pitch between lookAngle
		pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

	    transform.localEulerAngles = new Vector3(0, yaw, 0);
		playerCamera.transform.localEulerAngles = new Vector3(pitch, 0, 0);
		#endregion

    }

	//Queues the next jump
	void QueueJump()
	{
		if (Input.GetButtonDown("Jump"))
		{
			jumpQueue = true;
		}
		
		if(!Input.GetButton("Jump"))
		{
			jumpQueue = false;
			wishJump = false;
		}

		if (isGrounded && jumpQueue)
		{
			wishJump = true;
			jumpQueue = false;
		}
	}

	//Calculates wish acceleration
	public void Accelerate(Vector3 wishdir, float wishspeed, float accel)
	{
		currentspeed = Vector3.Dot(playerVelocity, wishdir);
		addspeed = wishspeed - currentspeed;
		if (addspeed <= 0)
			return;
		accelspeed = accel * Time.deltaTime * wishspeed;
		if (accelspeed > addspeed)
			accelspeed = addspeed;

		playerVelocity.x += accelspeed * wishdir.x;
		playerVelocity.z += accelspeed * wishdir.z;
	}

	//Execs when the player is in the air
	public void AirMove()
	{
		wishDir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
		wishDir = transform.TransformDirection(wishDir);

		wishspeed = wishDir.magnitude;

		wishspeed *= 7f;

		wishDir.Normalize();
		moveDirectionNorm = wishDir;

		// Aircontrol
		wishspeed2 = wishspeed;
		if (Vector3.Dot(playerVelocity, wishDir) < 0)
			accel = airDeacceleration;
		else
			accel = airAcceleration;

		// If the player is ONLY strafing left or right
		if (Input.GetAxis("Horizontal") == 0 && Input.GetAxis("Vertical") != 0)
		{
			if (wishspeed > sideStrafeSpeed)
				wishspeed = sideStrafeSpeed;
			accel = sideStrafeAcceleration;
		}

		Accelerate(wishDir, wishspeed, accel);

		AirControl(wishDir, wishspeed2);

		// !Aircontrol

		// Apply gravity
		playerVelocity.y += gravity * Time.deltaTime;

		/**
			* Air control occurs when the player is in the air, it allows
			* players to move side to side much faster rather than being
			* 'sluggish' when it comes to cornering.
			*/

		void AirControl(Vector3 wishdir, float wishspeed)
		{
			// Can't control movement if not moving forward or backward
			if (Input.GetAxis("Horizontal") == 0 || wishspeed == 0)
				return;

			zspeed = playerVelocity.y;
			playerVelocity.y = 0;
			/* Next two lines are equivalent to idTech's VectorNormalize() */
			speed = playerVelocity.magnitude;
			playerVelocity.Normalize();

			dot = Vector3.Dot(playerVelocity, wishdir);
			k = 32;
			k *= airControl * dot * dot * Time.deltaTime;

			// Change direction while slowing down
			if (dot > 0)
			{
				playerVelocity.x = playerVelocity.x * speed + wishdir.x * k;
				playerVelocity.y = playerVelocity.y * speed + wishdir.y * k;
				playerVelocity.z = playerVelocity.z * speed + wishdir.z * k;

				playerVelocity.Normalize();
				moveDirectionNorm = playerVelocity;
			}

			playerVelocity.x *= speed;
			playerVelocity.y = zspeed; // Note this line
			playerVelocity.z *= speed;

		}
	}
	/**
		* Called every frame when the engine detects that the player is on the ground
		*/
	public void GroundMove()
	{
		// Do not apply friction if the player is queueing up the next jump
		if (!wishJump)
			ApplyFriction(1.0f);
		else
			ApplyFriction(0);

		wishDir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
		wishDir = transform.TransformDirection(wishDir);
		wishDir.Normalize();
		moveDirectionNorm = wishDir;

		wishspeed = wishDir.magnitude;
		wishspeed *= moveSpeed;

		Accelerate(wishDir, wishspeed, runAcceleration);

		// Reset the gravity velocity
		playerVelocity.y = 0;

		if (wishJump)
		{
			playerVelocity.y = jumpSpeed;
			wishJump = false;
		}

		/**
			* Applies friction to the player, called in both the air and on the ground
			*/
		void ApplyFriction(float t)
		{
			vec = playerVelocity; // Equivalent to: VectorCopy();
			vec.y = 0f;
			speed = vec.magnitude;
			drop = 0f;

			/* Only if the player is on the ground then apply friction */
			if (controller.isGrounded)
			{
				control = speed < runDeacceleration ? runDeacceleration : speed;
				drop = control * friction * Time.deltaTime * t;
			}

			newspeed = speed - drop;
			playerFriction = newspeed;
			if (newspeed < 0)
				newspeed = 0;
			if (speed > 0)
				newspeed /= speed;

			playerVelocity.x *= newspeed;
			// playerVelocity.y *= newspeed;
			playerVelocity.z *= newspeed;
		}
	}
}