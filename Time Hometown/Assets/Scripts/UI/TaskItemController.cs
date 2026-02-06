using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

public class TaskItemController : MonoBehaviour
{
    public UnityEngine.UI.Text taskTitleText;   // 显示任务名称的文本
    public Button deleteButton;  // 删除按钮
    public Button completeButton;// 完成按钮

    private TaskManagerController manager;
    private TaskManagerController.TaskItemData taskData;

    void Awake()
    {
        // 获取父级管理器
        manager = GetComponentInParent<TaskManagerController>();
    }

    /// <summary>
    /// 初始化任务项
    /// </summary>
    public void Initialize(TaskManagerController.TaskItemData data)
    {
        taskData = data;

        // 更新显示文本
        UpdateVisualState();

        // 绑定按钮事件
        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(OnDeleteButtonClick);
        }

        if (completeButton != null)
        {
            completeButton.onClick.AddListener(OnCompleteButtonClick);
        }
    }

    public void UpdateVisualState()
    {
        if (taskTitleText != null)
        {
            string title = taskData.title;
            if (taskData.isImportant)
            {
                title = "[重要] " + title;
            }
            taskTitleText.text = title;
        }
    }

    void OnDeleteButtonClick()
    {
        if (manager != null && taskData != null)
        {
            manager.DeleteTask(taskData.id);
        }
    }

    void OnCompleteButtonClick()
    {
        if (manager != null && taskData != null)
        {
            manager.CompleteTask(taskData.id);
        }
    }
}