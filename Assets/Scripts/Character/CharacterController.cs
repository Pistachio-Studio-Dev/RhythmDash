//Written by Yuxuan Liao
using UnityEngine;

public class CharacterController : SessionBasedBehaviour
{
    [SerializeField] public Vehicle vehicle;

    protected float throttleInput;
    protected float steerInput;

    protected virtual void Awake()
    {
        if (vehicle == null)
            vehicle = GetComponent<Vehicle>();

        if (vehicle == null)
            vehicle = GetComponentInChildren<Vehicle>();

        if (vehicle == null)
            Debug.LogWarning($"[{name}] CharacterController expects a Vehicle on this object or child.", this);
        else
        {
            vehicle.isRidden = true;
        }
    }

    public virtual void SetVehicleInput(float throttle, float steer)
    {
        throttleInput = Mathf.Clamp01(throttle);
        steerInput = Mathf.Clamp(steer, -1f, 1f);

        if (vehicle != null)
            vehicle.SetInput(throttleInput, steerInput);
    }

    public virtual void SetVehicleOnBeat()
    {
        if (vehicle != null)
            vehicle.SetOnBeat();
    }
}
