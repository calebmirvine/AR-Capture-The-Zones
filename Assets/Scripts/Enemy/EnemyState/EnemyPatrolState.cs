using UnityEngine;

/// <summary>Walk toward capture targets; may switch to Chase, Capture, or Attack (no idle when idle targets).</summary>
public class EnemyPatrolState : EnemyStateMachineBehaviour
{
    private bool captureRequested;
    private bool chaseRequested;
    private bool idleHandoffTriggered;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        captureRequested = false;
        chaseRequested = false;
        idleHandoffTriggered = false;
        animator.SetBool(CapturingParam, false);

        agent.updateRotation = true;

        if (enemy.IsInCapturableZone() && !enemy.IsPlayerInAttackRange())
        {
            captureRequested = true;
            enemy.StopMovement();
            animator.SetFloat(SpeedParam, 0f);
            animator.SetBool(CapturingParam, true);
            return;
        }

        enemy.FindAndMoveToZone();
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (BlockStateWhenGrenadeStunned(animator))
        {
            return;
        }

        ResetChaseIfFar(enemy, ref chaseRequested);

        agent.updateRotation = true;

        if (TryTriggerAttackIfInRange(animator))
        {
            return;
        }

        if (HandleContestedZone(animator))
        {
            return;
        }

        if (HandleCapturableZone(animator, ref captureRequested))
        {
            return;
        }

        enemy.ResumeMovement();

        if (TryBeginChase(animator, ref chaseRequested))
        {
            return;
        }

        if (!enemy.HasEnemyCaptureTargets() && !enemy.ShouldChasePlayer())
        {
            enemy.StopMovement();
            animator.SetFloat(SpeedParam, 0f);
            if (!idleHandoffTriggered)
            {
                idleHandoffTriggered = true;
                animator.SetTrigger(IdleParam);
            }

            return;
        }

        idleHandoffTriggered = false;
        if (enemy.ShouldChooseNewZone())
        {
            enemy.FindAndMoveToZone();
        }

        animator.SetFloat(SpeedParam, agent.velocity.magnitude);
        animator.SetBool(CapturingParam, false);

        if (!enemy.HasEnemyCaptureTargets() && TryBeginChase(animator, ref chaseRequested))
        {
            return;
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        enemy.ResumeMovement();
        animator.SetFloat(SpeedParam, 0f);
    }
}
