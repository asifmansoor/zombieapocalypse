using UnityEngine;
using System.Collections;

/*
 * Class : RootMotionConfigurator
 * Description : This class is to manage the added properties for the animation states. 
 */
public class RootMotionConfigurator : AIStateMachineLink
{
    [SerializeField] private int _rootPosition = 0;
    [SerializeField] private int _rootRotation = 0;

    private bool _rootMotionProcessed = false;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo animStateInfo, int layerIndex)
    {
        if (_stateMachine)
        {
            //Debug.Log(_stateMachine.GetType().ToString());
            _stateMachine.AddRootMotionRequest(_rootPosition, _rootRotation);
            _rootMotionProcessed = true;
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo animStateInfo, int layerIndex)
    {
        if (_stateMachine && _rootMotionProcessed)
        {
            _stateMachine.AddRootMotionRequest(-_rootPosition, -_rootRotation);
            _rootMotionProcessed = false;
        }
    }

}

