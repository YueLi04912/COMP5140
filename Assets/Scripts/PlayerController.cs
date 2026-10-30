using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 2.5f;
    public float turnSpeed = 15f;
    
    // 1. 新增：把独立出去的 pointLookat 拖到这个槽位里
    public Transform pointLookat;

    private Animator animator;
    private CharacterController controller;
    private Transform mainCamera;

    void Start()
    {
        animator = GetComponent<Animator>();
        animator.applyRootMotion = false;
        controller = GetComponent<CharacterController>();
        mainCamera = Camera.main.transform; 
    }

    void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            Vector3 camForward = mainCamera.forward;
            Vector3 camRight = mainCamera.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = (camForward * inputDir.z + camRight * inputDir.x).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

            controller.Move(moveDir * moveSpeed * Time.deltaTime);
            animator.SetFloat("Speed", 1f);
        }
        else
        {
            animator.SetFloat("Speed", 0f);
        }

        if (!controller.isGrounded)
        {
            controller.Move(Vector3.down * 9.8f * Time.deltaTime);
        }

        UpdateLookAtPoint();
    }

    // 2. 关键：每一帧在这里更新空物体的位置，但绝对不继承角色的旋转！
    void LateUpdate()
    {
        UpdateLookAtPoint();
    }

    private void UpdateLookAtPoint()
    {
        if (pointLookat != null)
        {
            // 永远让 pointLookat 处于人物中心上方 1.4 米处，跟着人物走，但不随人物身体晃动
            pointLookat.position = transform.position + new Vector3(0f, 1.4f, 0f);
        }
    }
}
