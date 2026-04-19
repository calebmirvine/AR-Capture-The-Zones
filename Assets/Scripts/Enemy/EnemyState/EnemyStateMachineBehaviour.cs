using UnityEngine;
using UnityEngine.AI;

public class EnemyStateMachineBehaviour : StateMachineBehaviour
{
    protected Enemy enemy;
    protected NavMeshAgent agent;

    protected const string PatrolParam = "Patrol";
    protected const string ChaseParam = "Chase";
    protected const string IdleParam = "Idle";
    protected const string CapturingParam = "IsCapturing";
    protected const string DanceIdxParam = "DanceIndex";
    protected const string SpeedParam = "Speed";
    protected const string IsAttackingParam = "IsAttacking";

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        enemy = animator.GetComponentInParent<Enemy>();
        agent = enemy.Agent;
    }

    // Clears chaseRequested when out of aggro so Chase can trigger again later.
    protected static void ResetChaseIfFar(Enemy enemy, ref bool chaseRequested)
    {
        if (!enemy.ShouldChasePlayer())
        {
            chaseRequested = false;
        }
    }


    // Contested tile with player: stop and clear capture/attack; stay on current animator state (no Idle).
    protected bool HandleContestedZone(Animator animator)
    {
        if (!enemy.IsZoneContestedWithPlayer())
        {
            return false;
        }

        enemy.StopMovement();
        animator.SetFloat(SpeedParam, 0f);
        animator.SetBool(CapturingParam, false);
        animator.SetBool(IsAttackingParam, false);
        return true;
    }

    //Enter or continue capture dance; stops agent on first entry.
    protected bool HandleCapturableZone(Animator animator, ref bool captureRequested)
    {
        if (!enemy.IsInCapturableZone())
        {
            return false;
        }

        if (!captureRequested)
        {
            captureRequested = true;
            enemy.StopMovement();
            animator.SetFloat(SpeedParam, 0f);
        }

        animator.SetBool(CapturingParam, true);
        return true;
    }

    protected bool TryTriggerAttackIfInRange(Animator animator)
    {
        if (!enemy.IsPlayerInAttackRange())
        {
            return false;
        }

        animator.SetBool(CapturingParam, false);
        animator.SetBool(IsAttackingParam, true);
        return true;
    }

    protected bool TryBeginChase(Animator animator, ref bool chaseRequested)
    {
        if (!enemy.ShouldChasePlayer() || chaseRequested)
        {
            return false;
        }

        chaseRequested = true;
        animator.SetTrigger(ChaseParam);
        return true;
    }
}
