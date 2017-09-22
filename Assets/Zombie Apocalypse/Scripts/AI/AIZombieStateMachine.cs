using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum AIBoneControlType { Animated, Ragdoll, RagdollToAnim }

/*
 * Class : BodyPartSnapshot
 * Description : This class is to hold the information for the snapshot of the NPC body parts before it gets ragdolled so that it can be reanimated
 */
public class BodyPartSnapshot
{
    public Transform transform;
    public Vector3 position;
    public Quaternion rotation;
}

/*
 * Class : AIZombieStateMachine
 * Description : This class is to manage the state machine for the zombie NPC
 */
public class AIZombieStateMachine : AIStateMachine
{
    [SerializeField] [Range(10.0f, 360.0f)] float _fov = 120.0f;//50
    [SerializeField] [Range(0.0f, 1.0f)] float _sight = 0.5f;
    [SerializeField] [Range(0.0f, 1.0f)] float _hearing = 1.0f;
    [SerializeField] [Range(0.0f, 1.0f)] float _aggression = 0.5f;
    [SerializeField] [Range(0, 100)] int _health = 100;
    [SerializeField] [Range(0, 100)] int _lowerBodyDamage = 0;
    [SerializeField] [Range(0, 100)] int _upperBodyDamage = 0;
    [SerializeField] [Range(0, 100)] private int _upperBodyThreshold = 30;
    [SerializeField] [Range(0, 100)] private int _limpThreshold = 30;
    [SerializeField] [Range(0, 100)] private int _crawlThreshold = 90;
    [SerializeField] [Range(0.0f, 1.0f)] private float _intelligence = 1.0f;//0.5f;
    [SerializeField] [Range(0.0f, 1.0f)] private float _satisfaction = 0.45f; //1.0f;
    [SerializeField] private float _replenishRate = 2.13f;//0.5f;
    [SerializeField] private float _depletionRate = 0.1f;
    [SerializeField] private float _reanimationBlendTime = 0.5f;//1.5f;
    [SerializeField] private float _reanimationWaitTime = 3.0f;
    [SerializeField] private LayerMask _geometryLayers = 0;

    // Private
    private int _seeking = 0;
    private bool _feeding = false;
    private bool _crawling = false;
    private int _attackType = 0;
    private float _speed = 0.0f;

    // Ragdoll stuff
    private AIBoneControlType _boneControlType = AIBoneControlType.Animated;
    private List<BodyPartSnapshot> _bodyPartSnapshots = new List<BodyPartSnapshot>();
    private float _ragdollEndTime = float.MinValue;
    private Vector3 _ragdollHipPosition;
    private Vector3 _ragdollFeetPosition;
    private Vector3 _ragdollHeadPosition;
    private IEnumerator _reanimationCoroutine = null;
    private float _mecanimTransitionTime = 0.1f;

    // Hashes
    private int _speedHash = Animator.StringToHash("Speed");
    private int _seekingHash = Animator.StringToHash("Seeking");
    private int _feedingHash = Animator.StringToHash("Feeding");
    private int _attackHash = Animator.StringToHash("Attack");
    private int _crawlingHash = Animator.StringToHash("Crawling");
    private int _hitTriggerHash = Animator.StringToHash("Hit");
    private int _hitTypeHash = Animator.StringToHash("HitType");
    private int _upperBodyDamageHash = Animator.StringToHash("Upper Body Damage");
    private int _lowerBodyDamageHash = Animator.StringToHash("Lower Body Damage");
    private int _reanimateFromBackHash = Animator.StringToHash("Reanimate From Back");
    private int _reanimateFromFrontHash = Animator.StringToHash("Reanimate From Front");
    private int _stateHash = Animator.StringToHash("State");

    // Public properties
    public float replenishRate { get { return _replenishRate; } }
    public float fov { get { return _fov; } }
    public float sight { get { return _sight;} }
    public float hearing { get { return _hearing;} }
    public float aggression { get { return _aggression;} set { _aggression = value; } }
    public int health { get { return _health;} set { _health = value; } }
    public float intelligence { get { return _intelligence; } }
    public float satisfaction { get { return _satisfaction;} set { _satisfaction = value; } }
    public int seeking { get { return _seeking; } set { _seeking = value; } }
    public bool feeding { get { return _feeding; } set { _feeding = value; } }
    public bool crawling { get { return _crawling; } }
    public int attackType { get { return _attackType;} set { _attackType = value; } }
    public float speed { get { return _speed; } set { _speed = value; } }

