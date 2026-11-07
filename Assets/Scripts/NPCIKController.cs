using UnityEngine;

[RequireComponent(typeof(Animator))]
public class NPCIKController : MonoBehaviour
{
    public Transform Target; 
    [Range(0, 1)] public float lookWeight = 1f; 
    
    public float maxAngle = 45f; 
    public float turnSmoothTime = 0.5f; 
    
    private float turnSmoothVelocity; 
    public float turnDelay = 0.8f; 
    private float playerIdleTimer = 0f; 
    private Vector3 lastTargetPos; 
    private bool isRotatingBody = false; 
    
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (Target != null) lastTargetPos = Target.position;
    }

    void Update()
    {
        if (Target != null)
        {
            float distanceMoved = Vector3.Distance(Target.position, lastTargetPos);
            bool isPlayerMoving = distanceMoved > 0.005f; 
            lastTargetPos = Target.position;

            if (isPlayerMoving)
            {
                playerIdleTimer = 0f; 
                isRotatingBody = false; 
            }
            else
            {
                playerIdleTimer += Time.deltaTime; 
            }

            Vector3 dirToTarget = Target.position - transform.position;
            dirToTarget.y = 0f; 
            if (dirToTarget != Vector3.zero) dirToTarget.Normalize();

            float currentAngle = Vector3.Angle(transform.forward, dirToTarget);

            // --- 修复 1：触发新一轮转身时，必须将上一次残留的“物理惯性”彻底清零 ---
            if (playerIdleTimer >= turnDelay && currentAngle > maxAngle && !isRotatingBody)
            {
                isRotatingBody = true;
                turnSmoothVelocity = 0f; // 核心修复点：清空动能，确保每次转身都像第一次一样平滑起步
            }
            
            if (currentAngle < 1.5f)
            {
                isRotatingBody = false;
            }

            if (isRotatingBody)
            {
                float targetAngle = Mathf.Atan2(dirToTarget.x, dirToTarget.z) * Mathf.Rad2Deg;
                float currentYAngle = transform.eulerAngles.y;
                
                float smoothedAngle = Mathf.SmoothDampAngle(currentYAngle, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);
                
                if (animator != null) animator.SetBool("IsTurning", true);
            }
            else
            {
                if (animator != null) animator.SetBool("IsTurning", false);
            }
        }
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator != null && Target != null)
        {
            // --- 修复 2：如果身体正在被代码物理旋转，暂时关闭 IK 对脊椎的二次扭转，防止变成“麻花” ---
            float bodyIKWeight = isRotatingBody ? 0f : 0.2f;
            
            animator.SetLookAtWeight(lookWeight, bodyIKWeight, 0.8f, 1.0f, 0.5f);
            animator.SetLookAtPosition(Target.position);
        }
        else
        {
            if (animator != null) animator.SetLookAtWeight(0);
        }
    }
}