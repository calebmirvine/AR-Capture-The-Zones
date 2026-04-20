using UnityEngine;

public class EnemyAttackState : EnemyStateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        animator.SetBool(IsAttackingParam, true);
        animator.SetBool(CapturingParam, false);

        enemy.FaceTowardPlayer();
        enemy.StopAndClearNavPath();
        animator.SetFloat(SpeedParam, 0f);
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (HealthSystem.Instance != null && HealthSystem.Instance.IsGhost)
        {
            animator.SetBool(IsAttackingParam, false);
            return;
        }

        enemy.FaceTowardPlayer();
        animator.SetFloat(SpeedParam, 0f);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (agent != null)
        {
            agent.updateRotation = true;
        }

        animator.SetBool(IsAttackingParam, false);
        enemy.ResumeMovement();
    }
}
