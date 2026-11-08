using UnityEngine;

/// <summary>
/// 第一人称（GTA5 风格）角色控制：
/// - 相机绑定在角色头部骨骼位置，低头可以看到自己的身体和脚
/// - 鼠标左右旋转整个身体，鼠标上下只旋转相机（俯仰）
/// - WASD 相对身体朝向移动（W 前进，S 后退，A/D 横移）
/// - 运行时把头部骨骼缩小隐藏，避免相机看到头部内部的模型
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("移动")]
    public float moveSpeed = 2.5f;
    public float gravity = 9.8f;

    [Header("鼠标视角")]
    public float mouseSensitivity = 2f;
    public float minPitch = -70f;   // 抬头上限
    public float maxPitch = 80f;    // 低头上限（看脚）

    [Header("第一人称相机")]
    [Tooltip("相机相对头部骨骼向上的偏移（米）")]
    public float eyeUpOffset = 0.08f;
    [Tooltip("相机相对头部骨骼向前的偏移（米），避免看到脖子内部")]
    public float eyeForwardOffset = 0.12f;
    [Tooltip("相机近裁剪面，越小越不容易穿模")]
    public float cameraNearClip = 0.03f;
    public float cameraFOV = 65f;
    [Tooltip("运行时隐藏头部模型（缩放头部骨骼）")]
    public bool hideHead = true;

    // 保留旧字段，避免场景中的引用丢失（第一人称下不再使用）
    public Transform pointLookat;

    private Animator animator;
    private CharacterController controller;
    private Camera cam;
    private Transform headBone;

    private float yaw;
    private float pitch;

    void Start()
    {
        animator = GetComponent<Animator>();
        animator.applyRootMotion = false;
        controller = GetComponent<CharacterController>();

        cam = Camera.main;
        cam.transform.SetParent(null);
        cam.nearClipPlane = cameraNearClip;
        cam.fieldOfView = cameraFOV;

        if (animator.isHuman)
        {
            headBone = animator.GetBoneTransform(HumanBodyBones.Head);
        }
        if (headBone == null)
        {
            Debug.LogWarning("[PlayerController] 未找到头部骨骼，相机将使用固定高度。");
        }

        yaw = transform.eulerAngles.y;
        pitch = 0f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        // ── 鼠标：左右转身体，上下转相机 ──
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        // ── 键盘：相对身体朝向移动 ──
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(horizontal, 0f, vertical);

        if (input.sqrMagnitude >= 0.01f)
        {
            Vector3 moveDir = (transform.forward * vertical + transform.right * horizontal).normalized;
            controller.Move(moveDir * moveSpeed * Time.deltaTime);
            animator.SetFloat("Speed", 1f);
        }
        else
        {
            animator.SetFloat("Speed", 0f);
        }

        if (!controller.isGrounded)
        {
            controller.Move(Vector3.down * gravity * Time.deltaTime);
        }

        // Esc 释放鼠标，点击画面重新锁定
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // Animator 在 Update 之后、LateUpdate 之前更新骨骼，
    // 所以在 LateUpdate 里读取头部位置才是本帧动画后的最终位置
    void LateUpdate()
    {
        Vector3 eyePos;
        if (headBone != null)
        {
            eyePos = headBone.position
                     + transform.up * eyeUpOffset
                     + transform.forward * eyeForwardOffset;

            if (hideHead)
            {
                // 缩小头部骨骼，隐藏头部网格（不影响头部骨骼自身的位置）
                headBone.localScale = Vector3.one * 0.001f;
            }
        }
        else
        {
            eyePos = transform.position + Vector3.up * 1.6f;
        }

        cam.transform.position = eyePos;
        cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
