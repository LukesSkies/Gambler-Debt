using System.Collections;
using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
    public Animator Animator;
    [SerializeField] private float _resetPosDuration;
    private CapsuleCollider _attackCollider;
    private EnemyStateMachine _enemyStateMachine;

    void Awake()
    {
        Animator = GetComponent<Animator>();
        _enemyStateMachine = GetComponentInParent<EnemyStateMachine>();
        _attackCollider = transform.Find("AttackCollider").GetComponent<CapsuleCollider>();
    }

    private void Start()
    {
        _attackCollider.enabled = false;
    }

    private void SetMovementTrigger()
    {
        _enemyStateMachine.FinishedSpawning = true;
        Animator.applyRootMotion = false;
        StartCoroutine(ResetPos());
        if (_enemyStateMachine.CanRun)
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

    private void ZombieHit()
    {
        if (_enemyStateMachine.EnemyInSpawner && _enemyStateMachine.EnemySpawner != null)
        {
            Debug.Log("HittingBarrier");
            _enemyStateMachine.EnemySpawner.ZombieBarrierHit();
        }
        else if (!_enemyStateMachine.EnemyInSpawner)
        {
            StartCoroutine(AttackPlayer());
        }
    }

    private IEnumerator AttackPlayer()
    {
        _attackCollider.enabled = true;
        yield return new WaitForSeconds(0.1f);
        _attackCollider.enabled = false;
    }
}
