using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public enum AIStateType {  None, Idle, Alerted, Patrol, Attack, Feeding, Pursuit, Dead }
public enum AITargetType { None, Waypoint, Visual_Player, Visual_Light, Visual_Food, Audio }
public enum AITriggerEventType {  Enter, Stay, Exit }
public enum AIBoneAlignmentType { XAxis, YAxis, ZAxis, XAxisInverted, YAxisInverted, ZAxisInverted }

/*
 * Struct : AITarget
 * Description : Structure to manage the information related to an NPC target
 */
public struct AITarget
{
    private AITargetType _type;
    private Collider _collider;
    private Vector3 _position;
    private float _distance;
    private float _time;

    public AITargetType type { get { return _type; } }
    public Collider collider { get { return _collider; } }
    public Vector3 position { get { return _position; } }
    public float distance { get { return _distance;} set { _distance = value; } }
    public float time { get { return _time;} }

    public void Set(AITargetType t, Collider c, Vector3 p, float d)
    {
        _type = t;
        _collider = c;
        _position = p;
        _distance = d;
        _time = Time.time;
    }

    public void Clear()
    {
        _type = AITargetType.None;
        _collider = null;
        _position = Vector3.zero;
        _time = 0.0f;
        _distance = Mathf.Infinity;
    }
}

/*
 * Class : AIStateMachine
 * Description : This is the abstract base class to manage the State Machines for our intelligent NPCs
 */
public abstract class AIStateMachine : MonoBehaviour
{
    // Public
    public AITarget VisualThreat = new AITarget();
    public AITarget AudioThreat = new AITarget();

    // Protected
    protected AIState _currentState = null;
    protected Dictionary<AIStateType, AIState> _states = new Dictionary<AIStateType, AIState>();
    protected AITarget _target = new AITarget();
    protected int _rootPositionRefCount = 0;
    protected int _rootRotationRefCount = 0;
    protected bool _isTargetReached = false;
    protected List<Rigidbody> _bodyParts = new List<Rigidbody>();
    protected int _aiBodyPartLayer = -1;
    protected bool _cinematicEnabled = false;

    // Inspector assigned variables
    [SerializeField] protected AIStateType _currentStateType = AIStateType.Idle;
    [SerializeField] protected Transform _rootBone = null;
    [SerializeField] protected AIBoneAlignmentType _RootBoneAlignmentType = AIBoneAlignmentType.ZAxis;
    [SerializeField] protected SphereCollider _targetTrigger = null;
    [SerializeField] protected SphereCollider _sensorTrigger = null;
    [SerializeField] protected AIWaypointNetwork _waypointNetwork = null;
    [SerializeField] protected bool _randomPatrol = false;
    [SerializeField] protected int _currentWaypoint = -1;
    [SerializeField] [Range(0, 15)] protected float _stoppingDistance = 1.0f;

    // Component Cache
    protected Animator _animator = null;
    protected UnityEngine.AI.NavMeshAgent _navAgent = null;
    protected Collider _collider = null;
    protected Transform _transform = null;

    // Public Properties
    public bool isTargetReached { get { return _isTargetReached; } }
    public bool inMeleeRange { get; set; }
    public Animator animator { get { return _animator; } }
    public UnityEngine.AI.NavMeshAgent navAgent { get { return _navAgent; } }

    public AIWaypointNetwork waypointNetwork { set { _waypointNetwork = value; } }

    public Vector3 sensorPosition
    {
        get
        {
            if (_sensorTrigger == null) return Vector3.zero;
            Vector3 point = _sensorTrigger.transform.position;
            point.x += _sensorTrigger.center.x * _sensorTrigger.transform.lossyScale.x;
            point.y += _sensorTrigger.center.y * _sensorTrigger.transform.lossyScale.y;
            point.z += _sensorTrigger.center.z * _sensorTrigger.transform.lossyScale.z;
            return point;
        }
    }

    public float sensorRadius
    {
        get
        {
            if (_sensorTrigger == null) return 0.0f;
            float radius = Mathf.Max(_sensorTrigger.radius * _sensorTrigger.transform.lossyScale.x,
                _sensorTrigger.radius * _sensorTrigger.transform.lossyScale.y);

            return Mathf.Max(radius, _sensorTrigger.radius * _sensorTrigger.transform.lossyScale.z);
        }
    }

