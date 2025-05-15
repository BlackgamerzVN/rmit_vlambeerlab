using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GrapplingHook : PhysicsObject
{
    // Execute Grappling Hook mechanic using Velvet Intergration logic

    [Header("Grapple")]
    [SerializeField] private float _grappleLength;
    [SerializeField] private LayerMask _grappleLayer;

    // A check to determine whenever grappling hook logics should be running or not
    private Vector3 _grapplePoint;
    private bool _isGrappling;

    // This is where Velvet Intergration logic primarily be

    [Header("Rope")]
    [SerializeField] private int _numOfRopeSegments = 20;
    [SerializeField] private float _ropeSegmentLength = 0.225f;

    [Header("Local Rope Physic")]
    [SerializeField] private LayerMask _collisionMask;
    [SerializeField] private float _collisionRadius = 0.1f;
    [SerializeField] private float _bounceFactor = 0.1f;
    [SerializeField] private float _correctionClampAmount;

    static Pathmaker pathmaker;
    private LineRenderer _lineRenderer;
    private List<RopeSegment> _ropeSegments = new List<RopeSegment>();

    private Vector3 _ropeStartPoint;
    private Vector3 _ropeEndPoint;

    // Start is called before the first frame update
    void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = _numOfRopeSegments;
        _lineRenderer.enabled = false;
        _isGrappling = false;

        GameObject pathmakerObject = GameObject.FindGameObjectWithTag("Pathmaker");

        pathmaker = pathmakerObject.GetComponent<Pathmaker>();
    }
    // Compute core Grappling Hook vectors for Verlet Integration ropes.
    // This is where actions regarding Grappling Hook is executed. All other protected void below is logics regarding said 
    protected override void ComputeGrapplingHook()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Checks angle if it fits the constraints

            Vector3 mousePos;
            mousePos = Input.mousePosition;
            //Debug.Log("C1: " + mousePos.ToString());
            mousePos.z = 10;

            mousePos = Camera.main.ScreenToWorldPoint(mousePos);
            //Debug.Log("C2: " + mousePos.ToString());

            Vector2 dir = ((Vector2)mousePos - _rb2d.position).normalized;
            float dist = ((Vector2)mousePos - _rb2d.position).magnitude;
            Debug.Log("Click Direction: " + dir);

            // Only grapple if direction is upward
            if (dir.y > 0)
            {
                _isGrappling = true;

                //RaycastHit2D hit = Physics2D.Raycast(_rb2d.position, dir, Mathf.Infinity, _collisionMask);

                RaycastHit hit = new RaycastHit();
                if(Physics.Raycast(_rb2d.position, dir, out hit, Mathf.Infinity, _collisionMask))
                {
                    Vector3 offset = new Vector3(minMoveDistance, -minMoveDistance, 0);

                    _grapplePoint = new Vector3(hit.point.x, hit.point.y, 0);
                    Debug.Log("Grapple Point: " + _grapplePoint);

                    _lineRenderer.enabled = true;

                    // Attach the grapple point to starting point 
                    _ropeStartPoint = _grapplePoint;

                    for (int i = 0; i < _numOfRopeSegments; i++)
                    {
                        _ropeStartPoint.z = -1;
                        _ropeSegments.Add(new RopeSegment(_ropeStartPoint));

                        _ropeStartPoint = Vector3.MoveTowards(_ropeStartPoint, _rb2d.position, _ropeSegmentLength);

                        //_ropeStartPoint = Vector3.MoveTowards(_ropeStartPoint, _rb2d.position, dist / _ropeSegments.Count);
                    }
                }
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            _lineRenderer.enabled = false;
            _isGrappling = false;

            _ropeSegments.Clear(); // More efficient and safer than a for-loop
        }
    }

    // Compute drawing vectors for Verlet Integration ropes.
    protected override void ComputeGrapplingHookDraw()
    {
        if (_isGrappling)
        {
            Vector3[] ropePosition = new Vector3[_numOfRopeSegments];
            for (int i = 0; i < _numOfRopeSegments; i++)
            {
                ropePosition[i] = _ropeSegments[i].CurrentPosition;
            }

            _lineRenderer.SetPositions(ropePosition);
        }
    }
    // Compute simulations for Verlet Integration ropes.
    protected override void ComputeGrapplingHookSimulate(bool isGrounded)
    {
        if (_isGrappling)
        {
            for (int i = 0; i < _ropeSegments.Count; i++)
            {
                // Extract
                RopeSegment segment = _ropeSegments[i];

                // Compute
                Vector2 velocity = (segment.CurrentPosition - segment.OldPosition) * _dampingFactor;

                segment.OldPosition = segment.CurrentPosition;
                segment.CurrentPosition += velocity;
                segment.CurrentPosition += _gravityForce * Time.fixedDeltaTime;

                // Overwrite
                _ropeSegments[i] = segment;
            }
        }
    }
    // Compute constraints for Verlet Integration ropes.
    protected override void ComputeGrapplingHookApplyConstraints()
    {
        if (_isGrappling)
        {
            //Keep first point attached to designated transform
            //Updates overtime.

            Vector3 offset = new Vector3(0, 0, -1);

            RopeSegment firstSegment = _ropeSegments[0];
            firstSegment.CurrentPosition = _grapplePoint + offset;
            _ropeSegments[0] = firstSegment;

            for (int i = 0; i < _numOfRopeSegments - 1; i++)
            {
                // Extract
                RopeSegment currentSeg = _ropeSegments[i];
                RopeSegment nextSeg = _ropeSegments[i + 1];

                // Compute
                float dist = (currentSeg.CurrentPosition - nextSeg.CurrentPosition).magnitude;
                float difference = (dist - _ropeSegmentLength);

                Vector2 changeDir = (currentSeg.CurrentPosition - nextSeg.CurrentPosition).normalized;
                Vector2 changeVector = changeDir * difference;

                if (i != 0 && i != _numOfRopeSegments - 2)
                {
                    currentSeg.CurrentPosition -= (changeVector * 0.5f);
                    nextSeg.CurrentPosition += (changeVector * 0.5f);
                }
                else if (i == _numOfRopeSegments - 2)
                {
                    nextSeg.CurrentPosition += changeVector;
                    _rb2d.position = Vector2.MoveTowards(_rb2d.position, nextSeg.CurrentPosition, 0.5f);
                }
                else
                {
                    nextSeg.CurrentPosition += changeVector;
                }

                // Overwrite
                _ropeSegments[i] = currentSeg;
                _ropeSegments[i + 1] = nextSeg;
            }
        }
    }
    // Compute collisions for Verlet Integration ropes.
    protected override void ComputeGrapplingHookHandleCollision()
    {
        if (_isGrappling)
        {
            for (int i = 1; i < _ropeSegments.Count; i++)
            {
                // Extract
                RopeSegment segment = _ropeSegments[i];

                // Compute
                Vector2 velocity = segment.CurrentPosition - segment.OldPosition;
                int maxColliders = 32;

                Collider[] hitColliders = new Collider[maxColliders];

                int numOfColliders = Physics.OverlapSphereNonAlloc(segment.CurrentPosition, _collisionRadius * i, hitColliders, _collisionMask);

                // This must not be used with Duel Grid Tile collision!

                // Performance nuke if tied with 2D Collision Polygon provided by Duel Grid Tile. Consider attaching conventional 3D object to the grid system and collide with them instead!

                //Collider2D[] colliders = new Collider2D[maxColliders];

                //colliders = Physics2D.OverlapCircleAll(segment.CurrentPosition, _collisionRadius, _collisionMask);

                //foreach (Collider2D collider in colliders)
                for(int k = 0; k < numOfColliders; k++)
                {
                    Collider collider = hitColliders[k];
                    Vector2 closestPoint = collider.ClosestPoint(segment.CurrentPosition);
                    float distance = Vector2.Distance(segment.CurrentPosition, closestPoint);

                    // if within the collision radius
                    if (distance < _collisionRadius * i)
                    {
                        Vector2 normal = (segment.CurrentPosition - closestPoint).normalized;
                        if (normal == Vector2.zero)
                        {
                            // fallback method
                            normal = (segment.CurrentPosition - (Vector2)collider.transform.position).normalized;

                            float depth = _collisionRadius * i - distance;
                            segment.CurrentPosition += normal * depth;

                            velocity = Vector2.Reflect(velocity, normal) * _bounceFactor;
                        }
                    }
                }

                segment.OldPosition = segment.CurrentPosition - velocity;

                // Overwrite
                _ropeSegments[i] = segment;
            }
        }
    }
    public struct RopeSegment
    {
        public Vector2 CurrentPosition;
        public Vector2 OldPosition;

        public RopeSegment(Vector2 pos)
        {
            CurrentPosition = pos;
            OldPosition = pos;
        }
    }


    /*
    // Unfortunately, since the object doesn't ultilize Dynamic RigidBody 2D, its movement ignore the joint restriction. Joint instantly breaks on execution.
    // Subsequently, this means that instead of automatic grappling hook, it insteads became instant teleportation code.
    // This became inaccurate the further it is away from the screen
    // This serves no purpose whatsoever regarding physic. But since I coded it in anyway, might as well keep it!
    protected override void ComputeTeleportation()
    {
        Vector3 offset = new Vector3(0, 0, -1);

        Vector2 dir = Vector2.zero;

        if (Input.GetKeyDown(teleportingKey))
        {
            Vector3 mousePos;
            mousePos = Input.mousePosition;
            //Debug.Log("C1: " + mousePos.ToString());
            mousePos.z = 10;

            mousePos = Vector3Int.FloorToInt(Camera.main.ScreenToWorldPoint(mousePos));
            //Debug.Log("C2: " + mousePos.ToString());

            Debug.Log(mousePos);

            RaycastHit2D hit = Physics2D.Raycast
                (
                origin: mousePos,
                direction: Vector2.zero,
                distance: Mathf.Infinity,
                layerMask: _grappleLayer
                );
            Vector2 projectedPoint = hit.point + new Vector2(0.5f, 0.5f);
            if (hit.collider != null && pathmaker.FloorCheck(Vector3Int.FloorToInt(mousePos).x, Vector3Int.FloorToInt(mousePos).y) && pathmaker.FloorCheck(Vector3Int.FloorToInt(mousePos).x, Vector3Int.FloorToInt(mousePos).y + 1))
            {
                
                _grapplePoint = projectedPoint;
                _grapplePoint.z = 0;
                // Hook this up with rb2d from PhysicsObject will make it instantly teleport! This was not intentional! But it will stay as is.
                _rb2d.position = _grapplePoint;
                _joint.anchor = transform.position;
                _joint.connectedAnchor = _grapplePoint;
                _joint.enabled = true;
                _joint.distance = _grappleLength;
                _lineRenderer.SetPosition(0, _grapplePoint + offset);
                _lineRenderer.SetPosition(1,transform.position + offset);
                _lineRenderer.enabled = true;
                
            }
        }
        if (Input.GetKeyUp(teleportingKey))
        {
            
            _joint.enabled = false;
            _lineRenderer.enabled = false;
            
        }
        if (_lineRenderer.enabled)
        {
            _lineRenderer.SetPosition(1, transform.position + offset);
        }
    }
    */
}
