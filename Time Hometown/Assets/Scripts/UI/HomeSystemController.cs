using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HomeSystemController : MonoBehaviour
{
    [Header("房间视图")]
    [SerializeField] private Transform roomContainer;           // 房间容器
    [SerializeField] private GameObject[] roomViews;            // 房间视图数组
    [SerializeField] private Text currentRoomNameText;          // 当前房间名称显示

    [Header("切换按钮")]
    [SerializeField] private Button leftButton;                 // 左切换按钮
    [SerializeField] private Button rightButton;                // 右切换按钮
    [SerializeField] private Image leftButtonImage;             // 左按钮图片（用于状态显示）
    [SerializeField] private Image rightButtonImage;            // 右按钮图片（用于状态显示）

    [Header("格子系统")]
    [SerializeField] private Transform[] roomGridContainers;    // 每个房间的格子容器
    [SerializeField] private GameObject gridCellPrefab;         // 格子预制体

    [Header("编辑布局")]
    [SerializeField] private Button editLayoutButton;           // 编辑布局按钮
    [SerializeField] private Button confirmLayoutButton;        // 确认布局按钮
    [SerializeField] private Button cancelLayoutButton;         // 取消布局按钮
    [SerializeField] private GameObject layoutEditingPanel;     // 编辑布局时的操作面板
    [SerializeField][Range(0f, 1f)] private float normalGridAlpha = 0f;      // 正常状态格子透明度
    [SerializeField][Range(0f, 1f)] private float editModeGridAlpha = 1f;      // 编辑模式格子透明度

    [Header("房间名称配置")]
    [SerializeField] private string[] roomNames = { "户外", "书房", "客厅", "卧室" };

    [Header("按钮颜色")]
    [SerializeField] private Color normalButtonColor = new Color(1f, 1f, 1f, 1f);     // 正常状态
    [SerializeField] private Color disabledButtonColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 禁用状态
    [SerializeField] private Color editModeButtonColor = new Color(0f, 1f, 0f, 1f);   // 编辑模式按钮颜色（绿色）

    private int currentRoomIndex = 0; // 当前房间索引（0:户外,1:书房,2:客厅,3:卧室）
    private bool isEditMode = false;   // 是否处于编辑模式
    private bool isGridSystemReady = false; // 格子系统是否就绪

    // 房间切换事件
    public event System.Action<int> OnRoomChanged;
    // 编辑模式变更事件
    public event System.Action<bool> OnEditModeChanged;

    // Start is called before the first frame update
    void Start()
    {
        InitializeHomeSystem();
    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// 初始化家园系统
    /// </summary>
    private void InitializeHomeSystem()
    {
        // 设置按钮事件
        if (leftButton != null)
        {
            leftButton.onClick.RemoveAllListeners();
            leftButton.onClick.AddListener(OnLeftButtonClicked);
        }

        if (rightButton != null)
        {
            rightButton.onClick.RemoveAllListeners();
            rightButton.onClick.AddListener(OnRightButtonClicked);
        }

        // 设置编辑布局按钮
        if (editLayoutButton != null)
        {
            editLayoutButton.onClick.RemoveAllListeners();
            editLayoutButton.onClick.AddListener(OnEditLayoutButtonClicked);
        }

        if (confirmLayoutButton != null)
        {
            confirmLayoutButton.onClick.RemoveAllListeners();
            confirmLayoutButton.onClick.AddListener(OnConfirmLayoutButtonClicked);
        }

        if (cancelLayoutButton != null)
        {
            cancelLayoutButton.onClick.RemoveAllListeners();
            cancelLayoutButton.onClick.AddListener(OnCancelLayoutButtonClicked);
        }

        // 初始化房间视图
        InitializeRoomViews();

        // 初始化格子系统
        InitializeGridSystem();

        // 更新按钮状态
        UpdateButtonStates();

        // 更新房间名称显示
        UpdateRoomNameDisplay();

        // 初始化编辑模式UI
        UpdateEditModeUI();

        Debug.Log("家园系统初始化完成，房间顺序：户外、书房、客厅、卧室");
    }

    /// <summary>
    /// 初始化房间视图
    /// </summary>
    private void InitializeRoomViews()
    {
        if (roomViews == null || roomViews.Length == 0)
        {
            Debug.LogWarning("未设置房间视图");
            return;
        }

        // 确保房间数量为4
        if (roomViews.Length != 4)
        {
            Debug.LogWarning($"房间视图数量为 {roomViews.Length}，但预期是4个（户外、书房、客厅、卧室）");
        }

        // 确保所有视图都在容器内
        if (roomContainer != null)
        {
            // 如果roomViews为空，尝试从容器中获取
            if (roomViews.Length == 0)
            {
                List<GameObject> views = new List<GameObject>();
                foreach (Transform child in roomContainer)
                {
                    views.Add(child.gameObject);
                }
                roomViews = views.ToArray();
            }

            // 设置所有视图的父对象为容器
            foreach (GameObject view in roomViews)
            {
                if (view != null)
                {
                    view.transform.SetParent(roomContainer, false);
                }
            }
        }

        // 只激活当前房间，隐藏其他房间
        for (int i = 0; i < roomViews.Length; i++)
        {
            if (roomViews[i] != null)
            {
                roomViews[i].SetActive(i == currentRoomIndex);
            }
        }

        Debug.Log($"初始化房间视图，当前房间索引: {currentRoomIndex} ({GetRoomName(currentRoomIndex)})");
    }

    /// <summary>
    /// 左按钮点击事件
    /// </summary>
    private void OnLeftButtonClicked()
    {
        Debug.Log("点击左切换按钮");

        // 播放音效
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        // 切换到上一个房间
        SwitchToPreviousRoom();
    }

    /// <summary>
    /// 右按钮点击事件
    /// </summary>
    private void OnRightButtonClicked()
    {
        Debug.Log("点击右切换按钮");

        // 播放音效
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        // 切换到下一个房间
        SwitchToNextRoom();
    }

    /// <summary>
    /// 编辑布局按钮点击事件
    /// </summary>
    private void OnEditLayoutButtonClicked()
    {
        Debug.Log("点击编辑布局按钮");

        // 播放音效
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        // 进入编辑模式
        EnterEditMode();
    }

    /// <summary>
    /// 确认布局按钮点击事件
    /// </summary>
    private void OnConfirmLayoutButtonClicked()
    {
        Debug.Log("点击确认布局按钮");

        // 播放音效
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySuccessSound();

        // 退出编辑模式并保存
        ExitEditMode(true);
    }

    /// <summary>
    /// 取消布局按钮点击事件
    /// </summary>
    private void OnCancelLayoutButtonClicked()
    {
        Debug.Log("点击取消布局按钮");

        // 播放音效
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        // 退出编辑模式不保存
        ExitEditMode(false);
    }

    /// <summary>
    /// 进入编辑模式
    /// </summary>
    private void EnterEditMode()
    {
        if (isEditMode) return;

        isEditMode = true;

        // 显示所有格子的贴图（设置透明度为编辑模式值）
        SetAllGridsAlpha(editModeGridAlpha);

        // 更新编辑模式UI
        UpdateEditModeUI();

        // 更新按钮状态
        UpdateButtonStates();

        // 触发事件
        OnEditModeChanged?.Invoke(true);

        Debug.Log($"进入编辑模式，格子透明度: {editModeGridAlpha}");
    }

    /// <summary>
    /// 退出编辑模式
    /// </summary>
    /// <param name="saveChanges">是否保存更改</param>
    private void ExitEditMode(bool saveChanges)
    {
        if (!isEditMode) return;

        isEditMode = false;

        // 隐藏格子贴图（恢复透明度为正常值）
        SetAllGridsAlpha(normalGridAlpha);

        // 更新编辑模式UI
        UpdateEditModeUI();

        // 更新按钮状态
        UpdateButtonStates();

        // 触发事件
        OnEditModeChanged?.Invoke(false);

        if (saveChanges)
        {
            Debug.Log("退出编辑模式并保存更改，格子透明度恢复: " + normalGridAlpha);
            // 这里可以添加保存布局的逻辑
        }
        else
        {
            Debug.Log("退出编辑模式，不保存更改，格子透明度恢复: " + normalGridAlpha);
            // 这里可以添加取消更改的逻辑
        }
    }

    /// <summary>
    /// 更新编辑模式UI
    /// </summary>
    private void UpdateEditModeUI()
    {
        // 编辑按钮
        if (editLayoutButton != null)
        {
            editLayoutButton.gameObject.SetActive(!isEditMode);
        }

        // 编辑模式操作面板
        if (layoutEditingPanel != null)
        {
            layoutEditingPanel.SetActive(isEditMode);
        }
    }

    /// <summary>
    /// 设置所有格子的透明度
    /// </summary>
    // 在 HomeSystemController.cs 的 SetAllGridsAlpha 方法中
    private void SetAllGridsAlpha(float alpha)
    {
        if (GridSystemManager.Instance == null)
        {
            Debug.LogWarning("GridSystemManager.Instance 为 null，无法设置格子透明度");
            return;
        }

        // 确保alpha值在0-1之间
        alpha = Mathf.Clamp01(alpha);

        for (int roomIndex = 0; roomIndex < roomGridContainers.Length; roomIndex++)
        {
            GridCellUI[,] grid = GridSystemManager.Instance.GetCurrentRoomGrid(roomIndex);
            if (grid == null)
            {
                Debug.LogWarning($"房间 {roomIndex} 的格子数组为 null");
                continue;
            }

            int width = grid.GetLength(0);
            int height = grid.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y] != null)
                    {
                        // 直接调用 SetGridAlpha 方法设置透明度
                        grid[x, y].SetGridAlpha(alpha);
                    }
                }
            }
        }

        Debug.Log($"已设置所有格子透明度为: {alpha}");
    }

    /// <summary>
    /// 切换到上一个房间
    /// </summary>
    public void SwitchToPreviousRoom()
    {
        if (roomViews == null || roomViews.Length == 0) return;
        if (isEditMode) return; // 编辑模式下不能切换房间

        int targetIndex = currentRoomIndex - 1;
        if (targetIndex >= 0)
        {
            SwitchToRoom(targetIndex);
        }
        else
        {
            Debug.Log("已经在第一个房间");
            // 播放错误音效提示
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayErrorSound();
        }
    }

    /// <summary>
    /// 切换到下一个房间
    /// </summary>
    public void SwitchToNextRoom()
    {
        if (roomViews == null || roomViews.Length == 0) return;
        if (isEditMode) return; // 编辑模式下不能切换房间

        int targetIndex = currentRoomIndex + 1;
        if (targetIndex < roomViews.Length)
        {
            SwitchToRoom(targetIndex);
        }
        else
        {
            Debug.Log("已经在最后一个房间");
            // 播放错误音效提示
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayErrorSound();
        }
    }

    /// <summary>
    /// 切换到指定房间
    /// </summary>
    /// <param name="roomIndex">房间索引</param>
    public void SwitchToRoom(int roomIndex)
    {
        if (roomViews == null || roomViews.Length == 0) return;
        if (roomIndex < 0 || roomIndex >= roomViews.Length) return;
        if (roomIndex == currentRoomIndex) return;
        if (isEditMode) return; // 编辑模式下不能切换房间

        Debug.Log($"切换到房间: {roomIndex} ({GetRoomName(roomIndex)})");

        // 隐藏当前房间
        if (roomViews[currentRoomIndex] != null)
        {
            roomViews[currentRoomIndex].SetActive(false);
        }

        // 显示新房间
        if (roomViews[roomIndex] != null)
        {
            roomViews[roomIndex].SetActive(true);
        }

        // 更新当前索引
        currentRoomIndex = roomIndex;

        // 更新按钮状态
        UpdateButtonStates();

        // 更新房间名称显示
        UpdateRoomNameDisplay();

        // 触发房间变更事件
        OnRoomChanged?.Invoke(currentRoomIndex);

        Debug.Log($"房间切换完成，当前房间: {GetRoomName(currentRoomIndex)}");
    }

    /// <summary>
    /// 更新按钮状态
    /// </summary>
    private void UpdateButtonStates()
    {
        if (roomViews == null || roomViews.Length == 0) return;

        // 左按钮状态（编辑模式下禁用）
        if (leftButton != null)
        {
            bool canGoLeft = currentRoomIndex > 0 && !isEditMode;
            leftButton.interactable = canGoLeft;

            // 更新按钮颜色
            if (leftButtonImage != null)
            {
                leftButtonImage.color = canGoLeft ? normalButtonColor : disabledButtonColor;
            }
        }

        // 右按钮状态（编辑模式下禁用）
        if (rightButton != null)
        {
            bool canGoRight = currentRoomIndex < roomViews.Length - 1 && !isEditMode;
            rightButton.interactable = canGoRight;

            // 更新按钮颜色
            if (rightButtonImage != null)
            {
                rightButtonImage.color = canGoRight ? normalButtonColor : disabledButtonColor;
            }
        }

        // 编辑按钮状态
        if (editLayoutButton != null)
        {
            editLayoutButton.interactable = !isEditMode;
        }
    }

    /// <summary>
    /// 更新房间名称显示
    /// </summary>
    private void UpdateRoomNameDisplay()
    {
        if (currentRoomNameText != null)
        {
            currentRoomNameText.text = GetRoomName(currentRoomIndex);
        }
    }

    /// <summary>
    /// 获取房间名称
    /// </summary>
    private string GetRoomName(int index)
    {
        if (roomNames != null && index >= 0 && index < roomNames.Length)
        {
            return roomNames[index];
        }

        // 默认名称（按照要求：户外、书房、客厅、卧室）
        switch (index)
        {
            case 0: return "户外";
            case 1: return "书房";
            case 2: return "客厅";
            case 3: return "卧室";
            default: return $"房间{index + 1}";
        }
    }

    /// <summary>
    /// 获取当前房间索引
    /// </summary>
    public int GetCurrentRoomIndex()
    {
        return currentRoomIndex;
    }

    /// <summary>
    /// 获取当前房间名称
    /// </summary>
    public string GetCurrentRoomName()
    {
        return GetRoomName(currentRoomIndex);
    }

    /// <summary>
    /// 获取房间总数
    /// </summary>
    public int GetRoomCount()
    {
        return roomViews != null ? roomViews.Length : 0;
    }

    /// <summary>
    /// 检查是否在第一个房间
    /// </summary>
    public bool IsFirstRoom()
    {
        return currentRoomIndex == 0;
    }

    /// <summary>
    /// 检查是否在最后一个房间
    /// </summary>
    public bool IsLastRoom()
    {
        return roomViews != null && currentRoomIndex == roomViews.Length - 1;
    }

    /// <summary>
    /// 检查是否处于编辑模式
    /// </summary>
    public bool IsEditMode()
    {
        return isEditMode;
    }

    // 原有方法保留
    public void Initialize() { }

    public void RefreshHomeState()
    {
        Debug.Log("刷新家园状态");
        // 可以在这里添加刷新逻辑
    }

    public void BuyFurniture(string furnitureId)
    {
        Debug.Log($"购买家具: {furnitureId}");
        // 购买家具逻辑
    }

    public void ArrangeFurniture()
    {
        Debug.Log("布置家具");
        // 布置家具逻辑
    }

    public int GetCurrentCoins()
    {
        // 从GameDataManager获取
        if (GameDataManager.Instance != null)
        {
            return GameDataManager.Instance.GetCoins();
        }
        return 0;
    }

    private void InitializeGridSystem()
    {
        // 确保GridSystemManager存在
        if (GridSystemManager.Instance == null)
        {
            GameObject gridManagerObj = new GameObject("GridSystemManager");
            GridSystemManager gridManager = gridManagerObj.AddComponent<GridSystemManager>();

            // 设置格子容器
            gridManager.roomGridContainers = roomGridContainers;
            gridManager.gridCellPrefab = gridCellPrefab;

            Debug.Log("自动创建GridSystemManager");
        }

        // 等待一帧确保GridSystemManager完成初始化
        StartCoroutine(DelayedGridSetup());
    }

    /// <summary>
    /// 延迟格子设置，等待GridSystemManager初始化完成
    /// </summary>
    private IEnumerator DelayedGridSetup()
    {
        // 等待一帧，让GridSystemManager的Start方法执行
        yield return null;

        // 再等待一帧确保所有格子创建完成
        yield return null;

        // 标记格子系统就绪
        isGridSystemReady = true;

        // 初始设置格子透明度（正常模式）
        SetAllGridsAlpha(normalGridAlpha);

        Debug.Log($"格子系统设置完成，初始透明度: {normalGridAlpha}");
    }
}