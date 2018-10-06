using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class StateClient : StateMachine
{
    Animator animator;
    public float moveSpeed = 3f;
    public float jumpForce = 7f;

    //public FireBall fireball = null;
    //public Transform skillStartPosition = null;

    private Rigidbody rigidbody = null;
    private Collider collider = null;

    public float hitDistance = 0.35f;
    public LayerMask groundLayers;

    public bool grounded = true;
    //private bool canAttack = true;
    private bool inputJump = false;
    private bool inputAttack = false;

    public float attackCoolDown = 2f;
    protected float attackCoolDownEnd = 0f;
    protected float attackCoolDownRemaining() { return Time.time >= attackCoolDownEnd ? 0 : attackCoolDownEnd - Time.time; }

    public float attackCastTime = 0f;
    protected float attackCastTimeEnd = 0f;
    protected float attackCastTimeRemaining() { return Time.time >= attackCastTimeEnd ? 0 : attackCastTimeEnd - Time.time; }

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        collider = GetComponent<Collider>();

        if (collider == null) Debug.LogError(name + "-- StateClient.cs -- Collider가 존재하지 않습니다.");
    }

    private bool EventCheckMove() { return IsMoving() == true; }
    private bool EventCheckMoveEnd() { return IsMoving() == false && strState == "MOVING"; }
    private bool EventCheckJumpRequest() { return IsGround() == true && inputJump == true; }
    private bool EventCheckIsJumping() { return IsGround() == false; }
    private bool EventCheckJumpFinished() { return IsGround() == true && strState == "JUMP"; }
    //private bool EventCheckAttackRequest() { return inputAttack == true && attackCoolDownRemaining() <= 0f; }
    //private bool EventCheckAttackCastingFinished() { return strState == "ATTACK" && attackCastTimeRemaining() <= 0f; }

    private bool IsMoving()
    {
        return (Input.GetKey(KeyCode.W)) ||
        (Input.GetKey(KeyCode.S)) ||
        (Input.GetKey(KeyCode.A)) ||
        (Input.GetKey(KeyCode.D));

    }

    private bool IsGround()
    {

        if (grounded) hitDistance = 0.35f;
        else hitDistance = 0.15f;

        if (Physics.Raycast(transform.position - new Vector3(0f, -0.85f, 0f), transform.up, hitDistance, groundLayers))
        {
            return (grounded = true);
        }
        else
        {
            return (grounded = false);
        }
    }

    private string UpdateIDLE()
    {

        if (EventCheckJumpRequest())
        {
            return "JUMP";
        }

        if (EventCheckIsJumping())
        {
            return "JUMP";
        }

        //if (EventCheckAttackRequest())
        //{
        //    attackCastTimeEnd = Time.time + attackCastTime;
        //    return "ATTACK";
        //}

        if (EventCheckMove())
        {
            return "MOVE";
        }

        if (EventCheckMoveEnd()) { }
        if (EventCheckJumpFinished()) { }
        //if (EventCheckAttackCastingFinished()) { }

        return "IDLE";
    }
    private string UpdateMOVE()
    {

        if (EventCheckJumpRequest())
        {
            return "JUMP";
        }

        if (EventCheckIsJumping())
        {
            return "JUMP";
        }

        //if (EventCheckAttackRequest())
        //{
        //    attackCastTimeEnd = Time.time + attackCastTime;
        //    return "ATTACK";
        //}

        if (EventCheckMoveEnd())
        {
            return "IDLE";
        }

        if (EventCheckMove()) { }
        if (EventCheckJumpFinished()) { }
        //if (EventCheckAttackCastingFinished()) { }

        return "MOVE";
    }
    private string UpdateJUMP()
    {
        if (EventCheckJumpFinished())
        {
            grounded = true;
            return "IDLE";
        }

        if (EventCheckJumpRequest()) { }
        if (EventCheckIsJumping()) { }
        //if (EventCheckAttackRequest()) { }
        if (EventCheckMove()) { }
        if (EventCheckMoveEnd()) { }
        //if (EventCheckAttackCastingFinished()) { }

        return "JUMP";
    }

    //private string UpdateATTACK()
    //{

    //    if (EventCheckAttackCastingFinished())
    //    {
    //        attackCoolDownEnd = Time.time + attackCoolDown;
    //        //CastAttack ();
    //        return "IDLE";
    //    }

    //    if (EventCheckJumpRequest()) { }
    //    if (EventCheckIsJumping()) { }
    //    //if (EventCheckAttackRequest()) { }
    //    if (EventCheckMove()) { }
    //    if (EventCheckMoveEnd()) { }
    //    if (EventCheckJumpFinished()) { }

    //    return "ATTACK";
    //}

    protected override string UpdateState()
    {
        if (strState == "IDLE") return UpdateIDLE();
        if (strState == "MOVE") return UpdateMOVE();
        if (strState == "JUMP") return UpdateJUMP();
        //if (strState == "ATTACK") return UpdateATTACK();

        Debug.LogError(name + " -- StateClient::UpdateState -- No state named: " + strState);
        return null;
    }

    protected override void UpdateHandle()
    {

        if (strState == "IDLE" || strState == "MOVE")
        {
            MoveHandling();
            JumpHandling();
            AttackHandling();
        }
        else if (strState == "JUMP")
        {
            MoveHandling();
        }
        else if (strState == "ATTACK")
        {

        }
        else
        {
            Debug.LogError(name + " -- StateClient::UpdateHandle -- No state named: " + strState);
        }
    }

    private void MoveHandling()
    {
        if (Input.GetKey(KeyCode.W)) transform.position += transform.forward * moveSpeed * Time.fixedDeltaTime;
        if (Input.GetKey(KeyCode.S)) transform.position += transform.forward * -1f * moveSpeed * Time.fixedDeltaTime;
        if (Input.GetKey(KeyCode.A)) transform.position += transform.right * -1f * moveSpeed * Time.fixedDeltaTime;
        if (Input.GetKey(KeyCode.D)) transform.position += transform.right * moveSpeed * Time.fixedDeltaTime;
    }

    private void JumpHandling()
    {
        inputJump = false;

        if (Input.GetKeyDown(KeyCode.Space) && rigidbody.velocity.y == 0)
        {
            rigidbody.AddForce(transform.up * jumpForce);
            inputJump = true;
        }
        
    }

    private void AttackHandling()
    {

        inputAttack = false;

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            inputAttack = true;
        }
    }

    //private void CastAttack()
    //{

    //    //GameObject go = (GameObject)Instantiate(fireball.gameObject);
    //    //go.transform.position = skillStartPosition.position;
    //    //go.transform.rotation = skillStartPosition.rotation;
    //    //go.GetComponent<FireBall>().moveDirection = transform.forward;
    //}
}
