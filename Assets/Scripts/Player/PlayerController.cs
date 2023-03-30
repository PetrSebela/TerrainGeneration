using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerController : MonoBehaviour
{
    [Header("Camera settings")]
    [SerializeField] private Transform CameraTransform;
    [SerializeField] private Transform CameraOrientation;
    public float LookSensitivity = 1;

    
    [Header("Walking")]
    public float WalkForce;
    public float AcceleratedWalkScale;
    public float WalkingDrag = 6;
    public float FreeFallDrag = 1;
    public float FreeFallAccelerationScale = 0.25f;
    public float JumpForce;

    [Header("Flight")]
    public float FlightForce;
    public float AcceleratedFlightScale;
    public float FlightDrag = 6;    

    
    [Header("Exposed Variables")]
    [SerializeField] [Range(0f,10f)] private float CameraSmoothingValue;
    [SerializeField] [Range(0f,10f)] private float InputSmoothingValue;
     [SerializeField] [Range(0f,10f)] private float AccelerationSmoothingValue;

    [SerializeField] private Camera cam;
    [SerializeField] private int zoomFOV;
    public int normalFOV;
    [SerializeField] private ChunkManager chunkManager;


    [Header("KeyBinds")]
    public KeyCode ToggleMap;
    public KeyCode SwitchControllerType;
    public KeyCode ZoomCamera;
    public KeyCode PauseSimulation;
    public KeyCode FlyUpKey;
    public KeyCode FlyDownKey;
    public KeyCode Accelerate;
    public KeyCode InputSmoothingKey;


    private Vector2 MouseMovement;
    public Vector2 CameraRotation = new Vector2();
    public LayerMask GroundMask;
    private Vector3 Inputs = new Vector3(0, 0, 0);
    private Vector3 WishDirection = new Vector3(0, 0, 0);
    public Rigidbody Rigidbody;
    public GameObject PauseMenu;
    public RectTransform mapTranform;    
    public GameObject PauseMenuArea;

    //* Flags
    private bool FocusOnMap = false;
    private bool IsPaused = false;
    private bool JumpRequest = false;
    private bool Accelerated = false;
    private bool IsGrounded = false;
    private bool IgnoreGround = false;
    public bool InputSmoothing = false;

    private float moveForce = 0;


    private Vector2 RealRotation = Vector2.zero;
    public ControllerType ControllerType = ControllerType.Flight;

    void Start()
    {
        Application.targetFrameRate = 144;
        Rigidbody.drag = FlightDrag;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ToggleSimulation(){
        IsPaused = !IsPaused;

        FocusOnMap = false;
        mapTranform.sizeDelta = new Vector2(150,150);
        mapTranform.anchorMax = Vector2.one;
        mapTranform.anchorMin = Vector2.one;
        mapTranform.anchoredPosition = new Vector2(-25,-25);
        
        WishDirection = Vector3.zero;
        Inputs = Vector3.zero;
        Rigidbody.velocity = Vector3.zero;
        CameraRotation = Vector2.zero;
        RealRotation = Vector2.zero;
        
        Cursor.lockState = (IsPaused)? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = !Cursor.visible;
        
        chunkManager.SimulationState.IsPaused = IsPaused;
        
        PauseMenu.SetActive(IsPaused);
        PauseMenuArea.SetActive(true);
        
        if(!IsPaused && PauseMenu.GetComponentInChildren<Prompt>() != null){
            Destroy(PauseMenu.GetComponentInChildren<Prompt>().transform.gameObject);
        }
    }

    void Update()
    {
        if (!chunkManager.GenerationComplete)
            return;
        
        if (Input.GetKeyDown(PauseSimulation)){
            Inputs = Vector3.zero;
            ToggleSimulation();
        }

        if (IsPaused)
            return;


        FetchFunctionInputs();
        FetchKeyboardInputs();
        FetchMouseInputs();


        //! Placeholder for laptop keyboard without mouse and touchpad lock
        if (Input.GetKey(KeyCode.I))
        {
            MouseMovement.y += 1;
        }
        if (Input.GetKey(KeyCode.K))
        {
            MouseMovement.y -= 1;
        }

        if (Input.GetKey(KeyCode.L))
        {
            MouseMovement.x += 1;
        }

        if (Input.GetKey(KeyCode.J))
        {
            MouseMovement.x -= 1;
        }
    }

    void FetchFunctionInputs(){
        if(Input.GetKeyDown(ToggleMap)){
            FocusOnMap = !FocusOnMap;
            mapTranform.sizeDelta = (FocusOnMap)? new Vector2(512,512) : new Vector2(150,150);
            mapTranform.anchorMax = (FocusOnMap)? new Vector2(0.5f,0.5f) : new Vector2(1,1) ;
            mapTranform.anchorMin = (FocusOnMap)? new Vector2(0.5f,0.5f) : new Vector2(1,1) ;
            mapTranform.anchoredPosition = (FocusOnMap)? new Vector2(256,256) : new Vector2(-25,-25);
        }

        if(Input.GetKeyDown(SwitchControllerType)){
            ControllerType =        (ControllerType==ControllerType.Flight) ? ControllerType.Ground : ControllerType.Flight;
            Rigidbody.useGravity =  (ControllerType==ControllerType.Flight) ? false : true;
            Rigidbody.drag =        (ControllerType==ControllerType.Flight) ? FlightDrag : WalkingDrag;

            if(IsGrounded && ControllerType == ControllerType.Flight){
                Rigidbody.AddForce(Vector3.up * JumpForce,ForceMode.Acceleration);
                IgnoreGround = true;
            }
        }
        if(Input.GetKeyDown(InputSmoothingKey)){
            InputSmoothing = !InputSmoothing;
            chunkManager.UserConfig.InputSmoothing = InputSmoothing;
            UserConfig.SaveConfig(chunkManager.UserConfig);
            
            if (!InputSmoothing){
                CameraRotation = RealRotation;
            }
        }

        if (Input.GetKeyDown(ZoomCamera))
            cam.fieldOfView = zoomFOV;
        
        if (Input.GetKeyUp(ZoomCamera))
            cam.fieldOfView = normalFOV;
    }
    void FetchKeyboardInputs()
    {
        Inputs = Vector3.zero;
        Inputs.x = Input.GetAxisRaw("Horizontal");
        Inputs.y = Input.GetAxisRaw("Vertical");

        Accelerated = Input.GetKey(Accelerate);
        
        if (Input.GetKeyDown(FlyUpKey))
            JumpRequest = true;

        if (Input.GetKey(FlyUpKey))
            Inputs.z++;

        if (Input.GetKey(FlyDownKey))
            Inputs.z--;

        Inputs = Vector3.ClampMagnitude(Inputs,1);

        if(InputSmoothing){
            WishDirection = Vector3.Lerp(WishDirection, CameraOrientation.forward * Inputs.y + CameraOrientation.right * Inputs.x + CameraOrientation.up * Inputs.z, InputSmoothingValue * Time.deltaTime);
        }
        else{
            WishDirection = CameraOrientation.forward * Inputs.y + CameraOrientation.right * Inputs.x + CameraOrientation.up * Inputs.z;
        }
    }

    void FetchMouseInputs(){
        MouseMovement.x = Input.GetAxisRaw("Mouse X");
        MouseMovement.y = Input.GetAxisRaw("Mouse Y");

        CameraRotation.y += MouseMovement.x * LookSensitivity;
        CameraRotation.x -= MouseMovement.y * LookSensitivity;

        CameraRotation.x = Mathf.Clamp(CameraRotation.x, -90f, 90f);

        if(InputSmoothing){
            RealRotation = Vector2.Lerp(RealRotation, CameraRotation, CameraSmoothingValue * Time.deltaTime);
        }
        else{
            RealRotation = CameraRotation;
        }

        CameraTransform.localRotation = Quaternion.Euler(RealRotation.x, 0, 0);
        CameraOrientation.localRotation = Quaternion.Euler(0, RealRotation.y, 0);
    }

    void FixedUpdate()
    {
        if (IsPaused && !chunkManager.GenerationComplete)
            return;

        MovePlayer();
        UpdateSimulationState();
    }

    void UpdateSimulationState()
    {
        chunkManager.SimulationState.ViewerOrientation = CameraRotation;
        chunkManager.SimulationState.ViewerPosition = Rigidbody.position;
        chunkManager.SimulationState.ControllerType = ControllerType;
    }

    void MovePlayer()
    {
        RaycastHit raycastHit;
        Physics.Raycast(this.transform.position, Vector3.down, out raycastHit, 2f);

        IsGrounded = Physics.CheckSphere(this.transform.position - new Vector3(0,1,0), 0.2f, GroundMask);
        // WishDirection.Normalize();


        switch(ControllerType){
            case ControllerType.Flight:                
                Rigidbody.useGravity = false;
                Rigidbody.drag = FlightDrag;
                
                moveForce = Mathf.Lerp(moveForce, FlightForce * ((Accelerated)?AcceleratedFlightScale : 1),AccelerationSmoothingValue); 
                Rigidbody.AddForce(
                    new Vector3(
                        WishDirection.x * moveForce, 
                        WishDirection.y * FlightForce, 
                        WishDirection.z * moveForce), 
                    ForceMode.Acceleration);
                
                JumpRequest = false;

                if (IsGrounded && !IgnoreGround)
                    ControllerType = ControllerType.Ground;

                if(!IsGrounded)
                    IgnoreGround = false;

                break;

            case ControllerType.Ground:
                Rigidbody.useGravity = true;
                moveForce = Mathf.Lerp(moveForce, WalkForce * ((Accelerated)?AcceleratedWalkScale : 1),AccelerationSmoothingValue); 
                
                if (IsGrounded){
                    Rigidbody.drag = WalkingDrag;
                    Rigidbody.AddForce(Vector3.ProjectOnPlane(new Vector3(WishDirection.x,0,WishDirection.z), raycastHit.normal).normalized * moveForce, ForceMode.Acceleration);
                }
                else{
                    Rigidbody.drag = FreeFallDrag;
                    Rigidbody.AddForce(new Vector3(WishDirection.x,0,WishDirection.z) * moveForce * FreeFallAccelerationScale, ForceMode.Acceleration);
                }

                if(IsGrounded && JumpRequest){
                    Rigidbody.AddForce(Vector3.up * JumpForce, ForceMode.Acceleration);
                    JumpRequest = false;
                }
                else
                    JumpRequest = false;

                
                // if (!grounded && JumpRequest){
                //     ControllerType = ControllerType.Flight;
                //     JumpRequest = false;
                // }

                break;
            
            default:
                break;
        }
    }
}


public enum ControllerType
{
    Flight,
    Ground
}