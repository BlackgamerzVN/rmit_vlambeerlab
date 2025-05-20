using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsObject : MonoBehaviour
{
    [Header("Physic")]
    [SerializeField] private float _minGroundNormalY = 0.65f;
    [SerializeField] private float _gravityModifier = 1f;
    [SerializeField] protected float _dampingFactor = 1f;
    [SerializeField] protected Vector2 _gravityForce = new Vector2(0, -9.8f);
    protected bool _gravityEnabled = true;

    protected Vector2 _targetVelocity;
    protected Rigidbody2D _rb2d;
    protected Vector2 _velocity;
    protected ContactFilter2D _contactFilter;
    protected RaycastHit2D[] _hitBuffer = new RaycastHit2D[16];
    protected List<RaycastHit2D> _hitBufferList = new List<RaycastHit2D>(16);

    protected const float minMoveDistance = 0.001f;
    protected const float _shellRadius = 0.01f;

    private void OnEnable()
    {
        _rb2d = GetComponent<Rigidbody2D>();
    }

    // Start is called before the first frame update
    void Start()
    {
        _contactFilter.useTriggers = false;
        _contactFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        _contactFilter.useLayerMask = true;
    }

    // Update is called once per frame
    void Update()
    {
        _targetVelocity = Vector2.zero;
        ComputeVelocity();
        ComputeGrapplingHook();
        ComputeGrapplingHookDraw();
    }

    // Protected Virtual Void for added addons
    // Why Protected Virtual Void? It's this code equivalent of adding USB slots into your computer.
    // Additionally Protected Virtual Void grant asset of protected variables to selected key Addons.
    // Physic behaves normally even under the adsent of additional addons. Any uninserted addons will return Null and uncomputed.
    /// <summary>
    /// Where player input is primarily applied
    /// </summary>
    protected virtual void ComputeVelocity()
    {
        
    }
    /// <summary>
    /// Applies gravity, moves object, and handles collisions.
    /// </summary>
    protected virtual void ComputeGravity(bool isUnderGravityForce) // Should no plug in available, fall backs to following default
    {
        // Provide final velocity and ultimately, location.
        if (isUnderGravityForce)
        {
            _velocity += _gravityModifier * _gravityForce * Time.deltaTime;
        }

        // Override x velocity from input
        _velocity.x = _targetVelocity.x;

        _isGrounded = false; // Reset grounded flag

        Vector2 deltaPosition = _velocity * Time.deltaTime;

        // Move along the ground for horizontal movement
        Vector2 moveAlongGround = new Vector2(_groundNormal.y, -_groundNormal.x);
        Vector2 move = moveAlongGround * deltaPosition.x;
        Movement(move, false);

        // Apply vertical movement
        move = Vector2.up * deltaPosition.y;
        Movement(move, true);

    }
    protected virtual void ComputeGrapplingHook()
    {

    }
    protected virtual void ComputeGrapplingHookDraw()
    {

    }
    protected virtual void ComputeGrapplingHookSimulate(bool isGrounded)
    {

    }
    protected virtual void ComputeGrapplingHookHandleCollision()
    {

    }
    [Header("Constraints")]
    [SerializeField] private int _numOfConstrantRuns = 50;

    [Header("Optimization")]
    [SerializeField] private int _collisionSegmentInterval = 2;
    protected virtual void ComputeGrapplingHookApplyConstraints()
    {

    }
    void FixedUpdate()
    {
        ComputeGravity(_gravityEnabled);
        
        // Checks if the object is under influence of gravity
        ComputeGrapplingHookSimulate(_isGrounded);

        for (int i = 0; i < _numOfConstrantRuns; i++)
        {
            ComputeGrapplingHookApplyConstraints();

            // Run collision handling every _collisionSegmentInterval iterations
            if (_collisionSegmentInterval > 0 && i % _collisionSegmentInterval == 0)
            {
                ComputeGrapplingHookHandleCollision();
            }
        }
    }

    protected bool _isGrounded;

    protected Vector2 _groundNormal;
    /// <summary>
    /// Performs a collision-aware movement step.
    /// </summary>
    void Movement (Vector2 move, bool yMovement)
    {
        float distance = move.magnitude;

        if (distance > minMoveDistance)
        {
            int count = _rb2d.Cast(move, _contactFilter, _hitBuffer, distance + _shellRadius);
            _hitBufferList.Clear();

            for (int i = 0; i < count; i++)
            {
                _hitBufferList.Add(_hitBuffer[i]);
            }

            for (int i = 0;i < _hitBufferList.Count; i++)
            {
                RaycastHit2D hit = _hitBufferList[i];
                Vector2 currentNormal = hit.normal;

                // Check for ground contact
                if (currentNormal.y > _minGroundNormalY)
                {
                    _isGrounded = true;

                    if (yMovement)
                    {
                        _groundNormal = currentNormal;
                        currentNormal.x = 0; // Prevent horizontal interference
                    }
                }

                // === PUSH LOGIC FOR KINEMATIC BODIES ===
                Rigidbody2D hitBody = hit.rigidbody;
                if (hitBody != null && hitBody.bodyType == RigidbodyType2D.Kinematic && hitBody != _rb2d)
                {
                    // Calculate push direction (opposite of the normal)
                    Vector2 pushDirection = move.normalized;
                    float pushDistance = distance - hit.distance + _shellRadius;

                    // Only push if push distance is meaningful
                    if (pushDistance > 0.001f)
                    {
                        // Attempt to move the hit kinematic body
                        Vector2 targetPosition = hitBody.position + pushDirection * pushDistance;

                        // Optional: You could raycast or check for obstacles before moving
                        hitBody.MovePosition(targetPosition);
                    }
                }

                // Remove velocity component into the surface
                float projection = Vector2.Dot(_velocity, currentNormal);

                if (projection < 0)
                {
                    _velocity -= projection * currentNormal;
                }

                // Adjust movement to avoid overlap
                float modifiedDistance = _hitBufferList[i].distance - _shellRadius;
                distance = modifiedDistance < distance ? modifiedDistance : distance;
                // Equivalent to distance = Mathf.Min(modifiedDistance, distance);
            }
        }
        if (move.sqrMagnitude > 0.0001f)
        {
            _rb2d.position += move.normalized * distance;
        }
    }
}
