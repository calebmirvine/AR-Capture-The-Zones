using UnityEngine;

/// <summary>Animator idle clip; exits to patrol, chase, capture, or attack when rules fire (no timed wait).</summary>
public class EnemyIdleState : EnemyStateMachineBehaviour
{
    private bool patrolTriggered;
    private bool captureRequested;
    private bool chaseRequested;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        patrolTriggered = false;
        captureRequested = false;
        chaseRequested = false;
        animator.SetBool(CapturingParam, false);
        animator.SetFloat(SpeedParam, 0f);

        if (!enemy.IsZoneContestedWithPlayer() && enemy.IsInCapturableZone())
        {
            captureRequested = true;
            enemy.StopMovement();
            animator.SetBool(CapturingParam, true);
        }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        ResetChaseIfFar(enemy, ref chaseRequested);

        if (HandleContestedZoneWhileIdle(animator))
        {
            return;
        }

        if (HandleCapturableZone(animator, ref captureRequested))
        {
            return;
        }

        if (TryTriggerAttackIfInRange(animator))
        {
            return;
        }

        if (TryBeginChase(animator, ref chaseRequested))
        {
            return;
        }

        if (!patrolTriggered && enemy.ShouldRotateFromContestedZone())
        {
            patrolTriggered = true;
            animator.SetTrigger(PatrolParam);
            return;
        }

        enemy.ResumeMovement();
        if (!patrolTriggered)
        {
            patrolTriggered = true;
            animator.SetTrigger(PatrolParam);
        }
    }
}
