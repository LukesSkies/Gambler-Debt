using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyStates
    {
        Spawn,
        WalkToBarriers,
        BreakingBarriers,
        WalkToPlayer,
        RunToPlayer,
        HitPlayer,
    }

    [SerializeField] private EnemyStates _enemyState;

    private NavMeshAgent _agent;

    private EnemyAnimation _enemyAnimation;

    [SerializeField] private GameObject _barrierObject;
    private GameObject _player;

    public EnemySpawner EnemySpawner;

    public bool FinishedSpawning;
    public bool EnemyInSpawner;
    public bool CanRun;
    [SerializeField] private bool _walkedToBarriers;


    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _enemyAnimation = transform.GetChild(0).GetComponent<EnemyAnimation>();
        EnemySpawner = transform.parent.transform.parent.transform.parent.GetComponent<EnemySpawner>();
        _barrierObject = EnemySpawner.MovePoint;
        _player = GameObject.Find("NewPlayer0");
    }

    void Start()
    {
        _enemyState = EnemyStates.Spawn;
        _walkedToBarriers = false;
    }

    void Update()
    {
        switch (_enemyState)
        {
            case EnemyStates.Spawn:
                break;
            case EnemyStates.WalkToBarriers:
                _agent.SetDestination(_barrierObject.transform.position);
                break;
            case EnemyStates.BreakingBarriers:
                _agent.ResetPath();
                _enemyAnimation.Animator.SetBool("hit", true);
                break;
            case EnemyStates.WalkToPlayer:
                _agent.stoppingDistance = 1.5f;
                _enemyAnimation.Animator.SetBool("hit", false);
                _enemyAnimation.Animator.SetBool("isRunning", false);
                _agent.SetDestination(_player.transform.position);
                transform.LookAt(transform.position);
                if(DistanceToPlayer() <= _agent.stoppingDistance + 0.2f)
                {
                    _enemyState = EnemyStates.HitPlayer;
                }
                break;
            case EnemyStates.RunToPlayer:
                _agent.stoppingDistance = 1.2f;
                _enemyAnimation.Animator.SetBool("hit", false);
                _enemyAnimation.Animator.SetBool("isRunning", true);
                _agent.SetDestination(_player.transform.position);
                transform.LookAt(transform.position);
                if (DistanceToPlayer() <= _agent.stoppingDistance + 0.2f)
                {
                    _enemyState = EnemyStates.HitPlayer;
                }
                break;
            case EnemyStates.HitPlayer:
                _agent.ResetPath();
                _enemyAnimation.Animator.SetBool("hit", true);
                if(DistanceToPlayer() > _agent.stoppingDistance + 0.2f)
                {
                    SwitchState();
                }
                break;
            default:
                break;
        }
    }

    public void SwitchState()
    {
        if(EnemySpawner.BarrierHealth > 0)
        {
            if (_walkedToBarriers)
            {
                _enemyState = EnemyStates.BreakingBarriers;
                return;
            }
            else
            {
                _enemyState = EnemyStates.WalkToBarriers;
                return;
            }
        }
        else if(EnemySpawner.BarrierHealth <= 0)
        {
            if (CanRun)
            {
                if(DistanceToPlayer() <= _agent.stoppingDistance + 0.2f)
                {
                    _enemyState = EnemyStates.HitPlayer;
                }
                else
                {
                    _enemyState = EnemyStates.RunToPlayer;
                }
            }
            else
            {
                if (DistanceToPlayer() <= _agent.stoppingDistance + 0.2f)
                {
                    _enemyState = EnemyStates.HitPlayer;
                }
                else
                {
                    _enemyState = EnemyStates.WalkToPlayer;
                }
            }
        }
    }

    private float DistanceToPlayer()
    {
        return Vector3.Distance(transform.position, _player.transform.position);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.name == "MovePoint")
        {
            _walkedToBarriers = true;
            SwitchState();
        }
        if (other.tag == "EnemySpawn")
        {
            EnemyInSpawner = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "EnemySpawn")
        {
            if (FinishedSpawning)
            {
                transform.parent = null;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "EnemySpawn")
        {
            EnemyInSpawner = false;
        }
    }
}
