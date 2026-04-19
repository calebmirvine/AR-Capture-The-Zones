using UnityEngine;

public class EnemyAttackState : EnemyStateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        animator.ResetTrigger(AttackingParam);
        animator.SetBool(CapturingParam, false);
        animator.SetFloat(SpeedParam, 0f);
        enemy.StopMovement();
    }


    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        enemy.ResumeMovement();
        animator.SetTrigger(PatrolParam);
    }
}
