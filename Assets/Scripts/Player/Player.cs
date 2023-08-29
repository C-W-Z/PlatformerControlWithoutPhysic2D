using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private new Transform transform; // hide the default transform property
    [SerializeField] private Bounds bound;

    private enum TimeType { deltaTime, unscaledDeltaTime }
    [Header("Time Type")]
    [SerializeField] private TimeType timeType = TimeType.deltaTime;
    private float _deltaTime;

    private Vector2 _velocity, _lastVelocity;

    void Update()
    {
        _deltaTime = (timeType == TimeType.deltaTime) ? Time.deltaTime : Time.unscaledDeltaTime;
        UpdateTimer();
        CollisionDetect();
        CalculateRun();
        SetGravity();
        CalculateJump(); // possibly override the velocity.y
        RestrictVelocity(); // don't go through ground/wall/ceiling
        Move(); // actually do the transform update
    }

    // update the velocity last frame
    void LateUpdate() => _lastVelocity = _velocity;

#region Timer

    private struct Timer
    {
        public float JumpBuffer;
    }
    private Timer timer;
    private void UpdateTimer()
    {
        timer.JumpBuffer -= _deltaTime;
        if (Input.JumpDown)
            timer.JumpBuffer = jumpBufferTime;
    }

#endregion

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

        if (hitDown)
            _jumpCutting = false;
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
            v = _lastVelocity.x + rawH * runAcceleration * _deltaTime;
            v = Mathf.Clamp(v, -maxRunSpeed, maxRunSpeed);
        }
        else
            v = Mathf.MoveTowards(_lastVelocity.x, 0, runDecceleration * _deltaTime);

        _velocity.x = v;
    }

#endregion

#region Gravity

    [Header("Gravity")]
    [SerializeField] private float gravity = 80f;
    private const float gravityScale = 1;
    [SerializeField] private float maxFallSpeed = 40f;

    private void SetGravity()
    {
        float scale = gravityScale;
        if (_jumpCutting)
            scale *= jumpCutGravityMult;
        // v = v_0 + a * t
        float v = _lastVelocity.y - gravity * scale * _deltaTime;

        v = Mathf.Max(v, -maxFallSpeed);

        _velocity.y = v;
    }

#endregion

#region Jump

    [Header("Jump")]
    [SerializeField] private float jumpSpeed = 30f;
    [SerializeField] private float jumpCutSpeedMult = 0.5f;
    [SerializeField] private float jumpCutGravityMult = 2f;
    private bool _jumpCutting = false;
    [SerializeField] private float jumpBufferTime = 0.1f;

    private void CalculateJump()
    {
        float v = _velocity.y;

        if (timer.JumpBuffer > 0 && hitDown)
        {
            v = jumpSpeed;
            _jumpCutting = false;
        }

        if (!hitDown && Input.JumpUp && !_jumpCutting && v > 0)
        {
            _jumpCutting = true;
            v *= jumpCutSpeedMult;
        }

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
        Vector2 movement = _velocity * _deltaTime;
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
                // hit -> move to the position we check last time
                transform.position = posCanMove - bound.center;

                // we've landed on a corner or hit our head on a ledge. Nudge the player gently
                if (i == 1)
                {
                    if (_velocity.y < 0) _velocity.y = 0;
                    Vector3 dir = (Vector3)currentPos - hit.transform.position;
                    transform.position += dir.normalized * movement.magnitude;
                }

                // update the actual velocity (for last velocity)
                _velocity = ((Vector2)transform.position - currentPos + (Vector2)bound.center) / _deltaTime;

                return;
            }

            // no hit -> we can move to this position
            posCanMove = posToTry;
        }
    }

    private void RestrictVelocity()
    {
        if ((_velocity.x > 0 && hitRight) || (_velocity.x < 0 && hitLeft))
            _velocity.x = 0;
        if ((_velocity.y > 0 && hitUp) || (_velocity.y < 0 && hitDown))
            _velocity.y = 0;
    }

#endregion

#region Scene GUI

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + bound.center, bound.size);
    }

#endregion
}