    public bool isCrawling { get { return _lowerBodyDamage >= _crawlThreshold; } }

    public int lowerBodyDamage { set { _lowerBodyDamage = value; } }

    protected override void Start()
    {
        base.Start();

        // Capture the snapshots of each body part in the rig so that it can be reanimated after getting ragdolled
        if (_rootBone != null)
        {
            Transform[] transforms = _rootBone.GetComponentsInChildren<Transform>();
            foreach (Transform trans in transforms)
            {
                BodyPartSnapshot snapshot = new BodyPartSnapshot();
                snapshot.transform = trans;
                _bodyPartSnapshots.Add(snapshot);
            }
        }

        UpdateAnimatorDamage();
    }

    protected override void Update()
    {
        base.Update();

        if (_animator != null)
        {
            _animator.SetFloat(_speedHash, _speed);
            _animator.SetBool(_feedingHash, _feeding);
            _animator.SetInteger(_seekingHash, _seeking);
            _animator.SetInteger(_attackHash, _attackType);
            _animator.SetInteger(_stateHash, (int)_currentStateType);
        }

        // reduce the satisfaction property of the zombie according to the set depletion rate and its speed
        _satisfaction = Mathf.Max(0, _satisfaction - ((_depletionRate * Time.deltaTime) / 100.0f) * Mathf.Pow(_speed, 3.0f));
    }

    /*
     * Method : UpdateAnimatorDamage
     * Description : Thi method is to set the animator properties for the animations
     */
    protected void UpdateAnimatorDamage()
    {
        if (_animator != null)
        {
            _animator.SetBool(_crawlingHash, isCrawling);
            _animator.SetInteger(_lowerBodyDamageHash, _lowerBodyDamage);
            _animator.SetInteger(_upperBodyDamageHash, _upperBodyDamage);
        }
    }

