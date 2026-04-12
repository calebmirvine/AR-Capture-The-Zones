using UnityEngine;

public class EnemyIdleState : EnemyStateMachineBehaviour
{
    private float timer;
    private bool patrolTriggered;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        timer = 0;
        patrolTriggered = false;
        animator.SetBool(CapturingParam, false);
        animator.SetFloat(SpeedParam, 0f);
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (enemy == null) return;

        if (enemy.ShouldForceIdle())
        {
            enemy.HoldPositionInCurrentZone();
            animator.SetFloat(SpeedParam, 0f);
            animator.SetBool(CapturingParam, false);
            timer = 0f;
            return;
        }

        if (!patrolTriggered && enemy.ShouldRotateFromContestedZone())
        {
            patrolTriggered = true;
            animator.SetTrigger(PatrolParam);
            return;
        }

        enemy.ResumeMovement();
        timer += Time.deltaTime;
        if (!patrolTriggered && timer > enemy.IdleTime)
        {
            patrolTriggered = true;
            animator.SetTrigger(PatrolParam);
        }
    }
}
