using UnityEngine;

/// <summary>Plays capture dance facing the camera; can still trigger Attack.</summary>
public class EnemyCaptureState : EnemyStateMachineBehaviour
{
    [SerializeField] private int danceVariantCount = 10;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        animator.SetBool(CapturingParam, true);
        animator.SetInteger(DanceIdxParam, Random.Range(0, danceVariantCount));
        animator.SetFloat(SpeedParam, 0f);
        if (enemy != null)
        {
            enemy.StopMovement();
        }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (BlockStateWhenGrenadeStunned(animator))
        {
            return;
        }

        if (enemy != null && enemy.IsInCapturableZone())
        {
            enemy.StopMovement();
        }

        TryTriggerAttackIfInRange(animator);

        // Transitions out of capture require IsCapturing false before OnStateExit; Patrol/Chase SMBs
        // do not run during this state, so we clear the bool when capture is no longer valid.
        if (!enemy.IsInCapturableZone())
        {
            animator.SetBool(CapturingParam, false);
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool(CapturingParam, false);
    }
}
