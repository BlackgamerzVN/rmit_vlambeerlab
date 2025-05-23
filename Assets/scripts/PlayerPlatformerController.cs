using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerPlatformerController : PhysicsObject
{
    [Header("Local Player Settings")]
    [SerializeField] private int _maxHealth = 5;
    [SerializeField] private Animator _animator;
    private int _currentHealth;

    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _maxHealth;
    public bool IsAlive => _currentHealth > 0;

    [Header("Movement Settings")]
    [SerializeField] private float _movementSpeed = 5f;
    [SerializeField] private float _jumpTakeOffSpeed = 7f;

    [Header("Dash Settings")]
    [SerializeField] private float _dashSpeed = 10f;
    [SerializeField] private float _dashDuration = 0.2f;
    [SerializeField] private float _dashCooldown = 1f;
    [SerializeField] private AudioSource _movementAudio;

    [Header("References")]
    private Pathmaker pathmaker;

    private Camera _mainCamera;

    private bool _isMoving = true;
    private bool _isSlamming = false;
    private bool _isDashing = false;
    private bool _isJumping = false;

    private float _dashTimeRemaining = 0f;
    private float _dashCooldownTimer = 0f;
    private Vector2 _dashDirection;

    private UIHealthBar _uiHealth;

    // Start is called before the first frame update
    void Start()
    {
        _currentHealth = GameManager.Instance.currentHealth;
        _maxHealth = GameManager.Instance.maxHealth;

        if (pathmaker == null)
        {
            GameObject pathmakerObj = GameObject.FindGameObjectWithTag("Pathmaker");
            if (pathmakerObj != null)
            {
                pathmaker = pathmakerObj.GetComponent<Pathmaker>();
                if (pathmaker == null)
                    Debug.LogError("Pathmaker component not found on tagged GameObject.");
            }
            else
            {
                Debug.LogError("No GameObject found with the 'Pathmaker' tag.");
            }
        }
        if (_uiHealth == null)
        {
            GameObject uiHealthObj = GameObject.FindGameObjectWithTag("UIManager");
            if (uiHealthObj != null)
            {
                _uiHealth = uiHealthObj.GetComponent<UIHealthBar>();
                if (_uiHealth == null)
                    Debug.LogError("Pathmaker component not found on tagged GameObject.");
            }
            else
            {
                Debug.LogError("No GameObject found with the 'Pathmaker' tag.");
            }
        }
        _uiHealth?.SetMaxHealth(_maxHealth);
        _uiHealth?.SetHealth(_currentHealth);

        _mainCamera = Camera.main;
        _isMoving = true;
        _isSlamming = false;
        _isJumping = false;

    }
    protected override void ComputeVelocity()
    {
        Vector2 move = Vector2.zero;

        UpdateCameraFollow();

        if (_isDashing)
        {
            HandleDashMovement();
            return;
        }

        if (_isMoving)
        {
            HandleMovementInput(ref move);
            HandleJumpInput();
            HandleSlamInput();
            HandleDashInput();
        }

        if (_isSlamming && _isGrounded)
        {
            EndSlam();
        }

        HandleGravityToggle();
        HandleMoveAnimation(ref move);
        HandleYAnimation(ref _velocity.y);

        _targetVelocity = move * _movementSpeed;

        if (_dashCooldownTimer > 0)
            _dashCooldownTimer -= Time.deltaTime;
    }

    #region Movement & Input
    /// <summary>
    /// True if the player is actively moving via input or dash.
    /// </summary>
    public bool IsPlayerMoving
    {
        get
        {
            if (_isDashing) return true;
            return Mathf.Abs(_targetVelocity.x) > 0.01f;
        }
    }
    public bool IsPlayerInputtingMovement => Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f;
    private void HandleMovementInput(ref Vector2 move)
    {
        move.x = Input.GetAxis("Horizontal");
        if (!IsPlayerInputtingMovement && _isGrounded)
        {
            MovementAudio();
        }
    }
    private void HandleDashInput()
    {
        if (_isDashing || !_isMoving) return; // Don't dash if stunned or knocked

        if (Input.GetKeyDown(KeyCode.LeftShift) && _dashCooldownTimer <= 0 && IsPlayerInputtingMovement)
        {
            StartDash();
        }
    }
    private void HandleJumpInput()
    {
        if (Input.GetButtonDown("Jump") && _isGrounded)
        {
            _velocity.y = _jumpTakeOffSpeed;
            HandleJumpAnimation();
            _isJumping = true;
        }
        else if (Input.GetButtonUp("Jump") && _velocity.y > 0)
        {
            _velocity.y *= 0.5f; // cut jump height when button is released
        }
        if (_isGrounded)
        {
            _isJumping = false;
        }
    }
    private void HandleSlamInput()
    {
        if (!_isMoving || _isGrounded) return; // Prevent slam if stunned or grounded

        if (Input.GetKeyDown(KeyCode.S) && !_isGrounded)
        {
            const float SlamMultiplier = 2f;
            _velocity.y = -_jumpTakeOffSpeed * SlamMultiplier;
            _isMoving = false;
            _isSlamming = true;
        }
    }
    #endregion

    #region Dash
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

    #endregion

    #region Slam

    private void EndSlam()
    {
        _isMoving = true;
        _isSlamming = false;
        _velocity.y = _jumpTakeOffSpeed / 2;
        PerformSlamEffect(_rb2d.position);
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
    #endregion

    #region Player Damage and Knockback

    private bool _isInvincible = false;
    [SerializeField] private float _invincibilityDuration = 1.0f; // Customize in Inspector

    public void TakeDamage(int damage, Vector2 hitDirection, float knockbackForce)
    {
        if (_isInvincible || !IsAlive) return;

        // Recieve twice the damage if stepped on trap when not grounded
        if (_isSlamming)
        {
            _currentHealth -= damage * 4;
        }
        // Recieve four times the damage if stepped on trap when slamming
        else if (!_isGrounded)
        {
            _currentHealth -= damage * 2;
        }
        // Recieve normal the damage if stepped on trap otherwise
        else
        {
            _currentHealth -= damage;
        }
        GameManager.Instance.currentHealth = _currentHealth;
        _uiHealth?.AnimateHealth(_currentHealth);

        if (_currentHealth <= 0)
        {
            Die();
        }
        else
        {
            ApplyKnockback(hitDirection, knockbackForce);
        }
    }
    private void Die()
    {
        Debug.Log("Player has died.");

        _isMoving = false;
        _velocity = Vector2.zero;
        _targetVelocity = Vector2.zero;

        // Optional: play animation, disable input, fade screen, etc.
        gameObject.SetActive(false); // or trigger respawn/death screen

        GameManager.Instance.ResetHealth();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void Heal(int amount)
    {
        _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
        GameManager.Instance.currentHealth = _currentHealth;
        _uiHealth?.AnimateHealth(_currentHealth);
    }
    public void ApplyKnockback(Vector2 direction, float force, float duration = 0.2f)
    {
        StopAllCoroutines(); // Cancel dash or slam coroutines if needed
        StartCoroutine(HandleKnockback(direction.normalized, force, duration));
        StartCoroutine(InvincibilityRoutine()); // Start i-frames
    }
    private IEnumerator HandleKnockback(Vector2 direction, float force, float duration)
    {
        // Interrupt all current movement states
        _isDashing = false;
        _isSlamming = false;
        _isMoving = false;
        _gravityEnabled = true;
        _targetVelocity = Vector2.zero;

        // Apply knockback force
        _velocity = direction * force;

        yield return new WaitForSeconds(duration);

        _isMoving = true;
    }
    private IEnumerator InvincibilityRoutine()
    {
        _isInvincible = true;

        // Optional: flash sprite during i-frames
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float flashInterval = 0.1f;
        float elapsed = 0f;

        while (elapsed < _invincibilityDuration)
        {
            if (sr != null)
            {
                sr.enabled = !sr.enabled;
            }

            elapsed += flashInterval;
            yield return new WaitForSeconds(flashInterval);
        }

        if (sr != null) sr.enabled = true;

        _isInvincible = false;
    }
    #endregion

    #region Camera & Gravity

    private void UpdateCameraFollow()
    {
        Vector3 targetPos = new Vector3(transform.position.x, transform.position.y, _mainCamera.transform.position.z);
        _mainCamera.transform.position = Vector3.MoveTowards(_mainCamera.transform.position, targetPos, _movementSpeed * 5 * Time.deltaTime);
    }

    private void HandleGravityToggle()
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

    protected override void ComputeGravity(bool _)
    {
        base.ComputeGravity(_gravityEnabled);
    }
    #endregion

    #region Animation

    private void HandleMoveAnimation(ref Vector2 move)
    {
        _animator.SetFloat("xVelocity", move.x);
    }
    private void HandleYAnimation(ref float yVelocity)
    {
        _animator.SetFloat("yVelocity", yVelocity);
        _animator.SetBool("IsJumping", _isJumping);
    }
    private void HandleJumpAnimation()
    {
        _animator.SetBool("IsJumping", true);
    }

    #endregion

    #region Audio

    private void MovementAudio()
    {
        _movementAudio.Play();
    }

    #endregion
}
