using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCameraMovement : MonoBehaviour
{
    public Transform cameraTransform;

    // --- Tweak these values in the Inspector ---
    [Header("Look Settings")]
    public float mouseSensitivity = 2f;
    public Vector2 lookAngleMinMax = new Vector2(-80f, 80f); // Min/max vertical look angle
    [Tooltip("A smaller value makes the camera more responsive; a larger value makes it smoother.")]
    public float rotationSmoothTime = 0.05f;

    // --- Private variables for tracking rotation ---
    private float yaw;   // Rotation around the player's up axis (left/right)
    private float pitch; // Rotation around the camera's right axis (up/down)

    private float yawSmoothV;   // Velocity for yaw smoothing (used by SmoothDampAngle)
    private float pitchSmoothV; // Velocity for pitch smoothing

    private float currentYaw;
    private float currentPitch;

    void Update()
    {
        // 1. Get the raw mouse input and scale it by sensitivity.
        // We do this in Update() as it's best for handling input.
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity; // Inverted for standard controls

        // 2. Clamp the vertical rotation (pitch) to prevent the camera from flipping over.
        pitch = Mathf.Clamp(pitch, lookAngleMinMax.x, lookAngleMinMax.y);
    }

    void LateUpdate()
    {
        // 3. Smooth the target rotation values using SmoothDampAngle.
        // This is the magic that removes the choppiness!
        currentYaw = Mathf.SmoothDampAngle(currentYaw, yaw, ref yawSmoothV, rotationSmoothTime);
        currentPitch = Mathf.SmoothDampAngle(currentPitch, pitch, ref pitchSmoothV, rotationSmoothTime);

        // 4. Apply the rotations. We do this in LateUpdate() to ensure it runs
        // after the player's movement and physics have been calculated for the frame.

        // Rotate the player body left/right (yaw). This is crucial for a planet,
        // as it rotates around the player's specific "up" direction.
        transform.localRotation = Quaternion.AngleAxis(currentYaw, transform.up);

        // Rotate the camera up/down (pitch). This rotates around the camera's local right axis.
        cameraTransform.localRotation = Quaternion.AngleAxis(currentPitch, Vector3.right);
    }

}
