using UnityEngine;

// Grenade hit reaction: hold the agent until IsGrenadeStunActive expires, then clear IsStunned so the controller can exit.
public class EnemyStunnedState : EnemyStateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        animator.SetBool(CapturingParam, false);
        animator.SetBool(IsAttackingParam, false);
        animator.SetFloat(SpeedParam, 0f);

        if (enemy != null)
        {
            enemy.StopAndClearNavPath();
        }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (enemy == null)
        {
            return;
        }

        enemy.StopMovement();
        animator.SetFloat(SpeedParam, 0f);

        if (!enemy.IsGrenadeStunActive)
        {
            animator.SetBool(IsStunnedParam, false);
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (enemy != null)
        {
            enemy.ResumeMovement();
        }
    }
}
