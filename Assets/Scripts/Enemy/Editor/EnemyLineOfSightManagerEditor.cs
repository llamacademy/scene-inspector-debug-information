using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[CustomEditor(typeof(EnemyLineOfSightManager))]
public class EnemyLineOfSightManagerEditor : Editor
{
    private bool ShowDebugUI = true;
    private bool ShowLineOfSightInformation = false;
    private bool ExpandLOSDetails = false;

    private GUIStyle HealthStyle;
    private GUIStyle RaycastStyle;

    private EnemyLineOfSightManager Manager;

    private Dictionary<Enemy, GameObject> LineOfSightData = new();

    private void OnEnable()
    {
        HealthStyle = new GUIStyle()
        {
            normal = new GUIStyleState()
            {
                textColor = Color.green
            },
            fontSize = 18
        };
        RaycastStyle = new GUIStyle()
        {
            normal = new GUIStyleState()
            {
                textColor = Color.red
            },
            fontSize = 14
        };

        Manager = (EnemyLineOfSightManager)target;
        LineOfSightData = new();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Information", EditorStyles.boldLabel);
        ShowDebugUI = EditorGUILayout.Toggle("Show Debug UI", ShowDebugUI);
        ShowLineOfSightInformation = EditorGUILayout.Toggle("Show LOS Info", ShowLineOfSightInformation);

        if (ShowLineOfSightInformation)
        {
            ExpandLOSDetails = EditorGUILayout.Foldout(ExpandLOSDetails, "LOS Details");

            if (ExpandLOSDetails)
            {
                Vector3 playerPosition = Manager.Player.position 
                    + Vector3.up 
                    * Manager.PlayerHeightOffset;

                for (int i = 0; i < Manager.AliveEnemies.Count; i++)
                {
                    Enemy enemy = Manager.AliveEnemies[i];

                    if (Physics.SphereCast(
                        enemy.transform.position,
                        Manager.SpherecastRadius,
                        (playerPosition - enemy.transform.position).normalized,
                        out RaycastHit hit,
                        float.MaxValue,
                        Manager.LineOfSightLayers
                    ) && hit.collider != null)
                    {
                        if (!LineOfSightData.ContainsKey(enemy))
                        {
                            LineOfSightData.Add(enemy, hit.collider.gameObject);
                        }
                        else
                        {
                            LineOfSightData[enemy] = hit.collider.gameObject;
                        }
                    }
                }

                DrawLineOfSightInspector();
            }
        }
    }

    private void DrawLineOfSightInspector()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Enemy", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Object Hit", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Has LOS", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < Manager.AliveEnemies.Count; i++)
        {
            Enemy enemy = Manager.AliveEnemies[i];

            bool hasLOS;
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(enemy.name, EditorStyles.label))
            {
                EditorGUIUtility.PingObject(enemy);
                Selection.activeGameObject = enemy.gameObject;
                SceneView.lastActiveSceneView.FrameSelected();
                Selection.activeGameObject = Manager.gameObject;
            }

            if (LineOfSightData.TryGetValue(enemy, out GameObject hit))
            {
                hasLOS = hit.transform == Manager.Player;
                if (GUILayout.Button(hit.name, EditorStyles.label))
                {
                    EditorGUIUtility.PingObject(hit.gameObject);
                    Selection.activeGameObject = hit.gameObject;
                    SceneView.lastActiveSceneView.FrameSelected();
                    Selection.activeGameObject = Manager.gameObject;
                }

                EditorGUILayout.LabelField(hasLOS.ToString());
            }
            else
            {
                EditorGUILayout.LabelField("N/A");
                EditorGUILayout.LabelField("?");
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    private void OnSceneGUI()
    {
        if (Manager == null)
        {
            return;
        }

        if (ShowDebugUI)
        {
            foreach (Enemy enemy in Manager.AliveEnemies)
            {
                DrawSceneUIForEnemy(enemy);
                if (ShowLineOfSightInformation)
                {
                    DrawSceneUIForLineOfSight(enemy);
                }
            }
        }
    }

    private void DrawSceneUIForEnemy(Enemy Enemy)
    {
        Handles.Label(
            Enemy.transform.position + Vector3.up * 2,
            $"HP: {Enemy.Health.Health}",
            HealthStyle
        );

        if (Enemy.HasLineOfSight)
        {
            if (Vector3.Distance(
                    Enemy.transform.position,
                    Manager.Player.position
                ) > Enemy.Attack.AttackRange)
            {
                Handles.color = Color.yellow;
            }
            else
            {
                Handles.color = Color.green;
            }
        }
        else
        {
            Handles.color = Color.red;
        }
        Handles.DrawWireDisc(Enemy.transform.position, Vector3.up, 0.5f);

        NavMeshAgent agent = Enemy.Movement.GetComponent<NavMeshAgent>();
        if (agent.enabled && agent.hasPath)
        {
            NavMeshPath path = agent.path;
            Vector3[] corners = path.corners;

            Handles.color = Color.cyan;
            for (int i = 1; i < path.corners.Length; i++)
            {
                Handles.DrawLine(corners[i - 1], corners[i]);
                Handles.DrawSolidDisc(corners[i], Vector3.up, 0.125f);
            }

            Handles.color = Color.blue;
            Handles.ArrowHandleCap(
                EditorGUIUtility.GetControlID(FocusType.Passive),
                agent.transform.position + Vector3.up * agent.height,
                agent.transform.rotation,
                agent.velocity.magnitude,
                EventType.Repaint
            );
        }
    }

    private void DrawSceneUIForLineOfSight(Enemy Enemy)
    {
        Vector3 playerPosition = Manager.Player.position + Vector3.up * Manager.PlayerHeightOffset;
        Vector3 enemyPosition = Enemy.transform.position + Vector3.up * Manager.EnemyHeightOffset;
        if (Physics.SphereCast(
                enemyPosition,
                Manager.SpherecastRadius,
                (playerPosition - enemyPosition).normalized,
                out RaycastHit hit,
                float.MaxValue,
                Manager.LineOfSightLayers
            ))
        {
            bool hitPlayer = hit.transform == Manager.Player;
            
            Handles.color = hitPlayer ? Color.green : Color.red;
            Handles.DrawWireDisc(enemyPosition, (playerPosition - enemyPosition).normalized, Manager.SpherecastRadius);
            Handles.DrawDottedLine(enemyPosition, hit.point, 5f);
            if (!hitPlayer)
            {
                Handles.Label(
                    enemyPosition,
                    $"Blocked by\r\n" +
                    $"{hit.transform.name}",
                    RaycastStyle
                );

                Handles.SphereHandleCap(
                    EditorGUIUtility.GetControlID(FocusType.Passive),
                    hit.point, 
                    Quaternion.identity, 
                    Manager.SpherecastRadius, 
                    EventType.Repaint
                );
            }
        }
    }

    private struct EnemyLineOfSightData
    {
        public Enemy Enemy;
        public GameObject HitObject;
    }
}
