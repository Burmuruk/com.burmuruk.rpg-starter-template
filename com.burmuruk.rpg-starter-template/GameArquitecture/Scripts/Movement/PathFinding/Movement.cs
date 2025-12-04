using System;
using UnityEngine;

namespace Burmuruk.WorldG.Patrol
{
    //public class Movement : MonoBehaviour
    //{
    //    [Header("Movement")]
    //    [SerializeField] float speed = 3;
    //    [SerializeField] float rotationVelocity = 2;
    //    [SerializeField] bool turnWhileMove = true;

    //    [SerializeField] float m_speed, m_maxVel, m_maxSteerForce;
    //    [SerializeField] float health = 100;

    //    [Header("Behaviour")]
    //    //[SerializeField] float visionDistance = 4;
    //    //[SerializeField] float visionAngle = 90;
    //    [SerializeField] float minDistance = .8f;
    //    [SerializeField] bool isLeader = false;



    //    public float wanderDisplacement, wanderRadious;
    //    bool isReceivingDamage = false;
    //    public Transform Target;
    //    public Transform m_Leader;
    //    public Vector3? wandernextPosition = null;

    //    public Action OnFinished;

    //    private bool canMove = false;
    //    private Vector3 destiny = default;
    //    private bool canTurn = false;
    //    private float turnAngle = 0;
    //    private float curTurnAngle = 0;
    //    private Transform finalTAngle = null;
    //    private Transform target = null;
    //    private Vector3 Target
    //    {
    //        get
    //        {
    //            return target.position - transform.position + Vector3.up * (transform.position.y - target.position.y);
    //        }

    //        set
    //        {
    //            if (Vector3.Distance(transform.position, ability) >= minDistance)
    //            {
    //                destiny = ability;
    //                canMove = true;
    //            }
    //            else
    //            {
    //                target = default;
    //                destiny = default;
    //            }
    //        }
    //    }

    //    Vector3 Destiny
    //    {
    //        get => destiny;

    //        set
    //        {
    //            if (Vector3.Distance(transform.position, ability) >= minDistance)
    //            {
    //                destiny = ability;
    //            }

    //            else
    //            {
    //                destiny = default;
    //            }
    //        }
    //    }
    //    public float Speed { get => m_speed; }
    //    public float MaxVel { get => m_maxVel; }
    //    public float MaxSteerForce { get => m_maxSteerForce; }
    //    public bool IsLeader { get => isLeader; }

    //    private void Awake()
    //    {
    //    }

    //    void StartAction()
    //    {

    //    }

    //    void Update()
    //    {
    //        if (canMove)
    //        {
    //            if (target && Target.Magnitude >= minDistance)
    //            {
    //                transform.Translate(Target * Time.deltaTime * speed, Space.World);

    //                if (turnWhileMove)
    //                {
    //                    var dir = target.position - transform.position;
    //                    var angle = Vector3.SignedAngle(dir, transform.forward, Vector3.up);

    //                    if (angle < -5 || angle > 5)
    //                        transform.Rotate(Vector3.up, Time.deltaTime * rotationVelocity * angle < 0 ? 1 : -1, Space.Self);
    //                }

    //            }
    //            else if (!target && Vector3.Distance(transform.position, Destiny) >= minDistance)
    //            {
    //                var newPosition = Destiny - transform.position + Vector3.up * (transform.position.y - destiny.y);
    //                transform.Translate(newPosition * Time.deltaTime * speed);
    //            }
    //            else
    //            {
    //                canMove = false;
    //                OnFinished.Invoke();
    //            }
    //        }

    //        if (canTurn)
    //        {
    //            if (!finalTAngle && curTurnAngle < Mathf.Abs(turnAngle))
    //            {
    //                transform.Rotate(Vector3.up, rotationVelocity * Time.deltaTime * (turnAngle < 0 ? -1 : 1));
    //                curTurnAngle += rotationVelocity * Time.deltaTime;
    //            }
    //            else if (finalTAngle && (transform.rotation.eulerAngles.y - finalTAngle.position.y) > 1)
    //            {
    //                transform.Rotate(Vector3.up, finalTAngle.position.y);
    //            }
    //            else
    //            {
    //                curTurnAngle = 0;
    //                canTurn = false;
    //                OnFinished.Invoke();
    //            }
    //        }
    //    }

    //    public void MoveTo(Vector3 direction) => Destiny = direction;

    //    public void MoveTo(Transform target)
    //    {
    //        if (!target)
    //        {
    //            OnFinished?.Invoke();
    //            return;
    //        }

    //        canMove = true;
    //        this.target = target;
    //    }

    //    public void CancelAction() => Destiny = default;

    //    public void TurnTo(Transform target)
    //    {
    //        if (!target)
    //        {
    //            OnFinished?.Invoke();
    //            return;
    //        }

    //        finalTAngle = target;
    //        canTurn = true;
    //    }

    //    public void TurnTo(float angle)
    //    {
    //        turnAngle = angle;
    //        canTurn = true;
    //    }
    //} 
}
