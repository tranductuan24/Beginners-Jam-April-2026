using UnityEngine;

/// <summary>
/// Cho phép vật thể có thể nhặt lên.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PickupInteractable : MonoBehaviour, IInteractable
{
    [Header("Lời dẫn")]
    [SerializeField] private string interactPrompt = "Nhấn E để nhặt";

    [Header("Cấu hình Cầm (Offset)")]
    [SerializeField] private Vector3 holdOffset = Vector3.zero;
    [SerializeField] private Vector3 holdRotation = Vector3.zero;
    
    [Header("Cấu hình Model (Tùy chọn)")]
    [SerializeField] private GameObject meshObject; // Dùng để thay đổi model dễ dàng

    [Header("Cấu hình Vật lý (Physics)")]
    [SerializeField] private float massAmount = 1f; // Khối lượng
    [Range(0, 20)] [SerializeField] private float linearDampingAmount = 2f; // Chống trượt quá xa
    [Range(0, 40)] [SerializeField] private float angularDampingAmount = 10f; // Chống lăn lung tung
    [SerializeField] private float maxPopVelocity = 3f; // Giới hạn tốc độ bị bắn văng
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private float impactThreshold = 0.5f; // Vận tốc tối thiểu để phát âm thanh
    private AudioSource audioSource;

    private Rigidbody rb;
    private Collider[] colliders;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Cấu hình vật lý chuyên sâu để vật thể không bị văng (Unity 6)
        rb.mass = massAmount;
        rb.linearDamping = linearDampingAmount;
        rb.angularDamping = angularDampingAmount;
        rb.maxDepenetrationVelocity = maxPopVelocity; // CHỐT: Không cho phép bắn văng mạnh
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Tính toán va chạm liên tục
        
        colliders = GetComponentsInChildren<Collider>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        
        if (meshObject == null)
        {
            // Tự động tìm object con đầu tiên làm model nếu chưa gán
            if (transform.childCount > 0)
                meshObject = transform.GetChild(0).gameObject;
        }
    }

    /// <summary>
    /// Xử lý khi nhấn nút tương tác.
    /// </summary>
    public void Interact(GameObject interactor)
    {
        // Thử tìm PlayerItemHandler trên người tương tác (player)
        PlayerItemHandler itemHandler = interactor.GetComponent<PlayerItemHandler>();
        if (itemHandler != null)
        {
            itemHandler.PickupItem(this);
        }
        else
        {
            Debug.LogWarning($"[PickupInteractable] {interactor.name} không có PlayerItemHandler!");
        }
    }

    /// <summary>
    /// Lời dẫn khi nhìn vào vật phẩm.
    /// </summary>
    public string GetInteractPrompt() => interactPrompt;

    public Vector3 GetHoldOffset() => holdOffset;
    public Vector3 GetHoldRotation() => holdRotation;

    /// <summary>
    /// Gọi khi được nhặt lên. Tắt vật lý và va chạm.
    /// </summary>
    public void OnPickedUp()
    {
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero; // Triệt tiêu vận tốc cho Unity 6
        rb.angularVelocity = Vector3.zero;
        rb.interpolation = RigidbodyInterpolation.None; // Tắt nội suy để không bị trễ vị trí
        
        foreach (var col in colliders)
        {
            col.enabled = false;
        }
    }

    /// <summary>
    /// Gọi khi được thả ra. Bật lại vật lý và va chạm.
    /// </summary>
    public void OnDropped()
    {
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate; // Bật lại để vật phẩm rơi mượt
        
        foreach (var col in colliders)
        {
            col.enabled = true;
        }
    }

    /// <summary>
    /// Phát âm thanh khi vật phẩm va chạm với mặt đất hoặc vật khác.
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        // Chỉ phát âm thanh nếu vận tốc va chạm đủ lớn và âm thanh đã được gán
        if (impactSound != null && audioSource != null && collision.relativeVelocity.magnitude > impactThreshold)
        {
            audioSource.PlayOneShot(impactSound);
        }
    }

    /// <summary>
    /// Hàm tiện ích để đổi model.
    /// </summary>
    public void ChangeModel(GameObject newModelPrefab)
    {
        if (meshObject != null) Destroy(meshObject);
        meshObject = Instantiate(newModelPrefab, transform);
        meshObject.transform.localPosition = Vector3.zero;
        meshObject.transform.localRotation = Quaternion.identity;
    }
}
