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
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        TryTriggerAttackIfInRange(animator);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool(CapturingParam, false);
    }
}
