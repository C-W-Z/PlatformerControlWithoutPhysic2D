using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private new Transform transform; // hide the default transform property
    [SerializeField] private Bounds bound;

    private Vector2 _velocity, _lastVelocity;

    void Update()
    {
        CollisionDetect();
        CalculateRun();
        SetGravity();
        CalculateJump(); // possibly override the velocity.y
        Move();
    }

    void LateUpdate()
    {
        _lastVelocity = _velocity;
    }

#region Detects

    [Header("Detects")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private CheckBox upRays, downRays, leftRays, rightRays;

    private bool hitUp, hitDown, hitLeft, hitRight;

    private void CollisionDetect()
    {
        hitUp    = upRays   .Detect(groundLayer);
        hitDown  = downRays .Detect(groundLayer);
        hitLeft  = leftRays .Detect(groundLayer);
        hitRight = rightRays.Detect(groundLayer);
    }

#endregion

#region Run

    [Header("Run")]
    [SerializeField] private float maxRunSpeed = 13f;
    [SerializeField] private float runAcceleration = 90f, runDecceleration = 60f;

    private void CalculateRun()
    {
        float rawH = Input.RawH;

        float v;
        if (rawH != 0)
        {
            // v = v_0 + a * t
            v = _lastVelocity.x + rawH * runAcceleration * Time.deltaTime;
            v = Mathf.Clamp(v, -maxRunSpeed, maxRunSpeed);
        }
        else
            v = Mathf.MoveTowards(_lastVelocity.x, 0, runDecceleration * Time.deltaTime);

        if (v > 0 && hitRight || v < 0 && hitLeft)
            v = 0;

        _velocity.x = v;
    }

#endregion

#region Gravity

    [Header("Gravity")]
    [SerializeField] private float gravity = 80f;
    private float gravityScale = 1;
    [SerializeField] private float maxFallSpeed = 40f;

    private void SetGravity()
    {
        // v = v_0 + a * t
        float v = _lastVelocity.y - gravity * gravityScale * Time.deltaTime;

        v = Mathf.Max(v, -maxFallSpeed);

        if (v < 0 && hitDown)
            v = 0;

        _velocity.y = v;
    }

#endregion

#region Jump

    [Header("Jump")]
    [SerializeField] private float jumpSpeed = 30f;

    private void CalculateJump()
    {
        float v = _velocity.y;

        if (Input.JumpDown && hitDown)
            v = jumpSpeed;

        if (v > 0 && hitUp)
            v = 0;

        _velocity.y = v;
    }

#endregion

#region Transform Move

    [Header("Transform Move")]
    [SerializeField, Tooltip("The max iterations of finding a closer point when the future position to move has other colliders.\nRaising this value increases collision accuracy at the cost of performance.")]
    private int maxCheckColliderCount = 10;

    // we cast our bounds before moving to avoid future collisions
    private void Move()
    {
        // calculate the current position and the furthest point we can move if no collision
        Vector2 currentPos = transform.position + bound.center;
        Vector2 movement = _velocity * Time.deltaTime;
        Vector2 furthestPoint = currentPos + movement;

        // check furthest movement. If nothing will hit, just move and don't do extra checks
        Collider2D hit = Physics2D.OverlapBox(furthestPoint, bound.size, 0, groundLayer);
        if (hit == null)
        {
            transform.position = furthestPoint;
            return;
        }

        // otherwise increment away from current pos, see what closest position we can move to
        Vector3 posCanMove = transform.position;
        // 0 is the current position, (maxCheckColliderCount+1) is the furthest point
        for (int i = 1; i <= maxCheckColliderCount; i++)
        {
            // increment to check 'maxCheckColliderCount' points between current and furthest point
            float t = (float)i / (maxCheckColliderCount + 1);
            Vector2 posToTry = Vector2.Lerp(currentPos, furthestPoint, t);

            if (Physics2D.OverlapBox(posToTry, bound.size, 0, groundLayer))
            {
                transform.position = posCanMove - bound.center;

                // we've landed on a corner or hit our head on a ledge. Nudge the player gently
                if (i == 1)
                {
                    if (_velocity.y < 0) _velocity.y = 0;
                    Vector3 dir = (Vector3)currentPos - hit.transform.position;
                    transform.position += dir.normalized * movement.magnitude;
                }

                return;
            }

            posCanMove = posToTry;
        }
    }

#endregion

#region Scene GUI

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + bound.center, bound.size);
    }

#endregion
}