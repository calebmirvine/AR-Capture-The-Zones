using UnityEngine;

public class EnemyChaseState : EnemyStateMachineBehaviour
{
    private bool captureRequested;
    private bool nonChaseHandoffTriggered;
    private float chaseInvalidSince = -1f;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        captureRequested = false;
        nonChaseHandoffTriggered = false;
        chaseInvalidSince = -1f;
        animator.SetBool(CapturingParam, false);

        enemy.FaceTowardPlayer();
        enemy.ResumeMovement();
        enemy.SetChaseDestinationToPlayer();
        animator.SetFloat(SpeedParam, 0f);
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (BlockStateWhenGrenadeStunned(animator))
        {
            return;
        }

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
            if (chaseInvalidSince < 0f)
            {
                chaseInvalidSince = Time.time;
            }

            float handoffDelay = enemy.ChaseHandoffDelay;
            if (handoffDelay > 0f && Time.time - chaseInvalidSince < handoffDelay)
            {
                enemy.SetChaseDestinationToPlayer();
                animator.SetFloat(SpeedParam, agent.velocity.magnitude);
                animator.SetBool(CapturingParam, false);
                return;
            }

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

        chaseInvalidSince = -1f;
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
