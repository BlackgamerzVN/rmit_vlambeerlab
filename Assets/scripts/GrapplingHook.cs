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

    private Vector3 _grapplePoint;
    private DistanceJoint2D _joint;
    private bool _isGrappling;

    // This is where Velvet Intergration logic primarily be

    [Header("Rope")]
    [SerializeField] private int _numOfRopeSegments = 10; // KEEP NUM OF ROPE SEGMENTS LOW! IT KILLS PERFORMANCE!
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
        _joint = GetComponent<DistanceJoint2D>();
        _joint.enabled = false;
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = _numOfRopeSegments;
        _lineRenderer.enabled = true;

        Vector3 offset = new Vector3(0, 0, -1);

        // Attach the starting point to own gameObject
        _ropeStartPoint = _rb2d.position;

        for (int i = 0; i < _numOfRopeSegments; i++)
        {
            _ropeStartPoint.z = offset.z;
            _ropeSegments.Add(new RopeSegment(_ropeStartPoint));
            _ropeStartPoint.y -= _ropeSegmentLength;
        }

        GameObject pathmakerObject = GameObject.FindGameObjectWithTag("Pathmaker");

        pathmaker = pathmakerObject.GetComponent<Pathmaker>();
    }
    protected override void ComputeGrapplingHook()
    {
    }
    protected override void ComputeGrapplingHookDraw()
    {
        Vector3[] ropePosition = new Vector3[_numOfRopeSegments];
        for (int i = 0; i < _numOfRopeSegments; i++)
        {
            ropePosition[i] = _ropeSegments[i].CurrentPosition;
        }

        _lineRenderer.SetPositions(ropePosition);
    }
    protected override void ComputeGrapplingHookSimulate(bool isGrounded)
    {
        for (int i = 0; i < _ropeSegments.Count; i++)
        {
            // Extract
            RopeSegment segment = _ropeSegments[i];

            // Calculate
            Vector2 velocity = (segment.CurrentPosition - segment.OldPosition) * _dampingFactor;

            segment.OldPosition = segment.CurrentPosition;
            segment.CurrentPosition += velocity;
            segment.CurrentPosition += _gravityForce * Time.fixedDeltaTime;

            // Overwrite
            _ropeSegments[i] = segment;
        }
    }
    // Calculates constraints for Verlet Integration ropes.
    protected override void ComputeGrapplingHookApplyConstraints()
    {
        //Keep first point attached to designated transform
        //Updates overtime.

        Vector3 offset = new Vector3(0, 0, -1);

        RopeSegment firstSegment = _ropeSegments[0];
        firstSegment.CurrentPosition = (Vector3)_rb2d.position + offset;
        _ropeSegments[0] = firstSegment;

        for (int i = 0; i < _numOfRopeSegments - 1; i++)
        {
            // Extract
            RopeSegment currentSeg = _ropeSegments[i];
            RopeSegment nextSeg = _ropeSegments[i + 1];

            // Calculate
            float dist = (currentSeg.CurrentPosition - nextSeg.CurrentPosition).magnitude;
            float difference = (dist - _ropeSegmentLength);

            Vector2 changeDir = (currentSeg.CurrentPosition - nextSeg.CurrentPosition).normalized;
            Vector2 changeVector = changeDir * difference;

            if (i != 0)
            {
                currentSeg.CurrentPosition -= (changeVector * 0.5f);
                nextSeg.CurrentPosition += (changeVector * 0.5f);
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
    protected override void ComputeGrapplingHookHandleCollision()
    {
        for (int i = 1; i < _ropeSegments.Count; i++)
        {
            // Extract
            RopeSegment segment = _ropeSegments[i];

            // Calculate
            Vector2 velocity = segment.CurrentPosition - segment.OldPosition;
            Collider2D[] colliders = new Collider2D[32]; // Limit it down to 16! Performance nukes when too many of those collides trying to compute!
            
            colliders = Physics2D.OverlapCircleAll(segment.CurrentPosition, _collisionRadius, _collisionMask);

            foreach (Collider2D collider in colliders)
            {
                Vector2 closestPoint = collider.ClosestPoint(segment.CurrentPosition);
                float distance = Vector2.Distance(segment.CurrentPosition, closestPoint);

                // if within the collision radius
                if (distance < _collisionRadius)
                {
                    Vector2 normal = (segment.CurrentPosition - closestPoint).normalized;
                    if (normal == Vector2.zero)
                    {
                        // fallback method
                        normal = (segment.CurrentPosition - (Vector2)collider.transform.position).normalized;

                        float depth = _collisionRadius - distance;
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
