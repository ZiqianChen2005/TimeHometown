using UnityEngine;
using UnityEngine.UI; // 必须引用UI命名空间

public class TaskUIManager : MonoBehaviour
{
    // 单例
    public static TaskUIManager Instance;

    // 面板引用
    public GameObject TaskListPanel;
    public GameObject AddTagPanel;
    public GameObject AddTaskPanel;
    public GameObject MatrixModePanel;
    public GameObject TaskDetailPanel;

    // 新增：引用Dropdown组件（必须在Inspector中拖入）
    public Dropdown mainDropdown;

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
        // 初始化面板
        OpenPanel(TaskListPanel);
        ClosePanel(AddTagPanel);
        ClosePanel(AddTaskPanel);
        ClosePanel(MatrixModePanel);
        ClosePanel(TaskDetailPanel);

        // ========== 关键：代码绑定事件 ==========
        // 确保 mainDropdown 已在Inspector中拖入
        if (mainDropdown != null)
        {
            // 移除旧监听（防止重复绑定）
            mainDropdown.onValueChanged.RemoveAllListeners();
            // 添加新监听：绑定到 OnDropdownValueChanged 方法
            mainDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }
    }

    // 通用打开/关闭方法
    void OpenPanel(GameObject panel) { if (panel != null) panel.SetActive(true); }
    void ClosePanel(GameObject panel) { if (panel != null) panel.SetActive(false); }

    // 公共跳转方法
    public void GoToAddTask() { ClosePanel(TaskListPanel); OpenPanel(AddTaskPanel); }
    public void GoToAddTag() { ClosePanel(TaskListPanel); OpenPanel(AddTagPanel); }
    public void GoToMatrixMode() { ClosePanel(TaskListPanel); OpenPanel(MatrixModePanel); }
    public void GoToTaskDetail() { ClosePanel(TaskListPanel); OpenPanel(TaskDetailPanel); }

    // 返回清单
    public void BackToTaskList(GameObject currentPanel)
    {
        ClosePanel(currentPanel);
        OpenPanel(TaskListPanel);
    }

    // 下拉菜单回调（接收动态值）
    public void OnDropdownValueChanged(int value)
    {
        switch (value)
        {
            case 0: // 任务清单
                CloseCurrentPanel();
                OpenPanel(TaskListPanel);
                break;

            case 1: // 四象限
                CloseCurrentPanel();
                OpenPanel(MatrixModePanel);
                break;

            default:
                break;
        }
    }

    // 辅助：关闭当前激活的子面板
    private void CloseCurrentPanel()
    {
        if (AddTagPanel.activeSelf) ClosePanel(AddTagPanel);
        else if (AddTaskPanel.activeSelf) ClosePanel(AddTaskPanel);
        else if (TaskListPanel.activeSelf) ClosePanel(TaskListPanel);
        else if (MatrixModePanel.activeSelf) ClosePanel(MatrixModePanel);
        else if (TaskDetailPanel.activeSelf) ClosePanel(TaskDetailPanel);
    }
}