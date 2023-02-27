using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Transform viewPoint;
    public float mouseSensitivity = 1f;
    private float verticalRotationMovement;
    private Vector2 mouseInput;
    public bool invertLook;
    public float moveSpeed = 5f, runSpeed = 8f;
    private bool isRunning;
    private Vector3 moveDirection, movement;

    public CharacterController characterController;
    private Camera mainCamera;
    public float jumpForce = 12f, gravityModifier = 2.5f;

    public Transform groundCheckPoint;
    private bool isGrounded;
    public LayerMask groundLayers;
    public GameObject bulletImpact;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;

        verticalRotationMovement += mouseInput.y;
        verticalRotationMovement = Mathf.Clamp(verticalRotationMovement, -60f, 60f);

        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);
        viewPoint.rotation = Quaternion.Euler(invertLook ? verticalRotationMovement : -verticalRotationMovement, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
    
        moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

        isRunning = (Input.GetKey(KeyCode.LeftShift)) ? true : false;

        float currentY = movement.y;
        movement = Vector3.Normalize((transform.forward * moveDirection.z) + (transform.right * moveDirection.x)) * ((isRunning) ? runSpeed : moveSpeed);
        movement.y = (!characterController.isGrounded) ? currentY : 0f; // TODO: Replace with raycasy isGrounded?

        isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, .25f, groundLayers);

        if (Input.GetButtonDown("Jump") && isGrounded) {
            movement.y = jumpForce;
        };

        movement.y += Physics.gravity.y * Time.deltaTime * gravityModifier;

        characterController.Move(movement * Time.deltaTime);

        if(Input.GetMouseButtonDown(0)) {
            Shoot();
        }

        if (Input.GetKeyDown(KeyCode.Escape)) {
            Cursor.lockState = CursorLockMode.None;
        }
        
        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None) {
            Cursor.lockState =  CursorLockMode.Locked;
        }
    }

    private void LateUpdate() {
        mainCamera.transform.position = viewPoint.position;
        mainCamera.transform.rotation = viewPoint.rotation;
    }

    private void Shoot() {
        // TODO: Check why the viewpoint takes the size/position of the child if I create the gunplaceholder there
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
        ray.origin = mainCamera.transform.position;

        if (Physics.Raycast(ray, out RaycastHit raycastHit)) {
            Debug.Log(raycastHit.collider.gameObject.name);
            GameObject bulletImpactObj = Instantiate(bulletImpact, raycastHit.point + (raycastHit.normal * 0.002f), Quaternion.LookRotation(raycastHit.normal, Vector3.up));
            Destroy(bulletImpactObj, 10f);
        }
    }
}
