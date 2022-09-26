using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyAttack : MonoBehaviour
{
    public Enemy Enemy;
    [SerializeField]
    private ParticleSystem AttackParticleSystem;
    [SerializeField]
    private float TurnSpeed = 3;
    public float AttackRange = 10f;
    private Animator Animator;
    private Transform Target;

    private void Awake()
    {
        Animator = GetComponent<Animator>();
    }

    public void Attack(Transform Target)
    {
        StartCoroutine(TryAttack(Target));
    }

    private IEnumerator TryAttack(Transform Target)
    {
        while (Vector3.Distance(Target.position, transform.position) >= AttackRange)
        {
            yield return null;
        }

        Enemy.Movement.StopMoving();

        this.Target = Target;

        Quaternion targetRotation = Quaternion.LookRotation((Target.position - transform.position).normalized);
        Quaternion startRotation = transform.rotation;
        float time = 0;
        while (time < 1)
        {
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, time);
            time += Time.deltaTime * TurnSpeed;
            yield return null;
        }

        Animator.SetBool("attack", true);
    }

    private void BeginCasting()
    {
        if (AttackParticleSystem != null)
        {
            AttackParticleSystem.gameObject.SetActive(true);
        }
    }

    public void StopAttacking(Transform Target)
    {
        if (AttackParticleSystem != null)
        {
            AttackParticleSystem.gameObject.SetActive(false);
        }
        this.Target = null;
        StopAllCoroutines();
        Animator.SetBool("attack", false);
    }

    private void Update()
    {
        if (Animator.GetBool("attack"))
        {
            transform.LookAt(Target);
            if (Vector3.Distance(Target.position, transform.position) > AttackRange)
            {
                Animator.SetBool("attack", false);
                StopAttacking(Target);
                if (Target != null)
                {
                    Enemy.Movement.Follow(Target);
                }
                else
                {
                    Enemy.OnLoseSight(Target);
                }
            }
        }
    }
}
