using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class FocusHistoryWindowController : MonoBehaviour
{
    public static FocusHistoryWindowController Instance { get; private set; }

    [Header("窗口组件")]
    [SerializeField] private GameObject windowRoot;         // 整个窗口的根对象（包含遮罩和内容）
    [SerializeField] private CanvasGroup rootCanvasGroup;   // 根对象的CanvasGroup
    [SerializeField] private Button closeButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button backButton;

    [Header("遮罩组件")]
    [SerializeField] private Image maskImage;              // 遮罩背景Image
    [SerializeField] private Button maskButton;            // 遮罩点击按钮

    [Header("内容组件")]
    [SerializeField] private GameObject contentPanel;      // 内容面板
    [SerializeField] private Transform historyContent;     // ScrollView的Content
    [SerializeField] private GameObject historyItemPrefab; // 历史记录项预制体
    [SerializeField] private ScrollRect scrollRect;       // ScrollRect组件
    [SerializeField] private Text totalCountText;
    [SerializeField] private Text todayTotalText;
    [SerializeField] private Text weekTotalText;
    [SerializeField] private Text monthTotalText;

    [Header("空状态")]
    [SerializeField] private GameObject emptyStatePanel;
    [SerializeField] private Text emptyStateText;

    [Header("动画设置")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private List<FocusHistoryData> allHistoryRecords = new List<FocusHistoryData>();
    private bool isWindowOpen = false;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        // 设置单例
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeWindow();
    }

    private void Start()
    {
        // 初始完全隐藏整个窗口（包括遮罩）
        if (windowRoot != null)
        {
            windowRoot.SetActive(false);
            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.alpha = 0;
                rootCanvasGroup.interactable = false;
                rootCanvasGroup.blocksRaycasts = false;
            }
        }

        // 设置ScrollView滚动到顶部
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void Update()
    {
        // 按ESC键关闭窗口
        if (isWindowOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseWindow();
        }
    }

    /// <summary>
    /// 初始化窗口
    /// </summary>
    private void InitializeWindow()
    {
        // 设置按钮事件
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseWindow);

        if (backButton != null)
            backButton.onClick.AddListener(CloseWindow);

        if (clearButton != null)
            clearButton.onClick.AddListener(OnClearButtonClicked);

        // 设置遮罩点击关闭
        if (maskButton != null)
        {
            maskButton.onClick.AddListener(CloseWindow);
            maskButton.transition = Selectable.Transition.None;
        }

        // 设置Content的Layout组件
        if (historyContent != null)
        {
            VerticalLayoutGroup vlg = historyContent.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
            {
                vlg = historyContent.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 10;
                vlg.padding = new RectOffset(10, 10, 10, 10);
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
            }

            ContentSizeFitter csf = historyContent.GetComponent<ContentSizeFitter>();
            if (csf == null)
            {
                csf = historyContent.gameObject.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        Debug.Log("专注历史窗口初始化完成");
    }

    /// <summary>
    /// 打开历史记录窗口 - 整体淡入
    /// </summary>
    public void OpenWindow()
    {
        if (isWindowOpen) return;

        // 加载数据
        RefreshData();

        // 滚动到顶部
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }

        // 显示窗口根对象
        if (windowRoot != null)
        {
            windowRoot.SetActive(true);
            StartFadeAnimation(true);
        }

        isWindowOpen = true;

        // 播放音效
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        Debug.Log("专注历史窗口已打开");
    }

    /// <summary>
    /// 关闭历史记录窗口 - 整体淡出
    /// </summary>
    public void CloseWindow()
    {
        if (!isWindowOpen) return;

        StartFadeAnimation(false, () =>
        {
            // 淡出完成后隐藏整个根对象
            if (windowRoot != null)
                windowRoot.SetActive(false);

            isWindowOpen = false;
            Debug.Log("专注历史窗口已关闭");
        });

        // 播放音效
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
    }

    /// <summary>
    /// 开始淡入淡出动画 - 整个窗口统一动画
    /// </summary>
    private void StartFadeAnimation(bool fadeIn, Action onComplete = null)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeAnimation(fadeIn, onComplete));
    }

    private System.Collections.IEnumerator FadeAnimation(bool fadeIn, Action onComplete)
    {
        // 如果没有CanvasGroup，尝试获取或添加
        if (rootCanvasGroup == null && windowRoot != null)
        {
            rootCanvasGroup = windowRoot.GetComponent<CanvasGroup>();
            if (rootCanvasGroup == null)
                rootCanvasGroup = windowRoot.AddComponent<CanvasGroup>();
        }

        if (rootCanvasGroup == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        float startAlpha = fadeIn ? 0 : rootCanvasGroup.alpha;
        float endAlpha = fadeIn ? 1 : 0;
        float elapsed = 0f;

        // 淡入时先设置可交互状态
        if (fadeIn)
        {
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = true;
        }

        rootCanvasGroup.alpha = startAlpha;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float curvedT = fadeCurve.Evaluate(t);

            rootCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, curvedT);
            yield return null;
        }

        rootCanvasGroup.alpha = endAlpha;

        // 淡出完成后设置不可交互
        if (!fadeIn)
        {
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;
        }
        else
        {
            rootCanvasGroup.interactable = true;
            rootCanvasGroup.blocksRaycasts = true;
        }

        onComplete?.Invoke();
        fadeCoroutine = null;
    }

    /// <summary>
    /// 刷新数据
    /// </summary>
    private void RefreshData()
    {
        if (FocusHistoryManager.Instance != null)
        {
            allHistoryRecords = FocusHistoryManager.Instance.GetAllHistoryRecords();

            // 更新统计数据
            UpdateStatistics();

            // 更新列表显示
            RefreshList();

            // 更新空状态
            UpdateEmptyState();
        }
    }

    /// <summary>
    /// 刷新列表 - 直接显示所有记录，不用分页
    /// </summary>
    private void RefreshList()
    {
        // 清空现有内容
        foreach (Transform child in historyContent)
        {
            Destroy(child.gameObject);
        }

        if (allHistoryRecords.Count == 0)
            return;

        // 按时间倒序显示（最新的在前面）
        for (int i = 0; i < allHistoryRecords.Count; i++)
        {
            CreateHistoryItem(allHistoryRecords[i]);
        }

        Debug.Log($"显示 {allHistoryRecords.Count} 条历史记录");
    }

    /// <summary>
    /// 创建历史记录项
    /// </summary>
    private void CreateHistoryItem(FocusHistoryData data)
    {
        if (historyItemPrefab == null || historyContent == null) return;

        GameObject itemObj = Instantiate(historyItemPrefab, historyContent);
        FocusHistoryItemUI itemUI = itemObj.GetComponent<FocusHistoryItemUI>();

        if (itemUI != null)
        {
            itemUI.Initialize(data);
        }

        // 设置布局元素
        LayoutElement layout = itemObj.GetComponent<LayoutElement>();
        if (layout == null)
            layout = itemObj.AddComponent<LayoutElement>();

        layout.preferredHeight = 120;
        layout.minHeight = 100;
        layout.flexibleHeight = 0;
    }

    /// <summary>
    /// 更新统计数据
    /// </summary>
    private void UpdateStatistics()
    {
        if (FocusHistoryManager.Instance != null)
        {
            if (totalCountText != null)
                totalCountText.text = $"累计专注: {allHistoryRecords.Count}次";

            if (todayTotalText != null)
            {
                int todayMinutes = FocusHistoryManager.Instance.GetTodayTotalMinutes();
                todayTotalText.text = $"今日: {FormatMinutes(todayMinutes)}";
            }

            if (weekTotalText != null)
            {
                int weekMinutes = FocusHistoryManager.Instance.GetWeekTotalMinutes();
                weekTotalText.text = $"本周: {FormatMinutes(weekMinutes)}";
            }

            if (monthTotalText != null)
            {
                int monthMinutes = FocusHistoryManager.Instance.GetMonthTotalMinutes();
                monthTotalText.text = $"本月: {FormatMinutes(monthMinutes)}";
            }
        }
    }

    /// <summary>
    /// 格式化分钟显示
    /// </summary>
    private string FormatMinutes(int totalMinutes)
    {
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;

        if (hours > 0)
            return $"{hours}小时{minutes}分钟";
        else
            return $"{minutes}分钟";
    }

    /// <summary>
    /// 更新空状态
    /// </summary>
    private void UpdateEmptyState()
    {
        if (emptyStatePanel != null)
        {
            bool hasRecords = allHistoryRecords.Count > 0;
            emptyStatePanel.SetActive(!hasRecords);

            // 有记录时隐藏空状态，显示内容
            if (historyContent != null)
            {
                historyContent.gameObject.SetActive(hasRecords);
            }

            if (emptyStateText != null && !hasRecords)
            {
                emptyStateText.text = "暂无专注历史记录\n快去开始你的第一次专注吧！";
            }
        }
    }

    /// <summary>
    /// 清空历史记录按钮点击
    /// </summary>
    private void OnClearButtonClicked()
    {
        // 可以在这里添加确认对话框
        if (FocusHistoryManager.Instance != null)
        {
            FocusHistoryManager.Instance.ClearAllHistory();
            RefreshData();

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayButtonClick();

            Debug.Log("已清空所有专注历史记录");
        }
    }

    /// <summary>
    /// 检查窗口是否打开
    /// </summary>
    public bool IsWindowOpen()
    {
        return isWindowOpen;
    }

    /// <summary>
    /// 切换窗口开关状态
    /// </summary>
    public void ToggleWindow()
    {
        if (isWindowOpen)
            CloseWindow();
        else
            OpenWindow();
    }

    /// <summary>
    /// 滚动到最新记录（底部）
    /// </summary>
    public void ScrollToLatest()
    {
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    /// <summary>
    /// 滚动到最早记录（顶部）
    /// </summary>
    public void ScrollToEarliest()
    {
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }
}