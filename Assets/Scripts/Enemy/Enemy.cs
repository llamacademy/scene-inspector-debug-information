using UnityEngine;

public class Enemy : MonoBehaviour
{
    public EnemyMovement Movement;
    public EnemyAttack Attack;
    public EnemyHealth Health;

    [HideInInspector]
    public bool HasLineOfSight;

    private void Awake()
    {
        Health.Health = Random.Range(10, 100);
    }

    public void OnGainSight(Transform Target)
    {
        if (!HasLineOfSight)
        {
            Movement.Follow(Target);
            Attack.Attack(Target);
            HasLineOfSight = true;
        }
    }

    public void OnLoseSight(Transform Target)
    {
        if (HasLineOfSight)
        {
            Movement.StartRoaming();
            Attack.StopAttacking(Target);
            HasLineOfSight = false;
        }
    }
}
