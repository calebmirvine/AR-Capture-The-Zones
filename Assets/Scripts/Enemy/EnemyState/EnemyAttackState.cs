using UnityEngine;

public class EnemyAttackState : EnemyStateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        animator.SetBool(IsAttackingParam, true);
        animator.SetBool(CapturingParam, false);
        animator.SetFloat(SpeedParam, 0f);
        enemy.StopMovement();
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool(IsAttackingParam, false);
        enemy.ResumeMovement();
    }
}