    /*
     * Method : TakeDamage
     * Description : This method is to process the damage to the NPC when the player attacks it. It overrides the base functionality
     */
    public override void TakeDamage(Vector3 position, Vector3 force, int damage, Rigidbody bodyPart,
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

        float hitStrength = force.magnitude;

        if (_boneControlType == AIBoneControlType.Ragdoll)
        {
            // If the NPC is already ragdolled

            if (bodyPart != null)
            {
                if (hitStrength > 1.0f)
                    bodyPart.AddForce(force, ForceMode.Impulse);

                if (bodyPart.CompareTag("Head"))
                {
                    _health = Mathf.Max(_health - damage, 0);
                }
                else if (bodyPart.CompareTag("Upper Body"))
                {
                    _upperBodyDamage += damage;
                }
                else if (bodyPart.CompareTag("Lower Body"))
                {
                    _lowerBodyDamage += damage;
                }

                UpdateAnimatorDamage();

                // If health is > 0 then reanimate the ragdoll
                if (_health > 0)
                {
                    // Make sure we do not start the coroutine to reanimate if it is already processing. If it is already processing then stop first and then start
                    if (_reanimationCoroutine != null)
                        StopCoroutine(_reanimationCoroutine);

                    _reanimationCoroutine = Reanimate();
                    StartCoroutine(_reanimationCoroutine);
                }
            }

            return;
        }

        // Get local space position of attacker
        Vector3 attackerLocPos = transform.InverseTransformPoint(characterManager.transform.position);

        // Get local space position of hit
        Vector3 hitLocPos = transform.InverseTransformPoint(position);

        // The NPC should ragdoll if the hit strength is more than 1 i.e. a non melee weapon
        bool shouldRagdoll = (hitStrength > 1.0f);

        if (bodyPart != null)
        {

            if (bodyPart.CompareTag("Head"))
            {
                _health = Mathf.Max(_health - damage, 0);

                // Ragdoll the NPC if its health goes to 0
                if (_health == 0) shouldRagdoll = true;
            }
            else if (bodyPart.CompareTag("Upper Body"))
            {
                _upperBodyDamage += damage;
                UpdateAnimatorDamage();
            }
            else if (bodyPart.CompareTag("Lower Body"))
            {
                _lowerBodyDamage += damage;
                UpdateAnimatorDamage();

                // The NPC should ragdoll if it is hit at its lower body
                shouldRagdoll = true;
            }
        }

        // NPC should also ragdoll if is in the reanimation phase or if it is crawling or if it is feeding i.e. cinematic enabled or if it is attacked from behind
        if (_boneControlType != AIBoneControlType.Animated || isCrawling || cinematicEnabled || attackerLocPos.z < 0)
            shouldRagdoll = true;

        if (!shouldRagdoll)
        {
            // If the hit does not result in ragdolling then identify the direction of the hit to play the appropriate animation

            float angle = 0.0f;
            if (hitDirection == 0)
            {
                Vector3 vecToHit = (position - transform.position).normalized;
                angle = AIState.FindSignedAngle(vecToHit, transform.forward);
            }

            int hitType = 0;
            if (bodyPart.gameObject.CompareTag("Head"))
            {
                if (angle < -10 || hitDirection == -1) hitType = 1;
                else if (angle > 10 || hitDirection == 1) hitType = 3;
                else hitType = 2;
            }
            else if (bodyPart.gameObject.CompareTag("Upper Body"))
            {
                if (angle < -20 || hitDirection == -1) hitType = 4;
                else if (angle > 20 || hitDirection == 1) hitType = 6;
                else hitType = 5;
            }

            if (_animator)
            {
                _animator.SetInteger(_hitTypeHash, hitType);
                _animator.SetTrigger(_hitTriggerHash);
            }

            return; 
        }
        else
        {
            // ragdoll the NPC

            if (_currentState)
            {
                _currentState.OnExitState();
                _currentState = null;
                _currentStateType = AIStateType.None;
            }

            if (_navAgent) _navAgent.enabled = false;
            if (_animator) _animator.enabled = false;
            if (_collider) _collider.enabled = false;

            inMeleeRange = false;

            foreach (Rigidbody body in _bodyParts)
            {
                if (body != null)
                {
                    body.isKinematic = false;
                }
            }

            if (hitStrength > 1.0f)
            {
                bodyPart.AddForce(force, ForceMode.Impulse);
            }

            _boneControlType = AIBoneControlType.Ragdoll;

            if (_health > 0)
            {
                if (_reanimationCoroutine != null)
                    StopCoroutine(_reanimationCoroutine);

                _reanimationCoroutine = Reanimate();
                StartCoroutine(_reanimationCoroutine);
            }
        }
    }

    /*
     * Coroutine : 
     * Description : 
     */
    protected IEnumerator Reanimate()
    {
        if (_boneControlType != AIBoneControlType.Ragdoll || _animator== null) yield break;

        yield return new WaitForSeconds(_reanimationWaitTime);

        _ragdollEndTime = Time.time;

        foreach (Rigidbody body in _bodyParts)
        {
            body.isKinematic = true;
        }

        _boneControlType = AIBoneControlType.RagdollToAnim;

        foreach (BodyPartSnapshot snapShot in _bodyPartSnapshots)
        {
            snapShot.position = snapShot.transform.position;
            snapShot.rotation = snapShot.transform.rotation;
        }

        _ragdollHeadPosition = _animator.GetBoneTransform(HumanBodyBones.Head).transform.position;
        _ragdollFeetPosition = (_animator.GetBoneTransform(HumanBodyBones.LeftFoot).transform.position +
                                _animator.GetBoneTransform(HumanBodyBones.RightFoot).transform.position) * 0.5f;
        _ragdollHipPosition = _rootBone.position;

        _animator.enabled = true;

        if (_rootBone != null)
        {
            float forwardTest;

            switch (_RootBoneAlignmentType)
            {
                case AIBoneAlignmentType.ZAxis:
                    forwardTest = _rootBone.forward.y;
                    break;
                case AIBoneAlignmentType.ZAxisInverted:
                    forwardTest = -_rootBone.forward.y;
                    break;
                case AIBoneAlignmentType.YAxis:
                    forwardTest = _rootBone.up.y;
                    break;
                case AIBoneAlignmentType.YAxisInverted:
                    forwardTest = -_rootBone.up.y;
                    break;
                case AIBoneAlignmentType.XAxis:
                    forwardTest = _rootBone.right.y;
                    break;
                case AIBoneAlignmentType.XAxisInverted:
                    forwardTest = -_rootBone.right.y;
                    break;
                default:
                    forwardTest = _rootBone.forward.y;
                    break;
            }

            if (forwardTest >= 0)
                _animator.SetTrigger(_reanimateFromBackHash);
            else
                _animator.SetTrigger(_reanimateFromFrontHash);
        }
    }

