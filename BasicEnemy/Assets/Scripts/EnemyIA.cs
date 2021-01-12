using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class EnemyIA : MonoBehaviour
{
    [Header("Movement")]
    public NavMeshAgent navMeshAgent;               //  Nav mesh agent component
    [Tooltip("Wait time when the enemy change the status")]
    public float startWaitTime = 4;                 //  Wait time of every action
    [Tooltip("Wait time when the enemy hear near the player")]
    public float timeToRotate = 2;                  //  Wait time when the enemy detect near the player without seeing
    public float speedWalk = 6;                     //  Walking speed, speed in the nav mesh agent
    public float speedRun = 9;                      //  Running speed

    [Header("Field of View")]
    [Tooltip("Total view radius around the enemy")]
    public float viewRadius = 15;                   //  Radius of the enemy view
    [Tooltip("Angle of view in front of the enemy")]
    [Range(0, 360)]
    public float viewAngle = 90;                    //  Angle of the enemy view
    [Tooltip("Nearer radius of the enemy to detect the player very near")]
    public float listenRadius = 5;                  //  Radius to detect if the player is near
    public LayerMask playerMask;                    //  To detect the player with the raycast
    public LayerMask obstacleMask;                  //  To detect the obstacules with the raycast
    [Tooltip("Number of Raycasts render in the floeld of view mesh")]
    public float meshResolution = 1.0f;             //  How many rays will cast per degree
    public int edgeIterations = 4;                  //  Number of iterations to get a better performance of the mesh filter when the raycast hit an obstacule
    public float edgeDistance = 0.5f;               //  Max distance to calcule the a minumun and a maximum raycast when hits something
    [Tooltip("Game Object with a mesh filter and mesh renderes attach to the enemy gameobject")]
    public MeshFilter meshFilter;                   //  Mesh of the enemy view
    Mesh viewMeshFilter;

    [Header("Patrol Waypoints")]
    public Transform[] waypoints;                   //  All the waypoints where the enemy patrols
    int m_CurrentWaypointIndex;                     //  Current waypoint where the enemy is going to

    PlayerMovement playerMethods;
    Menu menuMethods;
    Animator m_Animator;                            //  Animator component

    Vector3 playerLastPosition = Vector3.zero;      //  Last position of the player when was near the enemy
    Vector3 m_PlayerPosition;                       //  Last position of the player when the player is seen by the enemy

    float m_WaitTime;                               //  Variable of the wait time that makes the delay
    float m_TimeToRotate;                           //  Variable of the wait time to rotate when the player is near that makes the delay
    bool m_playerInRange;                           //  If the player is in range of vision, state of chasing
    bool m_PlayerNear;                              //  If the player is near, state of hearing
    bool m_IsPatrol;                                //  If the enemy is patrol, state of patroling
    bool m_CaughtPlayer;                            //  if the enemy has caught the player

    void Start()
    {
        viewMeshFilter = new Mesh();                //  Create a mesh of the mesh filter
        viewMeshFilter.name = "View Mesh";
        meshFilter.mesh = viewMeshFilter;

        playerMethods = FindObjectOfType<PlayerMovement>();
        menuMethods = FindObjectOfType<Menu>();

        m_PlayerPosition = Vector3.zero;
        m_PlayerPosition = Vector3.zero;
        m_IsPatrol = true;
        m_CaughtPlayer = false;
        m_playerInRange = false;
        m_PlayerNear = false;
        m_WaitTime = startWaitTime;                 //  Set the wait time variable that will change
        m_TimeToRotate = timeToRotate;

        m_CurrentWaypointIndex = 0;                 //  Set the initial waypoint
        m_Animator = GetComponent<Animator>();      //  Get the animator component
        navMeshAgent = GetComponent<NavMeshAgent>();

        navMeshAgent.isStopped = false;
        navMeshAgent.speed = speedWalk;             //  Set the navemesh speed with the normal speed of the enemy
        navMeshAgent.SetDestination(waypoints[m_CurrentWaypointIndex].position);    //  Set the destination to the first waypoint
    }

    private void Update()
    {
        EnviromentView();                       //  Check whether or not the player is in the enemy's field of vision

        m_Animator.SetFloat("Speed", navMeshAgent.speed);   //  Set the speed in the animator controller to play the blend tree
        
        if (!m_IsPatrol)
        {
            //  The enemy is chasing the player
            m_PlayerNear = false;                       //  Set false that hte player is near beacause the enemy already sees the player
            playerLastPosition = Vector3.zero;          //  Reset the player near position
            menuMethods.EnemyState(1);                  //  Upate the state of the enemy to CHASING

            if (!m_CaughtPlayer)
            {
                Move(speedRun);
                navMeshAgent.SetDestination(m_PlayerPosition);          //  set the destination of the enemy to the player location
            }
            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)    //  Control if the enemy arrive to the player location
            {
                if (m_WaitTime <= 0 && !m_CaughtPlayer)
                {
                    //  Check if the enemy is not near to the player, returns to patrol after the wait time delay
                    m_IsPatrol = true;
                    m_PlayerNear = false;
                    Move(speedWalk);
                    m_TimeToRotate = timeToRotate;
                    m_WaitTime = startWaitTime;
                    navMeshAgent.SetDestination(waypoints[m_CurrentWaypointIndex].position);
                }
                else
                {                    
                    if (Vector3.Distance(transform.position, GameObject.FindGameObjectWithTag("Player").transform.position) <= 0.5)
                    {
                        //  If the enemy is very near the player, so the player lost
                        if (!m_CaughtPlayer)
                        {
                            Stop();
                            transform.LookAt(m_PlayerPosition);
                            CaughtPlayer();
                        }
                    }
                    else
                    {
                        //  Wait if the current position is not the player position
                        Stop();
                        m_WaitTime -= Time.deltaTime;
                    }
                }
            }
        }
        else
        {
            // The enemy is patroling
            EnviromentListening();
            if (m_PlayerNear)
            {
                //  Check if the enemy detect near the player, so the enemy will move to that position
                menuMethods.EnemyState(2);  //  Set the state of the enemy to HEARING
                if (m_TimeToRotate <= 0)
                {
                    Move(speedWalk);
                    LookingPlayer(playerLastPosition);
                }
                else
                {
                    //  The enemy wait for a moment and then go to the last player position
                    Stop();
                    m_TimeToRotate -= Time.deltaTime;
                }
            }
            else
            {
                menuMethods.EnemyState(3);      //  Set the enemy state to PATROL
                m_PlayerNear = false;           //  The player is no near when the enemy is platroling
                playerLastPosition = Vector3.zero;
                navMeshAgent.SetDestination(waypoints[m_CurrentWaypointIndex].position);    //  Set the enemy destination to the next waypoint
                if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
                {
                    //  If the enemy arrives to the waypoint position then wait for a moment and go to the next
                    if (m_WaitTime <= 0)
                    {
                        NextPoint();
                        Move(speedWalk);
                        m_WaitTime = startWaitTime;
                    }
                    else
                    {
                        Stop();
                        m_WaitTime -= Time.deltaTime;
                    }
                }
            }
        }
    }

    private void OnAnimatorMove()
    {

    }

    private void LateUpdate()
    {
        /*
         *  Its is called after the movement of the enemy
         * */
        MeshEnviromentView();
    }

    public void NextPoint()
    {
        /*Set the current waypoint to the next
         * Change the waypoint by adding plus 1 
         * and calculating the modulus of the waypoint array size, 
         * returning to the first value if the sum is greater than the array size
         * */
        m_CurrentWaypointIndex = (m_CurrentWaypointIndex + 1) % waypoints.Length;
        navMeshAgent.SetDestination(waypoints[m_CurrentWaypointIndex].position);
    }

    void Stop()
    {
        /*
         *  Stop the enemy movement
         * */
        navMeshAgent.isStopped = true;
        navMeshAgent.speed = 0;
    }

    void Move(float speed)
    {
        /*
         * float speed: velocity of the enemy
         *  -
         *  Reset the enemy movement
         * */
        navMeshAgent.isStopped = false;
        navMeshAgent.speed = speed;
    }

    void CaughtPlayer()
    {
        /*
         * If the enemy position is almost the same as the player
         * The player lost and show the lost menu
         * */
        m_CaughtPlayer = true;
        menuMethods.LostPlayer();
    }

    void LookingPlayer(Vector3 player)
    {
        /*
         *  Vector 3 player: last location of the player
         *  -
         *  When the enemy detect near the player
         *  the enemy goes to that position
         *  And then return to patrol
         * */
        navMeshAgent.SetDestination(player);
        if (Vector3.Distance(transform.position, player) <= 0.3)
        {
            if (m_WaitTime <= 0)
            {
                m_PlayerNear = false;
                Move(speedWalk);
                navMeshAgent.SetDestination(waypoints[m_CurrentWaypointIndex].position);
                m_WaitTime = startWaitTime;
                m_TimeToRotate = timeToRotate;
            }
            else
            {
                Stop();
                m_WaitTime -= Time.deltaTime;
            }
        }
    }

    void EnviromentView()
    {
        /*
         *  Function to control the view of the enemy and detect the obstacles and the player 
         *  with the parameters of vision already defined
         * */
        Collider[] playerInRange = Physics.OverlapSphere(transform.position, viewRadius, playerMask);   //  Make an overlap sphere around the enemy to detect the playermask in the view radius

        for (int i = 0; i < playerInRange.Length; i++)
        {
            /*
             *  When the player is in the radius of vision, the player's position is saved in a variable and 
             *  the vector direction of the player is calculated.
             * */
            Transform player = playerInRange[i].transform;
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dirToPlayer) < viewAngle / 2)
            {
                /*
                 * If the angle between the position of the player and the forward vector of the enemy is less than 
                 * the angle of vision divided by 2, the distance between the current position of the enemy and the player is calculated
                 */
                float dstToPlayer = Vector3.Distance(transform.position, player.position);          //  Distance of the enmy and the player
                if (!Physics.Raycast(transform.position, dirToPlayer, dstToPlayer, obstacleMask) && !playerMethods.safeZone)
                {
                    /*
                     *  Make a raycast to detect directly the player, avoiding the obstacules and cheling if the player is not in a safe zone
                     *  The raycast will be updated at all times while the player is still in the enemy's viewing angle
                     * */
                    m_playerInRange = true;             //  The player has been seeing by the enemy and then the nemy starts to chasing the player
                    m_IsPatrol = false;                 //  Change the state to chasing the player
                }
                else
                {
                    /*
                     *  If the player is behind a obstaculo the player position will not be registered
                     * */
                    m_playerInRange = false;
                }
            }
            if (Vector3.Distance(transform.position, player.position) > viewRadius || playerMethods.safeZone)
            {
                /*
                 *  If the player is further than the view radius, then the enemy will no longer keep the player's current position.
                 *  Or the enemy is a safe zone, the enemy will no chase
                 * */
                m_playerInRange = false;                //  Change the sate of chasing
            }
            if (m_playerInRange)
            {
                /*
                 *  If the enemy no longer sees the player, then the enemy will go to the last position that has been registered
                 * */
                m_PlayerPosition = player.transform.position;       //  Save the player's current position if the player is in range of vision
            }
        }
    }

    void EnviromentListening()
    {
        /*
         *  Fuction to check if the player is really near the enemy using the listen radius 
         *  The enemy also analizes if the player is walking or running and then go to the positions of the player
         *  If the enemy sees the player, then the enemy starts to chase the player using the EnviromentView() Fuction
         * */
        Collider[] playerNear = Physics.OverlapSphere(transform.position, listenRadius, playerMask);        //  Make an overlap sphere around the enemy to detect the playermask in the listen radius

        if (playerNear.Any())       //  Check if the player has been around the listen radius
        {
            Vector3 dirToPlayerNear = (playerNear[0].transform.position - transform.position).normalized;       //  Direction vector of the player's position

            if (Vector3.Distance(transform.forward, dirToPlayerNear) < listenRadius)
            {
                /*
                 *  If the distance between the enemy's forward vector and the player's direction vector is less than the listen radius
                 *  then the enemy will save the distance in a variable
                 * */
                float dstToPlayerNear = Vector3.Distance(transform.position, playerNear[0].transform.position);         //  Distance between the enemy and the player
                if (!Physics.Raycast(transform.position, dirToPlayerNear, dstToPlayerNear, obstacleMask) && !playerMethods.safeZone)
                {
                    /*
                     *  Make a raycast to check if the player is around the enemy avoiding the obstacules and checking if the player is in a safe zone
                     *  if not last the player's position will save
                     * */
                    if (playerMethods.IsRunning)
                    {
                        /*
                         *  Check if the player is running, if so the enemy immediately will look at the player and start to chasing
                         **/
                        transform.LookAt(playerNear[0].transform.position);     //  The enemy look at the last player position
                        m_PlayerNear = false;                                   //  Change the state of hearing
                    }
                    else if (playerMethods.IsWalking)
                    {
                        /*
                         *  Check if the player is walking, if so the last player position will saved an then the enemy will go to that position using the LookingPLayer fuction
                         * */
                        playerLastPosition = playerNear[0].transform.position;  //  Saved the last player position
                        m_PlayerNear = true;                                    //  Change the state of hearing
                    }
                }
            }
        }     
    }

    void MeshEnviromentView()
    {
        /*
         *  Function to draw the fields of view of the enemy using the mesh filter and the mesh of the field of view
         * */
        int stepCount = Mathf.RoundToInt(viewAngle * meshResolution);       //  Number of rays cast 
        float stepAngleSize = viewAngle / stepCount;                        //  Divide the number of rays per cast with the view angle
        List<Vector3> points = new List<Vector3>();                         //  All the point that the raycast hits                   
        ViewCastInfo oldViewCast = new ViewCastInfo();                      //  Old information to know whether or not the raycast is hitting something
        for (int i = 0; i <= stepCount; i++)
        {
            float angle = transform.eulerAngles.y - viewAngle / 2 + stepAngleSize * i;      //  Per every ray cast calculate the angle
            ViewCastInfo newViewCast = ViewCast(angle);

            if (i > 0)
            {
                bool edgeDistanceExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > edgeDistance;    //  Calculated is the raycast is hitting something further than the nearest object
                if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDistanceExceeded))
                {
                    /*
                     *  Calculated if the old viewcast is hitting something and the new one is not
                     * */
                    EdgeInfo edge = FindEdge(oldViewCast, newViewCast);     //  Edge information, new minumum and maximun raycast
                    if (edge.EdgeA != Vector3.zero)
                    {
                        //  If the edge info has changed, then add a new point to the mesh filter
                        points.Add(edge.EdgeA);
                    }
                    if (edge.EdgeB != Vector3.zero)
                    {
                        //  If the edge info has changed, then add a new point to the mesh filter
                        points.Add(edge.EdgeB);
                    }
                }
            }
            points.Add(newViewCast.point);
            oldViewCast = newViewCast;
        }

        int vertexCount = points.Count + 1;                     //  The number of vertices where the raycast hits
        Vector3[] vertices = new Vector3[vertexCount];          //  Array of the vertices amount
        int[] triangles = new int[(vertexCount - 2) * 3];       //  Total triangles between the vertices

        vertices[0] = Vector3.zero;                             //  Make the first vectices the local position of the object
        for (int i = 0; i < vertexCount - 1; i++)
        {
            vertices[i + 1] = transform.InverseTransformPoint(points[i]);   //  Make local the vertices position 

            if (i < vertexCount - 2)            //  does not let go Out of bounds of the array
            {
                //  Set the triangles by grouping each one with three vertices
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        viewMeshFilter.Clear();                 //  Clean the mesh filter and then update it
        viewMeshFilter.vertices = vertices;     //  Assign the created vertices to the vertices of the mesh filter
        viewMeshFilter.triangles = triangles;   //  Assign the created triangles to the triangles of the mesh filter
        viewMeshFilter.RecalculateNormals();    //  Recalculated normal to show the mesh in the correct position
    }


    EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
    {
        /*
         *  Fuction to get the edge of an obstacule to get a much better resolution of the field of view mesh
         *  The fuction will get a minumum and maxumun raycast between two raycast and make another one
         *  to make looks much better when the some raycast are hitting something and others are not
         * */
        float minAngle = minViewCast.angle;     //  Minimum angle of the hitting
        float maxAngle = maxViewCast.angle;     //  Maximum angle of the hitting
        Vector3 minEdge = Vector3.zero;         //  Minimum vector of the hit
        Vector3 maxEdge = Vector3.zero;         //  Maximum vector of the hit

        for (int i = 0; i < edgeIterations; i++)    //  Number of iterations
        {
            float angle = (minAngle + maxAngle) / 2;    //  Get the angle of the two raycast
            ViewCastInfo newViewCast = ViewCast(angle); //  New viewcast information

            bool edgeDistanceExceeded = Mathf.Abs(minViewCast.dst - newViewCast.dst) > edgeDistance;    //  Calculated is the raycast is hitting something further than the nearest object
            if (newViewCast.hit == minViewCast.hit && !edgeDistanceExceeded)
            {
                /*
                 *  Check if the new view cast is equial to the minimum view cast
                 *  then the minimun angle will be the new cast information
                 * */
                minAngle = angle;
                minEdge = newViewCast.point;
            }
            else
            {
                /*
                 *  If it is not equal than the minumun hit
                 *  The max angle will be the new cast information
                 * */
                maxAngle = angle;
                maxEdge = newViewCast.point;
            }
        }
        return new EdgeInfo(minEdge, maxEdge);      //  Return the new information of the edge to be able to get a middle raycast
    }


    ViewCastInfo ViewCast(float globalAngle)
    {
        Vector3 dir = DirFromAngle(globalAngle, true);              //  Direction of the raycast
        RaycastHit hit;                                             //  Hit of the raycast

        if (Physics.Raycast(transform.position, dir, out hit, viewRadius, obstacleMask))
        {
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);    //  Return the information of the raycast when hit an obstacule
        }
        else if (Physics.Raycast(transform.position, dir, out hit, viewRadius, playerMask))
        {
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);    //  Return the information of the raycast when hit the player
        }
        else
        {
            return new ViewCastInfo(false, transform.position + dir * viewRadius, viewRadius, globalAngle);     //  Return that the raycast is hitting nothing
        }
    }

    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        /*
         *  Transform the angles to be able to use in Unity
         * */
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    public struct ViewCastInfo
    {
        /*
         *  Store the raycasting information of the field of view
         * */
        public bool hit;            //  Wether or not the raycast hit something
        public Vector3 point;       //  End point of the ray
        public float dst;           //  Lenght of the rays
        public float angle;         //  Angle of the ray

        public ViewCastInfo(bool _hit, Vector3 _point, float _dst, float _angle)
        {
            hit = _hit;
            point = _point;
            dst = _dst;
            angle = _angle;
        }
    }

    public struct EdgeInfo
    {
        /*
         *  Store the edge information of an obstacule when one raycast is hitting something and another one is not
         * */
        public Vector3 EdgeA;
        public Vector3 EdgeB;

        public EdgeInfo(Vector3 _EdgeA, Vector3 _EdgeB)
        {
            EdgeA = _EdgeA;
            EdgeB = _EdgeB;
        }
    }
}
