using Event;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.XInput;

public class TutorialLevel : SessionBasedBehaviour
{
    [SerializeField] private GameObject TrackEndPrefab;
    [SerializeField] private GameObject StartFinish;
    [SerializeField] private GameObject Tracks;
    [SerializeField] private GameObject[] PowerUpPrefabs;
    [SerializeField] private string[] PowerUpInstructions;
    [SerializeField] private PlayerCharacterController Player;
    [SerializeField] private GameObject CheckPointTarget;
    [SerializeField] private GameObject TutorialEnemyPrefab;
    [SerializeField] private Vector3 TutorialEnemySpawnOffset;

    public enum ControllerType
    {
        Keyboard,
        Xbox,
        PlayStation,
        Switch,
        GenericGamepad
    }

    private class TutorialSubgoal
    {
        public Func<bool> CheckCompletion;
        public Action OnComplete;
        public bool IsComplete = false;
    }

    private class TutorialTask
    {
        public Action OnStart; // Fired when the task begins
        public Func<ControllerType, string> GetPromptText;
        public Func<bool> CheckCompletion; // Fallback if no subgoals are used
        public Action OnComplete; // Fired when the task ends

        public List<TutorialSubgoal> Subgoals = new List<TutorialSubgoal>();
    }

    private Queue<TutorialTask> tutorialQueue = new Queue<TutorialTask>();
    private TutorialTask currentTask;

    private InputAction moveAction;
    private InputAction sprintAction;
    private ControllerType lastController;

    // Track Management Variables
    private List<Transform> trackBlocks = new List<Transform>();
    private Vector3 trackDirection;
    private float blockLength;

    // Player Progress Variables
    private Vector3 initialPlayerPosition;
    private Vector3 initialPlayerDirection;
    private bool startFinishActive = true;
    private bool tutorialComplete = false;

    // End Block Avoidance Variables
    private bool hasEndBlock = false;
    private float preEndProj = 0f;
    private float postEndProj = 0f;

    // PowerUp Task Variables
    private bool powerUpCollected = false;

    private int beatHitCount = 0;
    private float lastBeatHitTime;
    private float lastPickupTime;
    private float lastEnemyHitTime;
    private bool tutorialEnemyHit = false;
    private bool tutorialFinished = false;

    private GameObject activePowerUp;
    private GameObject activeTutorialEnemy;
    void Start()
    {
        moveAction = InputSystem.actions?.FindAction("Move");
        sprintAction = InputSystem.actions?.FindAction("Sprint");

        if (Player != null)
        {
            initialPlayerPosition = Player.VehicleTransform.position;
            initialPlayerDirection = Player.VehicleTransform.forward;
        }
        lastController = GetInitialControllerType();
        InitializeTrackArray();
        BuildTutorialQueue();
    }

    void Update()
    {
        UpdateTrackInfinitely();
        CheckStartFinishProgress();

        ManageActivePowerUp();

        if (tutorialFinished && Input.anyKeyDown)
        {
            PauseManager.Instance?.Pause();
        }

        if (currentTask != null)
        {
            ControllerType currentController = GetActiveControllerType();
            if (currentController != lastController)
            {
                UpdateTutorialPrompt();
            }

            bool isTaskFinished = false;

            if (currentTask.Subgoals != null && currentTask.Subgoals.Count > 0)
            {
                bool allSubgoalsFinished = true;
                foreach (var subgoal in currentTask.Subgoals)
                {
                    if (!subgoal.IsComplete)
                    {
                        if (subgoal.CheckCompletion != null && subgoal.CheckCompletion())
                        {
                            subgoal.IsComplete = true;
                            subgoal.OnComplete?.Invoke();
                        }
                        else
                        {
                            allSubgoalsFinished = false; // At least one subgoal is still pending
                            break;
                        }
                    }
                }
                isTaskFinished = allSubgoalsFinished;
            }
            else
            {
                // Fallback for simple tasks without subgoals
                if (currentTask.CheckCompletion != null)
                {
                    isTaskFinished = currentTask.CheckCompletion();
                }
            }

            if (isTaskFinished)
            {
                AdvanceTutorial();
            }
        }
    }

    protected override void OnDisable()
    {
        NotificationCenter.Instance?.HideLongNotification();
    }

