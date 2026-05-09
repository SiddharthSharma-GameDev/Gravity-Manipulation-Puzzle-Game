using UnityEngine;

public class HologramDirectionPreview : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject hologramObject;

    [Header("Preview Position")]
    [SerializeField] private float previewDistance = 1.25f;
    [SerializeField] private float playerCenterHeight = 0.9f;
    [SerializeField] private float hologramCenterOffset = 0.9f;
    [SerializeField] private float wallPadding = 0.35f;
    [SerializeField] private float minimumSpaceNeeded = 1.1f;
    [SerializeField] private LayerMask wallLayer;

    [Header("Preview Smoothing")]
    [SerializeField] private float positionSmoothness = 18f;
    [SerializeField] private float rotationSmoothness = 18f;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool previewVisible;

    private void Start()
    {
        HidePreview();
    }

    private void Update()
    {
        if (!previewVisible || hologramObject == null)
            return;

        hologramObject.transform.position = Vector3.Lerp(
            hologramObject.transform.position,
            targetPosition,
            1f - Mathf.Exp(-positionSmoothness * Time.deltaTime)
        );

        hologramObject.transform.rotation = Quaternion.Slerp(
            hologramObject.transform.rotation,
            targetRotation,
            1f - Mathf.Exp(-rotationSmoothness * Time.deltaTime)
        );
    }

    public void ShowPreview(Vector3 gravityDirection)
    {
        if (player == null || hologramObject == null)
            return;

        gravityDirection = gravityDirection.normalized;

        Vector3 playerUp = player.up;
        Vector3 playerCenter = player.position + playerUp * playerCenterHeight;
        Vector3 previewUp = -gravityDirection;

        Vector3 placementDirection = GetSafePlacementDirection(playerCenter, gravityDirection);
        Vector3 anchorPoint = playerCenter + placementDirection * previewDistance;

        if (Physics.Raycast(playerCenter, placementDirection, out RaycastHit hit, previewDistance + wallPadding, wallLayer))
        {
            anchorPoint = hit.point - placementDirection * wallPadding;
        }

        Vector3 previewForward = Vector3.ProjectOnPlane(player.forward, previewUp).normalized;

        if (previewForward.sqrMagnitude < 0.01f)
            previewForward = Vector3.ProjectOnPlane(player.right, previewUp).normalized;

        if (previewForward.sqrMagnitude < 0.01f)
            previewForward = Vector3.forward;

        Quaternion previewRotation = Quaternion.LookRotation(previewForward, previewUp);

        Vector3 pivotPosition = anchorPoint - previewUp * hologramCenterOffset;

        targetPosition = pivotPosition;
        targetRotation = previewRotation;

        hologramObject.SetActive(true);
        previewVisible = true;

        hologramObject.transform.position = targetPosition;
        hologramObject.transform.rotation = targetRotation;
    }

    private Vector3 GetSafePlacementDirection(Vector3 playerCenter, Vector3 selectedDirection)
    {
        bool selectedSideBlocked = Physics.Raycast(
            playerCenter,
            selectedDirection,
            out RaycastHit selectedHit,
            minimumSpaceNeeded,
            wallLayer
        );

        if (!selectedSideBlocked)
            return selectedDirection;

        Vector3 oppositeDirection = -selectedDirection;

        bool oppositeSideBlocked = Physics.Raycast(
            playerCenter,
            oppositeDirection,
            out RaycastHit oppositeHit,
            minimumSpaceNeeded,
            wallLayer
        );

        if (!oppositeSideBlocked)
            return oppositeDirection;

        if (selectedHit.distance >= oppositeHit.distance)
            return selectedDirection;

        return oppositeDirection;
    }

    public void HidePreview()
    {
        previewVisible = false;

        if (hologramObject != null)
            hologramObject.SetActive(false);
    }
}