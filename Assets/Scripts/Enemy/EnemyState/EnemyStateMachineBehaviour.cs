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
    protected const string IsStunnedParam = "IsStunned";

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        enemy = animator.GetComponentInParent<Enemy>();
        agent = enemy != null ? enemy.Agent : null;
        if (enemy != null)
        {
            enemy.SetStateMachineHostAnimator(animator);
        }
    }

    /// <summary>Kinematic grenade stun runs on <see cref="Enemy"/> timing; block other SMB logic until the animator transitions to Stunned.</summary>
    protected bool BlockStateWhenGrenadeStunned(Animator animator)
    {
        if (enemy != null && enemy.IsGrenadeStunActive)
        {
            enemy.StopMovement();
            animator.SetFloat(SpeedParam, 0f);
            return true;
        }

        return false;
    }

    // Clears chaseRequested when out of aggro so Chase can trigger again later.
    protected static void ResetChaseIfFar(Enemy enemy, ref bool chaseRequested)
    {
        if (!enemy.ShouldChasePlayer())
        {
            chaseRequested = false;
        }
    }


    // Contested tile: never capture-dance here; let chase / attack run instead of freezing the agent.
    protected bool HandleContestedZone(Animator animator)
    {
        if (!enemy.IsZoneContestedWithPlayer())
        {
            return false;
        }

        animator.SetBool(CapturingParam, false);

        if (enemy.ShouldChasePlayer())
        {
            return false;
        }

        enemy.StopMovement();
        animator.SetFloat(SpeedParam, 0f);
        animator.SetBool(IsAttackingParam, false);
        return true;
    }

    // Enter or continue capture dance; keeps agent stopped while on tile.
    protected bool HandleCapturableZone(Animator animator, ref bool captureRequested)
    {
        if (!enemy.IsInCapturableZone())
        {
            captureRequested = false;
            return false;
        }

        if (!captureRequested)
        {
            captureRequested = true;
            animator.SetFloat(SpeedParam, 0f);
        }

        enemy.StopMovement();
        animator.SetBool(CapturingParam, true);
        return true;
    }

    protected bool TryTriggerAttackIfInRange(Animator animator)
    {
        if (enemy == null || !enemy.IsPlayerInAttackRange())
        {
            return false;
        }

        animator.SetBool(CapturingParam, false);
        animator.SetBool(IsAttackingParam, true);
        animator.SetFloat(SpeedParam, 0f);

        return true;
    }

    protected bool TryBeginChase(Animator animator, ref bool chaseRequested)
    {
        if (!enemy.ShouldChasePlayer() || chaseRequested)
        {
            return false;
        }

        // March to capturable zones first; only chase when contesting, hunting with no tiles left, or in attack range.
        if (enemy.HasEnemyCaptureTargets()
            && !enemy.IsZoneContestedWithPlayer()
            && !enemy.IsPlayerInAttackRange())
        {
            return false;
        }

        chaseRequested = true;
        animator.SetTrigger(ChaseParam);
        return true;
    }
}
