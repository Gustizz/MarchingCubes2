using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    [Header("Player Stats")]
    private Rigidbody rb;
    public float gravity;
    public float moveSpeed;
    public float jumpForce;

    public float mouseSensitivityX;
    public float mouseSensitivityY;
    public Vector2 lookAngleMinMax = new Vector2(-75, 80);

    public Transform cam;

    Vector3 desiredLocalVelocity;
    float verticalLookRotation;
    Vector3 smoothMoveVelocity;

    
    public bool pauseMovement;

    
    // Start is called before the first frame update
    private void Awake()
    {
        Time.fixedDeltaTime = 1f / 60f;

    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            pauseMovement = !pauseMovement;
            Cursor.visible = pauseMovement;
            Cursor.lockState = (pauseMovement) ? CursorLockMode.None : CursorLockMode.Locked;
            
            if (pauseMovement)
            {
                desiredLocalVelocity = Vector3.zero;
                rb.velocity = Vector3.zero;
            }
        }

        // Look rotation:
        transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * mouseSensitivityX);
        verticalLookRotation += Input.GetAxis("Mouse Y") * mouseSensitivityY;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, lookAngleMinMax.x, lookAngleMinMax.y);
        cam.transform.localEulerAngles = Vector3.left * verticalLookRotation;

        // Calculate movement:
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");
        
        Vector3 moveDir = new Vector3(inputX, 0, inputY).normalized;
        Vector3 targetMoveVelocity = moveDir * moveSpeed;
        desiredLocalVelocity = Vector3.SmoothDamp(desiredLocalVelocity, targetMoveVelocity, ref smoothMoveVelocity, .15f);
        
        if (Input.GetButtonDown("Jump"))
        {
            bool grounded = true;
            if (grounded)
            {
                rb.AddForce(transform.up * jumpForce, ForceMode.VelocityChange);
            }
        }
    }
    
    void FixedUpdate()
    {


        Vector3 planetCentre = Vector3.zero;
        Vector3 gravityUp = (rb.position - planetCentre).normalized;

        // Align body's up axis with the centre of planet
        Vector3 localUp = LocalToWorldVector(rb.rotation, Vector3.up);
        rb.rotation = Quaternion.FromToRotation(localUp, gravityUp) * rb.rotation;

        rb.velocity = CalculateNewVelocity(localUp);


    }
    
    // Transform vector from local space to world space (based on rotation)
    public static Vector3 LocalToWorldVector(Quaternion rotation, Vector3 vector)
    {
        return rotation * vector;
    }
    
    // Transform vector from world space to local space (based on rotation)
    public static Vector3 WorldToLocalVector(Quaternion rotation, Vector3 vector)
    {
        return Quaternion.Inverse(rotation) * vector;
    }
    
    Vector3 CalculateNewVelocity(Vector3 localUp)
    {
        // Apply movement and gravity to rigidbody
        float deltaTime = Time.fixedDeltaTime;
        Vector3 currentLocalVelocity = WorldToLocalVector(rb.rotation, rb.velocity);

        float localYVelocity = currentLocalVelocity.y + (-gravity) * deltaTime;

        Vector3 desiredGlobalVelocity = LocalToWorldVector(rb.rotation, desiredLocalVelocity);
        desiredGlobalVelocity += localUp * localYVelocity;
        return desiredGlobalVelocity;
    }
}
