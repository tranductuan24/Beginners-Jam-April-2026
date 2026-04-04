using UnityEngine;

/// <summary>
/// Interface cho các đối tượng có thể tương tác.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Gọi khi người chơi nhấn nút tương tác.
    /// </summary>
    /// <param name="interactor">GameObject của người tương tác</param>
    void Interact(GameObject interactor);

    /// <summary>
    /// Hiển thị thông báo khi nhìn vào object (tuỳ chọn).
    /// </summary>
    string GetInteractPrompt();
}