    protected override void OnSessionStateChanged(SessionState prevState, SessionState newState)
    {
        base.OnSessionStateChanged(prevState, newState);
        switch (newState)
        {
            case SessionState.Racing:
                AdvanceTutorial();
                break;
            default:
                break;
        }
    }

    private void InitializeTrackArray()
    {
        if (Tracks == null) return;

        foreach (Transform child in Tracks.transform)
        {
            trackBlocks.Add(child);
        }

        if (trackBlocks.Count >= 2)
        {
            Vector3 difference = trackBlocks[1].position - trackBlocks[0].position;
            blockLength = difference.magnitude;
            trackDirection = difference.normalized;

            if (Vector3.Dot(trackDirection, initialPlayerDirection) < 0)
            {
                trackDirection = -trackDirection;
            }

            trackBlocks.Sort((a, b) => Vector3.Dot(a.position, trackDirection).CompareTo(Vector3.Dot(b.position, trackDirection)));

            if (CheckPointTarget != null)
            {
                CheckPointTarget.transform.position = trackBlocks[trackBlocks.Count - 1].position;
            }
        }
    }

    private void UpdateTrackInfinitely()
    {
        if (trackBlocks.Count < 2 || Player == null) return;

        Transform firstBlock = trackBlocks[0];
        Transform lastBlock = trackBlocks[trackBlocks.Count - 1];

        float playerProj = Vector3.Dot(Player.VehicleTransform.position, trackDirection);
        float firstProj = Vector3.Dot(firstBlock.position, trackDirection);
        float lastProj = Vector3.Dot(lastBlock.position, trackDirection);

        float centerProj = (firstProj + lastProj) / 2f;

        if (playerProj > centerProj + (blockLength / 2f))
        {
            float gapMultiplier = 1.0f;

            if (hasEndBlock && Mathf.Abs(lastProj - preEndProj) < (blockLength * 0.1f))
            {
                gapMultiplier = 1.5f;
            }

            firstBlock.position = lastBlock.position + (trackDirection * (blockLength * gapMultiplier));
            trackBlocks.RemoveAt(0);
            trackBlocks.Add(firstBlock);
        }
        else if (playerProj < centerProj - (blockLength / 2f))
        {
            float gapMultiplier = 1.0f;

            if (hasEndBlock && Mathf.Abs(firstProj - postEndProj) < (blockLength * 0.1f))
            {
                gapMultiplier = 1.5f;
            }

            lastBlock.position = firstBlock.position - (trackDirection * (blockLength * gapMultiplier));
            trackBlocks.RemoveAt(trackBlocks.Count - 1);
            trackBlocks.Insert(0, lastBlock);
        }

        if (!hasEndBlock && CheckPointTarget != null)
        {
            CheckPointTarget.transform.position = trackBlocks[trackBlocks.Count - 1].position;
        }
    }

    private void CheckStartFinishProgress()
    {
        if (startFinishActive && StartFinish != null && Player != null)
        {
            float traveledDistance = Vector3.Dot(Player.VehicleTransform.position - initialPlayerPosition, initialPlayerDirection);

            if (traveledDistance > blockLength * 3f)
            {
                StartFinish.SetActive(false);
                startFinishActive = false;
            }
        }
    }

    private void ManageActivePowerUp()
    {
        if (activePowerUp != null && Player != null)
        {
            float playerProj = Vector3.Dot(Player.VehicleTransform.position, trackDirection);
            float powerUpProj = Vector3.Dot(activePowerUp.transform.position, trackDirection);

            float passTolerance = blockLength * 1.0f;

            if (playerProj > (powerUpProj + passTolerance) || Mathf.Abs(powerUpProj - playerProj) > blockLength * 3f)
            {
                activePowerUp.transform.position = GetNextBlockSpawnPosition();
            }
        }

        if (activeTutorialEnemy != null && Player != null)
        {
            float playerProj = Vector3.Dot(Player.VehicleTransform.position, trackDirection);
            float enemyProj = Vector3.Dot(activeTutorialEnemy.transform.position, trackDirection);

            float passTolerance = blockLength * 1.0f;

            if (playerProj > (enemyProj + passTolerance) || Mathf.Abs(enemyProj - playerProj) > blockLength * 3f)
            {
                activeTutorialEnemy.transform.position = GetNextBlockSpawnPosition() + TutorialEnemySpawnOffset;
            }
        }
    }

