using System.Collections;
using UnityEngine;

public class AIWheel : MonoBehaviour
{
    private Rigidbody carRB;
    private Transform carTransform;
    private AICar carMananger;
    private Transform visualWheel;
    //UNIQUE INTERNAL CONDITIONS
    private bool canDrive = false;
    private bool canSteer = false;
    private bool isRight = false;
    [SerializeField] private WheelInfo info;
    [SerializeField] int pointer;
    private bool first = true;
    private float boostTime;

    private float forwardInput = 0;

    private float steeringAngle = 0;

    void Awake()
    {
        transform.gameObject.SetActive(false);
        AIManager.assignValues += Initiate;
        AIWheelCaster.onRemove += AdjustPointer;
    }

    void OnDestroy()
    {
        AIWheelCaster.onRemove -= AdjustPointer;
        AIWheelCaster.Instance.toRemove.Add(pointer);
    }

    void AdjustPointer()
    {
        int prePointer = pointer;
        foreach (int i in AIWheelCaster.Instance.toRemove)
        {
            if (i < prePointer)
            {
                pointer--;
            }
        }
    }

    public void Initiate()
    {
        transform.gameObject.SetActive(true);

        visualWheel = transform.GetChild(0);
        carRB = transform.parent.GetComponent<Rigidbody>();
        carTransform = transform.parent.GetComponent<Transform>();
        carMananger = transform.parent.GetComponent<AICar>();

        boostTime = carMananger.boostTime;

        //important that wheels follow this naming scheme, could cause problems later tho

        if (name == "FL Wheel" || name =="FR Wheel")
        {
            canDrive = true;
            canSteer = true;
        }
        if (name == "FR Wheel" || name == "BR Wheel")
            isRight = true;

        info = new WheelInfo
        {
            wheelRadius = carMananger.wheelRadius,
            springLength = carMananger.springLength,
            springDampening = carMananger.springDampening,
            springStrength = carMananger.springStrength,
            traction = carMananger.traction,
            tireMass = carMananger.tireMass,
            tireFriction = carMananger.tireFriction,
            brakePower = carMananger.brakePower,
            maxSpeed = carMananger.maxSpeed,
            accelerationMultiplier = carMananger.accelerationMultiplier,
            powerCurve = carMananger.powerCurve,
            maxSteeringAngle = carMananger.maxSteeringAngle,
            steeringPower = carMananger.steeringPower,
            returnSpeed = carMananger.returnSpeed,
            camber = carMananger.camber,
            caster = carMananger.caster,
            toeIn = carMananger.toeIn,
            canSteer = this.canSteer,
            isRight = this.isRight,
            canDrive = this.canDrive
        };

        pointer = AIWheelCaster.Instance.info.Count;

        AIWheelCaster.Instance.info.Add(info);
        AIWheelCaster.Instance.transforms.Add(transform);
        AIWheelCaster.Instance.tireVelocitys.Add(carRB.GetPointVelocity(transform.position));
        AIWheelCaster.Instance.speedInput.Add(new Vector3(0, 0, 0));


        AIManager.assignValues -= Initiate;
        
    }
    void FixedUpdate()
    {
        float speed = carMananger.carSpeed;

        forwardInput = carMananger.forwardInput;
        steeringAngle = carMananger.steeringAngle;

        if (boostTime > 0)
        {
            boostTime -= Time.fixedDeltaTime;
            forwardInput = carMananger.boostPower;
        }

        AIWheelCaster.Instance.tireVelocitys[pointer] = carRB.GetPointVelocity(transform.position);
        AIWheelCaster.Instance.speedInput[pointer] = new Vector3(speed, forwardInput, 0f);

        if (!first)
        {
            Vector3 force = AIWheelCaster.Instance.forceBuffer[pointer];
            RaycastHit hit = AIWheelCaster.Instance.hits[pointer];

            //debug params, comment out for performance scenarios
            
            Debug.DrawRay(transform.position, transform.up * force.y / 10, Color.green);
            Debug.DrawRay(transform.position, transform.forward * force.z, Color.blue);
            Debug.DrawRay(transform.position, transform.right * force.x / 10, Color.red);
            Debug.DrawRay(transform.position, -transform.up * info.wheelRadius, Color.magenta);
              
            carRB.AddForceAtPosition(transform.rotation * force, transform.position);

            float offset = info.wheelRadius - hit.distance;

            if (offset != info.wheelRadius)
                visualWheel.transform.localPosition = new Vector3(0, offset, 0);
            else
                visualWheel.transform.localPosition = new Vector3(0, 0, 0);
        }
        else
            first = false;

        //steering is handled in individual wheels due to requiring constant acces to managed type transform

        if (canSteer)
        {
            if (steeringAngle > 90 + carMananger.maxSteeringAngle)
            {
                steeringAngle = 90 + carMananger.maxSteeringAngle;
            }
            else if (steeringAngle < 90 - carMananger.maxSteeringAngle)
            {
                steeringAngle = 90 - carMananger.maxSteeringAngle;
            }

            transform.localRotation = Quaternion.Euler(0, steeringAngle, 0);
        }
    }
}