using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;              
using UnityEngine.UI;
using UnityEngine.AI;

// 1. 定义我们期望 LLM 返回的 JSON 格式 (包含 AI 自主调色与质感参数)
[System.Serializable]
public class NPCActionParams
{
    public string speech;             
    public string targetName;         
    public string carColorHex; // 接收十六进制颜色代码 (如 "#FF0000" 或 "none")
    public float metallic;     // 金属度 (0.0 - 1.0)
    public float smoothness;   // 光滑度/反光度 (0.0 - 1.0)
}

// 2. 智谱 API 返回的底层 JSON 结构 (兼容 reasoning_content 推理字段)
[System.Serializable]
public class ZhipuResponse { public Choice[] choices; }
[System.Serializable]
public class Choice { public Message message; }
[System.Serializable]
public class Message 
{ 
    public string content; 
    public string reasoning_content; 
}

[System.Serializable]
public class ChatCompletionRequest
{
    public string model;
    public List<ChatRequestMessage> messages;
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
    public string modelName = "glm-4.5-air";
    private string endpoint = "https://open.bigmodel.cn/api/paas/v4/chat/completions";

    [Header("记忆管理")]
    public int maxMemoryRounds = 3; 
    private List<ChatRequestMessage> dialogueHistory = new List<ChatRequestMessage>();

    [Header("身体与导航组件")]
    public NavMeshAgent agent;           
    public NPCIKController ikController; 
    public Animator animator;            
    public Transform player;              
    public Transform[] exhibitionCars;   

    [Header("UI 交互组件")]
    public TMP_InputField chatInput;     
    public Button sendButton;            
    public TextMeshProUGUI subtitleText; 

    [Header("环境联动 (灯光与多部件材质)")]
    public Light[] kiaLights;               // 起亚展台聚光灯数组
    public Light[] teslaLights;             // 特斯拉展台聚光灯数组
    public float maxLightIntensity = 5f;    // 灯光最亮时的强度
    public float lightFadeSpeed = 1.5f;     // 灯光渐变速度 (值越小越慢)
    
    public Renderer[] kiaBodyRenderers;     // 起亚所有车漆部件 (车身、车门等)
    public Renderer[] teslaBodyRenderers;   // 特斯拉所有车漆部件 (车身、车门等)

    private bool isChatting = false;
    private Coroutine pointSequenceCoroutine;
    private Coroutine lightFadeCoroutine;

    void Start()
    {
        if (sendButton != null) sendButton.onClick.AddListener(OnSendButtonClicked);
        
        InitializeMemory(); 
        ToggleChatWindow(false);

        // 开局初始化：强制关闭所有展台灯光
        SetLightsInstant(kiaLights, 0f);
        SetLightsInstant(teslaLights, 0f);
    }

    private void InitializeMemory()
    {
        dialogueHistory.Clear();
        string context = "当前环境：玩家正在汽车虚拟展厅。你的任务是像一个真实的导购一样，向用户介绍展厅的车：起亚 Kia Xceed , 特斯拉 Tesla Roadster 2020 。";
        
        string systemPrompt = @"你是一个展厅AI导购。
[重要指令]: 跳过内部思考过程，直接输出纯JSON！不要包含markdown, 不要解释。
JSON必须包含以下五个字段，缺一不可：
1. speech: 你要对玩家说的话。
2. targetName: 你正在向玩家介绍的目标名称（只能是 'Kia', 'Tesla', 或 'Player'）。
3. carColorHex: 车辆颜色。只要玩家要求改色、或者询问/提到某种颜色（例如问“有没有蓝色”、“看看红色”），就立刻输出对应颜色的十六进制代码（如 '#0066FF'、'#FF0000'）直接展示给玩家看！只有完全没提颜色时才输出 'none'。
4. metallic: 金属度，范围 0.0(纯塑料) 到 1.0(纯金属)。如果不改色，输出 0。
5. smoothness: 光滑度，范围 0.0(极限哑光) 到 1.0(镜面反光)。如果不改色，输出 0。
6. 若用户用英文提问, 你必须用英文回答,否则用中文回答。

[输出示例（玩家询问或要求蓝色）]：
{
  ""speech"": ""当然有！我这就把这辆特斯拉切换成活力蓝，您看看这个质感怎么样？"",
  ""targetName"": ""Tesla"",
  ""carColorHex"": ""#0077FF"",
  ""metallic"": 0.7,
  ""smoothness"": 0.85
}";
        dialogueHistory.Add(new ChatRequestMessage { role = "system", content = systemPrompt + context });
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) ToggleChatWindow(!isChatting);

