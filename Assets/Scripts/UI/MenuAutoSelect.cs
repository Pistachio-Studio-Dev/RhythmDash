using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MenuAutoSelect : MonoBehaviour
{
    private const string NavigateActionName = "Navigate";
    private const string SubmitActionName = "Submit";
    private const string CancelActionName = "Cancel";

    [SerializeField] private Selectable defaultSelectable;

    private InputAction navigateAction;
    private InputAction submitAction;
    private InputAction cancelAction;
    private Selectable lastSelected;
    private Coroutine selectRoutine;

    private void Awake()
    {
        CacheActions();
    }

    private void OnEnable()
    {
        CacheActions();
        lastSelected = null;
        RequestSelection();
    }

    private void OnDisable()
    {
        if (selectRoutine != null)
        {
            StopCoroutine(selectRoutine);
            selectRoutine = null;
        }

        ClearOwnedSelection();
    }

    private void LateUpdate()
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return;
        }

        var currentSelected = eventSystem.currentSelectedGameObject;
        if (IsOwnedByMenu(currentSelected))
        {
            var selectable = currentSelected.GetComponent<Selectable>();
            if (IsSelectableAvailable(selectable))
            {
                lastSelected = selectable;
            }

            return;
        }

        if (currentSelected == null && HasControllerSelectionInputThisFrame())
        {
            SelectFallback();
        }
    }

    public void RequestSelection()
    {
        if (selectRoutine != null)
        {
            StopCoroutine(selectRoutine);
            selectRoutine = null;
        }

        if (HasConnectedController())
        {
            selectRoutine = StartCoroutine(SelectDefaultNextFrame());
        }
    }

    public void ClearOwnedSelection()
    {
        var eventSystem = EventSystem.current;
        if (eventSystem != null && IsOwnedByMenu(eventSystem.currentSelectedGameObject))
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }

    private IEnumerator SelectDefaultNextFrame()
    {
        yield return null;
        selectRoutine = null;
        SelectFallback();
    }

    private void CacheActions()
    {
        var actions = InputSystem.actions;
        navigateAction = actions?.FindAction(NavigateActionName);
        submitAction = actions?.FindAction(SubmitActionName);
        cancelAction = actions?.FindAction(CancelActionName);
    }

    private bool HasControllerSelectionInputThisFrame()
    {
        if (navigateAction == null || submitAction == null || cancelAction == null)
        {
            CacheActions();
        }

        return WasControllerPerformedThisFrame(navigateAction)
            || WasControllerPerformedThisFrame(submitAction)
            || WasControllerPerformedThisFrame(cancelAction);
    }

    private void SelectFallback()
    {
        if (!HasConnectedController())
        {
            return;
        }

        var eventSystem = EventSystem.current;
        var target = GetFallbackSelectable();
        if (eventSystem == null || target == null)
        {
            return;
        }

        eventSystem.SetSelectedGameObject(null);
        eventSystem.SetSelectedGameObject(target.gameObject);
        lastSelected = target;
    }

    private Selectable GetFallbackSelectable()
    {
        if (IsSelectableAvailable(lastSelected))
        {
            return lastSelected;
        }

        if (IsSelectableAvailable(defaultSelectable))
        {
            return defaultSelectable;
        }

        var selectables = GetComponentsInChildren<Selectable>(true);
        foreach (var selectable in selectables)
        {
            if (IsSelectableAvailable(selectable))
            {
                return selectable;
            }
        }

        return null;
    }

    private bool IsOwnedByMenu(GameObject target)
    {
        return target != null && target.transform.IsChildOf(transform);
    }

    private static bool IsSelectableAvailable(Selectable selectable)
    {
        return selectable != null && selectable.IsActive() && selectable.IsInteractable();
    }

    private static bool HasConnectedController()
    {
        return Gamepad.current != null || Joystick.current != null;
    }

    private static bool WasControllerPerformedThisFrame(InputAction action)
    {
        return action != null
            && action.WasPerformedThisFrame()
            && IsControllerDevice(action.activeControl?.device);
    }

    private static bool IsControllerDevice(InputDevice device)
    {
        return device is Gamepad || device is Joystick;
    }
}