    private Vector3 GetNextBlockSpawnPosition()
    {
        if (trackBlocks.Count == 0 || Player == null) return Vector3.zero;

        float playerProj = Vector3.Dot(Player.VehicleTransform.position, trackDirection);

        foreach (Transform block in trackBlocks)
        {
            float blockProj = Vector3.Dot(block.position, trackDirection);

            if (blockProj > playerProj + (blockLength * 0.5f))
            {
                return block.position + (Vector3.up * 1.2f);
            }
        }

        return trackBlocks[trackBlocks.Count - 1].position + (Vector3.up * 1.2f) + trackDirection * blockLength * 1.0f;
    }

    private void BuildTutorialQueue()
    {
        tutorialQueue.Clear();

        // --- CONTROLLER TASKS ---
        tutorialQueue.Enqueue(new TutorialTask
        {
            GetPromptText = (controller) =>
            {
                string inputName = controller switch
                {
                    ControllerType.Keyboard => "A and D keys",
                    _ => "Left Stick"
                };
                return $"Use the {inputName} to turn right or left.";
            },
            CheckCompletion = () =>
            {
                if (moveAction == null) return false;
                return Mathf.Abs(moveAction.ReadValue<Vector2>().x) > 0.1f;
            }
        });

        List<TutorialSubgoal> SprintSubgoals = new List<TutorialSubgoal>();
        SprintSubgoals.Add(new TutorialSubgoal
        {
            CheckCompletion = () => beatHitCount >= 10,
            OnComplete = () =>
            {
                EventManager.StopListening<BeatHit>(OnBeatHit);
            }
        });
        float sprintCompleteCooldown = 1.5f;
        SprintSubgoals.Add(new TutorialSubgoal
        {
            CheckCompletion = () => Time.time >= lastBeatHitTime + sprintCompleteCooldown
        });
        tutorialQueue.Enqueue(new TutorialTask
        {
            OnStart = () =>
            {
                beatHitCount = 0;
                // Start listening to the event
                EventManager.StartListening<BeatHit>(OnBeatHit);
            },
            GetPromptText = (controller) =>
            {
                string inputName = controller switch
                {
                    ControllerType.Keyboard => "Space key",
                    ControllerType.PlayStation => "R2 button",
                    ControllerType.Xbox => "RT",
                    ControllerType.Switch => "ZR button",
                    ControllerType.GenericGamepad => "Right Trigger",
                    _ => "Sprint button"
                };
                // Adding dynamic text so the player knows their current progress
                return $"Press the {inputName} to the beat! ({beatHitCount}/10)";
            },
            // Using the new subgoal system
            Subgoals = SprintSubgoals,
        });

        // --- TIMED TASK ---
        float taskStartTime = 0f;
        float hardcodedDuration = 12f;
        tutorialQueue.Enqueue(new TutorialTask
        {
            OnStart = () => { taskStartTime = Time.time; },
            GetPromptText = (controller) => "POWER-UPS AND DEBUFFS ARE ITEMS YOU CAN COLLECT DURING THE RACE",
            CheckCompletion = () => Time.time >= taskStartTime + hardcodedDuration
        });

        // --- POWERUP TASKS ---
        if (PowerUpPrefabs != null)
        {
            for (int i = 0; i < PowerUpPrefabs.Length; i++)
            {
                GameObject prefabToSpawn = PowerUpPrefabs[i];
                string instructionText = (PowerUpInstructions != null && i < PowerUpInstructions.Length)
                    ? PowerUpInstructions[i]
                    : "Pick up the next item on the track!";

                if (instructionText.Contains("UNICORN"))
                {
                    List<TutorialSubgoal> StarSubgoals = new List<TutorialSubgoal>();
                    float powerUpCompleteCooldown = 5.0f;
                    StarSubgoals.Add(new TutorialSubgoal
                    {
                        CheckCompletion = () => tutorialEnemyHit,
                        OnComplete = () =>
                        {
                            tutorialEnemyHit = false;
                            if (trackBlocks.Count > 0)
                            {
                                Vector3 spawnPos = GetNextBlockSpawnPosition();
                                Quaternion spawnRot = Quaternion.Euler(0.0f, 90.0f, 0.0f);
                                activePowerUp = Instantiate(prefabToSpawn, spawnPos, spawnRot, Tracks.transform);
                            }
                            string prompt = "COLLECT STAR TO BECOME INVINCIBLE FOR A SHORT TIME\nNOW TRY TO HIT THE ENEMY AGAIN!";
                            NotificationCenter.Instance?.UpdateLongNotification(prompt);
                        }
                    });
                    StarSubgoals.Add(new TutorialSubgoal
                    {
                        CheckCompletion = () => powerUpCollected && tutorialEnemyHit,
                        OnComplete = () =>
                        {
                            EventManager.StopListening<CollectPowerUp, GameObject>(OnCollectPowerUp);
                            EventManager.StopListening<TutorialEnemyHit>(OnTutorialEnemyHit);
                        }
                    });
                    StarSubgoals.Add(new TutorialSubgoal
                    {
                        CheckCompletion = () => Time.time >= lastPickupTime + powerUpCompleteCooldown && Time.time >= lastEnemyHitTime + powerUpCompleteCooldown,
                        OnComplete = () =>
                        {
                            if (activePowerUp != null)
                            {
                                Destroy(activePowerUp);
                                activePowerUp = null;
                            }

                            if (activeTutorialEnemy != null)
                            {
                                Destroy(activeTutorialEnemy);
                                activeTutorialEnemy = null;
                            }
                        }
                    });
                    tutorialQueue.Enqueue(new TutorialTask
                    {
                        OnStart = () =>
                        {
                            powerUpCollected = false;
                            EventManager.StartListening<CollectPowerUp, GameObject>(OnCollectPowerUp);
                            EventManager.StartListening<TutorialEnemyHit>(OnTutorialEnemyHit);
                            tutorialEnemyHit = false;

                            if (trackBlocks.Count > 0)
                            {
                                Vector3 spawnPos = GetNextBlockSpawnPosition();
                                Quaternion spawnRot = Quaternion.Euler(0.0f, 180.0f, 0.0f);
                                activeTutorialEnemy = Instantiate(TutorialEnemyPrefab, spawnPos + TutorialEnemySpawnOffset, spawnRot, Tracks.transform);
                            }
                        },
                        GetPromptText = (controller) => instructionText,
                        Subgoals = StarSubgoals
                    });
                }
                else
                {
                    List<TutorialSubgoal> subgoals = new List<TutorialSubgoal>();
                    float powerUpCompleteCooldown = 8.0f;
                    subgoals.Add(new TutorialSubgoal {
                        CheckCompletion = () => powerUpCollected,
                        OnComplete = () =>
                        {
                            EventManager.StopListening<CollectPowerUp, GameObject>(OnCollectPowerUp);

                            if (activePowerUp != null)
                            {
                                Destroy(activePowerUp);
                                activePowerUp = null;
                            }
                        }
                    });
                    subgoals.Add(new TutorialSubgoal
                    {
                        CheckCompletion = () => Time.time >= lastPickupTime + powerUpCompleteCooldown
                    });
                    tutorialQueue.Enqueue(new TutorialTask
                    {
                        OnStart = () =>
                        {
                            powerUpCollected = false;
                            EventManager.StartListening<CollectPowerUp, GameObject>(OnCollectPowerUp);

                            if (trackBlocks.Count > 0)
                            {
                                Vector3 spawnPos = GetNextBlockSpawnPosition();
                                Quaternion spawnRot = Quaternion.Euler(0.0f, 90.0f, 0.0f);
                                activePowerUp = Instantiate(prefabToSpawn, spawnPos, spawnRot, Tracks.transform);
                            }
                        },
                        GetPromptText = (controller) => instructionText,
                        Subgoals = subgoals
                    });
                }
            }
        }

        taskStartTime = 0f;
        hardcodedDuration = 6f;
        tutorialQueue.Enqueue(new TutorialTask
        {
            OnStart = () => { SoundManager.Instance?.Play("RaceFinish"); taskStartTime = Time.time; },
            GetPromptText = (controller) => "COLLECT THEM STRATEGICALLY TO WIN THE RACE! GOOD LUCK!\r\nPRESS ANY KEY TO FINISH THE TUTORIAL",
            CheckCompletion = () => Time.time >= taskStartTime + hardcodedDuration,
            OnComplete = () =>
            {
                    tutorialFinished = true;
            }
        });
    }

