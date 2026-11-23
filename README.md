# COMP5140: Virtual Car Showroom with Intelligent AI Sales Guide
# COMP5140：虚拟汽车展厅与智能 AI 导购交互系统

<div align="center">

![Unity Version](https://img.shields.io/badge/Unity-2022.3.62f3%20LTS-blue.svg?logo=unity)
![Render Pipeline](https://img.shields.io/badge/URP-14.0.12-lightgrey.svg)
![AI Backend](https://img.shields.io/badge/LLM-Zhipu%20AI%20(GLM--4.5--Air)-green.svg)
![License](https://img.shields.io/badge/License-Academic%20%2F%20MIT-orange.svg)

[English](#english) | [中文](#chinese)

</div>

---

<a name="english"></a>
## English

### 1. Overview
**COMP5140 Virtual Car Showroom** is an interactive, next-generation 3D virtual automotive showroom developed with Unity 2022.3 LTS and the Universal Render Pipeline (URP).

The project combines an immersive **GTA V-style True First-Person Controller** with a cloud-powered **Large Language Model (LLM) Sales Agent**. Visitors can interact with a virtual salesman using natural language to inquire about vehicle specifications, inspect dynamic platform lighting, request real-time car paint and finish customization, and be guided around the showroom through a multi-stage kinematic behavioral sequence.

---

### 2. Key Features

#### 🚗 GTA V-Style True First-Person Controller
- **Head-Bone Anchored Camera**: The camera dynamically tracks the character's humanoid head bone (`HumanBodyBones.Head`), delivering realistic walking head bobbing and physical immersion.
- **Full Body Visibility**: Looking down reveals the player's torso, legs, and feet without visual disconnect.
- **Decoupled Mouse Look**: Horizontal mouse movement yaws the player's entire body, while vertical mouse movement pitches the camera independently.
- **Anti-Clipping Bone Scaling**: Head bones are procedurally scaled down at runtime (`localScale = 0.001f`) during `LateUpdate`, preventing the camera from clipping inside the character model while preserving accurate world eye coordinates.

#### 🤖 LLM-Driven AI Sales Guide (Zhipu AI GLM)
- **Cloud LLM Integration**: Communicates with the Zhipu AI Open Platform (`glm-4.5-air` / `glm-4-flash`) via an asynchronous `UnityWebRequest` chat completion pipeline.
- **Structured JSON Action Output**: The LLM outputs strict JSON objects containing:
  - `speech`: Contextual dialogue delivered to the user.
  - `targetName`: Target vehicle focus (`"Kia"`, `"Tesla"`, or `"Player"`).
  - `carColorHex`: Hexadecimal color code (e.g., `#0066FF`) for real-time paint customization.
  - `metallic`: Vehicle metallic finish intensity (`0.0` to `1.0`).
  - `smoothness`: Surface glossiness / reflection level (`0.0` to `1.0`).
- **Contextual Memory Management**: Maintains a multi-turn conversation memory window (`maxMemoryRounds = 3`) with automatic context pruning to maintain high responsiveness and token efficiency.
- **Automatic Bilingual Detection**: Automatically replies in English when the user speaks English, and in Chinese when addressed in Chinese.

#### 🎨 Real-Time Multi-Component Car Paint Customization
- Interprets user color queries (e.g., *"Can I see this car in metallic blue?"*) and maps parameters directly to car body renderers.
- Traverses multiple mesh components (body panels, doors, hood) and modifies shader properties (`_BaseColor`, `_Metallic`, `_Smoothness` / `_Glossiness`) across URP materials in real time.

#### 💡 Dynamic Platform Spotlight Cross-Fading
- Exhibition platforms feature dual spotlight arrays for the **Kia Xceed** and **Tesla Roadster 2020**.
- Spotlights dynamically cross-fade using non-linear interpolation (`Mathf.MoveTowards`), illuminating the active exhibition platform while smoothly dimming inactive ones.

#### 🚶‍♂️ 5-Stage Kinematic & IK Action Sequence
When the AI introduces a vehicle, it performs an uninterrupted, natural 5-stage behavioral routine:
1. **Geometric Pointing**: Calculates vector dot product (`Vector3.Dot(transform.right, dirToTarget)`) relative to the NPC's forward heading to accurately trigger left or right arm pointing (`PointLeft` / `PointRight`).
2. **In-Place Stepping Turn**: Utilizes an angular hysteresis state machine (`maxAngle = 45°`, `stopAngle = 5°`, `turnSpeed = 75°/s`) to smoothly turn towards the car without animation jitter.
3. **NavMesh Navigation**: Activates `NavMeshAgent` pathfinding towards the vehicle platform, blending movement speed smoothly with the animator (`Mathf.Lerp`).
4. **Arrival & Reorientation**: Decelerates naturally at the target location, then executes an in-place turn to re-align facing the player.
5. **Conversational Eye Contact**: Restores head/eye LookAt IK (`NPCIKController`) with natural neck and torso weighting (`0.8` head, `0.2` body) to engage the player.

#### 🔤 High-Definition Bilingual TextMeshPro Font System
- Equipped with a high-resolution 2048x2048 dynamic SDF font asset (`MSYH_Dynamic_SDF.asset`) combined with `LiberationSans SDF` and `regular SDF` fallback tables.
- Guarantees artifact-free, crisp rendering of both complex Chinese characters and ASCII English text without glyph missing or atlas clearing glitches.

---

### 3. Controls & Interaction Guide

| Key / Input | Action |
| :--- | :--- |
| **W / A / S / D** | Move player forward, backward, left, right |
| **Mouse Movement** | Look around (Horizontal: Body Yaw / Vertical: Camera Pitch) |
| **Tab** | Toggle AI chat input interface (Show / Hide) |
| **Enter / Keypad Enter** | Send prompt / chat message to AI Sales Guide |
| **Esc** | Unlock mouse cursor (Show cursor) |
| **Left Mouse Click** | Lock mouse cursor back to game view |

---

### 4. Project Structure

```text
COMP5140/
├── Assets/
│   ├── Animation/              # Humanoid animation clips, state controllers, FBX rigs
│   │   ├── NPC_Animator.controller
│   │   ├── Player_Animator.controller
│   │   ├── PointLeft.fbx / PointRight.fbx
│   │   └── X Bot@*.fbx
│   ├── ChineseFont/            # Dynamic SDF fonts & fallbacks (MSYH, regular, simhei)
│   ├── Material/               # Showroom & vehicle URP PBR materials
│   ├── Models/                 # 3D models (Kia Suv.FBX, Tesla Roadster 2020.fbx)
│   ├── Scenes/
│   │   ├── SampleScene.unity   # Primary showroom exhibition scene
│   │   └── MainScene.unity
│   ├── Scripts/                # Core C# gameplay & AI architecture
│   │   ├── NPCBrain.cs         # LLM integration, JSON parsing, action sequencing
│   │   ├── NPCIKController.cs   # Head/eye LookAt IK & hysteresis turning
│   │   └── PlayerController.cs # True first-person controller & camera binding
│   └── Settings/               # URP asset profiles (HighFidelity, Balanced, Performant)
├── Packages/                   # Package dependencies (URP, NavMesh, TextMeshPro, Cinemachine)
└── ProjectSettings/            # Unity project settings & build configurations
```

---

### 5. Getting Started & Setup

#### Prerequisites
- **Unity Hub**: Recommended version **2022.3.62f3 LTS** (or compatible Unity 2022.3 LTS).
- **Git** with Git LFS support.
- **Zhipu AI API Key** (智谱 AI open platform API key).

#### Installation Steps
1. **Clone the Repository**:
   ```bash
   git clone https://github.com/YueLi04912/COMP5140.git
   cd COMP5140
   ```
2. **Open in Unity**:
   - Open Unity Hub, click **Add project from disk**, and select the `COMP5140` folder.
   - Ensure the editor version is set to **2022.3.62f3** (Universal Render Pipeline).
3. **Open the Scene**:
   - Navigate to `Assets/Scenes/SampleScene.unity` and double-click to load.
4. **Configure the AI API Key**:
   - In the Hierarchy window, select the **NPC** GameObject.
   - In the Inspector, locate the **NPCBrain** component.
   - Enter your Zhipu AI API Key into the **Api Key** field:
     ```text
     Api Key: <YOUR_ZHIPU_API_KEY>
     Model Name: glm-4.5-air (or glm-4-flash)
     ```
5. **Play & Interact**:
   - Click the **Play** button in the Unity Editor.
   - Walk around with `WASD`, press `Tab` to open the chat window, type your request (e.g., *"Introduce the Tesla Roadster and paint it metallic red"*), and press `Enter`.

---

<br/>

---

<a name="chinese"></a>
## 中文

### 1. 项目简介
**COMP5140 虚拟汽车展厅** 是基于 Unity 2022.3 LTS 与通用渲染管线（Universal Render Pipeline, URP）开发的新一代 3D 智能虚拟汽车展厅互动系统。

项目深度融合了 **GTA V 风格的真实沉浸式第一人称控制器** 与 **云端大语言模型（LLM）智能导购系统**。用户可在展厅内以自然语言与 AI 导购员进行无缝对话，获取车辆参数介绍，体验展台聚光灯动态切换，实现车身颜色、金属度与反光度的实时涂装变换，并观看 NPC 智能执行完整的五段式物理动作导览。

---

### 2. 核心特性

#### 🚗 GTA V 风格真实第一人称控制器 (PlayerController)
- **头部骨骼锚定相机**：摄像机动态绑定在角色人形骨骼的头部节点（`HumanBodyBones.Head`），完美呈现角色走动时自然的头部晃动与物理颠簸感。
- **全身视野沉浸感**：低头视角下可清晰看到角色躯干、双腿与脚步移动，杜绝传统“飘浮镜头”的悬空感。
- **解耦式鼠标视线控制**：鼠标横向移动带动角色身体整体旋转（Yaw），纵向移动仅控制摄像机仰角与俯角（Pitch）。
- **运行时防穿模骨骼收缩**：在 `LateUpdate` 中对角色头部骨骼进行程序化微缩（`localScale = 0.001f`），在保留头部真实世界坐标的同时，彻底消除近距离相机穿模看到模型内部网格的瑕疵。

#### 🤖 大语言模型驱动的智能导购 (NPCBrain)
- **云端大模型接口集成**：通过异步 `UnityWebRequest` 接入智谱 AI 开放平台（支持 `glm-4.5-air` / `glm-4-flash`）的 Chat Completions 接口。
- **结构化 JSON 指令输出**：AI 导购跳过内部思考冗余，直接返回结构化控制数据：
  - `speech`：导购对玩家说的自然语言内容。
  - `targetName`：当前介绍的目标对象（`"Kia"`、`"Tesla"` 或 `"Player"`）。
  - `carColorHex`：车辆改色十六进制代码（如 `#0066FF`），未要求改色时输出 `"none"`。
  - `metallic`：金属度质感参数（`0.0` 至 `1.0`）。
  - `smoothness`：漆面光滑度/高光反射参数（`0.0` 至 `1.0`）。
- **多轮会话记忆管理**：内置会话历史队列，采用滑动窗口机制（`maxMemoryRounds = 3`）自动修剪旧上下文，兼顾长期对话连贯性与响应速度。
- **自适应中英双语识别**：AI 自动根据玩家提问语言进行判定，玩家使用英文则以英文回应，使用中文则以中文回应。

#### 🎨 实时多部件车漆涂装定制
- 智能解析玩家改色意图（例如：“有没有蓝色？”、“帮我换成磨砂黑”），自动输出高精度色彩与材质质感参数。
- 自动遍历目标车辆的多部件渲染器（`Renderer[]`，包含车身、车门、引擎盖等部件），即时修改 URP 材质的 `_BaseColor`、`_Metallic`、`_Smoothness` / `_Glossiness` 着色器属性。

#### 💡 展台智能聚光灯平滑过渡
- 起亚（Kia Xceed）与特斯拉（Tesla Roadster 2020）两座展台分别配备专属聚光灯阵列。
- 采用非线性插值算法（`Mathf.MoveTowards`）实现光照强度的平滑渐变（Cross-Fade），自动点亮当前介绍车辆的展台，同时缓熄其他展台灯光。

#### 🚶‍♂️ 五段式导购动作流 (Kinematic & IK Sequence)
NPC 介绍车辆时执行严密自然的多阶段行为流程：
1. **站立指车**：根据 NPC 朝向与展车的相对几何向量，利用向量点积（`Vector3.Dot(transform.right, dirToTarget)`）精准匹配左右手指动作（`PointLeft` / `PointRight`），并配合精准过渡时长避免动作截断。
2. **原地倒脚转身**：结合角度回滞状态机（`maxAngle = 45°` 触发转身，`stopAngle = 5°` 停止，转速 `75°/s`），彻底解决阈值边缘抽搐抖动问题，平滑面朝目标车辆。
3. **正向起步寻路**：调用 `NavMeshAgent` 导航走至目标展台，与动画状态机平滑融合移动速度（`Mathf.Lerp`）。
4. **到达转向面对玩家**：车辆旁平稳驻步后，NPC 原地踏步转身，重新面朝玩家。
5. **视线追踪交互**：恢复全身 LookAt 头部/眼球骨骼 IK 追踪（`0.8` 头部权重、`0.2` 身体跟随权重），保持与玩家的生动眼神交流。

#### 🔤 高清双语 TextMeshPro 字体排版系统
- 搭载 2048x2048 高分辨率动态 SDF 字库（`MSYH_Dynamic_SDF.asset`），并绑定 `LiberationSans SDF` 和 `regular SDF` 作为 Fallback 后备字库。
- 彻底解决动态图集清理导致的文字乱码、方块“口口口”以及英文字符残缺问题。

---

### 3. 操作指南与交互控制

| 按键 / 操作 | 功能描述 |
| :--- | :--- |
| **W / A / S / D** | 控制角色前进、后退、向左横移、向右横移 |
| **鼠标移动** | 观察视角（水平移动旋转身体，垂直移动控制摄像机俯仰） |
| **Tab** | 开启 / 关闭 AI 聊天交互输入框 |
| **Enter / 小键盘 Enter** | 发送输入内容给 AI 导购员 |
| **Esc** | 释放并显示鼠标光标 |
| **鼠标左键点击画面** | 重新锁定光标至游戏窗口中 |

---

### 4. 项目目录结构

```text
COMP5140/
├── Assets/
│   ├── Animation/              # 角色人形动作剪辑、Animator 状态机控制器、FBX 资产
│   │   ├── NPC_Animator.controller
│   │   ├── Player_Animator.controller
│   │   ├── PointLeft.fbx / PointRight.fbx
│   │   └── X Bot@*.fbx
│   ├── ChineseFont/            # 动态 SDF 中英文字体资产（微软雅黑、regular、黑体等）
│   ├── Material/               # 展厅环境与车辆 PBR 材质
│   ├── Models/                 # 3D 展车模型（起亚 Suv.FBX、特斯拉 Roadster 2020.fbx）
│   ├── Scenes/
│   │   ├── SampleScene.unity   # 核心展厅主场景
│   │   └── MainScene.unity
│   ├── Scripts/                # 核心 C# 控制脚本
│   │   ├── NPCBrain.cs         # LLM 大模型对接、JSON 协议解析、环境与导览调度
│   │   ├── NPCIKController.cs   # LookAt 视线 IK 与回滞倒脚转身控制器
│   │   └── PlayerController.cs # GTA V 风格头部绑定第一人称控制器
│   └── Settings/               # URP 通用渲染管线配置资产 (HighFidelity / Balanced / Performant)
├── Packages/                   # Unity 包依赖清单 (URP, NavMesh, TextMeshPro, Cinemachine)
└── ProjectSettings/            # Unity 编辑器与项目构建全局设置
```

---

### 5. 环境要求与快速配置

#### 环境准备
- **Unity Hub**：推荐使用 **Unity 2022.3.62f3 LTS**（或 2022.3.x LTS 系列版本）。
- **Git**（推荐安装 Git LFS）。
- **智谱 AI API Key**（前往 [智谱开放平台](https://open.bigmodel.cn/) 获取）。

#### 快速启动步骤
1. **克隆项目到本地**：
   ```bash
   git clone https://github.com/YueLi04912/COMP5140.git
   cd COMP5140
   ```
2. **在 Unity Hub 中打开项目**：
   - 打开 Unity Hub，点击 **Add project from disk**（从磁盘添加工程），选择 `COMP5140` 根目录。
   - 确认编辑器版本选择 **2022.3.62f3**。
3. **加载核心场景**：
   - 在 Project 窗口打开 `Assets/Scenes/SampleScene.unity`。
4. **配置 AI 导购 API Key**：
   - 在 Hierarchy 层级树中选中 **NPC** 物体。
   - 在右侧 Inspector 检查器中找到 **NPCBrain** 组件。
   - 在 **Api Key** 输入框中填入您的智谱 API Key：
     ```text
     Api Key: <您的智谱API_KEY>
     Model Name: glm-4.5-air (或 glm-4-flash)
     ```
5. **运行体验**：
   - 点击 Unity 编辑器上方的 **Play (运行)** 按钮。
   - 使用 `WASD` 移动，按 `Tab` 键唤起输入框，尝试输入（例如：“你好，请为我介绍一下特斯拉，并把它改成鲜艳的红色”），按 `Enter` 键发送并体验智能导购！

---

### 6. 技术栈清单 (Tech Stack)

- **游戏引擎**: Unity 2022.3.62f3 LTS
- **渲染管线**: Universal Render Pipeline (URP 14.0.12)
- **大语言模型**: 智谱开放平台 GLM-4.5-Air / GLM-4-Flash (Chat Completions API)
- **导航寻路**: Unity AI Navigation (`com.unity.ai.navigation` 1.1.7)
- **UI & 字体系统**: TextMeshPro (`com.unity.textmeshpro` 3.0.7) + MSYH Dynamic SDF Multi-Atlas
- **动画与骨骼系统**: Humanoid Mecanim State Machine + Unity Inverse Kinematics (IK)
