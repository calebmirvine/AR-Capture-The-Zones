using UnityEngine;

/// <summary>Walk toward capture targets; may switch to Idle, Chase, Capture, or Attack.</summary>
public class EnemyPatrolState : EnemyStateMachineBehaviour
{
    private bool captureRequested;
    private bool idleRequested;
    private bool chaseRequested;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        captureRequested = false;
        idleRequested = false;
        chaseRequested = false;
        animator.SetBool(CapturingParam, false);

        agent.updateRotation = true;

        if (enemy.IsInCapturableZone())
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
        ResetChaseIfFar(enemy, ref chaseRequested);

        agent.updateRotation = true;

        if (HandleContestedZone(animator, ref idleRequested, fireIdleTrigger: true))
        {
            return;
        }

        enemy.ResumeMovement();

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

        if (enemy.ShouldChooseNewZone())
        {
            enemy.FindAndMoveToZone();
        }

        animator.SetFloat(SpeedParam, agent.velocity.magnitude);
        animator.SetBool(CapturingParam, false);

        if (enemy.CurrentTargetZone == null && enemy.HasReachedZone())
        {
            animator.SetTrigger(IdleParam);
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        enemy.ResumeMovement();
        animator.SetFloat(SpeedParam, 0f);
    }
}
