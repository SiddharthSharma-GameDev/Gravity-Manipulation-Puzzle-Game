using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private PlayerGravityController gravityController;

    [Header("Camera Position")]
    [SerializeField] private float distance = 4f;
    [SerializeField] private float heightOffset = 1.5f;
    [SerializeField] private float followSharpness = 18f;

    [Header("Mouse Control")]
    [SerializeField] private float mouseSensitivity = 2.5f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 55f;

    [Header("Gravity Alignment")]
    [SerializeField] private float gravityAlignmentSharpness = 7f;
    [SerializeField] private float rotationSharpness = 18f;

    [Header("Camera Collision")]
    [SerializeField] private LayerMask collisionLayer;
    [SerializeField] private float collisionRadius = 0.25f;
    [SerializeField] private float collisionPadding = 0.2f;
    [SerializeField] private float minimumDistance = 1.2f;

    private float yaw;
    private float pitch = 15f;
    private Vector3 currentUp = Vector3.up;

    private void Start()
    {
        if (target != null)
        {
            if (gravityController == null)
                gravityController = target.GetComponent<PlayerGravityController>();

            currentUp = GetTargetUp();
            yaw = target.eulerAngles.y;
        }
    }

    private void Update()
    {
        if (IsGameplayBlocked())
            return;

        HandleMouseInput();
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        if (IsGameplayBlocked())
            return;

        UpdateGravityAlignment();
        UpdateCameraPositionAndRotation();
    }

    private bool IsGameplayBlocked()
    {
        if (GameManager.Instance == null)
            return false;

        if (!GameManager.Instance.IsGameplayActive)
            return true;

        if (GameManager.Instance.IsPaused)
            return true;

        return false;
    }

    private void HandleMouseInput()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private Vector3 GetTargetUp()
    {
        if (gravityController != null)
            return gravityController.CurrentUpDirection.normalized;

        if (target != null)
            return target.up.normalized;

        return Vector3.up;
    }

    private void UpdateGravityAlignment()
    {
        Vector3 targetUp = GetTargetUp();

        currentUp = Vector3.Slerp(
            currentUp,
            targetUp,
            1f - Mathf.Exp(-gravityAlignmentSharpness * Time.deltaTime)
        ).normalized;
    }

    private void UpdateCameraPositionAndRotation()
    {
        Quaternion gravityAlignment = Quaternion.FromToRotation(Vector3.up, currentUp);
        Quaternion orbitRotation = gravityAlignment * Quaternion.Euler(pitch, yaw, 0f);

        Vector3 focusPoint = target.position + currentUp * heightOffset;
        Vector3 desiredDirection = -(orbitRotation * Vector3.forward).normalized;
        Vector3 desiredPosition = focusPoint + desiredDirection * distance;

        Vector3 finalPosition = GetCollisionAdjustedPosition(focusPoint, desiredDirection);

        transform.position = Vector3.Lerp(
            transform.position,
            finalPosition,
            1f - Mathf.Exp(-followSharpness * Time.deltaTime)
        );

        Quaternion desiredRotation = Quaternion.LookRotation(focusPoint - transform.position, currentUp);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            1f - Mathf.Exp(-rotationSharpness * Time.deltaTime)
        );
    }

    private Vector3 GetCollisionAdjustedPosition(Vector3 focusPoint, Vector3 desiredDirection)
    {
        float targetDistance = distance;

        if (collisionLayer.value != 0)
        {
            if (Physics.SphereCast(
                focusPoint,
                collisionRadius,
                desiredDirection,
                out RaycastHit hit,
                distance,
                collisionLayer,
                QueryTriggerInteraction.Ignore))
            {
                targetDistance = Mathf.Max(hit.distance - collisionPadding, minimumDistance);
            }
        }

        return focusPoint + desiredDirection * targetDistance;
    }
}