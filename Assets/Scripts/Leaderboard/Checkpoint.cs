// [ Written by Lindsey ]

using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public static Dictionary<int, Checkpoint> Instances {get; private set;} = new Dictionary<int, Checkpoint>();
    
    private int cpIndex;

    private void Awake() {
        cpIndex = transform.GetSiblingIndex();
        Instances[cpIndex] = this;
    }

    private void OnTriggerEnter(Collider vehicleCollider) {
        VehicleProgress vehicleProgress = vehicleCollider.GetComponentInParent<VehicleProgress>();
        if (vehicleProgress != null) {
            vehicleProgress.UpdateNewCheckpoint(cpIndex);
        }
    }

    private void OnDestroy()
    {
        Instances.Remove(cpIndex);
    }
}
