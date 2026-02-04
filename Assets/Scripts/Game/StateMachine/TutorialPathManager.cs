using System.Collections.Generic;
using UnityEngine;

public class TutorialPathManager : MonoBehaviour
{
    [Header("Checkpoints del tutorial (en orden)")]
    public List<Transform> tutorialCheckpoints = new List<Transform>();

    public Transform GetClosestCheckpoint(Vector3 position)
    {
        Transform closest = null;
        float minDist = float.MaxValue;
        foreach (var cp in tutorialCheckpoints)
        {
            float dist = Vector3.Distance(position, cp.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = cp;
            }
        }
        return closest;
    }
}
