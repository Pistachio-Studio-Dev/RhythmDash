using UnityEngine;

public class Vehicle : MonoBehaviour
{
    private VehicleMovement movementComp;
    private PlayerVehicleMovement playerMovementComp;

    [HideInInspector]
    public bool isRidden = false;

    public Vector3 VehicleUP => transform.up; // TODO: change to real visual directions
    public float Throttle => movementComp.Throttle;
    public float AutoSpeed => movementComp.AutoSpeed;
    public float MaxSpeed => movementComp.MaxSpeed;
    public float Steer => movementComp.Steer;
    public bool IsGroundedForces => movementComp.IsGroundedForces;
    public bool InSprint => movementComp.InSprint;
    public float CurrentSpeed => movementComp != null ? movementComp.CurrentSpeed : 0f;
    public bool IsPlayerVehicle => playerMovementComp != null;

    private void Awake()
    {
        movementComp = GetComponent<VehicleMovement>();
        if (movementComp == null)
            movementComp = GetComponentInChildren<VehicleMovement>();

        if (movementComp == null)
        {
            Debug.LogWarning($"[{name}] Vehicle expects a VehicleMovement on this object or child.", this);
        }
        else
        {
            movementComp.SetOwner(this);
            playerMovementComp = movementComp as PlayerVehicleMovement;
        }
    }

    public void SetInput(float throttle, float steer)
    {
        if (playerMovementComp != null)
        {
            playerMovementComp.SetInput(throttle, steer);
        }
        else
        {
            Debug.LogWarning($"[{name}] SetInput called on a non-player Vehicle. Ignoring.", this);
        }
    }

    public void SetOnBeat()
    {
        if (playerMovementComp != null)
        {
            playerMovementComp.SetBeatExtraBurst();
        }
    }
}
