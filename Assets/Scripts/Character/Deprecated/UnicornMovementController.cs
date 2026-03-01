using UnityEngine;
[RequireComponent(typeof(Animator), typeof(Rigidbody), typeof(Collider))]
public class UnicornMovementController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float boostMultiplier = 2f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Fall / Hit")]
    [SerializeField] private float hitRecoverLockTime = 1.25f;
    [SerializeField] private string groundTag = "ground";

    private Animator anim;
    private Rigidbody rb;

    private bool isFallen = false;
    private float fallLockTimer = 0f;

    private float inputX;   // A / D
    private float inputZ;   // W only
    private bool boostHeld;

    void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        // Do not accept movement input while in fall/recovery lock
        if (isFallen)
        {
            inputX = 0f;
            inputZ = 0f;
            boostHeld = false;

            fallLockTimer -= Time.deltaTime;
            if (fallLockTimer <= 0f)
            {
                isFallen = false;
            }

            UpdateAnimatorParameters(Vector3.zero, false);
            return;
        }

        // W -> forward
        inputZ = Input.GetKey(KeyCode.W) ? 1f : 0f;

        // A / D control direction
        inputX = 0f;
        if (Input.GetKey(KeyCode.A)) inputX -= 1f;
        if (Input.GetKey(KeyCode.D)) inputX += 1f;

        boostHeld = Input.GetKey(KeyCode.Space);

        Vector3 moveDir = GetMoveDirection(inputX, inputZ);
        bool isMoving = moveDir.sqrMagnitude > 0.0001f;

        UpdateAnimatorParameters(moveDir, isMoving);
    }

    void FixedUpdate()
    {
        if (isFallen)
            return;

        Vector3 moveDir = GetMoveDirection(inputX, inputZ);
        bool isMoving = moveDir.sqrMagnitude > 0.0001f;

        if (!isMoving)
            return;

        // Rotate unicorn to face movement direction
        Quaternion targetRotation = Quaternion.LookRotation(moveDir, Vector3.up);
        Quaternion newRotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(newRotation);

        float currentSpeed = moveSpeed;
        if (boostHeld)
            currentSpeed *= boostMultiplier;

        Vector3 newPosition = rb.position + moveDir * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }

    private Vector3 GetMoveDirection(float x, float z)
    {
        // No backward movement: z is only 0 or 1 from W
        // A / D shifts direction left/right while still moving "forward"
        Vector3 dir = new Vector3(x, 0f, z);

        if (dir.sqrMagnitude > 1f)
            dir.Normalize();

        return dir;
    }

    private void UpdateAnimatorParameters(Vector3 moveDir, bool isMoving)
    {
        anim.SetBool("isMoving", isMoving);
        anim.SetBool("isBoosted", isMoving && boostHeld);
    }

    private void TriggerFall()
    {
        if (isFallen)
            return;

        isFallen = true;
        fallLockTimer = hitRecoverLockTime;

        anim.SetBool("isMoving", false);
        anim.SetBool("isBoosted", false);

        anim.SetTrigger("fall");
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Ignore floor/ground collisions
        if (collision.gameObject.CompareTag(groundTag))
            return;

        // Only fall when colliding with another rigidbody object
        if (collision.rigidbody != null && collision.rigidbody != rb)
        {
            TriggerFall();
        }
    }
}