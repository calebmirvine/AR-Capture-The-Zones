using UnityEngine;

public class EnemyChaseState : EnemyStateMachineBehaviour
{
    private bool captureRequested;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        captureRequested = false;
        animator.SetBool(CapturingParam, false);

        agent.updateRotation = true;
        enemy.ResumeMovement();
        enemy.SetChaseDestinationToPlayer();
        animator.SetFloat(SpeedParam, 0f);
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent.updateRotation = true;

        enemy.ResumeMovement();

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

        if (!enemy.ShouldChasePlayer())
        {
            animator.SetTrigger(PatrolParam);
            return;
        }

        enemy.SetChaseDestinationToPlayer();
        animator.SetFloat(SpeedParam, agent.velocity.magnitude);
        animator.SetBool(CapturingParam, false);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        enemy.ResumeMovement();
        animator.SetFloat(SpeedParam, 0f);
    }
}