    private void OnTutorialEnemyHit()
    {
        tutorialEnemyHit = true;
        lastEnemyHitTime = Time.time;
    }

    private void OnBeatHit()
    {
        beatHitCount++;
        if (currentTask != null)
        {
            string prompt = currentTask.GetPromptText(lastController);
            NotificationCenter.Instance?.UpdateLongNotification(prompt);
        }
        lastBeatHitTime = Time.time;
    }

    // Call back for EventManager
    private void OnCollectPowerUp(GameObject pickedUpObj)
    {
        tutorialEnemyHit = false;
        powerUpCollected = true;
        activePowerUp = null;
        lastPickupTime = Time.time;
    }

    private void AdvanceTutorial()
    {
        if (currentTask != null)
        {
            currentTask.OnComplete?.Invoke();
        }

        if (tutorialQueue.Count > 0)
        {
            currentTask = tutorialQueue.Dequeue();
            currentTask.OnStart?.Invoke(); // Fire setup logic for the new task
            UpdateTutorialPrompt();
        }
        else
        {
            currentTask = null;

            if (!tutorialComplete)
            {
                tutorialComplete = true;
                SpawnEndBlock();
            }
        }
    }

    private void UpdateTutorialPrompt()
    {
        if (currentTask != null)
        {
            lastController = GetActiveControllerType();
            string prompt = currentTask.GetPromptText(lastController);
            NotificationCenter.Instance?.ShowLongNotification(prompt);
        }
    }

