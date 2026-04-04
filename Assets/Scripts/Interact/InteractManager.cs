using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Quản lý việc dò tìm và gọi các phương thức tương tác của vật phẩm.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class InteractManager : MonoBehaviour
{
    [Header("Cấu hình Tương tác")]
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private float rayRadius = 0.2f;
    [SerializeField] private LayerMask interactableLayer = ~0;
    [SerializeField] private Transform raycastOrigin;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Cấu hình Input")]
    [SerializeField] private string interactActionName = "Interact";

    [Header("UI Giao diện")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private IInteractable currentInteractable;
    private GameObject currentInteractableObject;
    private PlayerInput playerInput;
    private InputAction interactAction;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        // Tự động tìm action "Interact" để không cần kéo thả trong Inspector
        interactAction = playerInput.actions.FindAction(interactActionName);

        if (raycastOrigin == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) raycastOrigin = cam.transform;
            else Debug.LogError("[InteractManager] Không tìm thấy Camera chính!");
        }

        if (promptPanel != null) promptPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.performed += OnInteractAction;
        }
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.performed -= OnInteractAction;
        }
    }

    private void Update()
    {
        CheckForInteractable();
    }

    /// <summary>
    /// Xử lý sự kiện nhấn nút tương tác.
    /// </summary>
    private void OnInteractAction(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (currentInteractable != null && currentInteractableObject != null)
        {
            if (showDebugLogs) Debug.Log($"[InteractManager] Đang tương tác với: {currentInteractableObject.name}");
            currentInteractable.Interact(gameObject);
        }
    }

    /// <summary>
    /// Bắn tia quét hình cầu để tìm vật phẩm trong tầm nhìn.
    /// </summary>
    private void CheckForInteractable()
    {
        Ray ray = new Ray(raycastOrigin.position, raycastOrigin.forward);
        
        if (Physics.SphereCast(ray, rayRadius, out RaycastHit hit, interactRange, interactableLayer, triggerInteraction))
        {
            GameObject hitObj = hit.collider.gameObject;
            IInteractable interactable = hitObj.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                if (currentInteractable != interactable)
                {
                    SetCurrentInteractable(interactable, hitObj);
                }
                return;
            }
        }

        if (currentInteractable != null)
        {
            ClearCurrentInteractable();
        }
    }

    private void SetCurrentInteractable(IInteractable interactable, GameObject obj)
    {
        currentInteractable = interactable;
        currentInteractableObject = obj;
        ShowPrompt(interactable.GetInteractPrompt());
        if (showDebugLogs) Debug.Log($"[InteractManager] Đã tìm thấy vật thể: {obj.name}");
    }

    private void ClearCurrentInteractable()
    {
        currentInteractable = null;
        currentInteractableObject = null;
        HidePrompt();
    }

    private void ShowPrompt(string message)
    {
        if (promptPanel != null && promptText != null)
        {
            promptText.text = message;
            promptPanel.SetActive(true);
        }
    }

    private void HidePrompt()
    {
        if (promptPanel != null)
            promptPanel.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        if (raycastOrigin != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 targetPoint = raycastOrigin.position + raycastOrigin.forward * interactRange;
            Gizmos.DrawRay(raycastOrigin.position, raycastOrigin.forward * interactRange);
            Gizmos.DrawWireSphere(targetPoint, rayRadius);
        }
    }
}