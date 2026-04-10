using UnityEngine;
using UnityEngine.AI;

public class EnemyStateMachineBehaviour : StateMachineBehaviour
{
    protected Enemy enemy;
    protected NavMeshAgent agent;

    protected const string PatrolParam = "Patrol";
    protected const string IdleParam = "Idle";
    protected const string CapturingParam = "IsCapturing";
    protected const string DanceIdxParam = "DanceIndex";
    protected const string SpeedParam = "Speed";

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (TryResolveEnemyAndAgent(animator)) {
            return;
        }
    }
    protected bool TryResolveEnemyAndAgent(Animator animator)
    {
        
        enemy = animator.GetComponent<Enemy>();
        if (enemy != null) {
            agent = enemy.Agent;
            return true;
        }

        enemy = animator.GetComponentInParent<Enemy>();
        if (enemy != null) {
            agent = enemy.Agent;
            return true;
        }

        enemy = animator.GetComponentInChildren<Enemy>(true);
        if (enemy != null) {
            agent = enemy.Agent;
            return true;
        }

        agent = null;
        Debug.Log("No enemy found");
        return false;
    }
}
