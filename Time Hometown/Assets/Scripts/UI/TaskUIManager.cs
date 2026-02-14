using System; // 新增：用于Action委托
using UnityEngine;
using UnityEngine.UI;

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

    // 新增：遮罩管理器引用（必须在Inspector中拖入）
    public UIMaskManager maskManager;

    // 新增：引用Dropdown组件（必须在Inspector中拖入）
    public Dropdown mainDropdown;

    // 新增：存储当前打开的面板和类型
    private GameObject currentPopupPanel;
    private PopupType currentPopupType;

    // 新增：枚举定义弹出面板类型
    public enum PopupType
    {
        None,
        AddTask,    // 添加任务 - 直接关闭
        AddTag,     // 添加标签 - 直接关闭
        TaskDetail  // 任务详情 - 返回任务清单
    }

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

        // 新增：初始化状态
        currentPopupPanel = null;
        currentPopupType = PopupType.None;

        // 下拉菜单事件绑定
        if (mainDropdown != null)
        {
            mainDropdown.onValueChanged.RemoveAllListeners();
            mainDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }
    }

    // 通用打开/关闭方法
    void OpenPanel(GameObject panel) { if (panel != null) panel.SetActive(true); }
    void ClosePanel(GameObject panel) { if (panel != null) panel.SetActive(false); }

    // ========== 新增：统一的遮罩面板打开方法 ==========
    public void OpenPanelWithMask(GameObject panel, PopupType type)
    {
        // 关闭当前打开的面板
        if (currentPopupPanel != null)
        {
            CloseCurrentPanel();
        }

        currentPopupPanel = panel;
        currentPopupType = type;

        // 根据不同类型定义遮罩点击行为
        Action onMaskClick = null;

        switch (type)
        {
            case PopupType.AddTask:
            case PopupType.AddTag:
                onMaskClick = () => {
                    CloseCurrentPanel();
                    if (maskManager != null)
                        maskManager.HideMask();
                };
                break;

            case PopupType.TaskDetail:
                onMaskClick = () => {
                    SaveTaskData(); // 保存任务数据
                    CloseCurrentPanel();
                    OpenPanel(TaskListPanel);
                    if (maskManager != null)
                        maskManager.HideMask();
                };
                break;
        }

        // 打开面板并显示遮罩
        panel.SetActive(true);
        if (maskManager != null)
        {
            maskManager.ShowMask(panel, onMaskClick);
        }
    }

    // ========== 新增：保存任务数据方法 ==========
    private void SaveTaskData()
    {
        // 这里实现具体的任务数据保存逻辑
        Debug.Log("保存任务详情数据");
        // TODO: 调用你的任务数据管理系统
    }

    // ========== 修改：公共跳转方法 ==========
    public void GoToAddTask()
    {
        ClosePanel(TaskListPanel);
        OpenPanelWithMask(AddTaskPanel, PopupType.AddTask);
    }

    public void GoToAddTag()
    {
        ClosePanel(TaskListPanel);
        OpenPanelWithMask(AddTagPanel, PopupType.AddTag);
    }

    public void GoToTaskDetail()
    {
        ClosePanel(TaskListPanel);
        OpenPanelWithMask(TaskDetailPanel, PopupType.TaskDetail);
    }

    public void GoToMatrixMode()
    {
        CloseCurrentPopup(); // 关闭可能打开的遮罩面板
        ClosePanel(TaskListPanel);
        OpenPanel(MatrixModePanel);
    }

    // ========== 修改：返回清单 ==========
    public void BackToTaskList(GameObject currentPanel)
    {
        ClosePanel(currentPanel);
        OpenPanel(TaskListPanel);
        currentPopupPanel = null;
        currentPopupType = PopupType.None;

        // 关闭遮罩
        if (maskManager != null)
            maskManager.HideMask();
    }

    // ========== 新增：任务详情按钮专用方法 ==========
    public void SaveAndCloseTaskDetail()
    {
        if (currentPopupType == PopupType.TaskDetail)
        {
            SaveTaskData();
            BackToTaskList(currentPopupPanel);
        }
    }

    public void CancelAndCloseTaskDetail()
    {
        if (currentPopupType == PopupType.TaskDetail)
        {
            BackToTaskList(currentPopupPanel);
        }
    }

    // ========== 修改：下拉菜单回调 ==========
    public void OnDropdownValueChanged(int value)
    {
        // 先关闭当前所有面板和遮罩
        CloseCurrentPopup();
        CloseCurrentPanel();

        switch (value)
        {
            case 0: // 任务清单
                OpenPanel(TaskListPanel);
                break;

            case 1: // 四象限
                OpenPanel(MatrixModePanel);
                break;

            case 2: // 添加任务
                GoToAddTask();
                break;

            case 3: // 添加标签
                GoToAddTag();
                break;

            default:
                break;
        }
    }

    // ========== 修改：关闭当前激活的子面板 ==========
    private void CloseCurrentPanel()
    {
        if (AddTagPanel.activeSelf) ClosePanel(AddTagPanel);
        else if (AddTaskPanel.activeSelf) ClosePanel(AddTaskPanel);
        else if (TaskListPanel.activeSelf) ClosePanel(TaskListPanel);
        else if (MatrixModePanel.activeSelf) ClosePanel(MatrixModePanel);
        else if (TaskDetailPanel.activeSelf) ClosePanel(TaskDetailPanel);
    }

    // ========== 新增：关闭当前弹出面板和遮罩 ==========
    private void CloseCurrentPopup()
    {
        if (currentPopupPanel != null)
        {
            ClosePanel(currentPopupPanel);
            currentPopupPanel = null;
            currentPopupType = PopupType.None;
        }

        if (maskManager != null)
            maskManager.HideMask();
    }
}