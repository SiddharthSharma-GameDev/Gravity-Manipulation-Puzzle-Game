using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerGravityController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float inputSmoothness = 14f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private Transform cameraTransform;

    [Header("Gravity")]
    [SerializeField] private float gravityStrength = 25f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.35f;
    [SerializeField] private float gravityRotationSpeed = 10f;

    [Header("Gravity Transition Polish")]
    [SerializeField] private float gravityHoldTime = 0.15f;
    [SerializeField] private float gravityTransitionDuration = 1.1f;
    [SerializeField] private float transitionRotationSpeed = 6f;

    [Header("Free Fall Game Over")]
    [SerializeField] private float freeFallGameOverTime = 4f;
    [SerializeField] private float startGraceTime = 3f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float animationDampTime = 0.08f;

    [Header("Gravity Air Animation")]
    [SerializeField] private string gravityAirStateName = "Falling Idle";
    [SerializeField] private float gravityAirCrossFade = 0.08f;

    [Header("Gravity Preview")]
    [SerializeField] private HologramDirectionPreview hologramPreview;
    [SerializeField] private GravityDirectionUI gravityDirectionUI;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip gravityChangeSound;
    [SerializeField] private float gravitySoundVolume = 0.8f;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;

    private Vector3 gravityDirection = Vector3.down;
    private Vector3 selectedGravityDirection = Vector3.down;

    private bool isGrounded;
    private bool touchedSurfaceThisFrame;
    private bool isGravityTransitioning;

    private float moveAmount;
    private float currentGravityMultiplier = 1f;
    private float smoothedHorizontal;
    private float smoothedVertical;

    private Quaternion targetGravityRotation;
    private float lastSurfaceContactTime;
    private float gameStartTime;

    private Coroutine gravityTransitionRoutine;

    public Vector3 CurrentUpDirection => -gravityDirection;
    public bool IsGravityTransitioning => isGravityTransitioning;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (animator == null)
            animator = GetComponent<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        gravityDirection = SnapToCardinalDirection(gravityDirection);
        selectedGravityDirection = gravityDirection;
        targetGravityRotation = transform.rotation;

        gameStartTime = Time.time;
        lastSurfaceContactTime = Time.time;
    }

    private void Update()
    {
        CheckGrounded();
        HandleJump();
        HandleGravitySelection();
        UpdateAnimations();
        RotateToGravity();
        CheckFreeFallGameOver();
    }

    private void FixedUpdate()
    {
        touchedSurfaceThisFrame = false;
        ApplyGravity();
        MovePlayer();
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!IsInGroundLayer(collision.gameObject.layer))
            return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 contactNormal = collision.GetContact(i).normal;

            if (Vector3.Dot(contactNormal, -gravityDirection) > 0.25f)
            {
                touchedSurfaceThisFrame = true;
                isGrounded = true;
                lastSurfaceContactTime = Time.time;
                return;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsInGroundLayer(collision.gameObject.layer))
            return;

        touchedSurfaceThisFrame = true;
        isGrounded = true;
        lastSurfaceContactTime = Time.time;
    }

    private void ApplyGravity()
    {
        rb.AddForce(gravityDirection * gravityStrength * currentGravityMultiplier, ForceMode.Acceleration);
    }

    private void MovePlayer()
    {
        if (isGravityTransitioning)
        {
            moveAmount = 0f;
            smoothedHorizontal = 0f;
            smoothedVertical = 0f;
            rb.velocity = Vector3.Project(rb.velocity, gravityDirection);
            return;
        }

        float targetHorizontal = 0f;
        float targetVertical = 0f;
        bool hasInput = false;

        if (Input.GetKey(KeyCode.W))
        {
            targetVertical += 1f;
            hasInput = true;
        }

        if (Input.GetKey(KeyCode.S))
        {
            targetVertical -= 1f;
            hasInput = true;
        }

        if (Input.GetKey(KeyCode.D))
        {
            targetHorizontal += 1f;
            hasInput = true;
        }

        if (Input.GetKey(KeyCode.A))
        {
            targetHorizontal -= 1f;
            hasInput = true;
        }

        if (!hasInput)
        {
            smoothedHorizontal = 0f;
            smoothedVertical = 0f;
        }
        else
        {
            smoothedHorizontal = Mathf.Lerp(smoothedHorizontal, targetHorizontal, 1f - Mathf.Exp(-inputSmoothness * Time.fixedDeltaTime));
            smoothedVertical = Mathf.Lerp(smoothedVertical, targetVertical, 1f - Mathf.Exp(-inputSmoothness * Time.fixedDeltaTime));
        }

        if (cameraTransform == null)
            return;

        Vector3 currentUp = -gravityDirection;

        Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, currentUp).normalized;
        Vector3 cameraRight = Vector3.ProjectOnPlane(cameraTransform.right, currentUp).normalized;

        if (cameraForward.sqrMagnitude < 0.01f)
            cameraForward = Vector3.ProjectOnPlane(transform.forward, currentUp).normalized;

        if (cameraRight.sqrMagnitude < 0.01f)
            cameraRight = Vector3.Cross(currentUp, cameraForward).normalized;

        Vector3 inputDirection = new Vector3(smoothedHorizontal, 0f, smoothedVertical);

        if (inputDirection.magnitude > 1f)
            inputDirection.Normalize();

        Vector3 moveDirection = (cameraForward * inputDirection.z + cameraRight * inputDirection.x).normalized;

        moveAmount = hasInput ? Mathf.Clamp01(inputDirection.magnitude) : 0f;

        Vector3 gravityVelocity = Vector3.Project(rb.velocity, gravityDirection);
        Vector3 movementVelocity = hasInput ? moveDirection * moveSpeed * moveAmount : Vector3.zero;

        rb.velocity = movementVelocity + gravityVelocity;

        if (hasInput && moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion movementRotation = Quaternion.LookRotation(moveDirection, currentUp);
            transform.rotation = Quaternion.Slerp(transform.rotation, movementRotation, 12f * Time.fixedDeltaTime);
            targetGravityRotation = transform.rotation;
        }
    }

    private void HandleJump()
    {
        if (isGravityTransitioning)
            return;

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(-gravityDirection * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    private void HandleGravitySelection()
    {
        if (isGravityTransitioning)
            return;

        Vector3 currentUp = -gravityDirection;

        Vector3 referenceForward = Vector3.ProjectOnPlane(transform.forward, currentUp).normalized;

        if (referenceForward.sqrMagnitude < 0.01f && cameraTransform != null)
            referenceForward = Vector3.ProjectOnPlane(cameraTransform.forward, currentUp).normalized;

        if (referenceForward.sqrMagnitude < 0.01f)
            referenceForward = GetFallbackForward(currentUp);

        Vector3 referenceRight = Vector3.Cross(currentUp, referenceForward).normalized;
        referenceForward = Vector3.Cross(referenceRight, currentUp).normalized;

        if (Input.GetKeyDown(KeyCode.UpArrow))
            SelectGravityDirection(currentUp, "↑");

        if (Input.GetKeyDown(KeyCode.DownArrow))
            SelectGravityDirection(-currentUp, "↓");

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            SelectGravityDirection(-referenceRight, "←");

        if (Input.GetKeyDown(KeyCode.RightArrow))
            SelectGravityDirection(referenceRight, "→");

        if (Input.GetKeyDown(KeyCode.Return))
            ApplySelectedGravity();
    }

    private void SelectGravityDirection(Vector3 direction, string symbol)
    {
        if (direction.sqrMagnitude < 0.01f)
            return;

        selectedGravityDirection = SnapToCardinalDirection(direction);

        if (hologramPreview != null)
            hologramPreview.ShowPreview(selectedGravityDirection);

        if (gravityDirectionUI != null)
            gravityDirectionUI.ShowSelectedDirection(symbol);
    }

    private void ApplySelectedGravity()
    {
        if (isGravityTransitioning)
            return;

        if (selectedGravityDirection == gravityDirection)
            return;

        if (gravityTransitionRoutine != null)
            StopCoroutine(gravityTransitionRoutine);

        gravityTransitionRoutine = StartCoroutine(GravityTransitionRoutine());
    }

    private IEnumerator GravityTransitionRoutine()
    {
        isGravityTransitioning = true;

        gravityDirection = SnapToCardinalDirection(selectedGravityDirection);

        Vector3 currentUp = -gravityDirection;

        Vector3 currentForward = Vector3.ProjectOnPlane(transform.forward, currentUp).normalized;

        if (currentForward.sqrMagnitude < 0.01f && cameraTransform != null)
            currentForward = Vector3.ProjectOnPlane(cameraTransform.forward, currentUp).normalized;

        if (currentForward.sqrMagnitude < 0.01f)
            currentForward = GetFallbackForward(currentUp);

        currentForward = Vector3.ProjectOnPlane(SnapToCardinalDirection(currentForward), currentUp).normalized;

        if (currentForward.sqrMagnitude < 0.01f)
            currentForward = GetFallbackForward(currentUp);

        targetGravityRotation = Quaternion.LookRotation(currentForward, currentUp);

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        currentGravityMultiplier = 0f;
        lastSurfaceContactTime = Time.time;
        isGrounded = false;
        moveAmount = 0f;
        smoothedHorizontal = 0f;
        smoothedVertical = 0f;

        PlayGravityChangeSound();
        PlayGravityAirAnimation();

        if (hologramPreview != null)
            hologramPreview.HidePreview();

        if (gravityDirectionUI != null)
            gravityDirectionUI.HideDirection();

        float holdTimer = 0f;

        while (holdTimer < gravityHoldTime)
        {
            holdTimer += Time.deltaTime;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            yield return null;
        }

        float timer = 0f;

        while (timer < gravityTransitionDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / gravityTransitionDuration);
            currentGravityMultiplier = Mathf.SmoothStep(0f, 1f, t);
            yield return null;
        }

        currentGravityMultiplier = 1f;
        lastSurfaceContactTime = Time.time;
        isGravityTransitioning = false;
        gravityTransitionRoutine = null;
    }

    private void PlayGravityChangeSound()
    {
        if (audioSource == null || gravityChangeSound == null)
            return;

        audioSource.PlayOneShot(gravityChangeSound, gravitySoundVolume);
    }

    private void PlayGravityAirAnimation()
    {
        if (animator == null)
            return;

        if (string.IsNullOrWhiteSpace(gravityAirStateName))
            return;

        animator.CrossFade(gravityAirStateName, gravityAirCrossFade);
    }

    private void RotateToGravity()
    {
        float rotationSpeed = isGravityTransitioning ? transitionRotationSpeed : gravityRotationSpeed;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetGravityRotation, rotationSpeed * Time.deltaTime);
    }

    private void CheckGrounded()
    {
        if (isGravityTransitioning)
        {
            isGrounded = false;
            return;
        }

        bool sphereGrounded = CheckGroundBySphere();

        if (sphereGrounded || touchedSurfaceThisFrame)
        {
            isGrounded = true;
            lastSurfaceContactTime = Time.time;
        }
        else
        {
            isGrounded = false;
        }
    }

    private bool CheckGroundBySphere()
    {
        if (capsuleCollider == null)
            return false;

        Vector3 center = transform.TransformPoint(capsuleCollider.center);

        float scaledHeight = capsuleCollider.height * Mathf.Abs(transform.lossyScale.y);
        float scaledRadius = capsuleCollider.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));

        float footDistance = Mathf.Max((scaledHeight * 0.5f) - scaledRadius, 0.1f);
        Vector3 footPoint = center + gravityDirection * footDistance;

        return Physics.CheckSphere(
            footPoint,
            groundCheckRadius,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );
    }

    private void CheckFreeFallGameOver()
    {
        if (isGravityTransitioning)
            return;

        if (Time.time - gameStartTime < startGraceTime)
            return;

        if (isGrounded)
            return;

        if (Time.time - lastSurfaceContactTime >= freeFallGameOverTime)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.GameOver();
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null)
            return;

        float animationSpeed = isGravityTransitioning ? 0f : moveAmount;
        bool animationGrounded = isGrounded && !isGravityTransitioning;

        animator.SetFloat("Speed", animationSpeed, animationDampTime, Time.deltaTime);
        animator.SetBool("IsGrounded", animationGrounded);
    }

    private bool IsInGroundLayer(int layer)
    {
        return (groundLayer.value & (1 << layer)) != 0;
    }

    private Vector3 SnapToCardinalDirection(Vector3 direction)
    {
        direction.Normalize();

        Vector3[] directions =
        {
            Vector3.up,
            Vector3.down,
            Vector3.left,
            Vector3.right,
            Vector3.forward,
            Vector3.back
        };

        Vector3 closestDirection = Vector3.down;
        float highestDot = -1f;

        for (int i = 0; i < directions.Length; i++)
        {
            float dot = Vector3.Dot(direction, directions[i]);

            if (dot > highestDot)
            {
                highestDot = dot;
                closestDirection = directions[i];
            }
        }

        return closestDirection;
    }

    private Vector3 GetFallbackForward(Vector3 currentUp)
    {
        Vector3 fallbackForward = Vector3.ProjectOnPlane(Vector3.forward, currentUp).normalized;

        if (fallbackForward.sqrMagnitude < 0.01f)
            fallbackForward = Vector3.ProjectOnPlane(Vector3.right, currentUp).normalized;

        if (fallbackForward.sqrMagnitude < 0.01f)
            fallbackForward = Vector3.Cross(currentUp, Vector3.right).normalized;

        return fallbackForward;
    }
}