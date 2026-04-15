using UnityEngine;

public class EnemyCaptureState : EnemyStateMachineBehaviour
{
    private const int danceVariantCount = 8;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);

        int danceIndex = Random.Range(0, danceVariantCount);

        animator.SetBool(CapturingParam, true);
        animator.SetInteger(DanceIdxParam, danceIndex);

        if (enemy != null)
        {
            enemy.HoldPositionInCurrentZone();
        }

        animator.SetFloat(SpeedParam, 0f);

        if (agent != null)
        {
            agent.updateRotation = false;
        }
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (enemy == null)
        {
            return;
        }

        // Prevent camera-facing rotation while blending out to patrol/idle.
        if (animator.IsInTransition(layerIndex) || !animator.GetBool(CapturingParam))
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Vector3 lookAtPosition = mainCamera.transform.position;
        lookAtPosition.y = enemy.transform.position.y;
        enemy.transform.LookAt(lookAtPosition);
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool(CapturingParam, false);

        if (enemy != null)
        {
            enemy.ResumeMovement();
        }

        if (agent != null)
        {
            agent.updateRotation = true;
        }
    }
}
