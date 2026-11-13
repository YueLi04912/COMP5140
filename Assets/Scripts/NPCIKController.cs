using UnityEngine;

[RequireComponent(typeof(Animator))]
public class NPCIKController : MonoBehaviour
{
    [Tooltip("LLM 或代码指定的目标（比如玩家的头，或者某辆展车）")]
    public Transform Target; 
    
    [Range(0, 1)] 
    public float lookWeight = 1f; // 追踪权重，后续可用于做平滑的“转头/回头”过渡
    
    [Tooltip("超过该角度（度）开始触发全身转身")]
    public float maxAngle = 45f;
    [Tooltip("转身直到与目标夹角小于该角度（度）才停止，防止在阈值边缘反复触发重播动画")]
    public float stopAngle = 5f;
    [Tooltip("身体每秒旋转角速度（度/秒）")]
    public float turnSpeed = 75f;

    [Tooltip("外部锁定转身（例如正在播放指向动作、或者寻路移动中禁止原地倒脚）")]
    public bool lockTurning = false;
    
    private Animator animator;
    private bool isTurning = false;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // 1. 如果被外部锁定（如指车中、寻路移动中），立刻中止原地倒脚并退出
        if (lockTurning)
        {
            if (isTurning)
            {
                isTurning = false;
                if (animator != null) animator.SetBool("IsTurning", false);
            }
            return;
        }

        // --- 身体跟随旋转与倒脚动画逻辑 ---
        if (Target != null)
        {
            // 1. 获取目标在水平面上的方向
            Vector3 dirToTarget = Target.position - transform.position;
            dirToTarget.y = 0f; 
            if (dirToTarget.sqrMagnitude < 0.0001f) return;
            dirToTarget.Normalize();

            // 2. 计算 NPC 正前方与目标的夹角
            float currentAngle = Vector3.Angle(transform.forward, dirToTarget);

            // 3. 回滞状态机：超过 maxAngle 开始转身，一旦开始必须转到小于 stopAngle（正对目标）才停下
            if (!isTurning && currentAngle > maxAngle)
            {
                isTurning = true;
            }
            else if (isTurning && currentAngle <= stopAngle)
            {
                isTurning = false;
            }

            if (isTurning)
            {
                Quaternion targetRotation = Quaternion.LookRotation(dirToTarget);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
                
                if (animator != null) animator.SetBool("IsTurning", true);
            }
            else
            {
                if (animator != null) animator.SetBool("IsTurning", false);
            }
        }
    }

    // 必须在这个特定的回调函数中处理 IK
    void OnAnimatorIK(int layerIndex)
    {
        if (animator != null && Target != null)
        {
            // 设置看目标的权重参数：(全局权重, 身体跟随权重, 头部权重, 眼睛权重, 关节限制权重)
            // 身体 0.2，头部 0.8，让动作显得自然，不会像猫头鹰一样脖子扭断
            animator.SetLookAtWeight(lookWeight, 0.2f, 0.8f, 1.0f, 0.5f);
            
            // 强制让头部看向目标的绝对空间坐标
            animator.SetLookAtPosition(Target.position);
        }
        else
        {
            // 如果没有目标，权重归零，恢复普通的站立动画状态
            if (animator != null) animator.SetLookAtWeight(0);
        }
    }
}
