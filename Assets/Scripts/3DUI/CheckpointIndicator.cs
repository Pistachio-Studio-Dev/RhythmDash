using System;
using DG.Tweening;
using UnityEngine;


public class CheckpointIndicator : SessionBasedBehaviour
{
    public GameObject OverrideTarget;
    public GameObject Arrow;
    public Quaternion OffsetRotation = Quaternion.Euler(15, 0, 0);
    
    private VehicleProgress progress = null;
    private Vector3 originalScale;
    private Tween visibilityTween;
    private bool useOverrideTarget = false;

    private void Awake()
    {
        originalScale = Arrow.transform.localScale;
        if (OverrideTarget != null)
        {
            useOverrideTarget = true;
        }
        
        SetVisibility(false);
    }

    protected override void OnSessionStateChanged(SessionState prevState, SessionState newState)
    {
        base.OnSessionStateChanged(prevState, newState);
        
        if (newState == SessionState.Racing)
        {
            progress = PlayerCharacterController.Instance?.vehicle?.GetComponent<VehicleProgress>();
            SetVisibility(progress != null);
        }
        else
        {
            progress = null;
            SetVisibility(false);
        }
    }
    
    private void Update()
    {
        if (progress == null)
        {
            return;
        }

        const float rotationSpeed = 10f;

        Vector3 targetPosition;
        
        if (useOverrideTarget)
        {
            targetPosition = OverrideTarget.transform.position;
        }
        else
        {
            int nextCheckpointIndex = progress.NextCheckpointIndex;
            if (nextCheckpointIndex < 0 || nextCheckpointIndex >= Checkpoint.Instances.Count)
            {
                return;
            }

            Checkpoint checkpoint = Checkpoint.Instances[progress.NextCheckpointIndex];
            targetPosition = checkpoint.transform.position;
        }

        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction) * OffsetRotation;

            Arrow.transform.rotation = Quaternion.Slerp(
                Arrow.transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
    
    private void SetVisibility(bool visible)
    {
        Vector3 targetScale = visible ? originalScale : Vector3.zero;
        float duration = 0.8f;

        if (visible && Arrow.activeSelf)
        {
            return;
        }
        
        if (!visible && !Arrow.activeSelf)
        {
            return;
        }
        
        Arrow.SetActive(true);
        visibilityTween?.Kill();
        visibilityTween = DOTween.Sequence()
            .Append(Arrow.transform.DOScale(targetScale, duration))
            .AppendCallback(() => Arrow.SetActive(visible));
    }
}
