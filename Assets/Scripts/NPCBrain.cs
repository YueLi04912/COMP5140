using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;              // 这一行用来识别 TMP_InputField 和 TextMeshProUGUI
using UnityEngine.UI;     // 这一行用来识别 Button

// 1. 定义我们期望 LLM 返回的 JSON 格式
[System.Serializable]
public class NPCActionParams
{
    public string speech;             // NPC 要说的话
    public string targetName;         // NPC 要介绍的目标名字 (比如 "Kia", "Tesla", 或 "Player")
}

// 2. 智谱 API 返回的底层 JSON 结构 (简化版，用于 JsonUtility 解析)
[System.Serializable]
public class ZhipuResponse
{
    public Choice[] choices;
}
[System.Serializable]
public class Choice
{
    public Message message;
}
[System.Serializable]
public class Message
{
    public string content;
}

[System.Serializable]
public class ChatCompletionRequest
{
    public string model;
    public ChatRequestMessage[] messages;
}

[System.Serializable]
public class ChatRequestMessage
{
    public string role;
    public string content;
}

public class NPCBrain : MonoBehaviour
{
    [Header("大模型配置")]
    public string apiKey = "你的智谱API_KEY";
    public string modelName = "glm-4-flash";
    private string endpoint = "https://open.bigmodel.cn/api/paas/v4/chat/completions";

    [Header("身体组件引用")]
    public NPCIKController ikController; 
    public Animator animator;             
    public Transform player;              
    public Transform[] exhibitionCars;   

    [Header("UI 交互组件")]
    public TMP_InputField chatInput;     // 玩家输入框
    public Button sendButton;            // 发送按钮
    public TextMeshProUGUI subtitleText; // NPC 说话字幕

    private bool isChatting = false;
    private Coroutine pointSequenceCoroutine;

