using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KinematicObject : PhysicsObject
{
    // Destroy itself once it hits the ground
    protected override void ComputeVelocity()
    {
        if(_isGrounded)
        {
            Destroy(gameObject);
        }
        else // failsafe
        {
            Destroy(gameObject, 1f);
        }
    }
    protected override void ComputeGravity(bool isUnderGravityForce)
    {
        base.ComputeGravity(true);
    }
}
