using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.XInput;
using UnityEngine.InputSystem.Switch;
using UnityEngine.UI;

public class TutorialSimple : SessionBasedBehaviour
{
    [SerializeField] private TMP_Text TutorialTMP;
    [SerializeField] private Image Banner;

    public enum ControllerType
    {
        Keyboard,
        Xbox,
        PlayStation,
        Switch,
        GenericGamepad
    }

    private class TutorialTask
    {
        public Func<ControllerType, string> GetPromptText; // Now accepts our new enum
        public Func<bool> CheckCompletion;
    }

    private Queue<TutorialTask> tutorialQueue = new Queue<TutorialTask>();
    private TutorialTask currentTask;

    private InputAction moveAction;
    private InputAction sprintAction;

    void Start()
    {
        TutorialTMP.text = "";
        Banner.gameObject.SetActive(false);

        moveAction = InputSystem.actions?.FindAction("Move");
        sprintAction = InputSystem.actions?.FindAction("Sprint");

        BuildTutorialQueue();
    }

    void Update()
    {
        if (currentTask != null)
        {
            // Determine the exact device currently being used for this action
            ControllerType currentController = GetActiveControllerType(moveAction);

            // Generate the correct text for that specific device
            TutorialTMP.text = currentTask.GetPromptText(currentController);

            if (currentTask.CheckCompletion())
            {
                AdvanceTutorial();
            }
        }
    }

    protected override void OnSessionStateChanged(SessionState prevState, SessionState newState)
    {
        base.OnSessionStateChanged(prevState, newState);
        switch (newState)
        {
            case SessionState.Racing:
                Banner.gameObject.SetActive(true);
                AdvanceTutorial();
                break;
            default:
                break;
        }
    }

    private void BuildTutorialQueue()
    {
        tutorialQueue.Clear();

        // Step 1: Steer
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

        // Step 2: Sprint
        tutorialQueue.Enqueue(new TutorialTask
        {
            GetPromptText = (controller) =>
            {
                // Here is where the enum shines: specific button prompts per console
                string inputName = controller switch
                {
                    ControllerType.Keyboard => "Space key",
                    ControllerType.PlayStation => "R2 button",
                    ControllerType.Xbox => "RT",
                    ControllerType.Switch => "ZR button",
                    ControllerType.GenericGamepad => "Right Trigger",
                    _ => "Sprint button"
                };
                return $"Press the {inputName} to accelerate.";
            },
            CheckCompletion = () =>
            {
                if (sprintAction == null) return false;
                return sprintAction.IsPressed();
            }
        });
    }

    private void AdvanceTutorial()
    {
        if (tutorialQueue.Count > 0)
        {
            currentTask = tutorialQueue.Dequeue();
        }
        else
        {
            Banner.gameObject.SetActive(false);
            currentTask = null;
            TutorialTMP.text = "";
        }
    }

    private ControllerType GetActiveControllerType(InputAction action)
    {
        // Get the specific hardware device currently driving this action
        InputDevice device = action?.activeControl?.device;

        // Fallbacks for keyboard/mouse or no active device
        if (device == null || device is Keyboard || device is Mouse)
        {
            return ControllerType.Keyboard;
        }

        // Check against specific Input System hardware classes
        if (device is DualShockGamepad) return ControllerType.PlayStation;
        if (device is XInputController) return ControllerType.Xbox;
        if (device is SwitchProControllerHID) return ControllerType.Switch;

        // Catch-all for third-party controllers that don't match the big three
        if (device is Gamepad) return ControllerType.GenericGamepad;

        return ControllerType.Keyboard;
    }
}