    private void SpawnEndBlock()
    {
        if (TrackEndPrefab != null && trackBlocks.Count > 0)
        {
            Transform lastBlock = trackBlocks[trackBlocks.Count - 1];
            Vector3 spawnPos = lastBlock.position + (trackDirection * (blockLength * 0.75f));

            Instantiate(TrackEndPrefab, spawnPos, lastBlock.rotation, Tracks.transform);

            float endProj = Vector3.Dot(spawnPos, trackDirection);

            preEndProj = endProj - (blockLength * 0.75f);
            postEndProj = endProj + (blockLength * 0.75f);

            if (CheckPointTarget != null)
            {
                CheckPointTarget.transform.position = spawnPos;
            }

            hasEndBlock = true;
        }
    }

    private ControllerType GetInitialControllerType()
    {
        bool hasPlayStation = false;
        bool hasXInput = false;
        bool hasSwitchPro = false;
        bool hasGenericGamepad = false;
        foreach (var device in InputSystem.devices)
        {
            if (device is DualSenseGamepadHID || device is DualShockGamepad)
            {
                hasPlayStation = true;
            }
            else if (device is XInputController)
            {
                hasXInput = true;
            }
            else if (device is SwitchProControllerHID)
            {
                hasSwitchPro = true;
            }
            else if (device is Gamepad)
            {
                hasGenericGamepad = true;
            }
        }

        if (hasPlayStation) return ControllerType.PlayStation;
        if (hasXInput) return ControllerType.Xbox;
        if (hasSwitchPro) return ControllerType.Switch;
        if (hasGenericGamepad) return ControllerType.GenericGamepad;

        return ControllerType.Keyboard;
    }

    private ControllerType GetActiveControllerType()
    {
        InputDevice activeDevice = null;

        if (moveAction != null && moveAction.ReadValue<Vector2>().sqrMagnitude > 0.01f)
        {
            activeDevice = moveAction.activeControl?.device;
        }
        else if (sprintAction != null && sprintAction.IsPressed())
        {
            activeDevice = sprintAction.activeControl?.device;
        }

        if (activeDevice != null)
        {
            if (activeDevice is DualShockGamepad || activeDevice is DualSenseGamepadHID) return ControllerType.PlayStation;
            if (activeDevice is XInputController) return ControllerType.Xbox;
            if (activeDevice is SwitchProControllerHID) return ControllerType.Switch;
            if (activeDevice is Gamepad) return ControllerType.GenericGamepad;
            if (activeDevice is Keyboard || activeDevice is Mouse) return ControllerType.Keyboard;
        }

        return lastController;
    }
}