    protected virtual void LateUpdate()
    {
        if (_boneControlType == AIBoneControlType.RagdollToAnim)
        {
            if (Time.time <= _ragdollEndTime + _mecanimTransitionTime)
            {
                Vector3 animatedToRagdoll = _ragdollHipPosition - _rootBone.position;
                Vector3 newRootPosition = transform.position + animatedToRagdoll;

                float y = (newRootPosition + (Vector3.up * 0.25f)).y;
                Debug.Log(y);
                RaycastHit[] hits = Physics.RaycastAll(newRootPosition + (Vector3.up * 0.25f), Vector3.down, float.MaxValue, _geometryLayers);
                newRootPosition.y = float.MinValue;
                foreach (RaycastHit hit in hits)
                {
                    if (!hit.transform.IsChildOf(transform))
                    {
                        newRootPosition.y = Mathf.Max(hit.point.y, newRootPosition.y);
                    }
                }
                Debug.Log("Check floor below");
                Debug.Log(newRootPosition.y);

                UnityEngine.AI.NavMeshHit navMeshHit;
                Vector3 baseOffset = Vector3.zero;
                if (_navAgent) baseOffset.y = _navAgent.baseOffset;
                if (UnityEngine.AI.NavMesh.SamplePosition(newRootPosition, out navMeshHit, 25.0f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    transform.position = navMeshHit.position + baseOffset;
                }
                else
                {
                    transform.position = newRootPosition + baseOffset;
                }

                Vector3 ragdollDirection = _ragdollHeadPosition - _ragdollFeetPosition;
                ragdollDirection.y = 0.0f;

                Vector3 meanFeetPosition = (_animator.GetBoneTransform(HumanBodyBones.LeftFoot).transform.position +
                                _animator.GetBoneTransform(HumanBodyBones.RightFoot).transform.position) * 0.5f;
                Vector3 animatedDirection = _animator.GetBoneTransform(HumanBodyBones.Head).transform.position -
                                            meanFeetPosition;
                animatedDirection.y = 0.0f;

                transform.rotation *= Quaternion.FromToRotation(animatedDirection.normalized, ragdollDirection.normalized);
            }

            float blendAmount = Mathf.Clamp01((Time.time - _ragdollEndTime - _mecanimTransitionTime) / _reanimationBlendTime);

            foreach (BodyPartSnapshot snapshot in _bodyPartSnapshots)
            {
                if (snapshot.transform == _rootBone)
                {
                    snapshot.transform.position = Vector3.Lerp(snapshot.position, snapshot.transform.position,
                        blendAmount);
                }

                snapshot.transform.rotation = Quaternion.Slerp(snapshot.rotation,
                    snapshot.transform.rotation, blendAmount);

                // Exit reanimation
                if (blendAmount == 1)
                {
                    _boneControlType = AIBoneControlType.Animated;
                    if (_navAgent) _navAgent.enabled = true;
                    if (_collider) _collider.enabled = true;

                    AIState newState = null;
                    if (_states.TryGetValue(AIStateType.Alerted, out newState))
                    {
                        if (_currentState != null) _currentState.OnExitState();
                        newState.OnEnterState();
                        _currentState = newState;
                        _currentStateType = AIStateType.Alerted;
                    }
                }
            }
        }
    }

}