    public bool useRootPosition {  get { return _rootPositionRefCount > 0; } }
    public bool useRootRotation { get { return _rootRotationRefCount > 0; } }

    public AITargetType targetType { get { return _target.type; } }
    public Vector3 targetPosition { get { return _target.position; } }

    public int targetColliderID
    {
        get
        {
            if (_target.collider)
                return _target.collider.GetInstanceID();
            else
                return -1;
        }
    }

    public bool cinematicEnabled { get { return _cinematicEnabled; } set { _cinematicEnabled = value; } }


    protected virtual void Awake()
    {
        _transform = transform;
        _animator = GetComponent<Animator>();
        _navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        _collider = GetComponent<Collider>();

        _aiBodyPartLayer = LayerMask.NameToLayer("AI Body Part");

        // Register the state machine instance with the NPC nav mesh collider and senson trigger
        if (GameSceneManager.instance != null)
        {
            if (_collider)
            {
                GameSceneManager.instance.RegisterAIStateMachines(_collider.GetInstanceID(), this);
            }
            if (_sensorTrigger)
            {
                GameSceneManager.instance.RegisterAIStateMachines(_sensorTrigger.GetInstanceID(), this);
            }
        }

        // Register the state machine instance with the body part colliders
        if (_rootBone != null)
        {
            Rigidbody[] bodies = _rootBone.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody bodyPart in bodies)
            {
                if (bodyPart != null && bodyPart.gameObject.layer == _aiBodyPartLayer)
                {
                    _bodyParts.Add(bodyPart);
                    GameSceneManager.instance.RegisterAIStateMachines(bodyPart.GetInstanceID(), this);
                }
            }
        }
    }
    protected virtual void Start()
    {
        if (_sensorTrigger != null)
        {
            AISensor script = _sensorTrigger.GetComponent<AISensor>();
            if (script != null)
            {
                script.ParentStateMachine = this;
            }
        }

        // Save all the state instances associated with the game object in the array
        AIState[] states = GetComponents<AIState>();
        foreach (AIState state in states)
        {
            if (state != null && !_states.ContainsKey(state.GetStateType()))
            {
                _states[state.GetStateType()] = state;
                state.SetStateMachine(this);
            }
        }

        if (_states.ContainsKey(_currentStateType))
        {
            _currentState = _states[_currentStateType];
            _currentState.OnEnterState();
        }
        else
        {
            _currentState = null;
        }

        if (_animator)
        {
            AIStateMachineLink[] scripts = _animator.GetBehaviours<AIStateMachineLink>();
            foreach (AIStateMachineLink script in scripts)
            {
                script.stateMachine = this;
            }
        }
    }

    /*
     * Method : GetWaypointPosition
     * Description : This method is to get the waypoint in the network where the NPC has to go
     */
    public Vector3 GetWaypointPosition(bool increment)
    {
        if (_currentWaypoint == -1) // if the current way point is not set then set to 0 or a random value based on the randomPatrol flag
            _currentWaypoint = _randomPatrol ? Random.Range(0, _waypointNetwork.Waypoints.Count) : 0;
        else
            if (increment)  // if the parameter is true then jump to the next waypoint else return the current waypoint
                NextWaypoint();

        if (_waypointNetwork.Waypoints[_currentWaypoint] != null)
        {
            Transform newWaypoint = _waypointNetwork.Waypoints[_currentWaypoint];

            SetTarget(AITargetType.Waypoint, null, newWaypoint.position, Vector3.Distance(newWaypoint.position, transform.position));

            return newWaypoint.position;
        }

        return Vector3.zero;

    }

    /*
     * Method : NextWaypoint
     * Description : This method is to get the next waypoint in the waypoint network
     */
    private void NextWaypoint()
    {
        Debug.Log("getting next waypoint");
        if (_randomPatrol && _waypointNetwork.Waypoints.Count > 1)
        {
            int oldWaypoint = _currentWaypoint;
            while (_currentWaypoint == oldWaypoint)
            {
                _currentWaypoint = Random.Range(0, _waypointNetwork.Waypoints.Count);
            }
        }
        else
        {
            _currentWaypoint = _currentWaypoint == _waypointNetwork.Waypoints.Count - 1 ? 0 : _currentWaypoint + 1;
            Debug.Log(_currentWaypoint);
        }

    }

    public void SetTarget(AITargetType t, Collider c, Vector3 p, float d)
    {
        _target.Set(t, c, p, d);

        if (_targetTrigger != null)
        {
            _targetTrigger.radius = _stoppingDistance;
            _targetTrigger.transform.position = _target.position;
            _targetTrigger.enabled = true;
        }
    }

    public void SetTarget(AITargetType t, Collider c, Vector3 p, float d, float s)
    {
        _target.Set(t, c, p, d);

        if (_targetTrigger != null)
        {
            _targetTrigger.radius = s;
            _targetTrigger.transform.position = _target.position;
            _targetTrigger.enabled = true;
        }
    }

    public void SetTarget(AITarget t)
    {
        _target = t;

        if (_targetTrigger != null)
        {
            _targetTrigger.radius = _stoppingDistance;
            _targetTrigger.transform.position = _target.position;
            _targetTrigger.enabled = true;
        }
    }

    public void ClearTarget()
    {
        _target.Clear();

        if (_targetTrigger != null)
        {
            _targetTrigger.enabled = false;
        }
    }

    protected virtual void FixedUpdate()
    {
        VisualThreat.Clear();
        AudioThreat.Clear();

        if (_target.type != AITargetType.None)
        {
            _target.distance = Vector3.Distance(_transform.position, _target.position);
        }

        _isTargetReached = false;
    }

    protected virtual void Update()
    {
        if (_currentState == null) return;

        // Call the update on the current state which returns the next state the NPC should be in
        AIStateType newstateType = _currentState.OnUpdate();

        // If the returned state is not the current state then do a state switch
        if (newstateType != _currentStateType)
        {
            AIState newState = null;
            if (_states.TryGetValue(newstateType, out newState))
            {
                _currentState.OnExitState();
                newState.OnEnterState();
                _currentState = newState;
            }
            else
            if (_states.TryGetValue(AIStateType.Idle, out newState))
            {
                _currentState.OnExitState();
                newState.OnEnterState();
                _currentState = newState;
            }

            _currentStateType = newstateType;
        }
    }

    /*
     * This method is to detect if the NPC has collided with its target trigger which was set to the target position
     */
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (_targetTrigger == null || other != _targetTrigger) return;

        _isTargetReached = true;

        if (_currentState)
        {
            _currentState.OnDestinationReached(true);
        }
    }

    protected virtual void OnTriggerStay(Collider other)
    {
        if (_targetTrigger == null || other != _targetTrigger) return;

        _isTargetReached = true;
    }


    protected virtual void OnTriggerExit(Collider other)
    {
        if (_targetTrigger == null || other != _targetTrigger) return;

        _isTargetReached = false;

        if (_currentState)
        {
            _currentState.OnDestinationReached(false);
        }
    }

    public virtual void OnTriggerEvent(AITriggerEventType type, Collider other)
    {
        if (_currentState != null)
        {
            _currentState.OnTriggerEvent(type, other);
        }
    }

    protected virtual void OnAnimatorMove()
    {
        if (_currentState != null)
        {
            _currentState.OnAnimatorUpdated();
        }
    }

    protected virtual void OnAnimatorIK(int layerIndex)
    {
        if (_currentState != null)
        {
            _currentState.OnAnimatorIKUpdated();
        }
    }

    public void NavAgentControl(bool updatePosition, bool updateRotation)
    {
        if (_navAgent)
        {
            _navAgent.updatePosition = updatePosition;
            _navAgent.updateRotation = updateRotation;
        }
    }

    public void AddRootMotionRequest(int rootPosition, int rootRotation)
    {
        _rootPositionRefCount += rootPosition;
        _rootRotationRefCount += rootRotation;
    }

    /*
     * Method : TakeDamage
     * Description : This method is to process the damage to the NPC by the player
     */
    public virtual void TakeDamage(Vector3 position, Vector3 force, int damage, Rigidbody bodyPart,
        CharacterManager characterManager, int hitDirection = 0)
    {
        if (GameSceneManager.instance != null && GameSceneManager.instance.bloodParticles != null)
        {
            ParticleSystem sys = GameSceneManager.instance.bloodParticles;
            sys.transform.position = position;
            ParticleSystem.MainModule main = sys.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            //sys.simulationSpace = ParticleSystemSimulationSpace.World;
            sys.Emit(60);
        }
    }

}
