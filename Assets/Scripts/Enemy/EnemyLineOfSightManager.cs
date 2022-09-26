using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.AI;

public class EnemyLineOfSightManager : MonoBehaviour
{
    public Transform Player;
    [Range(0, 2)]
    public float PlayerHeightOffset = 1f;
    [Range(0, 2)]
    public float EnemyHeightOffset = 1f;
    [SerializeField]
    private Enemy[] EnemyPrefabs;
    [SerializeField]
    [Range(1, 500)]
    private int NumberOfEnemies = 50;
    private int LastNumberOFEnemies = 0;
    [Range(0.001f, 1f)]
    public float SpherecastRadius = 0.15f;
    public LayerMask LineOfSightLayers;
    [SerializeField]
    private bool UseJobs;
    [SerializeField]
    [Range(1, 500)]
    private int MinJobSize = 10;

    private NavMeshTriangulation Triangulation;
    public List<Enemy> AliveEnemies = new List<Enemy>();

    private void Awake()
    {
        Triangulation = NavMesh.CalculateTriangulation();
        HandleEnemyNumberChange();
        LastNumberOFEnemies = NumberOfEnemies;
    }

    private void Update()
    {
        if (LastNumberOFEnemies != NumberOfEnemies)
        {
            HandleEnemyNumberChange();
        }

        LastNumberOFEnemies = NumberOfEnemies;

        CheckForLineOfSight();
    }

    private void CheckForLineOfSight()
    {
        if (UseJobs)
        {
            DoJobsLineOfSightCheck();
        }
        else
        {
            DoSingleThreadedLineOfSightCheck();
        }
    }

    private void DoJobsLineOfSightCheck()
    {
        NativeArray<SpherecastCommand> spherecastCommands = new NativeArray<SpherecastCommand>(
            AliveEnemies.Count,
            Allocator.TempJob
        );
        NativeArray<RaycastHit> raycastHits = new NativeArray<RaycastHit>(
            AliveEnemies.Count,
            Allocator.TempJob
        );

        Vector3 playerPosition = Player.position + Vector3.up * PlayerHeightOffset;
        for (int i = 0; i < AliveEnemies.Count; i++)
        {
            Vector3 enemyPosition = AliveEnemies[i].transform.position + Vector3.up * EnemyHeightOffset;
            
            spherecastCommands[i] = new SpherecastCommand(
                enemyPosition,
                SpherecastRadius,
                (playerPosition - enemyPosition).normalized,
                float.MaxValue,
                LineOfSightLayers
            );
        }

        JobHandle spherecastJob = SpherecastCommand.ScheduleBatch(
            spherecastCommands,
            raycastHits,
            MinJobSize
        );

        spherecastJob.Complete();

        for (int i = 0; i < AliveEnemies.Count; i++)
        {
            if (raycastHits[i].collider != null 
                && raycastHits[i].collider.gameObject == Player.gameObject)
            {
                AliveEnemies[i].OnGainSight(Player);
            }
            else
            {
                AliveEnemies[i].OnLoseSight(Player);
            }
        }

        spherecastCommands.Dispose();
        raycastHits.Dispose();
    }

    private void DoSingleThreadedLineOfSightCheck()
    {
        Vector3 playerPosition = Player.position + Vector3.up * PlayerHeightOffset;

        for (int i = 0; i < AliveEnemies.Count; i++)
        {
            Vector3 enemyPosition = AliveEnemies[i].transform.position + Vector3.up * EnemyHeightOffset;
            
            if (Physics.SphereCast(
                enemyPosition,
                SpherecastRadius,
                (playerPosition - AliveEnemies[i].transform.position).normalized,
                out RaycastHit hit,
                float.MaxValue,
                LineOfSightLayers
            ) && hit.collider != null && hit.collider.gameObject == Player.gameObject)
            {
                AliveEnemies[i].OnGainSight(Player);
            }
            else
            {
                AliveEnemies[i].OnLoseSight(Player);
            }
        } 
    }

    private void HandleEnemyNumberChange()
    {
        int difference = Mathf.Abs(LastNumberOFEnemies - NumberOfEnemies);
        if (LastNumberOFEnemies > NumberOfEnemies)
        {
            while (difference > 0)
            {
                Destroy(AliveEnemies[0].gameObject);
                AliveEnemies.RemoveAt(0);
                difference--;
            }
        }
        else
        {
            while (difference > 0)
            {
                Enemy enemy = Instantiate(EnemyPrefabs[Random.Range(0, EnemyPrefabs.Length)],
                    Triangulation.vertices[Random.Range(0, Triangulation.vertices.Length)],
                    Quaternion.identity
                );

                enemy.Movement.Triangulation = Triangulation;
                AliveEnemies.Add(enemy);
                enemy.name = $"Enemy {AliveEnemies.IndexOf(enemy)}";
                difference--;
            }
        }
    }
}