        if (isChatting && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            OnSendButtonClicked();
        }

        // 平滑速度并传递给 Animator
        if (agent != null && animator != null)
        {
            float targetSpeed = agent.velocity.magnitude;
            
            if (agent.isStopped || (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance))
            {
                targetSpeed = 0f;
            }

            float currentAnimatorSpeed = animator.GetFloat("Speed");
            animator.SetFloat("Speed", Mathf.Lerp(currentAnimatorSpeed, targetSpeed, Time.deltaTime * 6f));
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
            subtitleText.text = "Sales Man is thinking..."; 
        } 

        OnPlayerSpeak(userInput);
    }

    public void OnPlayerSpeak(string userInput)
    {
        StartCoroutine(ThinkAndAct(userInput));
    }

    private IEnumerator ThinkAndAct(string userInput)
    {
        string safeUserInput = userInput.Replace("\"", "'").Replace("\n", " ").Replace("\r", "");
        dialogueHistory.Add(new ChatRequestMessage { role = "user", content = safeUserInput });

        int maxMessages = (maxMemoryRounds * 2) + 1; 
        while (dialogueHistory.Count > maxMessages)
        {
            dialogueHistory.RemoveAt(1); 
            dialogueHistory.RemoveAt(1); 
        }

        ChatCompletionRequest requestBody = new ChatCompletionRequest
        {
            model = modelName,
            messages = dialogueHistory 
        };
        string requestJson = JsonUtility.ToJson(requestBody);

        Debug.Log($"<color=#55FF55><b>[The original request to AI]</b></color>\n{requestJson}");

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
                Debug.Log($"<color=#55FFFF><b>[The original data returned by AI]</b></color>\n{rawResponse}");
                ParseAndExecute(rawResponse);
            }
            else
            {
                if (subtitleText != null) subtitleText.text = "Error: AI brain crashed!";
                Debug.LogError("AI brain crashed: " + request.error);
                dialogueHistory.RemoveAt(dialogueHistory.Count - 1);
            }
        }
    }

    private void ParseAndExecute(string responseText)
    {
        try
        {
            ZhipuResponse zhipuRes = JsonUtility.FromJson<ZhipuResponse>(responseText);
            if (zhipuRes == null || zhipuRes.choices == null || zhipuRes.choices.Length == 0) return;

            // 兼容推理字段
            string llmContent = zhipuRes.choices[0].message.content;
            if (string.IsNullOrEmpty(llmContent)) llmContent = zhipuRes.choices[0].message.reasoning_content;

            // 裁切提取纯 JSON 块，过滤大模型多余的思考文本
            int startIndex = llmContent.IndexOf('{');
            int endIndex = llmContent.LastIndexOf('}');
            
            if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
            {
                llmContent = llmContent.Substring(startIndex, endIndex - startIndex + 1);
            }
            else
            {
                Debug.LogError("AI failed to output valid JSON，Original content.：" + llmContent);
                return;
            }

            dialogueHistory.Add(new ChatRequestMessage { role = "assistant", content = llmContent });

            llmContent = llmContent.Replace("```json", "").Replace("```", "").Replace("*", "").Trim();
            NPCActionParams action = JsonUtility.FromJson<NPCActionParams>(llmContent);
            if (action == null) return;

            if (subtitleText != null) subtitleText.text = action.speech;

            string safeTargetName = string.IsNullOrEmpty(action.targetName) ? "Player" : action.targetName;
            Transform targetTransform = player;

            // ================= 1. 材质与颜色深度控制 (支持多部件车漆) =================
            if (!string.IsNullOrEmpty(action.carColorHex) && action.carColorHex.ToLower() != "none")
            {
                Color newColor;
                if (ColorUtility.TryParseHtmlString(action.carColorHex, out newColor))
                {
                    Renderer[] targetRenderers = null;
                    if (safeTargetName == "Kia") targetRenderers = kiaBodyRenderers;
                    else if (safeTargetName == "Tesla") targetRenderers = teslaBodyRenderers;

                    if (targetRenderers != null && targetRenderers.Length > 0)
                    {
                        foreach (Renderer rend in targetRenderers)
                        {
                            if (rend == null) continue;

                            // 遍历该部件上的所有材质槽
                            foreach (Material mat in rend.materials)
                            {
                                // 修改颜色 (兼容 URP 的 _BaseColor 和 Standard 的 _Color)
                                if (mat.HasProperty("_BaseColor"))
                                    mat.SetColor("_BaseColor", newColor);
                                else
                                    mat.color = newColor;

                                // 修改金属度
                                if (mat.HasProperty("_Metallic"))
                                    mat.SetFloat("_Metallic", Mathf.Clamp01(action.metallic));

                                // 修改光滑度
                                if (mat.HasProperty("_Smoothness"))
                                    mat.SetFloat("_Smoothness", Mathf.Clamp01(action.smoothness));
                                else if (mat.HasProperty("_Glossiness"))
                                    mat.SetFloat("_Glossiness", Mathf.Clamp01(action.smoothness));
                            }
                        }
                    }
                }
            }

            // ================= 2. 展台聚光灯渐变控制 =================
            if (lightFadeCoroutine != null) StopCoroutine(lightFadeCoroutine);

            if (safeTargetName == "Kia")
            {
                lightFadeCoroutine = StartCoroutine(CrossFadeLightsRoutine(kiaLights, teslaLights));
            }
            else if (safeTargetName == "Tesla")
            {
                lightFadeCoroutine = StartCoroutine(CrossFadeLightsRoutine(teslaLights, kiaLights));
            }
            else if (safeTargetName == "Player")
            {
                lightFadeCoroutine = StartCoroutine(CrossFadeLightsRoutine(null, GetAllLights()));
            }
            // =========================================================

            if (safeTargetName != "Player" && exhibitionCars != null)
            {
                foreach (Transform car in exhibitionCars)
                {
                    if (car != null && car.name.ToLower().Contains(safeTargetName.ToLower()))
                    {
                        targetTransform = car;
                        break;
                    }
                }
            }

            if (pointSequenceCoroutine != null) StopCoroutine(pointSequenceCoroutine);
            pointSequenceCoroutine = StartCoroutine(DoPointThenTurnRoutine(targetTransform, safeTargetName));
        }
        catch (System.Exception e)
        {
            Debug.LogError("解析大模型指令失败: " + e.Message);
        }
    }

    // ================= 灯光控制底层方法 =================
    private Light[] GetAllLights()
    {
        List<Light> allLights = new List<Light>();
        if (kiaLights != null) allLights.AddRange(kiaLights);
        if (teslaLights != null) allLights.AddRange(teslaLights);
        return allLights.ToArray();
    }

    private void SetLightsInstant(Light[] lights, float intensity)
    {
        if (lights == null) return;
        foreach (var l in lights) { if (l != null) l.intensity = intensity; }
    }

    private IEnumerator CrossFadeLightsRoutine(Light[] lightsToOn, Light[] lightsToOff)
    {
        while (true)
        {
            bool allDone = true;
            float step = Time.deltaTime * maxLightIntensity * lightFadeSpeed;

            if (lightsToOn != null)
            {
                foreach (var l in lightsToOn)
                {
                    if (l != null)
                    {
                        l.intensity = Mathf.MoveTowards(l.intensity, maxLightIntensity, step);
                        if (l.intensity < maxLightIntensity) allDone = false;
                    }
                }
            }

            if (lightsToOff != null)
            {
                foreach (var l in lightsToOff)
                {
                    if (l != null)
                    {
                        l.intensity = Mathf.MoveTowards(l.intensity, 0f, step);
                        if (l.intensity > 0f) allDone = false;
                    }
                }
            }

            if (allDone) break;
            yield return null;
        }
    }

    /// <summary>
    /// 标准五段式导购动作流：指车 -> 原地转身对准车 -> 迈步走过去 -> 到达平稳停步 -> 原地转身重新面对玩家
    /// </summary>
    private IEnumerator DoPointThenTurnRoutine(Transform target, string targetName)
    {
        if (target == null) yield break;

        if (targetName == "Player" || target == player)
        {
            if (agent != null)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
            if (ikController != null)
            {
                ikController.lockTurning = false;
                ikController.Target = player;
            }
            yield break;
        }

        // 阶段一：站立指车
        if (ikController != null)
        {
            ikController.lockTurning = true; 
            ikController.Target = target;    
        }

        float pointDuration = 2.4f; 

        if (animator != null)
        {
            Vector3 dirToTarget = target.position - transform.position;
            dirToTarget.y = 0f;
            float dotRight = Vector3.Dot(transform.right, dirToTarget);
            string triggerName = (dotRight >= 0f) ? "PointRight" : "PointLeft";
            
            pointDuration = (dotRight >= 0f) ? 2.8f : 2.4f;
            animator.SetTrigger(triggerName);
        }
        yield return new WaitForSeconds(pointDuration);

        // 阶段二：原地倒脚转身
        Debug.Log($"<color=#FF55FF><b>[阶段二：原地转身]</b></color> 身体平滑转向 {target.name}");
        if (ikController != null)
        {
            ikController.lockTurning = false; 
            ikController.Target = target;
        }

        Vector3 carDir = target.position - transform.position;
        carDir.y = 0f;
        while (carDir.sqrMagnitude > 0.01f && Vector3.Angle(transform.forward, carDir.normalized) > 10f)
        {
            carDir = target.position - transform.position;
            carDir.y = 0f;
            yield return null;
        }

        if (animator != null) animator.SetBool("IsTurning", false);
        if (ikController != null) ikController.lockTurning = true; 

        yield return new WaitForSeconds(0.35f);

        // 阶段三：正向起步迈步走过去
        if (agent != null)
        {
            Debug.Log($"<color=#FF55FF><b>[阶段三：正向迈步]</b></color> 身体已对准，走向 {target.name}");
            
            agent.isStopped = false;
            agent.SetDestination(target.position);

            while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
            {
                yield return null; 
            }

            agent.isStopped = true;
            
            while (animator != null && animator.GetFloat("Speed") > 0.05f)
            {
                yield return null;
            }
            agent.velocity = Vector3.zero;
        }

        yield return new WaitForSeconds(0.2f);

        // 阶段四：到达展车旁后，原地转身重新面对玩家
        if (player != null && ikController != null)
        {
            Debug.Log("<color=#FF55FF><b>[阶段四：到达车旁，转身面对玩家]</b></color>");
            ikController.lockTurning = false; 
            ikController.Target = player;     

            Vector3 playerDir = player.position - transform.position;
            playerDir.y = 0f;
            while (playerDir.sqrMagnitude > 0.01f && Vector3.Angle(transform.forward, playerDir.normalized) > 10f)
            {
                playerDir = player.position - transform.position;
                playerDir.y = 0f;
                yield return null;
            }

            if (animator != null) animator.SetBool("IsTurning", false);
            
            yield return new WaitForSeconds(0.3f);
        }

        // 阶段五：流程结束，保持面对玩家并专注看向玩家
        if (ikController != null)
        {
            ikController.lockTurning = false;
            ikController.Target = player;
        }
    }
}