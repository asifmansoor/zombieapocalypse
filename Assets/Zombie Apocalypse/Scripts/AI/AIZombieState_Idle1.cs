﻿using UnityEngine;
using Random = UnityEngine.Random;

/*
 * Class : 
 * Description : 
 */
public class AIZombieState_Idle1 : AIZombieState
{
    // Inspector assigned
    [SerializeField] private Vector2 _idleTimeRange = new Vector2(5.0f, 10.0f);//(10.0f, 60.0f);

    // Private
    private float _idleTime = 0.0f;
    private float _timer = 0.0f;

    public override AIStateType GetStateType()
    {
        return AIStateType.Idle;
    }

    public override void OnEnterState()
    {
        Debug.Log("Entering Idle State");
        base.OnEnterState();
        if (_zombieStateMachine == null) return;
        _idleTime = Random.Range(_idleTimeRange.x, _idleTimeRange.y);
        _timer = 0.0f;

        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.speed = 0;
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.feeding = false;
        _zombieStateMachine.attackType = 0;
        _zombieStateMachine.ClearTarget();
    }

    public override AIStateType OnUpdate()
    {
        if (_zombieStateMachine == null) return AIStateType.Idle;

        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Player)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Pursuit;
        }

        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Light)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Alerted;
        }

        if (_zombieStateMachine.AudioThreat.type == AITargetType.Audio)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
            return AIStateType.Alerted;
        }

        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Food)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Pursuit;
        }


        _timer += Time.deltaTime;
        if (_timer > _idleTime)
        {
            Debug.Log("Going to Alerted");
            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(false));
            _zombieStateMachine.navAgent.Resume();
            return AIStateType.Alerted;
        }

        return AIStateType.Idle;
    }
}
