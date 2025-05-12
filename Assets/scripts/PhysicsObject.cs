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

    protected Vector2 _targetVelocity;
    protected Rigidbody2D _rb2d;
    protected Vector2 _velocity;
    protected ContactFilter2D _contactFilter;
    protected RaycastHit2D[] _hitBuffer = new RaycastHit2D[16];
    protected List<RaycastHit2D> _hitBufferList = new List<RaycastHit2D>();

    protected const float minMoveDistance = 0.001f;
    protected const float shellRadius = 0.01f;

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
        ComputeTeleportation();
        ComputeGrapplingHook();
        ComputeGrapplingHookDraw();
    }

    // Protected Virtual Void for added addons
    // Why Protected Virtual Void? It's this code equivalent of adding USB slots into your computer.
    // Additionally Protected Virtual Void grant asset of protected variables to selected key Addons.
    // Physic behaves normally even under the adsent of additional addons. Any uninserted addons will return Null and uncomputed.

    protected virtual void ComputeVelocity()
    {

    }
    protected virtual void ComputeTeleportation()
    {

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
        // Checks if the object is under influence of gravity
        ComputeGrapplingHookSimulate(_isGrounded);

        for (int i = 0; i < _numOfConstrantRuns; i++)
        {
            ComputeGrapplingHookApplyConstraints();

            // Checks if i is divisable by 0
            if (i % _collisionSegmentInterval == 0)
            {
                ComputeGrapplingHookHandleCollision();
            }
        }

        // Provide final velocity and ultimately, location.
        _velocity += _gravityModifier * _gravityForce * Time.deltaTime;
        _velocity.x = _targetVelocity.x;

        _isGrounded = false;

        Vector2 deltaPosition = _velocity * Time.deltaTime;

        Vector2 moveAlongGround = new Vector2(_groundNormal.y, -_groundNormal.x);

        Vector2 move = moveAlongGround * deltaPosition.x;

        // Second value of Movement() excecute whenever the object is moving on Y axis

        Movement(move, false);

        move = Vector2.up * deltaPosition.y;

        Movement(move, true);
    }

    protected bool _isGrounded;

    protected Vector2 _groundNormal;

    void Movement (Vector2 move, bool yMovement)
    {
        float distance = move.magnitude;

        if (distance > minMoveDistance)
        {
            int count = _rb2d.Cast(move, _contactFilter, _hitBuffer, distance + shellRadius);
            _hitBufferList.Clear();

            for (int i = 0; i < count; i++)
            {
                _hitBufferList.Add(_hitBuffer[i]);
            }

            for (int i = 0;i < _hitBufferList.Count; i++)
            {
                Vector2 currentNormal = _hitBufferList[i].normal;
                if (currentNormal.y > _minGroundNormalY)
                {
                    _isGrounded = true;

                    if (yMovement)
                    {
                        _groundNormal = currentNormal;
                        currentNormal.x = 0;
                    }
                }

                float projection = Vector2.Dot(_velocity, currentNormal);

                if (projection < 0)
                {
                    _velocity -= projection * currentNormal;
                }

                float modifiedDistance = _hitBufferList[i].distance - shellRadius;

                distance = modifiedDistance < distance ? modifiedDistance : distance;
            }
        }

        _rb2d.position += move.normalized * distance;

    }
}
