using UnityEngine;

public class EnemyCaptureState : EnemyStateMachineBehaviour
{
    private const int DanceVariantCount = 4;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);

        int danceIndex = Random.Range(0, DanceVariantCount);

        animator.SetBool(CapturingParam, true);
        animator.SetInteger(DanceIdxParam, danceIndex);


        if (agent != null)
        {
            agent.updateRotation = false;
        }
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (enemy == null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Vector3 lookAtPosition = mainCamera.transform.position;
        lookAtPosition.y = enemy.transform.position.y;
        enemy.transform.LookAt(lookAtPosition);
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool(CapturingParam, false);

        if (agent != null)
        {
            agent.updateRotation = true;
        }
    }
}
