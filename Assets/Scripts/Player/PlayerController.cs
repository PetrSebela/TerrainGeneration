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
    public float _mouseSens = 1;

    [Header("Controller settings")]
    public ControllerType ControllerType = ControllerType.Flight;

    [Header("Walking")]
    public float WalkForce;
    public float AcceleratedWalkScale;
    private float WalkingDrag = 12;
    public float JumpForce;

    [Header("Flight")]
    public float FlightForce;
    public float AcceleratedFlightScale;
    public float FlightDrag = 6;    
    
    [Header("Exposed Variables")]
    [SerializeField] private Camera cam;
    [SerializeField] private int zoomFOV;
    [SerializeField] private int normalFOV;
    [SerializeField] private ChunkManager chunkManager;

    [Header("KeyBinds")]
    public KeyCode ToggleMap;
    public KeyCode DayNightSwitch;
    public KeyCode SwitchControllerType;
    public KeyCode ZoomCamera;
    public KeyCode PauseSimulation;
    public KeyCode FlyUpKey;
    public KeyCode FlyDownKey;
    public KeyCode Accelerate;
    public KeyCode ToggleShadows;





    private Vector2 MouseMovement;
    public Vector2 cameraRotation = new Vector2();
    private Vector3 Inputs = new Vector3(0, 0, 0);
    private Vector3 WishDirection = new Vector3(0, 0, 0);
    private Rigidbody rb;

    public GameObject Sun;
    public GameObject Moon;
    public Light MainLight;


    public bool IsPaused = false;
    public GameObject PauseMenu;
    
    private bool useGravityController = false;
    public RectTransform mapTranform;    
    public bool FocusOnMap = false;
    private bool Accelerated = false;

    public bool Jump = false;
    public bool CastShadows = true;

    public Material DaySkybox;
    public Material NightSkybox;

    public Color DayFogColor;
    public Color NightFogColor;
    

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
        
        if (Input.GetKeyDown(PauseSimulation)){
            Inputs = Vector3.zero;
            IsPaused = !IsPaused;

            Cursor.lockState = (IsPaused)? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = !Cursor.visible;
            
            chunkManager.simulationState.IsPaused = IsPaused;
            PauseMenu.SetActive(IsPaused);
        }

        if (IsPaused)
            return;

        if (Input.GetKeyDown(DayNightSwitch)){
            Sun.SetActive(!Sun.activeSelf);
            Moon.SetActive(!Moon.activeSelf);
            RenderSettings.ambientIntensity = (Sun.activeSelf)?1f:0.25f;
            RenderSettings.skybox = (Sun.activeSelf)?DaySkybox:NightSkybox;
            RenderSettings.fogColor = (Sun.activeSelf)?DayFogColor:NightFogColor;
        }


        if(Input.GetKeyDown(ToggleMap)){
            FocusOnMap = !FocusOnMap;
            mapTranform.sizeDelta = (FocusOnMap)? new Vector2(512,512) : new Vector2(150,150);
            mapTranform.anchorMax = (FocusOnMap)? new Vector2(0.5f,0.5f) : new Vector2(1,1) ;
            mapTranform.anchorMin = (FocusOnMap)? new Vector2(0.5f,0.5f) : new Vector2(1,1) ;
            mapTranform.anchoredPosition = (FocusOnMap)? new Vector2(256,256) : new Vector2(-25,-25);

        }

        if(Input.GetKeyDown(ToggleShadows)){
            CastShadows = !CastShadows;
            Sun.GetComponent<Light>().shadows = (CastShadows)?LightShadows.Soft:LightShadows.None;
            Moon.GetComponent<Light>().shadows = (CastShadows)?LightShadows.Soft:LightShadows.None;
        }

        if(Input.GetKeyDown(SwitchControllerType)){
            ControllerType = (ControllerType==ControllerType.Flight)?ControllerType.Ground : ControllerType.Flight;
            useGravityController = !useGravityController;
            rb.useGravity = useGravityController;
            rb.drag = (useGravityController)? WalkingDrag:FlightDrag;
        }


        if (Input.GetKeyDown(ZoomCamera)){
            cam.fieldOfView = zoomFOV;
        }

        if (Input.GetKeyUp(ZoomCamera)){
            cam.fieldOfView = normalFOV;
        }


        Inputs.x = Input.GetAxisRaw("Horizontal");
        Inputs.y = Input.GetAxisRaw("Vertical");
        Inputs.z = 0;

        if (Input.GetKeyDown(Accelerate))
            Accelerated = true;
        else if (Input.GetKeyUp(Accelerate))
            Accelerated = false;

        if (Input.GetKey(FlyUpKey))
        {
            Inputs.z++;
            Jump = true;
        }

        if (Input.GetKey(FlyDownKey))
            Inputs.z--;

        WishDirection = _orientation.forward * Inputs.y + _orientation.right * Inputs.x + _orientation.up * Inputs.z;

        MouseMovement.x = Input.GetAxisRaw("Mouse X");
        MouseMovement.y = Input.GetAxisRaw("Mouse Y");


        //! Placeholder for laptop keyboard without mouse
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


        cameraRotation.y += MouseMovement.x * _mouseSens;
        cameraRotation.x -= MouseMovement.y * _mouseSens;

        cameraRotation.x = Mathf.Clamp(cameraRotation.x, -90f, 90f);

        _cam.localRotation = Quaternion.Euler(cameraRotation.x, 0, 0);
        _orientation.localRotation = Quaternion.Euler(0, cameraRotation.y, 0);
    }

    void FixedUpdate()
    {
        if (!IsPaused && chunkManager.GenerationComplete){
            MovePlayer();
            UpdateSimulationState();
        }
    }

    void UpdateSimulationState()
    {
        chunkManager.simulationState.ViewerOrientation = cameraRotation;
        chunkManager.simulationState.ViewerPosition = rb.position;
        chunkManager.simulationState.ControllerType = ControllerType;
    }

    void MovePlayer()
    {
        float force;
        switch(ControllerType){
            case ControllerType.Flight:
                rb.useGravity = false;
                force = FlightForce * ((Accelerated)?AcceleratedFlightScale : 1); 
                rb.AddForce(WishDirection * force, ForceMode.Acceleration);
                break;
            
            case ControllerType.Ground:
                rb.useGravity = true;
                force = WalkForce * ((Accelerated)?AcceleratedWalkScale : 1); 
                RaycastHit hit;

                bool grounded = Physics.Raycast(this.transform.position, Vector3.down, out hit, 3);
                if (grounded){
                    rb.AddForce(Vector3.ProjectOnPlane(new Vector3(WishDirection.x,0,WishDirection.z), hit.normal) * force, ForceMode.Acceleration);
                }
                else{
                    rb.AddForce(new Vector3(WishDirection.x,0,WishDirection.z) * force, ForceMode.Acceleration);
                }
                if(grounded && Jump){
                    rb.AddForce(hit.normal * JumpForce,ForceMode.Acceleration);
                    Jump = false;
                }
                else if(Jump){
                    Jump = false;
                }
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