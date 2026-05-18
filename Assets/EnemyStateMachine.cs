using UnityEngine;
using UnityEngine.AI;
using UnityHFSM;
using UnityHFSM.Visualization;

public class EnemyStateMachine : MonoBehaviour
{
    [SerializeField] private bool _walkedToBarriers;
    [SerializeField] private bool _barriersBroken;
    public bool FinishedSpawning;
    public bool EnemyInSpawner;
    [SerializeField] private Transform _target;

    private NavMeshAgent _agent;

    private EnemyAnimation _enemyAnimation;
    public EnemySpawner EnemySpawner;

    private StateMachine _zombieFSM;

    private GameObject _player;

    [SerializeField] private Animator _debugAnimator;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _zombieFSM = new StateMachine();
        _enemyAnimation = transform.GetChild(0).GetComponent<EnemyAnimation>();
        EnemySpawner = transform.parent.transform.parent.transform.parent.GetComponent<EnemySpawner>();
        _player = GameObject.Find("NewPlayer0");
    }

    private void Start()
    {
        _walkedToBarriers = false;
        FinishedSpawning = false;

        _zombieFSM.AddState("Spawn");
        _zombieFSM.AddState("WalkingToBarriers",
            onEnter: state => WalkToBarriers(),
            onLogic: state =>
            {
                if (_agent.pathStatus == NavMeshPathStatus.PathComplete && _agent.remainingDistance < 0.1)
                    FinishedWalkingToBarriers();
            });


        _zombieFSM.AddState("BreakingBarriers",
            onLogic: state =>
            {
                if (EnemySpawner.Barriers.Count == 0)
                    BarriersBroken();
            });

        _zombieFSM.AddState("JumpOverBarriers",
            onLogic: state =>
            {
                transform.position += new Vector3(0, 0, 0.007f);
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
            "WalkingToBarriers",
            transition => FinishedSpawning != false
            );

        _zombieFSM.AddTransition(
            "WalkingToBarriers",
            "BreakingBarriers",
            transition => _walkedToBarriers != false
            );

        _zombieFSM.AddTransition(
            "BreakingBarriers",
            "JumpOverBarriers",
            transition => _barriersBroken != false
            );

        _zombieFSM.AddTransition(
            "JumpOverBarriers",
            "WalkToPlayer",
            transition => EnemyInSpawner != true && _enemyAnimation.Animator.GetBool("isRunning") != true
            );

        _zombieFSM.AddTransition(
            "JumpOverBarriers",
            "RunToPlayer",
            transition => EnemyInSpawner != true && _enemyAnimation.Animator.GetBool("isRunning") != false
            );

        _zombieFSM.AddTransition(
            "WalkToPlayer",
            "HitPlayer",
            transition => DistanceToPlayer() <= _agent.stoppingDistance
        );

        _zombieFSM.AddTransition(
            "RunToPlayer",
            "HitPlayer",
            transition => DistanceToPlayer() <= _agent.stoppingDistance
        );

        _zombieFSM.AddTransition(
            "HitPlayer",
            "WalkToPlayer",
            transition => DistanceToPlayer() > _agent.stoppingDistance
                       && !_enemyAnimation.Animator.GetBool("isRunning")
        );

        _zombieFSM.AddTransition(
            "HitPlayer",
            "RunToPlayer",
            transition => DistanceToPlayer() > _agent.stoppingDistance
                       && _enemyAnimation.Animator.GetBool("isRunning")
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
        _agent.SetDestination(_target.position);
    }

    private void FinishedWalkingToBarriers()
    {
        _agent.ResetPath();
        _walkedToBarriers = true;
        _enemyAnimation.Animator.SetBool("hit", true);
    }

    private void BarriersBroken()
    {
        _barriersBroken = true;
        _enemyAnimation.Animator.SetTrigger("climb");
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
}
