using System.Collections;
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

    // wait for the collider build
    private bool _active = false;
    void Start() => StartCoroutine(Active());
    private IEnumerator Active()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        _active = true;
    }

    void Update()
    {
        if (!_active) return;
        _deltaTime = (timeType == TimeType.deltaTime) ? Time.deltaTime : Time.unscaledDeltaTime;
        CollisionDetect();
        UpdateTimer();
        CheckJumpApex(); // affect run speed and gravity
        CalculateRun();
        SetGravity();
        CalculateJump(); // possibly override the velocity.y
        RestrictVelocity(); // don't go through ground/wall/ceiling
        Move(); // actually do the transform update
    }

    // update the velocity last frame
    void LateUpdate()
    {
        _lastVelocity = _velocity;
        _lastHitDown = _hitDown;
    }

#region Timer

    [System.Serializable]
    private struct Timer
    {
        public float JumpBuffer;
        public float LastOnGround;
    }
    private Timer timer;
    private void UpdateTimer()
    {
        timer.JumpBuffer   -= _deltaTime;
        timer.LastOnGround -= _deltaTime;
        if (Input.JumpDown)
            timer.JumpBuffer = jumpBufferTime;
        if (_hitDown)
            timer.LastOnGround = coyoteTime;
    }

#endregion

#region Detects

    [Header("Detects")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private CheckBox upRays, downRays, leftRays, rightRays;

    private bool _hitUp, _hitDown, _hitLeft, _hitRight;
    private bool _lastHitDown;

    [System.Serializable]
    private struct CornerRay
    {
        [SerializeField] private CheckBox outer, inner;
        public bool Detected { get; private set; }
        public void Detect(LayerMask layer) =>
            Detected = outer.Detect(layer) && !inner.Detect(layer);
    }

    [SerializeField] private CornerRay leftBottomRay, rightBottomRay;
    [SerializeField] private CornerRay bottomLeftRay, bottomRightRay;
    [SerializeField] private CornerRay leftTopRay, rightTopRay;
    [SerializeField] private CornerRay topLeftRay, topRightRay;

    private void CollisionDetect()
    {
        _hitUp    = upRays   .Detect(groundLayer);
        _hitDown  = downRays .Detect(groundLayer);
        _hitLeft  = leftRays .Detect(groundLayer);
        _hitRight = rightRays.Detect(groundLayer);

        leftBottomRay.Detect(groundLayer);
        rightBottomRay.Detect(groundLayer);
        bottomLeftRay.Detect(groundLayer);
        bottomRightRay.Detect(groundLayer);
        leftTopRay.Detect(groundLayer);
        rightTopRay.Detect(groundLayer);
        topLeftRay.Detect(groundLayer);
        topRightRay.Detect(groundLayer);

        if (_hitDown)
        {
            _jumpCutting = false;
            if (!_lastHitDown)
                _jumping = false;
        }
    }

#endregion

#region Run

    [Header("Run")]
    [SerializeField] private float maxRunSpeed = 13f;
    [SerializeField] private float runAcceleration = 90f, runDecceleration = 60f;
    [SerializeField] private float jumpApexBonusMoveSpeed = 2f;

    private void CalculateRun()
    {
        float rawH = Input.RawH;

        float v;
        if (rawH != 0)
        {
            // v = v_0 + a * t
            v = _lastVelocity.x + rawH * runAcceleration * _deltaTime;
            v = Mathf.Clamp(v, -maxRunSpeed, maxRunSpeed);
            // bonus speed at jump apex
            if (_atJumpApex)
                v += rawH * jumpApexBonusMoveSpeed;
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
        if (_atJumpApex)
            scale *= jumpApexGravityMult;
        else if (_jumpCutting)
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
    [SerializeField] private float coyoteTime = 0.1f;
    private bool _jumping = false;
    [SerializeField] private float jumpApexSpeedThreshold = 0.5f;
    [SerializeField] private float jumpApexGravityMult = 0.5f;
    private bool _atJumpApex = false;

    private void CheckJumpApex() =>
        _atJumpApex = _jumping && Mathf.Abs(_lastVelocity.y) <= jumpApexSpeedThreshold;

    private void CalculateJump()
    {
        float v = _velocity.y;

        if ((!_hitUp || leftTopRay.Detected || rightTopRay.Detected) &&
            ((_hitDown && timer.JumpBuffer > 0) ||
            (Input.JumpDown && !_jumping && timer.LastOnGround > 0)))
        {
            v = jumpSpeed;
            _jumping = true;
            _jumpCutting = false;
        }

        if (!_hitDown && Input.JumpUp && !_jumpCutting && _jumping)
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

    [SerializeField] private float cornerCorrectDisplacement = 0.1f;

    // we cast our bounds before moving to avoid future collisions
    private void Move()
    {
        /* corner corrections */
        // if hit head on corner -> push forward a little bit
        if (leftTopRay.Detected && Input.RawH >= 0 && _velocity.y > 0)
            transform.position += cornerCorrectDisplacement * Vector3.right;
        if (rightTopRay.Detected && Input.RawH <= 0 && _velocity.y > 0)
            transform.position += cornerCorrectDisplacement * Vector3.left;

        // calculate the current position and the furthest point we can move if no collision
        Vector2 currentPos = transform.position + bound.center;
        Vector2 movement = _velocity * _deltaTime;
        Vector2 furthestPoint = currentPos + movement;

        // check furthest movement. If nothing will hit, just move and don't do extra checks
        if (!Physics2D.OverlapBox(furthestPoint, bound.size, 0, groundLayer))
        {
            transform.position = furthestPoint;
            return;
        }

        /* corner corrections */
        // almost land on platform -> push forward a little bit
        if (leftBottomRay.Detected && !_hitLeft && Input.RawH < 0)
            transform.position += cornerCorrectDisplacement * new Vector3(-1, 2, 0).normalized;
        if (rightBottomRay.Detected && !_hitRight  && Input.RawH > 0)
            transform.position += cornerCorrectDisplacement * new Vector3(1, 2, 0).normalized;
        // almost jumped onto the platform -> help player to jump on it
        if (bottomLeftRay.Detected && _velocity.y <= 0 && Input.RawH < 0)
            transform.position += cornerCorrectDisplacement * new Vector3(-19, 1, 0).normalized;
        if (bottomRightRay.Detected && _velocity.y <= 0 && Input.RawH > 0)
            transform.position += cornerCorrectDisplacement * new Vector3(19, 1, 0).normalized;
        // prevent stuck in corner
        if (topLeftRay.Detected || topRightRay.Detected)
            transform.position += cornerCorrectDisplacement * Vector3.down;

        // if have done some corner correction
        if (currentPos != (Vector2)(transform.position + bound.center))
        {
            // update current position, furthest point and recalculate
            currentPos = transform.position + bound.center;
            furthestPoint = currentPos + movement;
            if (!Physics2D.OverlapBox(furthestPoint, bound.size, 0, groundLayer))
            {
                transform.position = furthestPoint;
                return;
            }
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
        if ((_velocity.x > 0 && _hitRight) || (_velocity.x < 0 && _hitLeft))
            _velocity.x = 0;
        if ((_velocity.y > 0 && _hitUp && !leftTopRay.Detected && !rightTopRay.Detected) || (_velocity.y < 0 && _hitDown))
            _velocity.y = 0;
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