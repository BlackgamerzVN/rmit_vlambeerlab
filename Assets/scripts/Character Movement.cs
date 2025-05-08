using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    public float moveSpeed = 10.0f;

    public Rigidbody rb;

    public LayerMask wall;

    Vector3 movement;

    bool isMoving = true;

    bool isSlammingDown = false;

    // Update is called once per frame
    void Update()
    {

        movement.x = Input.GetAxisRaw("Horizontal");
        if (Physics.Raycast(rb.position, Vector3.down, 1f, wall))
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                rb.AddForce(Vector3.up * moveSpeed * 100);
            }
        }
        if (rb.velocity.y != 0)
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                rb.AddForce(Vector3.down * moveSpeed * 300);
                isSlammingDown = true;
                isMoving = false;
            }
        }
        if (Physics.Raycast(rb.position, Vector3.down, 1f, wall) && isSlammingDown == true)
        {
            isSlammingDown = false;
            isMoving = true;
            Debug.Log("Slammed!");
            if (rb.velocity.y > -150)
            {
                rb.AddForce(Vector3.up * moveSpeed * 450);
            }
        }
    }

    void FixedUpdate()
    {
        if (isMoving == true)
        {
            // Regular movement command
            rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
        }
    }
}
