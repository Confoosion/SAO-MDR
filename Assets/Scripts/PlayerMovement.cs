using UnityEngine;
using System.Collections;

public enum Movement { Standing, Moving, Jumping, Dashing, Guarding, Parrying, Attacking }
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody2D playerRb;
    [SerializeField] private Movement currentAction;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float maxMoveDistance;

    [Header("Flick Settings")]
    [SerializeField] private float minFlickTime;

    [Header("Jump Settings")]
    [SerializeField] private float gravity;
    [SerializeField] private float jumpForce;
    [SerializeField] private float minUpwardAngle;
    private bool isJumping = false;
    private float startingJumpY;

    [Header("Dash Settings")]
    [SerializeField] private float dashForce;
    [SerializeField] private float dashTime;
    private bool isDashing = false;
    private Coroutine dashCoroutine;

    [SerializeField] private bool showDebugGizmos;

    private Vector2 touchStartPosition;
    private Vector2 currentTouchPosition;
    private bool isTouching = false;
    private int activeTouchId = -1;
    private float touchStartTime;

    private Vector2 moveDirection;
    private bool isGrounded = true;

    void Update()
    {
        if(Input.touchCount > 0)
        {
            for(int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                if(touch.phase == TouchPhase.Began && activeTouchId == -1)
                {
                    activeTouchId = touch.fingerId;
                    touchStartPosition = touch.position;
                    currentTouchPosition = touch.position;
                    touchStartTime = Time.time;
                    isTouching = true;
                }
                else if(touch.fingerId == activeTouchId)
                {
                    if(touch.phase == TouchPhase.Moved)
                    {
                        currentTouchPosition = touch.position;
                        if(!isJumping && !isDashing)
                            CalculateMovement();
                    }
                    else if(touch.phase == TouchPhase.Stationary)
                    {
                        
                    }
                    else if(touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        if(currentAction == Movement.Standing || currentAction == Movement.Moving || currentAction == Movement.Guarding)
                            DetectFlick();
                        else
                            currentAction = Movement.Standing;
                        isTouching = false;
                        activeTouchId = -1;
                        moveDirection = Vector2.zero;
                    }
                }
            }
        }

        // #if UNITY_EDITOR
        // if (Input.GetMouseButtonDown(0))
        // {
        //     touchStartPosition = Input.mousePosition;
        //     currentTouchPosition = Input.mousePosition;
        //     touchStartTime = Time.time;
        //     isTouching = true;
        // }
        // else if (Input.GetMouseButton(0) && isTouching)
        // {
        //     if(!isJumping)
        //     {
        //         currentTouchPosition = Input.mousePosition;
        //         CalculateMovement();
        //     }
        // }
        // else if (Input.GetMouseButtonUp(0))
        // {
        //     DetectFlick();
        //     isTouching = false;
        //     moveDirection = Vector2.zero;
        // }
        // #endif
    }

    void FixedUpdate()
    {
        if(moveDirection != Vector2.zero)
        {
            currentAction = Movement.Moving;
            playerRb.linearVelocity = moveDirection * moveSpeed;
        }
        else if(isJumping)
        {
            if(transform.position.y <= startingJumpY)
            {
                playerRb.gravityScale = 0f;
                transform.position = new Vector2(transform.position.x, startingJumpY);
                isJumping = false;
            }
        }
        else if(!isDashing)
        {
            playerRb.linearVelocity = Vector2.zero;
        }
    }

    void CalculateMovement()
    {
        Vector2 offset = currentTouchPosition - touchStartPosition;
        float distance = offset.magnitude;
        
        if (distance > 0)
        {
            moveDirection = offset.normalized;
            float speedMultiplier = Mathf.Clamp01(distance / maxMoveDistance);
            moveDirection *= speedMultiplier;
        }
        else
        {
            moveDirection = Vector2.zero;
        }
    }

    void DetectFlick()
    {
        float timeDelta = Time.time - touchStartTime;
        if(timeDelta <= 0) timeDelta = 0.001f;

        if(timeDelta > minFlickTime)
        {
            Debug.Log("Flick too slow");
            return;
        }

        Vector2 swipeVector = currentTouchPosition - touchStartPosition;
        float swipeSpeed = swipeVector.magnitude / timeDelta;

        float angle = Mathf.Atan2(swipeVector.y, swipeVector.x) * Mathf.Rad2Deg;
        bool isUpwardFlick = angle >= minUpwardAngle && angle <= (180f - minUpwardAngle);

        if(isUpwardFlick)
        {
            if(!isJumping)
            {
                Debug.Log("Jumping!");
                Jump(swipeVector.normalized);
            }
        }
        else
        {
            if(!isDashing)
            {
                Debug.Log("Dashing!");
                dashCoroutine = StartCoroutine(Dash(swipeVector.normalized));
            }
        }
    }

    void Jump(Vector2 flickDirection)
    {
        currentAction = Movement.Jumping;
        isJumping = true;
        startingJumpY = transform.position.y;

        // Debug.Log("FLICK DIRECTION: " + flickDirection);
        playerRb.gravityScale = gravity;
        playerRb.AddForce(new Vector2(flickDirection.x, 1f) * jumpForce, ForceMode2D.Impulse);
    }

    IEnumerator Dash(Vector2 flickDirection)
    {
        currentAction = Movement.Dashing;
        isDashing = true;

        playerRb.AddForce(flickDirection * dashForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(dashTime);

        dashCoroutine = null;
        isDashing = false;
    }

    void OnDrawGizmos()
    {
        // Draw touch debug
        if (showDebugGizmos && isTouching && Camera.main != null)
        {
            Vector3 startWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(touchStartPosition.x, touchStartPosition.y, 10));
            Vector3 currentWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(currentTouchPosition.x, currentTouchPosition.y, 10));
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startWorldPos, 0.3f);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(currentWorldPos, 0.2f);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(startWorldPos, currentWorldPos);
        }
    }
}