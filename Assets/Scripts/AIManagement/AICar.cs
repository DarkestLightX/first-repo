using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AICar : MonoBehaviour
{
    [Header("Suspension")]
    public float wheelRadius = 0.5f;
    public float springLength = 0.5f;
    public float springStrength = 100;
    public float springDampening = 30;

    [Header("Traction")]
    public float traction;
    public float tireMass = 1f;
    public float camber;
    public float caster;
    public float toeIn;

    [Header("Acceleration and Steering")]

    public float tireFriction;
    public float brakePower = 5;
    public float maxSpeed = 100;
    public float accelerationMultiplier;
    public float powerCurve;
    public float maxSteeringAngle = 20;
    public float steeringPower = 3;
    public float returnSpeed = 1f;
    public float boostTime = 0.5f;
    public float boostPower = 5f;
    [Header("Damage")]
    public float resistance;

    [Header("AI Logic")]

    [SerializeField] private float stopDistance = 1;
    [SerializeField] private float detectionDistance = 5;
    public bool forward = false;
    [SerializeField] private List<string> stopTags; 
    [SerializeField] private int targetDist; 
    [SerializeField] private float maxAnger; 
    [SerializeField] [Tooltip("Set as a percentatge of acceptable range for most ai params")] private float acceptableDist; 

    [Header("Internal State")]
    public State currentState {get; private set;}
    private float normalizedDistance;
    private float distance;
    private float targetSpeed;
    public int targetIndex;
    public int lane;
    public float forwardInput {get; private set;}
    public float steeringAngle {get; private set;}
    public float carSpeed {get; private set;}
    [SerializeField] private float currentAnger;
    private Rigidbody rb;
    private RaycastHit hit;
    [SerializeField] private Transform leftPoint;
    [SerializeField] private Transform rightPoint;
    private ProceduralRoad road;


    void Awake()
    {
        if (ProceduralRoad.Instance != null)
            road = ProceduralRoad.Instance;

        ProceduralRoad.onGenerate += ShiftIndex;
        currentState = State.Driving;
        rb = GetComponent<Rigidbody>();
        currentAnger = maxAnger / 2;
    }
    void OnDestroy()
    {
        ProceduralRoad.onGenerate -= ShiftIndex;
    }
    void Update()
    {
        CheckAhead();

        CarLogic();
    }

    public void ShiftIndex()
    {
        targetIndex --;

        if (targetIndex <= 0 || targetIndex >= road.roadPoints.Count)
        {
            Debug.Log("Removing Car...");
            Destroy(transform.gameObject);
            return;
        }
    }
    void CheckAhead()
    {
        if (Physics.Raycast(leftPoint.position, leftPoint.forward, out hit, detectionDistance) || Physics.Raycast(rightPoint.position, rightPoint.forward, out hit, detectionDistance))
        {
            if (stopTags.Contains(hit.collider.transform.tag))
            {
                bool isAI = hit.transform.gameObject.TryGetComponent(out AICar other);

                if (isAI && other.forward == forward || !isAI)
                {

                    Debug.DrawLine(transform.position, hit.point, Color.yellow);

                    distance = hit.distance;

                    //Debug.Log(normalizedDistance);

                    if (distance < stopDistance)
                    {
                        currentState = State.Reversing;
                    }
                    else
                    {
                        currentState = State.Stopping;
                    }
                }
                else
                {
                    currentState = State.Driving;
                }
            }
            else
            {
                currentState = State.Driving;
            } 
        }
        else
        {
            currentState = State.Driving;
        }
    }

    void CarLogic()
    {
        //200 is an arbitrairy plane of death if for some reason gameplay occurs below y -200, this will need to be edited

        if (targetIndex <= 0 || targetIndex >= road.roadPoints.Count || transform.position.y < -200)
        {
            Debug.Log("Removing Car...");
            Destroy(transform.gameObject);
            return;
        }

        carSpeed = Vector3.Dot(transform.right, rb.linearVelocity);

        switch (currentState)
        {
            case State.Stopping:
                normalizedDistance = (distance - stopDistance) / (detectionDistance - stopDistance); //may have risk of dividing by zero, highly unlikely tho
                targetSpeed = maxSpeed * normalizedDistance;

                currentAnger += Time.deltaTime;

                if (currentAnger > maxAnger)
                {
                    if (lane > 1)
                        lane--;
                    else
                        lane++;
                        
                    currentAnger = maxAnger / 2;
                }

                if (road != null)
                    SteeringLogic();

                if (carSpeed > targetSpeed)
                    forwardInput = -1;
                else
                    forwardInput = 1; //* normalizedDistance;
    
                break;
            case State.Reversing:
                normalizedDistance = distance / stopDistance;
                forwardInput = -1 * (1 - normalizedDistance);

                leftPoint.transform.localRotation = Quaternion.Euler(0, 90, 0);
                rightPoint.transform.localRotation = Quaternion.Euler(0, 90, 0);

                break;
            case State.Driving:
                currentAnger -= Time.deltaTime;

                if (currentAnger < 0)
                {
                    if (lane < road.lanes)
                        lane++;
                    currentAnger = maxAnger;
                }

                if (road != null)
                    SteeringLogic();
                forwardInput = 1;
                break;

        }



        //forwardInput = 0;
    }

    void SteeringLogic()
    {
        Vector3 target;

        //calculates and adds the increments of road spacing to the target for driving in different lanes

        float inc = road.roadWidth / (road.lanes * 4);

        if (forward)
            target = road.roadPoints[targetIndex] + (road.pointNormals[targetIndex] * (inc * (lane * 2) - inc));
        else
            target = road.roadPoints[targetIndex] - (road.pointNormals[targetIndex] * (inc * (lane * 2) - inc));

        Vector3 dir = (target - transform.position).normalized;

        float angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
        float dist = Vector3.Distance(transform.position, target);

        //points collision detection towards target
        leftPoint.transform.localRotation = Quaternion.Euler(0, angle, 0);
        rightPoint.transform.localRotation = Quaternion.Euler(0, angle, 0);

        Debug.DrawLine(transform.position, target, Color.burlywood);

        if (angle < 89 || angle > 91)
        {
            steeringAngle = angle;
        }
        else
        {
            steeringAngle = 90;
        }


        if (dist < acceptableDist)
        {
            if (forward)
            {
                targetIndex += targetDist / road.segmentLength;
            }
            else
            {
                targetIndex -= targetDist / road.segmentLength;
            }
        }

    }

    public enum State
    {
        Driving,
        Stopping,
        Reversing
    }
}