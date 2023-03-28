using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks
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
    public float muzzleDisplayTime;
    private float muzzleCounter;
    private float shotCounter;
    public float maxHeat = 10f, coolRate = 4f, overHeatCoolRate = 5f;
    private float heatCounter;
    private bool overHeated;
    public Gun[] guns;
    private int selectedGun;

    public GameObject playerHitImpact;

    public int maxHealth = 100;
    private int currentHealth;

    public Animator anim;
    public GameObject playerModel;

    public Transform modelGunPoint, gunHolder; 

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        mainCamera = Camera.main;
        UIController.instance.weaponTempSlider.maxValue = maxHeat;
        photonView.RPC("SetGun", RpcTarget.All, selectedGun);

        currentHealth = maxHealth;

        if (photonView.IsMine) {
            playerModel.SetActive(false);
            
            UIController.instance.healthSlider.maxValue = maxHealth;
            UIController.instance.healthSlider.value = currentHealth;
        } else {
            gunHolder.parent = modelGunPoint;
            gunHolder.localPosition = Vector3.zero;
            gunHolder.localRotation = Quaternion.identity;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (photonView.IsMine) {
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

            if (guns[selectedGun].muzzleFlash.activeInHierarchy) {
                muzzleCounter -= Time.deltaTime;
                if (muzzleCounter <= 0) guns[selectedGun].muzzleFlash.SetActive(false);
            }

            if (!overHeated) {
                if (Input.GetMouseButtonDown(0)) {
                    Shoot();
                }

                if (Input.GetMouseButton(0) && guns[selectedGun].isAutomatic) {
                    shotCounter -= Time.deltaTime;
                    if (shotCounter <= 0) {
                        Shoot();
                    }
                }
                heatCounter -= coolRate * Time.deltaTime;
            } else {
                heatCounter -= overHeatCoolRate * Time.deltaTime;
                if (heatCounter <= 0) {
                    overHeated = false;
                    UIController.instance.overheatedMessage.gameObject.SetActive(false);
                }
            }

            if (heatCounter < 0) {
                heatCounter = 0; // clamp?, better to not decrement in the previous step instead of this line
            }

            UIController.instance.weaponTempSlider.value = heatCounter;

            if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f) {
                selectedGun++;
                if (selectedGun >= guns.Length) selectedGun = 0;
                photonView.RPC("SetGun", RpcTarget.All, selectedGun);
            }

            if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f) {
                selectedGun--;
                if (selectedGun < 0) selectedGun = guns.Length - 1;
                photonView.RPC("SetGun", RpcTarget.All, selectedGun);
            }

            for (int i = 0; i < guns.Length; i++) {
                if (Input.GetKeyDown((i + 1).ToString())) {
                    selectedGun = i;
                    photonView.RPC("SetGun", RpcTarget.All, selectedGun);
                }
            }

            anim.SetBool("grounded", isGrounded);
            anim.SetFloat("speed", moveDirection.magnitude);

            if (Input.GetKeyDown(KeyCode.Escape)) {
                Cursor.lockState = CursorLockMode.None;
            }
            
            if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None) {
                Cursor.lockState =  CursorLockMode.Locked;
            }
        }
    }

    private void LateUpdate() {
        if (photonView.IsMine) {
            mainCamera.transform.position = viewPoint.position;
            mainCamera.transform.rotation = viewPoint.rotation;
        }
    }

    private void Shoot() {
        // TODO: Check why the viewpoint takes the size/position of the child if I create the gunplaceholder there
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
        ray.origin = mainCamera.transform.position;

        if (Physics.Raycast(ray, out RaycastHit raycastHit)) {
            if (raycastHit.collider.gameObject.tag == "Player") {
                PhotonNetwork.Instantiate(playerHitImpact.name, raycastHit.point, Quaternion.identity);
                raycastHit.collider.gameObject.GetPhotonView().RPC("DealDamage", RpcTarget.All, photonView.Owner.NickName, guns[selectedGun].shotDamage);
            } else {
                // What if I use Quaternion.identity for the regular shots instead of the lookrotation?
                GameObject bulletImpactObj = Instantiate(bulletImpact, raycastHit.point + (raycastHit.normal * 0.002f), Quaternion.LookRotation(raycastHit.normal, Vector3.up));
                Destroy(bulletImpactObj, 10f);
            }
        }

        shotCounter = guns[selectedGun].timeBetweenShots;
        heatCounter += guns[selectedGun].heatPerShot;

        if (heatCounter >= maxHeat) {
            heatCounter = maxHeat; // Clamp at addition step?
            overHeated = true;
            UIController.instance.overheatedMessage.gameObject.SetActive(true);
        }

        guns[selectedGun].muzzleFlash.SetActive(true);
        muzzleCounter = muzzleDisplayTime;
    }

    [PunRPC]
    public void DealDamage(string damager, int damageAmount) {
        TakeDamage(damager, damageAmount);
    }

    public void TakeDamage(string damager, int damageAmount) {
        if (photonView.IsMine) {
            currentHealth -= damageAmount;

            if (currentHealth <= 0) {
                currentHealth = 0;
                PlayerSpawner.instance.Die(damager);
            }

            UIController.instance.healthSlider.value = currentHealth;
        }
    }

    void SwitchGun() {
        foreach (Gun gun in guns)
        {
            gun.gameObject.SetActive(false);
        }

        guns[selectedGun].gameObject.SetActive(true);
        guns[selectedGun].muzzleFlash.SetActive(false);
    }

    [PunRPC]
    public void SetGun(int gun) {
        if (gun < guns.Length) {
            selectedGun = gun;
            SwitchGun();
        }
    }
}
