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

        if (!enemy.IsZoneContestedWithPlayer()
            && enemy.IsInCapturableZone()
            && !enemy.IsPlayerInAttackRange())
        {
            captureRequested = true;
            enemy.StopMovement();
            animator.SetBool(CapturingParam, true);
        }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (BlockStateWhenGrenadeStunned(animator))
        {
            return;
        }

        ResetChaseIfFar(enemy, ref chaseRequested);

        if (enemy.IsZoneContestedWithPlayer())
        {
            animator.SetBool(CapturingParam, false);
        }

        if (TryTriggerAttackIfInRange(animator))
        {
            return;
        }

        if (TryBeginChase(animator, ref chaseRequested))
        {
            return;
        }

        if (HandleCapturableZone(animator, ref captureRequested))
        {
            return;
        }

        if (!patrolTriggered && enemy.ShouldRotateFromContestedZone())
        {
            patrolTriggered = true;
            animator.SetTrigger(PatrolParam);
            return;
        }

        if (!enemy.HasEnemyCaptureTargets() && !enemy.ShouldChasePlayer())
        {
            enemy.StopMovement();
            animator.SetFloat(SpeedParam, 0f);
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
