using UnityEngine;

public class DamageManager : MonoBehaviour
{
    private CarManager manager;
    public float health = 100;
    [Tooltip("Divides impulse force by x to evaluate damage. Collision at 108km/h ~1100 force.")]
    public float resistance = 20;
    [SerializeField]
    [Tooltip("Minimum force required to apply damage")]
    private float impulseThreshold = 150;
    void Sync()
    {
        resistance = manager.info.resistance;
    }
    void OnCollisionEnter(Collision collision)
    {
        float damage = 0;
        float impulse = (Mathf.Abs(collision.impulse.x) + Mathf.Abs(collision.impulse.y) + Mathf.Abs(collision.impulse.z));
        if (impulse > impulseThreshold)
        {
            damage = impulse / resistance;
            health -= damage;
        }
        if (damage < 0)
        {
            Debug.Log("Dead");
        }
        //Debug.Log(transform.name + " collided with an impulse force of " + impulse + " resulting in an applied damage of " + damage);       
    }
    private void OnDestroy()
    {
        CarManager.OnSync -= Sync;
    }
    void Awake()
    {
        manager = GameObject.FindGameObjectWithTag("Player").GetComponent<CarManager>();
        CarManager.OnSync += Sync;
    }
} 


