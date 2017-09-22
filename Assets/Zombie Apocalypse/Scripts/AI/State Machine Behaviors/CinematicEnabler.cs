using UnityEngine;
using System.Collections;

/*
 * Class : CinematicEnabler
 * Description : This class is to manage the cinematic animation play
 */
public class CinematicEnabler : AIStateMachineLink
{
    public bool OnEnter = false;
    public bool OnExit = false;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo animStateInfo, int layerIndex)
    {
        if (_stateMachine)
        {
            _stateMachine.cinematicEnabled = OnEnter;
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo animStateInfo, int layerIndex)
    {
        if (_stateMachine)
        {
            _stateMachine.cinematicEnabled = OnExit;
        }
    }

}
