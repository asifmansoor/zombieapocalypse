using UnityEngine;
using System.Collections;

[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
public class NavAgentRootMotion : MonoBehaviour {

    // Inspector assigned variable
    public AIWaypointNetwork WaypointNetwork = null;
    public int CurrentIndex = 0;
    public bool HasPath = false;
    public bool PathPending = false;
    public bool PathStale = false;
    public float RemainingDistance = 0.0f;
    public UnityEngine.AI.NavMeshPathStatus PathStatus = UnityEngine.AI.NavMeshPathStatus.PathInvalid;
    public AnimationCurve JumpCurve = new AnimationCurve();
    public bool MixedMode = true;

    // Private members
    private UnityEngine.AI.NavMeshAgent _navAgent = null;
    private Animator _animator = null;
    private float _smoothAngle = 0.0f;

	// Use this for initialization
	void Start () {
        // Cache NavMeshAgent reference
        _navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
	    _animator = GetComponent<Animator>();

	    _navAgent.updateRotation = false;

        if (WaypointNetwork == null) return;

        SetNextDestination(false);
	}

    void SetNextDestination(bool increment)
    {
        if (!WaypointNetwork) return;

        int incStep = increment ? 1 : 0;
        
        Transform nextWaypointTransform = null;

        int nextWaypoint = (CurrentIndex + incStep >= WaypointNetwork.Waypoints.Count) ? 0 : CurrentIndex + incStep;
        nextWaypointTransform = WaypointNetwork.Waypoints[nextWaypoint];

        if (nextWaypointTransform != null)
        {
            CurrentIndex = nextWaypoint;
            _navAgent.destination = nextWaypointTransform.position;
            return;
        }

        CurrentIndex++;


    }
	
	// Update is called once per frame
	void Update ()
	{

        HasPath = _navAgent.hasPath;
        PathPending = _navAgent.pathPending;
        PathStale = _navAgent.isPathStale;
        PathStatus = _navAgent.pathStatus;
	    RemainingDistance = _navAgent.remainingDistance;

	    Vector3 localDesiredVelocity = transform.InverseTransformVector(_navAgent.desiredVelocity);
	    float angle = Mathf.Atan2(localDesiredVelocity.x, localDesiredVelocity.z) * Mathf.Rad2Deg;

	    _smoothAngle = Mathf.MoveTowardsAngle(_smoothAngle, angle, 80.0f * Time.deltaTime);

	    float speed = localDesiredVelocity.z;

        _animator.SetFloat("Angle", _smoothAngle);
        _animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);

	    if (_navAgent.desiredVelocity.sqrMagnitude > Mathf.Epsilon)
	    {
	        if (!MixedMode ||
	            (MixedMode && Mathf.Abs(angle) < 80.0f &&
	             _animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Locomotion")))
	        {
	            Quaternion lookRotation = Quaternion.LookRotation(_navAgent.desiredVelocity, Vector3.up);
	            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 5.0f * Time.deltaTime);
	        }
	    }

	    /*if (_navAgent.isOnOffMeshLink)
	    {
	        StartCoroutine(Jump(1.0f));
	        return;
	    }*/

        if ((_navAgent.remainingDistance <= _navAgent.stoppingDistance && !PathPending) || PathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
        {
            SetNextDestination(true);
        }
        else if (PathStale)
        {
            SetNextDestination(false);
        }
	}

    void OnAnimatorMove()
    {
        if (MixedMode && !_animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Locomotion"))
        {
            transform.rotation = _animator.rootRotation;
        }

        _navAgent.velocity = _animator.deltaPosition / Time.deltaTime;
    }

    IEnumerator Jump(float duration)
    {
        UnityEngine.AI.OffMeshLinkData data = _navAgent.currentOffMeshLinkData;
        Vector3 startPos = _navAgent.transform.position;
        Vector3 endPos = data.endPos + (_navAgent.baseOffset * Vector3.up);
        float time = 0.0f;

        while (time <= duration)
        {
            float t = time / duration;
            _navAgent.transform.position = Vector3.Lerp(startPos, endPos, t) + (JumpCurve.Evaluate(t) * Vector3.up);
            time += Time.deltaTime;
            yield return null;
        }

        _navAgent.CompleteOffMeshLink();
    }
}
