using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class EnemyMovement : MonoBehaviour
{
    public NavMeshTriangulation Triangulation;
    private NavMeshAgent Agent;
    private Animator Animator;
    [SerializeField]
    [Range(0f, 3f)]
    private float WaitDelay = 1f;

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Animator = GetComponent<Animator>();
    }

    private void Start()
    {
        StartRoaming();
    }

    public void StartRoaming()
    {
        StopAllCoroutines();
        StartCoroutine(Roam());
    }

    public void Follow(Transform Target)
    {
        StopAllCoroutines();
        StartCoroutine(FollowTarget(Target));
    }

    public void StopMoving()
    {
        StopAllCoroutines();
        Agent.enabled = false;
    }

    private IEnumerator FollowTarget(Transform Target)
    {
        Agent.enabled = true;
        Agent.SetDestination(Target.position);
        while(true)
        {
            Agent.SetDestination(Target.position);
            yield return null;
        }
    }

    private IEnumerator Roam()
    {
        Agent.enabled = true;
        WaitForSeconds Wait = new WaitForSeconds(WaitDelay);
        while (true)
        {
            int index = Random.Range(1, Triangulation.vertices.Length - 1);
            Agent.SetDestination(Vector3.Lerp(
                Triangulation.vertices[index],
                Triangulation.vertices[index + (Random.value > 0.5f ? -1 : 1)],
                Random.value)
            );

            yield return null;
            yield return new WaitUntil(() => Agent.remainingDistance <= Agent.stoppingDistance);
            yield return Wait;
        }
    }

    private void Update()
    {
        if (Agent.enabled)
        {
            float locomotion = Agent.velocity.magnitude / Agent.speed;
            Animator.SetFloat("locomotion", locomotion);
            Animator.SetBool("move", locomotion > 0.25f);
            if (locomotion > 0.01f)
            {
                Animator.speed = locomotion / 1.5f; 
            }
            else
            {
                Animator.speed = 1;
            }
        }
        else
        {
            Animator.speed = 1;
            Animator.SetBool("move", false);
        }
    }

    private void PlayAudio()
    {
        // Do nothing
    }
}
