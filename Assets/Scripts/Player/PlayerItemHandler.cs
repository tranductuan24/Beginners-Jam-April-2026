using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Quản lý việc cầm và thả vật phẩm của người chơi.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class PlayerItemHandler : MonoBehaviour
{
    [Header("Cấu hình Cầm")]
    [SerializeField] private Transform holdPoint;
    [SerializeField] private float dropForwardForce = 5f;
    [SerializeField] private float dropUpwardForce = 2f;

    [Header("Âm thanh")]
    [SerializeField] private AudioClip pickupSound;
    private AudioSource audioSource;

    [Header("Input")]
    [SerializeField] private string dropActionName = "Drop";
    [SerializeField] private KeyCode backupDropKey = KeyCode.G; // Dự phòng nếu input action chưa được tạo

    private PickupInteractable currentItem;
    private PlayerInput playerInput;
    private InputAction dropAction;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Tìm hành động Drop trong Input System
        dropAction = playerInput.actions.FindAction(dropActionName);

        if (holdPoint == null)
        {
            // Tự tìm camera nếu chưa gán holdPoint
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) holdPoint = cam.transform;
            else Debug.LogWarning("[PlayerItemHandler] Chưa gán Hold Point và không tìm thấy Camera!");
        }
    }

    private void OnEnable()
    {
        if (dropAction != null) dropAction.performed += OnDropAction;
    }

    private void OnDisable()
    {
        if (dropAction != null) dropAction.performed -= OnDropAction;
    }

    private void Update()
    {
        // Sử dụng hệ thống Input mới cho phím dự phòng G để tránh lỗi Player Settings
        if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame && currentItem != null)
        {
            DropItem();
        }
    }

    private void OnDropAction(InputAction.CallbackContext context)
    {
        if (currentItem != null) DropItem();
    }

    /// <summary>
    /// Nhặt một vật phẩm, tự động thả vật đang cầm nếu có.
    /// </summary>
    public void PickupItem(PickupInteractable item)
    {
        // 1. Nếu đang cầm thứ khác, thả nó ra trước
        if (currentItem != null)
        {
            DropItem();
        }

        // 2. Gán vật phẩm mới
        currentItem = item;

        // 3. Tắt vật lý (OnPickedUp) và triệt tiêu mọi lực để không bị giật trong Unity 6
        currentItem.OnPickedUp();

        // 4. Gán cha với worldPositionStays = false để snap ngay lập tức vào vị trí Hold Point
        currentItem.transform.SetParent(holdPoint, false);
        
        // 5. Đồng bộ hóa Transform ngay lập tức
        Physics.SyncTransforms();

        // 6. Gán vị trí và xoay (đã gán cha nên localPosition = Vector3.zero sẽ là ở đúng HoldPoint)
        currentItem.transform.localPosition = currentItem.GetHoldOffset();
        currentItem.transform.localRotation = Quaternion.Euler(currentItem.GetHoldRotation());
        
        // Phát âm thanh nhặt đồ
        if (pickupSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(pickupSound);
        }

        Debug.Log($"[PlayerItemHandler] Đã nhặt & đổi: {item.name}");
    }

    /// <summary>
    /// Thả vật phẩm rơi tự do tại chỗ (không ném).
    /// </summary>
    public void DropItem()
    {
        if (currentItem == null) return;

        PickupInteractable itemToDrop = currentItem;
        currentItem = null;

        // 1. Tách khỏi người chơi
        itemToDrop.transform.SetParent(null);
        
        // 2. Dịch chuyển vật thể ra trước một chút (0.2m) để tránh kẹt collider, nhưng vẫn giữ cảm giác rơi tại chỗ
        itemToDrop.transform.position = holdPoint.position + holdPoint.forward * 0.2f;
        
        // 3. Bật lại vật lý để vật phẩm rơi tự nhiên
        itemToDrop.OnDropped();

        // 4. Reset vận tốc để vật phẩm rơi thẳng xuống
        Rigidbody rb = itemToDrop.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log($"[PlayerItemHandler] Đã thả vật phẩm tại chỗ: {itemToDrop.name}");
    }

    public bool IsHoldingItem() => currentItem != null;
}
