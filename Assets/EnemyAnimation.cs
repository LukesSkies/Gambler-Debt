using System.Collections;
using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
    public Animator Animator;
    [SerializeField] private float _resetPosDuration;

    private EnemyStateMachine _enemyStateMachine;

    void Awake()
    {
        Animator = GetComponent<Animator>();
        _enemyStateMachine = GetComponentInParent<EnemyStateMachine>();
    }

    private void SetMovementTrigger()
    {
        _enemyStateMachine.FinishedSpawning = true;
        Animator.applyRootMotion = false;
        StartCoroutine(ResetPos());
        if (Animator.GetBool("isRunning"))
        {
            Animator.SetTrigger("run");
        }
        else
        {
            Animator.SetTrigger("walk");
        }
    }

    private IEnumerator ResetPos()
    {
        float timeElapsed = 0;

        while (timeElapsed < _resetPosDuration)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, Vector3.zero, timeElapsed / _resetPosDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = Vector3.zero;
    }

    private void RemoveBarriers()
    {
        if(_enemyStateMachine.EnemyInSpawner && _enemyStateMachine.EnemySpawner != null && _enemyStateMachine.EnemySpawner.Barriers.Count > 0)
        {
            GameObject barrier = _enemyStateMachine.EnemySpawner.Barriers[0];
            barrier.SetActive(false);
            _enemyStateMachine.EnemySpawner.Barriers.Remove(barrier);
        }
    }
}
