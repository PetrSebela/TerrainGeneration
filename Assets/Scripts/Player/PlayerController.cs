using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField]
    private Transform _cam;
    
    [SerializeField]
    private Transform _orientation;

    [Header("Key Binds")]
    [SerializeField]
    private KeyCode _moveDownKey;
    [SerializeField]
    private KeyCode _moveUpKey;
    [SerializeField]
    private float _mouseSens = 1;

    [Header("Controller settings")]
    public PlayerControllerType ControllerType = PlayerControllerType.Flight;

    [Header("Walking")]
    public float WalkForce;
    public float AcceleratedWalkScale;
    private float WalkingDrag = 12;    

    [Header("Flight")]
    public float FlightForce;
    public float AcceleratedFlightScale;
    private float FlightDrag = 6;    
    
    [Header("Exposed Variables")]
    [SerializeField] private Camera cam;
    [SerializeField] private int zoomFOV;
    [SerializeField] private int normalFOV;
    [SerializeField] private ChunkManager chunkManager;

    private Vector2 _mouseMovement;
    private Vector2 cameraRotation;
    private Vector3 _inputs = new Vector3(0, 0, 0);
    private Vector3 WishDirection = new Vector3(0, 0, 0);
    private Rigidbody rb;

    public GameObject Sun;
    public GameObject Moon;


    public bool IsPaused = false;
    public GameObject PauseMenu;
    
    private bool useGravityController = false;
    public RectTransform mapTranform;    
    public bool FocusOnMap = false;
    private bool Accelerated = false;

    

    void Start()
    {
        Application.targetFrameRate = 144;
        rb = GetComponent<Rigidbody>();
        rb.drag = FlightDrag;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!chunkManager.GenerationComplete)
            return;
        
        if (Input.GetKeyDown(KeyCode.Escape)){
            _inputs = Vector3.zero;
            IsPaused = !IsPaused;
            Cursor.lockState = (IsPaused)? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = !Cursor.visible;
            PauseMenu.SetActive(IsPaused);
        }

        if (Input.GetKeyDown(KeyCode.T)){
            Sun.SetActive(!Sun.activeSelf);
            Moon.SetActive(!Moon.activeSelf);
            RenderSettings.ambientIntensity = (Sun.activeSelf)?1f:0.2f;
        }


        if(Input.GetKeyDown(KeyCode.Tab)){
            FocusOnMap = !FocusOnMap;
            mapTranform.sizeDelta = (FocusOnMap)? new Vector2(1024,1024) : new Vector2(150,150);
        }



        if(Input.GetKeyDown(KeyCode.X)){
            ControllerType = (ControllerType==PlayerControllerType.Flight)?PlayerControllerType.Ground : PlayerControllerType.Flight;

            useGravityController = !useGravityController;
            rb.useGravity = useGravityController;
            rb.drag = (useGravityController)? WalkingDrag:FlightDrag;
        }

        if (IsPaused)
            return;

        if (Input.GetKeyDown(KeyCode.C)){
            cam.fieldOfView = zoomFOV;
        }
        if (Input.GetKeyUp(KeyCode.C)){
            cam.fieldOfView = normalFOV;
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

        WishDirection = _orientation.forward * _inputs.y + _orientation.right * _inputs.x + _orientation.up * _inputs.z;

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

        if (Input.GetKeyDown(KeyCode.LeftShift))
            Accelerated = true;
        else if (Input.GetKeyUp(KeyCode.LeftShift))
            Accelerated = false;

        cameraRotation.y += _mouseMovement.x * _mouseSens;
        cameraRotation.x -= _mouseMovement.y * _mouseSens;

        cameraRotation.x = Mathf.Clamp(cameraRotation.x, -90f, 90f);

        _cam.localRotation = Quaternion.Euler(cameraRotation.x, 0, 0);
        _orientation.localRotation = Quaternion.Euler(0, cameraRotation.y, 0);
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    void MovePlayer()
    {
        float force;
        switch(ControllerType){
            case PlayerControllerType.Flight:
                force = FlightForce * ((Accelerated)?AcceleratedFlightScale : 1); 
                rb.AddForce(WishDirection * force, ForceMode.Acceleration);
                break;
            
            case PlayerControllerType.Ground:
                force = WalkForce * ((Accelerated)?AcceleratedWalkScale : 1); 
                RaycastHit hit;

                if (Physics.Raycast(this.transform.position, Vector3.down, out hit, 3)){
                    rb.AddForce(Vector3.ProjectOnPlane(WishDirection, hit.normal) * force, ForceMode.Acceleration);
                }
                else{
                    rb.AddForce(WishDirection * force, ForceMode.Acceleration);
                }
                break;
            
            default:
                break;
        }
    }
}


public enum PlayerControllerType
{
    Flight,
    Ground
}