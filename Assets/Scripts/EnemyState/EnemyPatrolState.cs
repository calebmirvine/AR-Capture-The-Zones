using UnityEngine;

public class EnemyPatrolState : EnemyStateMachineBehaviour
{
    private bool captureRequested;
    private bool idleRequested;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        captureRequested = false;
        idleRequested = false;
        animator.SetBool(CapturingParam, false);

        if (enemy == null || agent == null)
        {
            return;
        }

        enemy.FindAndMoveToTarget();
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (enemy == null || agent == null) return;

        if (enemy.ShouldForceIdle())
        {
            enemy.HoldPositionInCurrentZone();
            animator.SetFloat(SpeedParam, 0f);
            animator.SetBool(CapturingParam, false);

            if (!idleRequested)
            {
                idleRequested = true;
                animator.SetTrigger(IdleParam);
            }

            return;
        }

        enemy.ResumeMovement();

        if (enemy.ShouldChooseNewTarget())
        {
            enemy.FindAndMoveToTarget();
        }

        animator.SetFloat(SpeedParam, agent.velocity.magnitude);

        if (enemy.IsInCapturableZone())
        {
            if (!captureRequested)
            {
                captureRequested = true;
            }

            animator.SetBool(CapturingParam, true);
            return;
        }

        animator.SetBool(CapturingParam, false);

        if (enemy.CurrentTargetZone == null && enemy.HasReachedDestination())
        {
            animator.SetTrigger(IdleParam);
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (enemy != null)
        {
            enemy.ResumeMovement();
        }

        animator.SetFloat(SpeedParam, 0f);
    }
}
