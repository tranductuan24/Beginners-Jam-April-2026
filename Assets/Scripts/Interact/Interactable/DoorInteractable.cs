using UnityEngine;

/// <summary>
/// Cho phép cửa có thể mở/đóng khi tương tác.
/// </summary>
public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Cấu hình Cửa")]
    [SerializeField] private bool isOpen = false;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float speed = 2f;

    private Quaternion openRotation;
    private Quaternion closeRotation;

    private void Start()
    {
        closeRotation = transform.localRotation;
        openRotation = closeRotation * Quaternion.Euler(0, openAngle, 0);
    }

    private void Update()
    {
        // Sử dụng RotateTowards để việc đóng/mở mượt mà và chính xác hơn Slerp
        Quaternion targetRot = isOpen ? openRotation : closeRotation;
        transform.localRotation = Quaternion.RotateTowards(transform.localRotation, targetRot, speed * 100f * Time.deltaTime);
    }

    /// <summary>
    /// Xử lý khi người chơi nhấn nút tương tác.
    /// </summary>
    public void Interact(GameObject interactor)
    {
        isOpen = !isOpen;
        Debug.Log($"[DoorInteractable] {interactor.name} đã {(isOpen ? "mở" : "đóng")} cửa: {gameObject.name}");
    }

    /// <summary>
    /// Lời dẫn khi nhìn vào cửa.
    /// </summary>
    public string GetInteractPrompt()
    {
        return isOpen ? "Nhấn E để đóng cửa" : "Nhấn E để mở cửa";
    }
}