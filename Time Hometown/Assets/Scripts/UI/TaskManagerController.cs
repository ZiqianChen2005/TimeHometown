using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaskManagerController : MonoBehaviour
{
    // 公共变量，用于在 Inspector 中拖拽赋值
    public InputField taskInput;       // 输入任务名称的 InputField
    public Dropdown typeDropdown;      // 选择任务类型的 Dropdown
    public Toggle importantToggle;     // 重要性 Toggle
    public Button addButton;           // 添加任务按钮
    public Transform contentPanel;     // 任务列表的 Content 容器
    public GameObject taskItemPrefab;  // 任务项预制体

    private List<TaskItemData> taskList = new List<TaskItemData>();
    private int taskIdCounter = 0;

    [System.Serializable]
    public class TaskItemData
    {
        public string id;
        public string title;
        public string type;
        public bool isImportant;
        public bool isCompleted;
        public GameObject uiObject; // 对应的 UI 实例
    }

    void Start()
    {
        // 初始化：给按钮添加点击事件监听
        if (addButton != null)
        {
            addButton.onClick.AddListener(OnAddTaskButtonClick);
        }

        // 初始化下拉菜单选项 (例如：工作、学习、生活)
        if (typeDropdown != null)
        {
            typeDropdown.AddOptions(new List<string> { "工作", "学习", "生活", "其他" });
        }
    }

    /// <summary>
    /// 点击“添加任务”按钮时调用
    /// </summary>
    void OnAddTaskButtonClick()
    {
        string taskTitle = taskInput.text.Trim();
        if (string.IsNullOrEmpty(taskTitle))
        {
            Debug.LogWarning("任务名称不能为空！");
            return;
        }

        // 1. 获取用户输入
        string type = typeDropdown.options[typeDropdown.value].text;
        bool isImportant = importantToggle.isOn;

        // 2. 创建任务数据
        TaskItemData newTask = new TaskItemData
        {
            id = "Task_" + taskIdCounter++,
            title = taskTitle,
            type = type,
            isImportant = isImportant,
            isCompleted = false
        };

        // 3. 创建 UI 实例并添加到列表
        GameObject newItem = Instantiate(taskItemPrefab, contentPanel);
        TaskItemController itemController = newItem.GetComponent<TaskItemController>();

        if (itemController != null)
        {
            itemController.Initialize(newTask);
            newTask.uiObject = newItem; // 保存 UI 引用
        }

        // 4. 将任务添加到数据列表
        taskList.Add(newTask);

        // 5. 清空输入框
        taskInput.text = "";
    }

    /// <summary>
    /// 根据 ID 删除任务
    /// </summary>
    public void DeleteTask(string taskId)
    {
        TaskItemData taskToRemove = taskList.Find(t => t.id == taskId);

        if (taskToRemove != null)
        {
            // 从列表中移除数据
            taskList.Remove(taskToRemove);

            // 销毁对应的 UI 物体
            if (taskToRemove.uiObject != null)
            {
                Destroy(taskToRemove.uiObject);
            }
        }
    }

    /// <summary>
    /// 根据 ID 标记任务为已完成
    /// </summary>
    public void CompleteTask(string taskId)
    {
        TaskItemData task = taskList.Find(t => t.id == taskId);

        if (task != null)
        {
            task.isCompleted = true;

            // 更新 UI (通过查找对应的 TaskItemController)
            TaskItemController controller = task.uiObject?.GetComponent<TaskItemController>();
            controller?.UpdateVisualState();
        }
    }
}