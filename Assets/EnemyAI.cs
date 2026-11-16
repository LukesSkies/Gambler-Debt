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
        JogToPlayer,
        HitPlayer,
    }

    [SerializeField] private float _zombieTurningSpeed = 10;
    [SerializeField] private float _attackRange;

    [SerializeField] private EnemyStates _enemyState;

    private NavMeshAgent _agent;

    private EnemyAnimation _enemyAnimation;

    [SerializeField] private GameObject _barrierObject;
    private GameObject _player;

    public EnemySpawner EnemySpawner;

    public bool FinishedSpawning;
    public bool EnemyInSpawner;
    public bool CanRun;
    public bool CanJog;
    [SerializeField] private bool _walkedToBarriers;

    private bool InAttackRange()
    {
        return DistanceToPlayer() <= _attackRange;
    }


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
                if(!CanJog && CanRun)
                {
                    _enemyAnimation.Animator.SetBool("isRunning", true);
                }
                else if (!CanJog && !CanRun)
                {
                    _enemyAnimation.Animator.SetBool("isStumbling", true);
                }
                break;
            case EnemyStates.BreakingBarriers:
                _agent.ResetPath();
                _enemyAnimation.Animator.SetBool("hit", true);
                if(EnemySpawner.BarrierHealth <= 0)
                {
                    SwitchState();
                }
                break;
            case EnemyStates.WalkToPlayer:
                _agent.stoppingDistance = 1.5f;
                _enemyAnimation.Animator.SetBool("hit", false);
                _enemyAnimation.Animator.SetBool("isStumbling", true);
                _agent.SetDestination(_player.transform.position);
                FacePlayer();
                if (InAttackRange())
                {
                    _enemyState = EnemyStates.HitPlayer;
                }
                break;
            case EnemyStates.RunToPlayer:
                _agent.stoppingDistance = 1.2f;
                _enemyAnimation.Animator.SetBool("hit", false);
                _enemyAnimation.Animator.SetBool("isRunning", true);
                _enemyAnimation.Animator.SetBool("isStumbling", false);
                _agent.SetDestination(_player.transform.position);
                FacePlayer();
                if (InAttackRange())
                {
                    _enemyState = EnemyStates.HitPlayer;
                }
                break;
            case EnemyStates.JogToPlayer:
                _agent.stoppingDistance = 1.2f;
                _enemyAnimation.Animator.SetBool("hit", false);
                _enemyAnimation.Animator.SetBool("isRunning", false);
                _enemyAnimation.Animator.SetBool("isStumbling", false);
                _agent.SetDestination(_player.transform.position);
                FacePlayer();
                if (InAttackRange())
                {
                    _enemyState = EnemyStates.HitPlayer;
                }
                break;
            case EnemyStates.HitPlayer:
                _agent.ResetPath();
                FacePlayer();
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
            if (CanJog)
            {
                _agent.stoppingDistance = 1.2f;
                _enemyState = InAttackRange() ? EnemyStates.HitPlayer : EnemyStates.JogToPlayer;
            }
            else if (CanRun)
            {
                _agent.stoppingDistance = 1.2f;
                _enemyState = InAttackRange() ? EnemyStates.HitPlayer : EnemyStates.RunToPlayer;
            }
            else
            {
                _agent.stoppingDistance = 1.5f;
                _enemyState = InAttackRange() ? EnemyStates.HitPlayer : EnemyStates.WalkToPlayer;
            }
        }
    }
    private void FacePlayer()
    {
        Vector3 dir = _player.transform.position - transform.position;
        dir.y = 0f;

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), _zombieTurningSpeed * Time.deltaTime);
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
