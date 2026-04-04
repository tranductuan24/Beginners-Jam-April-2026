using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Cài đặt Di chuyển")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Kiểm tra Mặt đất (Ground Check)")]
    [SerializeField] private LayerMask groundMask = ~0; 
    [SerializeField] private float groundCheckRadius = 0.3f; // Bán kính khối cầu quét đất
    [SerializeField] private float groundCheckOffset = 0.1f; // Độ lệch từ chân nhân vật

    [Header("Tham chiếu Camera")]
    [SerializeField] private Transform cameraTransform;

    [Header("Tiếng bước chân (Footsteps)")]
    [SerializeField] private AudioSource footstepAudioSource;
    [SerializeField] private AudioClip footstepClip;
    [SerializeField] private float footstepInterval = 0.4f;
    [SerializeField] private float sprintFootstepInterval = 0.28f;
    [SerializeField] private float minPitch = 0.85f;
    [SerializeField] private float maxPitch = 1.15f;

    private Rigidbody rb;
    private Vector2 moveInputRaw;           
    private Vector3 moveInputDirection;     
    private bool isSprinting;
    
    [Header("Thông tin Debug (Chỉ đọc)")]
    [SerializeField] private bool isGrounded; 
    [SerializeField] private float currentVelocityMag; 
    private float footstepTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (cameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null) cameraTransform = mainCam.transform;
        }

        // Tự động gán AudioSource
        if (footstepAudioSource == null)
        {
            footstepAudioSource = GetComponent<AudioSource>();
            // Thiết lập mặc định cho AudioSource nếu cần
            footstepAudioSource.playOnAwake = false;
            footstepAudioSource.spatialBlend = 1f; // 3D sound
        }

        if (footstepClip == null)
        {
            Debug.LogError("<color=red>Lỗi:</color> Bạn chưa gán file âm thanh cho <b>Footstep Clip</b>!");
        }
    }

    private void Update()
    {
        HandleFootsteps();
    }

    private void FixedUpdate()
    {
        UpdateMovementDirection();
        CheckGround();
        ApplyGravity();
        MovePlayer();
    }

    // --- Ground Check dùng SphereCast (Ổn định hơn Raycast) ---
    private void CheckGround()
    {
        // Vị trí bắt đầu quét: hơi cao hơn chân một chút
        Vector3 origin = transform.position + Vector3.up * groundCheckOffset;
        
        // Quét một hình cầu nhỏ xuống dưới
        isGrounded = Physics.CheckSphere(origin, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
        
        // Vẽ màu đỏ nếu trên không, xanh nếu chạm đất (Chỉ thấy trong Scene View)
        Debug.DrawLine(origin, origin + Vector3.down * groundCheckRadius, isGrounded ? Color.green : Color.red);
    }

    // --- Logic Tiếng Bước Chân ---
    private void HandleFootsteps()
    {
        // Lấy vận tốc thực tế của nhân vật (bỏ qua trục Y để không kêu khi đang rơi thẳng đứng)
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        currentVelocityMag = horizontalVelocity.magnitude;

        // Điều kiện để phát tiếng: Đang chạm đất VÀ tốc độ di chuyển > 0.5
        if (!isGrounded || currentVelocityMag < 0.5f)
        {
            footstepTimer = 0f; // Reset để bước đầu tiên kêu ngay lập tức khi quay lại
            return;
        }

        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0f)
        {
            PlayFootstep();
            // Tốc độ càng nhanh (sprint) thì khoảng cách giữa các bước càng ngắn
            footstepTimer = isSprinting ? sprintFootstepInterval : footstepInterval;
        }
    }

    // Menu chuột phải trong Inspector để Test âm thanh
    [ContextMenu("Play Test Footstep")]
    public void PlayFootstep()
    {
        if (footstepAudioSource == null || footstepClip == null) 
        {
            Debug.LogWarning("Không thể phát âm thanh: Thiếu Clip hoặc AudioSource!");
            return;
        }

        // Tạo sự tự nhiên bằng cách đổi Pitch ngẫu nhiên
        footstepAudioSource.pitch = Random.Range(minPitch, maxPitch);
        
        // Đảm bảo Volume không bị tắt
        if (footstepAudioSource.volume <= 0) footstepAudioSource.volume = 1f;

        footstepAudioSource.PlayOneShot(footstepClip);
    }

    // --- Phần còn lại của script di chuyển ---
    private void MovePlayer()
    {
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
        Vector3 targetVelocity = moveInputDirection * currentSpeed;
        
        // Áp dụng vận tốc ngang, giữ nguyên vận tốc rơi tự do
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInputRaw = context.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed) isSprinting = true;
        else if (context.canceled) isSprinting = false;
    }

    private void UpdateMovementDirection()
    {
        if (cameraTransform == null)
        {
            moveInputDirection = new Vector3(moveInputRaw.x, 0, moveInputRaw.y).normalized;
            return;
        }

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        moveInputDirection = (forward * moveInputRaw.y + right * moveInputRaw.x).normalized;
    }

    private void ApplyGravity()
    {
        if (!isGrounded)
        {
            rb.linearVelocity += Vector3.up * gravity * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, -1f, rb.linearVelocity.z);
        }
    }

    // Vẽ hình cầu kiểm tra đất trong Scene View để bạn chỉnh Radius
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 origin = transform.position + Vector3.up * groundCheckOffset;
        Gizmos.DrawWireSphere(origin, groundCheckRadius);
    }
}