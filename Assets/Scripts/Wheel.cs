
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class Wheel : MonoBehaviour
{
    [Header("References")]
    public Rigidbody carRB;
    public Transform carTransform;
    [SerializeField] RaycastHit hit;
    private CarInfo carMananger;
    private Transform visualWheel;
    public bool grounded;

    [Header("Suspension")]
    public float wheelRadius = 0.5f;
    public float springLength = 0.5f;

    public float springStrength = 100;
    public float springDampening = 30; 
    [SerializeField] private float upforce;

    [Header("Traction")]
    public float traction;
    public float tireMass = 1f;
    public float camber;
    public float caster;
    public float toeIn;

    [Header("Acceleration and Steering")]

    public bool canDrive;
    public bool canSteer;
    public float tireFriction;
    public float brakePower = 5;
    public float maxSpeed = 100;
    public float accelerationMultiplier;
    public float maxSteeringAngle = 20;
    public float steeringPower = 3;
    public float returnSpeed = 1f;

    //Internal state
    private float forwardforce;
    private bool isRight;
    private float sideforce;
    private float normalizedSpeed;
    private float turnInput = 0;
    private float relativeRotation;
    private RaycastHit tireHit;
    private InputAction moveAction;

    void Awake()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        carMananger = GameObject.FindGameObjectWithTag("Player").GetComponent<CarManager>().info;
        //carMananger = transform.parent.GetComponent<CarManager>();
        visualWheel = transform.GetChild(0);
        CarManager.OnSync += Sync;
    }
    public void Sync()
    {
        wheelRadius = carMananger.wheelRadius;
        springLength = carMananger.springLength;
        springDampening = carMananger.springDampening;
        springStrength = carMananger.springStrength;
        traction = carMananger.traction;
        tireMass = carMananger.tireMass;
        tireFriction = carMananger.tireFriction;
        brakePower = carMananger.brakePower;
        maxSpeed = carMananger.maxSpeed;
        accelerationMultiplier = carMananger.accelerationMultiplier;
        maxSteeringAngle = carMananger.maxSteeringAngle;
        steeringPower = carMananger.steeringPower;
        returnSpeed = carMananger.returnSpeed;
        camber = carMananger.camber;
        caster = carMananger.caster;
        toeIn = carMananger.toeIn;
    }
    void Update()
    {
        Debug.DrawRay(transform.position, new Vector3(0, transform.up.y, caster) * upforce / 10, Color.green);
        Debug.DrawRay(transform.position, transform.forward * forwardforce / 10, Color.blue);
        Debug.DrawRay(transform.position, transform.right * sideforce / 10, Color.red);
        Debug.DrawRay(transform.position, -transform.up * wheelRadius, Color.magenta);


        if (Physics.Raycast(transform.position, -transform.up, out tireHit, wheelRadius, 7))
        {
            grounded = true;
        }
        else
        {
            grounded = false;
        }

        float offset = wheelRadius - tireHit.distance;

        if (offset != wheelRadius)
            visualWheel.transform.localPosition = new Vector3(0, offset, 0);
        else
            visualWheel.transform.localPosition = new Vector3(0, 0, 0);
    }   
    void FixedUpdate()
    {
        Suspension(tireHit);
        Traction();
        WheelInput();
    }
    void Suspension(RaycastHit tireHit)
    {
        if (grounded)
        {
            Vector3 tireVelocity = carRB.GetPointVelocity(transform.position);
            float offset = springLength - tireHit.distance;
            float vel = Vector3.Dot(transform.up, tireVelocity);
            upforce = (offset * springStrength) - (vel * springDampening);
            if (upforce < 0)
            {
                upforce = 0;
            }
            if (!canSteer)
            {
                caster = 0;
            }
            carRB.AddForceAtPosition(new Vector3(0, transform.up.y, caster).normalized * upforce, transform.position);
        }
        else
        {
            upforce = 0;
        }
    }
    void Traction()
    {
        if (grounded)
        {
            Debug.DrawRay(transform.position, transform.forward, Color.yellow);
            Vector3 tireVelocity = carRB.GetPointVelocity(transform.position);
            float vel = Vector3.Dot(transform.right, tireVelocity);
            float desiredVel =  -vel * traction;
            float desriedAccel = desiredVel;
            sideforce = desriedAccel * tireMass;
            carRB.AddForceAtPosition(transform.right * sideforce, transform.position);
        }
        else
        {
            sideforce = 0;
        }
    }
    void WheelInput()
    {
        Vector2 moveValue = moveAction.ReadValue<Vector2>();

        float forwardInput = moveValue.y;
        float steeringInput = moveValue.x;

        if (grounded)
        {
            float carSpeed = Vector3.Dot(carTransform.right, carRB.linearVelocity);
            normalizedSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / maxSpeed);
            Debug.DrawRay(carTransform.position, carTransform.right, Color.magenta);
            if (forwardInput > 0f && canDrive && normalizedSpeed < 1f)
            {
                forwardforce = forwardInput * accelerationMultiplier;
                carRB.AddForceAtPosition(transform.forward * forwardforce, transform.position);
            }
            else if (forwardInput < 0f && canDrive)
            {
                forwardforce = forwardInput * brakePower;
                carRB.AddForceAtPosition(transform.forward * forwardforce, transform.position);
            }
            else
            {
                if (carSpeed > 0) forwardforce = -1 * tireFriction;
                if (carSpeed < 0) forwardforce = 1 * tireFriction;
                carRB.AddForceAtPosition(transform.forward * forwardforce, transform.position);
            }
            if (canSteer)
            {

                turnInput = steeringInput * steeringPower * (1.1f - normalizedSpeed);

                relativeRotation = Quaternion.Angle(transform.rotation, carTransform.rotation) - 90;
                if (relativeRotation > -maxSteeringAngle && relativeRotation < maxSteeringAngle)
                {
                    transform.localRotation *= Quaternion.Euler(0, turnInput, 0);
                }
                else if (relativeRotation > maxSteeringAngle && steeringInput < 0)
                {
                    transform.localRotation *= Quaternion.Euler(0, turnInput, 0);
                }
                else if (relativeRotation < -maxSteeringAngle && steeringInput > 0)
                {
                    transform.localRotation *= Quaternion.Euler(0, turnInput, 0);
                }
                if (turnInput == 0)
                {
                    transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(0, 90, isRight ? camber : -camber), returnSpeed * (1.1f - normalizedSpeed) * Time.fixedDeltaTime);
                }
            }
        }
        else
        {
            forwardforce = 0;
        }
    }

    private void OnDestroy()
    {
        CarManager.OnSync -= Sync;
    }

}
