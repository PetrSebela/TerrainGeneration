using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField]
    private float _mouseSens = 1;

    [SerializeField]
    private Transform _cam;
    [SerializeField]
    private Transform _orientation;

    [Header("Movement")]
    [SerializeField]
    private KeyCode _moveDownKey;
    [SerializeField]
    private KeyCode _moveUpKey;
    [SerializeField]
    private KeyCode _acceleratedMovement;


    [SerializeField]
    private float _movementSpeed = 5;
    [SerializeField]
    private float _acceleratedMovementSpeed = 25;
    [SerializeField]
    private float _movementDrag = 6;
    
    [SerializeField]
    private new Camera camera;

    [SerializeField]

    private int zoomFOV;
    [SerializeField]
    private int normalFOV;
    private Vector2 _mouseMovement;
    private Vector2 _cameraRotation;
    private Vector3 _inputs = new Vector3(0, 0, 0);
    private Vector3 _wishDirection = new Vector3(0, 0, 0);
    private Rigidbody _rb;

    public bool IsPaused = false;
    public GameObject PauseMenu;
    [SerializeField] private ChunkManager chunkManager;
    void Start()
    {
        Application.targetFrameRate = 144;
        _rb = GetComponent<Rigidbody>();
        _rb.drag = _movementDrag;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && chunkManager.GenerationComplete){
            _inputs = Vector3.zero;
            IsPaused = !IsPaused;
            Cursor.lockState = (IsPaused)? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = !Cursor.visible;
            PauseMenu.SetActive(IsPaused);
        }

        if (chunkManager.GenerationComplete && !IsPaused)
        {
            if (Input.GetKeyDown(KeyCode.C)){
                camera.fieldOfView = zoomFOV;
            }
            if (Input.GetKeyUp(KeyCode.C)){
                camera.fieldOfView = normalFOV;
            }

            if (Input.GetKeyDown(KeyCode.F)){
                chunkManager.FullRender = ! chunkManager.FullRender;
            }



            _inputs.x = Input.GetAxisRaw("Horizontal");
            _inputs.y = Input.GetAxisRaw("Vertical");
            _inputs.z = 0;

            if (Input.GetKey(_moveUpKey))
                _inputs.z++;
            if (Input.GetKey(_moveDownKey))
                _inputs.z--;

            _wishDirection = _orientation.forward * _inputs.y + _orientation.right * _inputs.x + _orientation.up * _inputs.z;


            _mouseMovement.x = Input.GetAxisRaw("Mouse X");
            _mouseMovement.y = Input.GetAxisRaw("Mouse Y");



            if (Input.GetKey(KeyCode.I))
            {
                _mouseMovement.y += 1;
            }
            if (Input.GetKey(KeyCode.K))
            {
                _mouseMovement.y -= 1;
            }

            if (Input.GetKey(KeyCode.L))
            {
                _mouseMovement.x += 1;
            }

            if (Input.GetKey(KeyCode.J))
            {
                _mouseMovement.x -= 1;
            }


            _cameraRotation.y += _mouseMovement.x * _mouseSens;
            _cameraRotation.x -= _mouseMovement.y * _mouseSens;

            _cameraRotation.x = Mathf.Clamp(_cameraRotation.x, -90f, 90f);

            _cam.localRotation = Quaternion.Euler(_cameraRotation.x, 0, 0);
            _orientation.localRotation = Quaternion.Euler(0, _cameraRotation.y, 0);
        }
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    void MovePlayer()
    {
        if (Input.GetKey(_acceleratedMovement))
            _rb.AddForce(_wishDirection.normalized * _acceleratedMovementSpeed, ForceMode.Acceleration);
        else
            _rb.AddForce(_wishDirection.normalized * _movementSpeed, ForceMode.Acceleration);

    }
}
