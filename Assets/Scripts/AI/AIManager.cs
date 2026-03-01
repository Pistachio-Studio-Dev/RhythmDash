// [written by Xinyi]

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using System.Collections;

[System.Serializable]
public class AIProfile
{
    public string aiName;
    [Range(0.5f, 2f)] public float speedMultiplier = 1f;
}

public class AIManager : MonoBehaviour
{
    [Header("AI Settings")]
    public GameObject aiPrefab; 
    public SplineContainer spline;
    public VehicleProgress playerProgress;         
    public int aiCount = 8;    
    public AIProfile[] aiProfiles;         

    [Header("Spawn Settings")]
    public Transform[] spawnPoints;      

    [Header("Visual Settings")]
    public GameObject[] skins;           

    private List<GameObject> aiList = new List<GameObject>();

    void Start()
    {
        SpawnAllAI();
    }

    void SpawnAllAI()
    {
        for (int i = 0; i < aiCount; i++)
        {
            Transform spawn = spawnPoints[i % spawnPoints.Length];
            GameObject ai = Instantiate(aiPrefab, spawn.position, spawn.rotation);
            AIVehicleMovement controller = ai.GetComponent<AIVehicleMovement>();

            if (controller == null)
            {
                Debug.LogError($"AI prefab is missing AIController component!", ai);
                continue;
            }

            AIProfile profile = aiProfiles[i % aiProfiles.Length];

            ApplySkin(ai, i);
            SetParameters(controller, profile);
            ai.name = profile.aiName;
            aiList.Add(ai);
        }
    }

    void ApplySkin(GameObject ai, int index)
    {
        Transform visual = ai.transform.Find("Visual");

        if (visual == null)
        {
            Debug.LogError("Missing Visual node!");
            return;
        }

        // Clear existing children before applying new skin
        foreach (Transform child in visual)
        {
            Destroy(child.gameObject);
        }

        GameObject skinInstance = Instantiate(skins[index], visual);
        skinInstance.transform.localPosition = Vector3.zero;
        skinInstance.transform.localRotation = Quaternion.identity;

        StartCoroutine(RebindNextFrame(ai));
    }

    IEnumerator RebindNextFrame(GameObject ai)
    {
        // Wait for Destroy to complete
        yield return null;

        Animator animator = ai.GetComponent<Animator>();
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    void SetParameters(AIVehicleMovement controller, AIProfile profile) 
    {
        controller.spline = spline;
        controller.playerProgress = playerProgress;
        controller.baseSpeedMultiplier = profile.speedMultiplier; 
    }
}