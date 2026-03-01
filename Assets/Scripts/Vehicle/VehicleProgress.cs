// [ Written by Lindsey, modified by Jiayi ]

using UnityEngine;
using Event;

public class VehicleProgress : MonoBehaviour
{
    private GameObject checkpoints;
    private GameObject teleportPoints;
    private int prevCpIndex = -1;
    private int cpCount = 0;
    private int currLap = 0;
    private bool hasFinished = false;
    [SerializeField] public bool isPlayer = false;
    private double startTime;
    private float raceDuration; 

    public int PreviousCheckpointIndex => prevCpIndex;
    public int NextCheckpointIndex => cpCount > 0 ? (prevCpIndex + 1) % cpCount : -1;
    public int CurrentLap => currLap;
    public int CheckpointCount => cpCount;
    public bool HasFinished => hasFinished;
    public float RaceDuration => raceDuration;
    public float CurrentDuration => (float)(GameplayClock.Time - startTime);

    private void Start() {
        checkpoints = GameObject.Find("Checkpoints");
        teleportPoints = GameObject.Find("Teleport Points");
        cpCount = checkpoints.transform.childCount;
        startTime = GameplayClock.TimeAsDouble;
    }

    void Update()
    {
        if (GameplayClock.IsPaused)
        {
            return;
        }

        if (RuntimeSettings.Instance.EnableTeleport && isPlayer) {
            if (Input.GetKeyDown(KeyCode.Alpha6)) Teleport(6);
            if (Input.GetKeyDown(KeyCode.Alpha5)) Teleport(5);
            if (Input.GetKeyDown(KeyCode.Alpha4)) Teleport(4);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Teleport(3);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Teleport(2);
            if (Input.GetKeyDown(KeyCode.Alpha1)) Teleport(1);
            if (Input.GetKeyDown(KeyCode.Alpha0)) Teleport(0);
        }
    }

    private void Teleport(int tpIndex) {
        Transform tp = teleportPoints.transform.GetChild(tpIndex);
        Transform cp = checkpoints.transform.GetChild(tpIndex);

        VehicleMovement vm = GetComponent<VehicleMovement>();
        vm.FreezeRigidbody();
        Vector3 dir = cp.position - tp.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f) {
            transform.rotation = Quaternion.LookRotation(dir);
            vm.SetYaw(transform.rotation.eulerAngles.y);
        }
        transform.position = tp.position;
        vm.RestoreRigidbody();
    }
    
    public void UpdateNewCheckpoint(int cpIndex) {
        if (hasFinished || cpCount == 0)
            return;

        Debug.Log($"{name} @ checkpoint {cpIndex}");

        if (prevCpIndex == cpCount-1 && cpIndex == 0) {
            FinishLap();
        }

        if (cpIndex == (prevCpIndex + 1) % cpCount) {
            Debug.Log($"{name}: {prevCpIndex} -> {cpIndex}");
            prevCpIndex = cpIndex;
            // if (isPlayer)
            // {
            //     NotificationCenter.Instance?.ShowNotification($"Player Reached\nCheckpoint {cpIndex}\n");
            // }
        } else {
            Debug.Log($"{name} reached checkpoint {cpIndex} but previous checkpoint was {prevCpIndex}");
            if (isPlayer) {
                NotificationCenter.Instance?.ShowNotification($"Skipped to Checkpoint {cpIndex}!\nBrought back to Checkpoint {prevCpIndex}");
                Teleport(prevCpIndex);
                cpIndex = prevCpIndex;
                prevCpIndex = cpIndex - 1;
            }

        }
    }

    private void FinishLap() {
        Debug.Log($"{name} finished lap {currLap}");
        currLap++;
        EventManager.TriggerEvent<VehicleLapFinished, VehicleProgress>(this);

        if (currLap >= GlobalConfig.Instance.LapsPerSession) {
            FinishRace();
        }
    }

    private void FinishRace() {
        if (hasFinished)
            return;

        hasFinished = true;
        Debug.Log($"{name} finished race!");
        raceDuration = (float)(GameplayClock.TimeAsDouble - startTime);

        EventManager.TriggerEvent<VehicleFinished, VehicleProgress>(this);
        
        if (isPlayer) {
            SessionManager.Instance.SetState(SessionState.Finished);
            SoundManager.Instance?.Play("RaceFinish");
            // NotificationCenter.Instance?.ShowNotification("Player\nFinished Race");
        }
    }
}
