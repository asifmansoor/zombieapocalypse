using UnityEngine;
using System.Collections;

/*
 * Class : 
 * Description : 
 */
public class AIZombieState_Alerted1 : AIZombieState
{
    // Inspector assigned
    [SerializeField] [Range(1, 60)] private float _maxDuration = 3.0f;
    [SerializeField] private float _waypointAngleThreshold = 25.0f;//90.0f;
    [SerializeField] private float _threatAngleThreshold = 10.0f;
    [SerializeField] private float _directionChangeTime = 1.5f;
    [SerializeField] private float _slerpSpeed = 10.0f;//45.0f;

    // Private
    private float _timer = 0.0f;
    private float _directionChangeTimer = 0.0f;

    public override AIStateType GetStateType()
    {
        return AIStateType.Alerted;
    }

    public override void OnEnterState()
    {
        Debug.Log("Entering Alerted State");
        base.OnEnterState();
        if (_zombieStateMachine == null) return;

        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.speed = 0;
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.feeding = false;
        _zombieStateMachine.attackType = 0;

        _timer = _maxDuration;
        _directionChangeTimer = 0.0f;
    }


    public override AIStateType OnUpdate()
    {
        _timer -= Time.deltaTime;
        _directionChangeTimer += Time.deltaTime;

        if (_timer <= 0.0f)
        {
            //Debug.Log("Timer up!!!!");
            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(false));
            _zombieStateMachine.navAgent.Resume();
            _timer = _maxDuration;
        }

        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Player)
        {
            //Debug.Log("Detected player");
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Pursuit;
        }

        if (_zombieStateMachine.AudioThreat.type == AITargetType.Audio)
        {
            //Debug.Log("Detected Sound Aggrevator");
            _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
            _timer = _maxDuration;
        }

        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Light)
        {
            //Debug.Log("Detected Light Aggravator");
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            _timer = _maxDuration;
        }

        if (_zombieStateMachine.AudioThreat.type == AITargetType.None &&
            _zombieStateMachine.VisualThreat.type == AITargetType.Visual_Food && _zombieStateMachine.targetType == AITargetType.None)
        {
            //Debug.Log("Detected food");
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Pursuit;
        }

        float angle;

        if ((_zombieStateMachine.targetType == AITargetType.Audio || _zombieStateMachine.targetType == AITargetType.Visual_Light) && !_zombieStateMachine.isTargetReached)
        {
            angle = AIState.FindSignedAngle(_zombieStateMachine.transform.forward,
                _zombieStateMachine.targetPosition - _zombieStateMachine.transform.position);

            if (_zombieStateMachine.targetType == AITargetType.Audio && Mathf.Abs(angle) < _threatAngleThreshold)
            {
                return AIStateType.Pursuit;
            }

            if (_directionChangeTimer > _directionChangeTime)
            {
                if (Random.value < _zombieStateMachine.intelligence)
                {
                    _zombieStateMachine.seeking = (int) Mathf.Sign(angle);
                }
                else
                {
                    _zombieStateMachine.seeking = (int) Mathf.Sign(Random.Range(-1.0f, 1.0f));
                }

                _directionChangeTimer = 0.0f;
            }
        }
        else if (_zombieStateMachine.targetType == AITargetType.Waypoint && !_zombieStateMachine.navAgent.pathPending)
        {
            //Debug.Log("Waypoint to be reached");
            if (_zombieStateMachine.isCrawling) _zombieStateMachine.speed = 0;
            angle = AIState.FindSignedAngle(_zombieStateMachine.transform.forward,
                _zombieStateMachine.navAgent.steeringTarget - _zombieStateMachine.transform.position);

            if (Mathf.Abs(angle) < _waypointAngleThreshold) return AIStateType.Patrol;
            //else Debug.Log("Angle too large");
            if (_directionChangeTimer > _directionChangeTime)
            {
                _zombieStateMachine.seeking = (int)Mathf.Sign(angle);
                _directionChangeTimer = 0.0f;
            }
        }
        else
        {
            //Debug.Log("Path pending or target is none");
            if (_directionChangeTimer > _directionChangeTime)
            {
                _zombieStateMachine.seeking = (int)Mathf.Sign(Random.Range(-1.0f, 1.0f));
                _directionChangeTimer = 0.0f;
            }
        }

        if (!_zombieStateMachine.useRootRotation)
            _zombieStateMachine.transform.Rotate(new Vector3(0.0f,
                _slerpSpeed * _zombieStateMachine.seeking * Time.deltaTime, 0.0f));

        return AIStateType.Alerted;
    }
}
