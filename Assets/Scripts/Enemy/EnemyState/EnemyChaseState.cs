using UnityEngine;

public class EnemyChaseState : EnemyStateMachineBehaviour
{
    private bool captureRequested;
    private bool nonChaseHandoffTriggered;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        captureRequested = false;
        nonChaseHandoffTriggered = false;
        animator.SetBool(CapturingParam, false);

        enemy.FaceTowardPlayer();
        enemy.ResumeMovement();
        enemy.SetChaseDestinationToPlayer();
        animator.SetFloat(SpeedParam, 0f);
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        enemy.FaceTowardPlayer();

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

        if (!enemy.ShouldChasePlayer())
        {
            if (!nonChaseHandoffTriggered)
            {
                nonChaseHandoffTriggered = true;
                if (enemy.HasEnemyCaptureTargets())
                {
                    animator.SetTrigger(PatrolParam);
                }
                else
                {
                    enemy.StopMovement();
                    animator.SetFloat(SpeedParam, 0f);
                    animator.SetTrigger(IdleParam);
                }
            }

            return;
        }

        nonChaseHandoffTriggered = false;
        enemy.SetChaseDestinationToPlayer();
        animator.SetFloat(SpeedParam, agent.velocity.magnitude);
        animator.SetBool(CapturingParam, false);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (agent != null)
        {
            agent.updateRotation = true;
        }

        enemy.ResumeMovement();
        animator.SetFloat(SpeedParam, 0f);
    }
}
