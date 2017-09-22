using UnityEngine;
using System.Collections;

[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
public class NavAgentExample : MonoBehaviour {

    // Inspector assigned variable
    public AIWaypointNetwork WaypointNetwork = null;
    public int CurrentIndex = 0;
    public bool HasPath = false;
    public bool PathPending = false;
    public bool PathStale = false;
    public UnityEngine.AI.NavMeshPathStatus PathStatus = UnityEngine.AI.NavMeshPathStatus.PathInvalid;
    public AnimationCurve JumpCurve = new AnimationCurve();

    // Private members
    private UnityEngine.AI.NavMeshAgent _navAgent = null;

	// Use this for initialization
	void Start () {
        // Cache NavMeshAgent reference
        _navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();

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
	void Update () {
        HasPath = _navAgent.hasPath;
        PathPending = _navAgent.pathPending;
        PathStale = _navAgent.isPathStale;
        PathStatus = _navAgent.pathStatus;

	    if (_navAgent.isOnOffMeshLink)
	    {
	        StartCoroutine(Jump(1.0f));
	        return;
	    }

        if ((!HasPath && !PathPending) || PathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
        {
            SetNextDestination(true);
        }
        else if (PathStale)
        {
            SetNextDestination(false);
        }
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
