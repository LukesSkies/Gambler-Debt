using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;
using UnityHFSM;
using UnityHFSM.Visualization;

public class EnemyStateMachine : MonoBehaviour
{
    public bool FinishedSpawning;
    public bool EnemyInSpawner;
    public bool CanRun;
    private bool _walkedToBarriers;

    private NavMeshAgent _agent;

    private EnemyAnimation _enemyAnimation;
    public EnemySpawner EnemySpawner;

    private StateMachine _zombieFSM;

    private GameObject _barrierObject;
    private GameObject _player;

    [SerializeField] private Animator _debugAnimator;

    public List<Collider> EnemyColliders;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _zombieFSM = new StateMachine();
        _enemyAnimation = transform.GetChild(0).GetComponent<EnemyAnimation>();
        EnemySpawner = transform.parent.transform.parent.transform.parent.GetComponent<EnemySpawner>();
        _barrierObject = EnemySpawner.Barrier;
        _player = GameObject.Find("NewPlayer0");
    }

    private void Start()
    {
        FinishedSpawning = false;
        EnemyInSpawner = true;
        _walkedToBarriers = false;

        _zombieFSM.AddState("Spawn");

        _zombieFSM.AddState("WalkToBarriers",
            onEnter: state => WalkToBarriers(),
            onLogic: state =>
            {
                if (_agent.pathStatus == NavMeshPathStatus.PathComplete && _agent.remainingDistance < 0.1)
                    FinishedWalkingToBarriers();
            });


        _zombieFSM.AddState("BreakingBarriers",
            onLogic: state =>
            {
                if (EnemySpawner.BarrierHealth == 0)
                    BarrierBroken();
            });

        _zombieFSM.AddState("WalkToPlayer",
            onEnter: state =>
            {
                SetWalkTrigger();
            },

            onLogic: state =>
            {
                MoveToPlayer();
            });

        _zombieFSM.AddState("RunToPlayer",
            onEnter: state =>
            {
                SetWalkTrigger();
            },

            onLogic: state =>
            {
                MoveToPlayer();
            });
        _zombieFSM.AddState("HitPlayer",
            onEnter: state =>
            {
                _enemyAnimation.Animator.SetBool("hit", true);
            },

            onExit: state =>
            {
                _enemyAnimation.Animator.SetBool("hit", false);
            });

        _zombieFSM.AddTransition(
            "Spawn",
            "WalkToPlayer",
            transition => FinishedSpawning != false && CanRun != true && EnemySpawner.BarrierHealth <= 0
            );

        _zombieFSM.AddTransition(
            "Spawn",
            "RunToPlayer",
            transition => FinishedSpawning != false && CanRun != false && EnemySpawner.BarrierHealth <= 0
            );

        _zombieFSM.AddTransition(
            "Spawn",
            "WalkToBarriers",
            transition => FinishedSpawning != false && CanRun != true && EnemySpawner.BarrierHealth > 0
            );

        _zombieFSM.AddTransition(
            "WalkToBarriers",
            "BreakingBarriers",
            transition => _walkedToBarriers != false
            );

        _zombieFSM.AddTransition(
            "BreakingBarriers",
            "WalkToPlayer",
            transition => EnemyInSpawner != true && CanRun != true
            );

        _zombieFSM.AddTransition(
            "BreakingBarriers",
            "RunToPlayer",
            transition => EnemyInSpawner != true && CanRun != false
            );

        _zombieFSM.AddTransition(
            "WalkToPlayer",
            "HitPlayer",
            transition => DistanceToPlayer() <= _agent.stoppingDistance + 0.2f
        );

        _zombieFSM.AddTransition(
            "RunToPlayer",
            "HitPlayer",
            transition => DistanceToPlayer() <= _agent.stoppingDistance + 0.2f
        );

        _zombieFSM.AddTransition(
            "HitPlayer",
            "WalkToPlayer",
            transition => DistanceToPlayer() > _agent.stoppingDistance + 0.2f
                       && CanRun != true
        );

        _zombieFSM.AddTransition(
            "HitPlayer",
            "RunToPlayer",
            transition => DistanceToPlayer() > _agent.stoppingDistance + 0.2f
                       && CanRun != false
        );

        _zombieFSM.SetStartState("Spawn");
        _zombieFSM.Init();

        #if UNITY_EDITOR
                HfsmAnimatorGraph.CreateAnimatorFromStateMachine(
                    _zombieFSM,
                    outputFolderPath: "Assets/DebugAnimators",
                    animatorName: "StateMachineAnimatorGraph.controller"
                );
        #endif
    }

    void Update()
    {
        _zombieFSM.OnLogic();
        print(_zombieFSM.ActiveStateName);

        #if UNITY_EDITOR
                HfsmAnimatorGraph.PreviewStateMachineInAnimator(_zombieFSM, _debugAnimator);
        #endif
    }

    private void WalkToBarriers()
    {
        _agent.SetDestination(_barrierObject.transform.position + new Vector3(0, 0, -1f));
    }

    private void FinishedWalkingToBarriers()
    {
        _agent.ResetPath();
        _walkedToBarriers = true;
        _enemyAnimation.Animator.SetBool("hit", true);
    }

    private void BarrierBroken()
    {
        EnemyInSpawner = false;
        _enemyAnimation.Animator.SetTrigger("walk");
    }

    private void SetWalkTrigger()
    {
        _agent.stoppingDistance = 1.5f;
        _enemyAnimation.Animator.SetTrigger("walkTrigger");
        _enemyAnimation.Animator.SetBool("hit", false);
    }

    private void MoveToPlayer()
    {
        _agent.SetDestination(_player.transform.position);
        transform.LookAt(transform.position);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "EnemySpawn")
        {
            EnemyInSpawner = true;
            transform.parent = null;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "EnemySpawn")
        {
            EnemyInSpawner = false;
        }
    }

    private float DistanceToPlayer()
    {
        return Vector3.Distance(transform.position, _player.transform.position);
    }

    public void DisableBarrierCollider(Collider barrier)
    {
        foreach (Collider enemyColliders in EnemyColliders)
        {
            Physics.IgnoreCollision(barrier, enemyColliders, true);
        }
    }

    public void EnableBarrierCollider(Collider barrier)
    {
        foreach (Collider enemyColliders in EnemyColliders)
        {
            Physics.IgnoreCollision(barrier, enemyColliders, false);
        }
    }
}
