using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public CharacterController controller;
    public float playerSpeed = 6.0f;            //  Speed of the player movement
    public float jumpForce = 10.0f;              //  Force of the player jumping
    [Tooltip("Fall velocity when the player is on a slope")]
    public float fallSlopeVelocity = 8f;      //  Slope fall velocity affected too by the gravity
    [Tooltip("Force in the Y-axis when the player in on a slope")]
    public float fallSlopeForce = 10.0f;        //  Force that affects in the fall of the player in the slopes
    //  Status variables accessible from other objects
    [HideInInspector]
    public bool safeZone;       //  If the player is in a safe zone the enemy cant chase the player
    [HideInInspector]
    public bool IsRunning;      //  When the player is running the enemy reacts immediately to chasing the player
    [HideInInspector]
    public bool IsWalking;      //  When the player is walking the enemy knows the position of the player
    //  Movement variables
    Vector3 m_PlayerMovement;   //  Movement Vector
    Vector3 m_Movement;         //  Input movement Vector
    float m_Speed;              //  Speed of the player, this changes if the player run
    float m_Gravity = 20.0f;    //  Gravity
    float m_FallVelocity;       //  Variable that saves and applies gravity to the player
    //  Slope variables
    bool m_OnSlope = false;     //  If the player is on a slope
    Vector3 m_HitNormal;        //  Get the normal of a plane

    Animator m_Animator;        //  Animator component    

    void Start()
    {
        m_Animator = GetComponent<Animator>();
        m_Speed = playerSpeed;
    }

    void Update()
    {
        float horizontalMove = Input.GetAxis("Horizontal");                     //  Get the input axis (w,a,s,d)
        float verticalMove = Input.GetAxis("Vertical");

        m_Movement.Set(horizontalMove, 0f, verticalMove);                       //  Set the movement vector with the input
        m_Movement = Vector3.ClampMagnitude(m_Movement, 1f);

        bool hasHorizontalInput = !Mathf.Approximately(horizontalMove, 0f);     //  Approximates and equalizes the input value if it is zero
        bool hasVerticalInput = !Mathf.Approximately(verticalMove, 0f);
        bool isMove = hasHorizontalInput || hasVerticalInput;                   //  Determine whether or not the player is movement

        m_PlayerMovement = m_Movement * m_Speed;                                //  Set the player movement vector multiply the movement vector and the currently speed of the player

        controller.transform.LookAt(controller.transform.position + m_Movement);//  Set the rotation of the player looking at the input vector

        Gravity();
        Jump();

        controller.Move(m_PlayerMovement * Time.deltaTime);                 //  Move the player using the character controller
        //Debug.Log(controller.velocity.magnitude);

        if (isMove)
        {
            //  If the player is movement, then do an action
            Action();
        }
        else
        {
            m_Animator.SetFloat("Speed", 0);        //  When the player is not moving or the input vector does not have any value, the player play the idle animation in zero
        }
    }

    private void OnAnimatorMove()
    {
        
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        m_HitNormal = hit.normal;                   //  Take the normal of the plane where the player is walking
    }

    void Action()
    {
        /*
         *  Determine if the player is walking, running, etc
         *  Play the animations for each action
         */
        if (Input.GetButton("Sprint"))
        {
            m_Speed = playerSpeed * 1.5f;           //  Multiply the initial speed to be able to run, if the speed change the blendtree of the animation have to change as well
            IsWalking = false;
            IsRunning = true;
            m_Animator.SetFloat("Speed", controller.velocity.magnitude);    //  The animator controller take the speed
        }
        else
        {
            m_Speed = playerSpeed;
            IsRunning = false;
            IsWalking = true;
            m_Animator.SetFloat("Speed", controller.velocity.magnitude);
        }
    }

    void Gravity()
    {
        /*
         *  Gravity applies to the player
         *  Take the gravity variable nad it is apply when the player is on the ground or when the player jumps
         * */
        if (controller.isGrounded)
        {
            m_FallVelocity = -m_Gravity * Time.deltaTime;
            m_PlayerMovement.y = m_FallVelocity;
        }
        else
        {
            m_FallVelocity -= m_Gravity * Time.deltaTime;
            m_PlayerMovement.y = m_FallVelocity;
            m_Animator.SetFloat("AirSpeed", controller.velocity.y);
            //  control if the player is falling for a long time
            if(controller.velocity.y <= -25 || controller.velocity.y >= 20)
            {
                m_Animator.SetTrigger("Jump");
            }
        }
        m_Animator.SetBool("IsGrounded", controller.isGrounded);
        FallSlope();
    }

    void Jump()
    {
        /*
         *  When the player press the space bar the jump force is apply to the player
         * */
        if (controller.isGrounded && Input.GetButtonDown("Jump"))
        {
            m_FallVelocity = jumpForce;                 //  Set the fall velocity when the player jump with the jump force to be able to the player goes up
            m_PlayerMovement.y = m_FallVelocity;
            m_Animator.SetTrigger("Jump");              //  Set the trigger in the animator to jump
        }
    }

    void FallSlope()
    {
        /*
         *  When the player is in a slope the player goes down,
         *  calculating the angle between the Y-axis and the normal slope and 
         *  comparing it with the slope limit of the character's controller to know 
         *  if the player has to fall down the slope that he cannot climb
         *  and compare if the slope is a platform with a 90 degrees plane to nor allow
         *  the player to fall down from that slope
         * */
        m_OnSlope = Vector3.Angle(m_HitNormal, Vector3.up) > controller.slopeLimit && Vector3.Angle(m_HitNormal, Vector3.up) < 89;

        if (m_OnSlope)
        {
            /*
             * Set the player position applying a the fall velocity to every X-Z axis calculating the inclination of the slope
             * And apply a force on the Y-axis to avoid bouncing
             * */
            m_PlayerMovement.x += ((1f - m_HitNormal.y) * m_HitNormal.x) * fallSlopeVelocity;
            m_PlayerMovement.z += ((1f - m_HitNormal.y) * m_HitNormal.z) * fallSlopeVelocity;
            m_PlayerMovement.y -= fallSlopeForce;
        }
    }
}
