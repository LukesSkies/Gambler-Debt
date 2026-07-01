using UnityEngine;

public class EnemyTakeDamage : MonoBehaviour
{
    public enum BodyType
    {
        Head,
        Chest,
        Abdomen
    }

    public EnemyHealth EnemyHealth;
    public EnemyStateMachine EnemyStateMachine;
    public float DamageMultiplier;
    public BodyType BulletBodyType;

    void Start()
    {
        EnemyStateMachine.EnemyColliders.Add(transform.GetComponent<Collider>());
    }
}
