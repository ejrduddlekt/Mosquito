using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ECM2;


public class PlayerMovement : Character
{
    private void Update()
    {
        // Movement input

        Vector2 inputMove = new Vector2()
        {
            x = Input.GetAxisRaw("Horizontal"),
            y = Input.GetAxisRaw("Vertical")
        };

        Vector3 movementDirection = Vector3.zero;

        movementDirection += Vector3.right * inputMove.x;
        movementDirection += Vector3.forward * inputMove.y;

        // If character has a camera assigned...

        if (camera)
        {
            // Make movement direction relative to its camera view direction

            movementDirection = movementDirection.relativeTo(cameraTransform);
        }

        SetMovementDirection(movementDirection);

        // Crouch input

        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C))
            Crouch();
        else if (Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.C))
            UnCrouch();

        // Jump input

        if (Input.GetButtonDown("Jump"))
            Jump();
        else if (Input.GetButtonUp("Jump"))
            StopJumping();
    }
}
