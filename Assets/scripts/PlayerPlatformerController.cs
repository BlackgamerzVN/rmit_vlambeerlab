using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPlatformerController : PhysicsObject
{
    // Movement variables
    [SerializeField] private float _movementSpeed = 7f;
    [SerializeField] private float _jumpTakeOffSpeed = 15f;

    private bool _isMoving;
    private bool _isSlamming;

    // Dash variables
    [SerializeField] private float _dashSpeed = 15f;
    [SerializeField] private float _dashDuration = 0.2f;
    [SerializeField] private float _dashCooldown = 1f;

    private bool _isDashing = false;
    private float _dashTimeRemaining;
    private float _dashCooldownTimer = 0f;
    private Vector2 _dashDirection;

    static Pathmaker pathmaker;
    
    // Start is called before the first frame update
    void Start()
    {
        _isMoving = true;
        _isSlamming = false;

        GameObject pathmakerObject = GameObject.FindGameObjectWithTag("Pathmaker");
        if (pathmakerObject != null)
        {
            pathmaker = pathmakerObject.GetComponent<Pathmaker>();
            if (pathmaker == null)
            {
                Debug.LogError("Pathmaker component not found on object with 'Pathmaker' tag.");
            }
        }
        else
        {
            Debug.LogError("No GameObject found with the 'Pathmaker' tag.");
        }
    }

    protected override void ComputeVelocity()
    {
        Vector2 move = Vector2.zero;

        if (_isDashing)
        {
            HandleDashMovement();
            return;
        }

        if (_isMoving)
        {
            HandleHorizontalMovement(ref move);
            HandleJumpInput();
            HandleSlamInput();

            if (Input.GetKeyDown(KeyCode.LeftShift) && _dashCooldownTimer <= 0)
            {
                StartDash();
                return;
            }
        }

        if (_isSlamming && _isGrounded)
        {
            EndSlam();
        }

        HandleGravityToggleWithMouse();

        _targetVelocity = move * _movementSpeed;

        // Update dash cooldown timer
        if (_dashCooldownTimer > 0)
            _dashCooldownTimer -= Time.deltaTime;
    }

    private void HandleHorizontalMovement(ref Vector2 move)
    {
        move.x = Input.GetAxis("Horizontal");
    }

    private void HandleJumpInput()
    {
        if (Input.GetButtonDown("Jump") && _isGrounded)
        {
            _velocity.y = _jumpTakeOffSpeed;
        }
        else if (Input.GetButtonUp("Jump") && _velocity.y > 0)
        {
            _velocity.y *= 0.5f; // cut jump height when button is released
        }
    }

    private void HandleSlamInput()
    {
        if (Input.GetKeyDown(KeyCode.S) && !_isGrounded)
        {
            const float SlamMultiplier = 2f;
            _velocity.y = -_jumpTakeOffSpeed * SlamMultiplier;
            _isMoving = false;
            _isSlamming = true;
        }
    }

    private void EndSlam()
    {
        _isMoving = true;
        _isSlamming = false;
        _velocity.y = _jumpTakeOffSpeed / 2;
        PerformSlamEffect(_rb2d.position);
    }

    private void HandleGravityToggleWithMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _gravityEnabled = false;
            _velocity.y = 0;
        }

        if (Input.GetMouseButtonUp(0))
        {
            _gravityEnabled = true;
        }
    }
    private void StartDash()
    {
        _isDashing = true;
        _dashTimeRemaining = _dashDuration;
        _dashCooldownTimer = _dashCooldown;

        // Determine dash direction based on input or facing
        float dashInput = Input.GetAxisRaw("Horizontal");
        _dashDirection = dashInput != 0 ? new Vector2(dashInput, 0).normalized : Vector2.right * transform.localScale.x;

        // Optional: disable gravity during dash
        _gravityEnabled = false;
    }

    private void HandleDashMovement()
    {
        _targetVelocity = _dashDirection * _dashSpeed;
        _dashTimeRemaining -= Time.deltaTime;

        if (_dashTimeRemaining <= 0)
        {
            EndDash();
        }
    }

    private void EndDash()
    {
        _isDashing = false;
        _gravityEnabled = true;
    }

    protected override void ComputeGravity(bool i)
    {
        // Simplified override
        base.ComputeGravity(_gravityEnabled);
    }

    private void PerformSlamEffect(Vector2 playerPos)
    {
        if (pathmaker != null)
        {
            const int slamRadius = 1;
            pathmaker.DestructiveTile(playerPos.x, playerPos.y - 1, slamRadius);
        }
        else
        {
            Debug.LogWarning("Attempted to perform slam effect, but 'pathmaker' is null.");
        }
    }
}
