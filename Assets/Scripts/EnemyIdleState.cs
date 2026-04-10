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

        timer += Time.deltaTime;
        if (!patrolTriggered && timer > enemy.IdleTime) {
            patrolTriggered = true;
            animator.SetTrigger(PatrolParam);
        }
    }
}
