using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPlatformerController : PhysicsObject
{
    public float _maxSpeed = 7f;

    public float _jumpTakeOffSpeed = 21f;

    private bool _isMoving;
    private bool _isSlamming;
    
    static Pathmaker pathmaker;
    
    // Start is called before the first frame update
    void Start()
    {
        _isMoving = true;

        _isSlamming = false;

        GameObject pathmakerObject = GameObject.FindGameObjectWithTag("Pathmaker");

        pathmaker = pathmakerObject.GetComponent<Pathmaker>();

    }

    protected override void ComputeVelocity()
    {
        Vector2 move = Vector2.zero;
        if (_isMoving)
        {
            move.x = Input.GetAxis("Horizontal");

            if (Input.GetButtonDown("Jump") && _isGrounded)
            {
                _velocity.y = _jumpTakeOffSpeed;
            }
            else if (Input.GetButtonUp("Jump"))
            {
                if (_velocity.y > 0)
                {
                    _velocity.y *= .5f;
                }
            }
            else if (Input.GetKeyDown(KeyCode.S) && _isGrounded == false)
            {
                _velocity.y = -_jumpTakeOffSpeed * 2;
                _isMoving=false;
                _isSlamming=true;
            }
        }
        if (_isSlamming && _isGrounded)
        {
            _isMoving = true ;
            _isSlamming = false ;
            SlamProperties(_rb2d.position);
            _velocity.y = _jumpTakeOffSpeed / 2;
        }

        _targetVelocity = move * _maxSpeed;
    }

    static void SlamProperties(Vector2 playerPos)
    {
        float x = playerPos.x;

        float y = playerPos.y;

        int r = 1;

        pathmaker.DestructiveTile(x, y, r);
    }
}
