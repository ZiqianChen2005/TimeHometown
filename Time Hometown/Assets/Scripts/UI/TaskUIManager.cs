using UnityEngine;

public class TaskUIManager : MonoBehaviour
{
    // 单例，方便任务模块内部其他脚本（如列表项、按钮）调用
    public static TaskUIManager Instance;

    // 引用各个子面板
    public GameObject TaskListPanel;
    public GameObject AddTagPanel;
    public GameObject AddTaskPanel;
    public GameObject MatrixModePanel;
    public GameObject TaskDetailPanel;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 初始化：显示清单，隐藏其他
        OpenPanel(TaskListPanel);
        ClosePanel(AddTagPanel);
        ClosePanel(AddTaskPanel);
        ClosePanel(MatrixModePanel);
        ClosePanel(TaskDetailPanel);
    }

    // 通用的打开方法
    void OpenPanel(GameObject panel)
    {
        if (panel != null) panel.SetActive(true);
    }

    // 通用的关闭方法
    void ClosePanel(GameObject panel)
    {
        if (panel != null) panel.SetActive(false);
    }

    // ============ 供按钮点击调用的公共方法 ============

    // 从清单跳转到其他界面
    public void GoToAddTask()
    {
        ClosePanel(TaskListPanel);
        OpenPanel(AddTaskPanel);
    }

    public void GoToAddTag()
    {
        ClosePanel(TaskListPanel);
        OpenPanel(AddTagPanel);
    }

    public void GoToMatrixMode()
    {
        ClosePanel(TaskListPanel);
        OpenPanel(MatrixModePanel);
    }

    public void GoToTaskDetail()
    {
        ClosePanel(TaskListPanel);
        OpenPanel(TaskDetailPanel);
    }

    // ============ 返回按钮逻辑 ============
    // 通常每个子界面都有一个“返回”按钮，都调用这个方法回到清单
    public void BackToTaskList(GameObject currentPanel)
    {
        ClosePanel(currentPanel);
        OpenPanel(TaskListPanel);
    }
}