    void Start()
    {
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendButtonClicked);
        }
        
        ToggleChatWindow(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleChatWindow(!isChatting);
        }

        if (isChatting && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            OnSendButtonClicked();
        }
    }

    private void ToggleChatWindow(bool open)
    {
        isChatting = open;
        
        if (chatInput != null) chatInput.gameObject.SetActive(open);
        if (sendButton != null) sendButton.gameObject.SetActive(open);

        if (open)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (chatInput != null) chatInput.ActivateInputField();
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            if (chatInput != null) chatInput.text = ""; 
        }
    }

    private void OnSendButtonClicked()
    {
        string userInput = chatInput.text;
        if (string.IsNullOrEmpty(userInput)) return;

        ToggleChatWindow(false);

        if (subtitleText != null)
        {
            subtitleText.text = "AI导购思考中..."; 
        } 

        OnPlayerSpeak(userInput);
    }

    public void OnPlayerSpeak(string userInput)
    {
        StartCoroutine(ThinkAndAct(userInput));
    }

    private IEnumerator ThinkAndAct(string userInput)
    {
        string context = "当前环境：玩家正在汽车虚拟展厅。你的任务是像一个真实的导购一样，向用户介绍展厅的车：起亚 Kia Xceed , 特斯拉 Tesla Roadster 2020 。";
        // 让大模型专注返回 speech 和 targetName
        string systemPrompt = @"你是一个展厅AI导购。你必须严格输出合法的JSON格式，不要包含markdown。
JSON必须包含以下两个字段，缺一不可：
1. speech: 你要对玩家说的话。
2. targetName: 你正在向玩家介绍的目标名称（只能是 'Kia', 'Tesla', 或 'Player'）。

【输出格式示例】：
{
  ""speech"": ""这辆特斯拉 Roadster 性能非常强劲。"",
  ""targetName"": ""Tesla""
}";
        ChatCompletionRequest requestBody = new ChatCompletionRequest
        {
            model = modelName,
            messages = new[]
            {
                new ChatRequestMessage
                {
                    role = "system",
                    content = systemPrompt + context
                },
                new ChatRequestMessage
                {
                    role = "user",
                    content = userInput
                }
            }
        };
        string requestJson = JsonUtility.ToJson(requestBody);

        // 打印发给 AI 的原始请求（绿色高亮）
        Debug.Log($"<color=#55FF55><b>[发给AI的原始请求]</b></color>\n{requestJson}");

        using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string rawResponse = request.downloadHandler.text;
                
                // 打印 AI 返回的原始数据（青色高亮）
                Debug.Log($"<color=#55FFFF><b>[AI返回的原始数据]</b></color>\n{rawResponse}");

                ParseAndExecute(rawResponse);
            }
            else
            {
                if (subtitleText != null) subtitleText.text = "网络连接失败，请稍后再试。";
                Debug.LogError("大脑宕机: " + request.error + " | 详细原因: " + request.downloadHandler.text);
            }
        }
    }

    private void ParseAndExecute(string responseText)
    {
        try
        {
            ZhipuResponse zhipuRes = JsonUtility.FromJson<ZhipuResponse>(responseText);
            string llmContent = zhipuRes.choices[0].message.content;
            llmContent = llmContent.Replace("```json", "").Replace("```", "").Trim();
            NPCActionParams action = JsonUtility.FromJson<NPCActionParams>(llmContent);

            // 1. 把大模型的台词推送到 UI 字幕上
            if (subtitleText != null)
            {
                subtitleText.text = action.speech;
            }

            // 2. 找到对应的目标 Transform
            Transform targetTransform = null;
            if (action.targetName == "Player")
            {
                targetTransform = player;
            }
            else
            {
                foreach (Transform car in exhibitionCars)
                {
                    if (car != null && car.name.ToLower().Contains(action.targetName.ToLower()))
                    {
                        targetTransform = car;
                        break;
                    }
                }
            }

            // 3. 执行时序逻辑：先指车，动作完成后再转身看车
            if (pointSequenceCoroutine != null)
            {
                StopCoroutine(pointSequenceCoroutine);
            }
            pointSequenceCoroutine = StartCoroutine(DoPointThenTurnRoutine(targetTransform, action.targetName));
        }
        catch (System.Exception e)
        {
            Debug.LogError("解析大模型指令失败: " + e.Message);
        }
    }

    /// <summary>
    /// 时序协程：面向玩家伸手指向展车 -> 播放完毕 -> 锁定解除，NPC转身正对展车
    /// </summary>
    private IEnumerator DoPointThenTurnRoutine(Transform target, string targetName)
    {
        if (target == null) yield break;

        // 如果是跟玩家说话，不需要指车，直接看向并转身对齐玩家
        if (targetName == "Player" || target == player)
        {
            if (ikController != null)
            {
                ikController.lockTurning = false;
                ikController.Target = player;
            }
            yield break;
        }

        // --- 阶段一：先指车（身体保持原状，锁定不转身） ---
        if (ikController != null)
        {
            ikController.lockTurning = true;   // 禁止身体转身
            ikController.Target = target;      // 头部/视线可以转过去看车（显得生动自然）
        }

        float pointDuration = 2.0f; // 默认指向动画持续时间

        if (animator != null)
        {
            // 计算车辆在 NPC 当前身体的左边还是右边
            Vector3 dirToTarget = target.position - transform.position;
            dirToTarget.y = 0f;
            float dotRight = Vector3.Dot(transform.right, dirToTarget);
            string triggerName = (dotRight >= 0f) ? "PointRight" : "PointLeft";

            // PointRight 动画较长(约3.8s)，我们等待手臂完整伸出并展示完毕(约2.2s)再转身；PointLeft 约2.0s
            pointDuration = (dotRight >= 0f) ? 2.2f : 2.0f;

            Debug.Log($"<color=#FF55FF><b>[阶段一：开始指车]</b></color> 目标: {target.name}, 触发动画: {triggerName}, 保持身体不动 {pointDuration} 秒");
            animator.SetTrigger(triggerName);
        }

        // 等待指向手势核心展示阶段结束
        yield return new WaitForSeconds(pointDuration);

        // --- 阶段二：再转身（解除身体锁定，让 NPC 自然全身转向展车） ---
        if (ikController != null)
        {
            Debug.Log("<color=#FF55FF><b>[阶段二：手势完成，开始转身]</b></color> 解除转身锁定，全身转向展车");
            ikController.lockTurning = false;
            ikController.Target = target;
        }
    }
}
