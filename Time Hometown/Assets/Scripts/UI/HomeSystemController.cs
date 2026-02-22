using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class HomeSystemController : MonoBehaviour
{
    [Header("房间视图")]
    [SerializeField] private Transform roomContainer;           // 房间容器
    [SerializeField] private GameObject[] roomViews;            // 房间视图数组
    [SerializeField] private Text currentRoomNameText;          // 当前房间名称显示

    [Header("切换按钮")]
    [SerializeField] private Button leftButton;                 // 左切换按钮
    [SerializeField] private Button rightButton;                // 右切换按钮
    [SerializeField] private Image leftButtonImage;             // 左按钮图片
    [SerializeField] private Image rightButtonImage;            // 右按钮图片

    [Header("格子系统")]
    [SerializeField] private Transform[] roomGridContainers;        // 每个房间的格子容器
    [SerializeField] private Transform[] roomFurnitureContainers;   // 每个房间的家具容器
    [SerializeField] private GameObject gridCellPrefab;             // 格子预制体

    [Header("编辑布局")]
    [SerializeField] private Button editLayoutButton;           // 编辑布局按钮
    [SerializeField] private Button confirmLayoutButton;        // 确认布局按钮
    [SerializeField] private Button cancelLayoutButton;         // 取消布局按钮
    [SerializeField] private GameObject layoutEditingPanel;     // 编辑布局时的操作面板
    [SerializeField] private float normalGridAlpha = 0.2f;      // 正常状态格子透明度
    [SerializeField] private float editModeGridAlpha = 1f;      // 编辑模式格子透明度

    [Header("持有家具栏 - ScrollView")]
    [SerializeField] private ScrollRect ownedFurnitureScrollRect;    // ScrollRect组件
    [SerializeField] private Transform ownedFurnitureContent;        // ScrollView的Content
    [SerializeField] private GameObject ownedFurniturePrefab;        // 持有家具项预制体
    [SerializeField] private GridLayoutGroup ownedFurnitureGrid;     // 网格布局组件
    [SerializeField] private float furnitureBarOffset = 600f;       // 家具栏移动距离

    [Header("编辑按钮栏")]
    [SerializeField] private Transform buttonBarContainer;           // 按钮栏容器
    [SerializeField] private float buttonBarOffset = -600f;          // 按钮栏移动距离

    [Header("垃圾桶")]
    [SerializeField] public TrashCanController trashCan;           // 垃圾桶控制器
    [SerializeField] public bool enableTrashCan = true;            // 是否启用垃圾桶

    [Header("动画设置")]
    [SerializeField] private float moveDuration = 0.3f;              // 移动动画持续时间
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("房间名称配置")]
    [SerializeField] private string[] roomNames = { "户外", "书房", "客厅", "卧室" };

    [Header("按钮颜色")]
    [SerializeField] private Color normalButtonColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color disabledButtonColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    [SerializeField] private Color editModeButtonColor = new Color(0f, 1f, 0f, 1f);

    [Header("调试")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private Button testSaveButton;
    [SerializeField] private Button testLoadButton;

    // 布局数据
    private Dictionary<int, List<FurniturePlacementData>> roomLayouts = new Dictionary<int, List<FurniturePlacementData>>();
    private Dictionary<int, List<FurniturePlacementData>> tempLayouts = new Dictionary<int, List<FurniturePlacementData>>();

    // 当前状态
    private int currentRoomIndex = 0;
    private bool isEditMode = false;
    private bool isBarMoving = false;

    // 持有家具列表
    private List<string> ownedFurnitureIds = new List<string>();
    private List<GameObject> ownedFurnitureItems = new List<GameObject>();

    // 原始位置
    private Vector2 ownedFurnitureOriginalPos;
    private Vector2 buttonBarOriginalPos;

    // ScrollView的RectTransform
    private RectTransform ownedFurnitureRect;

    // 房间切换事件
    public event System.Action<int> OnRoomChanged;
    public event System.Action<bool> OnEditModeChanged;

    private void Start()
    {
        InitializeHomeSystem();
        LoadOwnedFurniture();
        LoadRoomLayouts();
        PopulateOwnedFurniture();

        // 初始化完成后设置可见性
        SetRoomVisibility();

        if (testSaveButton != null)
        {
            testSaveButton.onClick.AddListener(TestSaveLayout);
        }

        if (testLoadButton != null)
        {
            testLoadButton.onClick.AddListener(TestLoadLayout);
        }
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

        // 获取ScrollView的RectTransform
        if (ownedFurnitureScrollRect != null)
        {
            ownedFurnitureRect = ownedFurnitureScrollRect.GetComponent<RectTransform>();
            ownedFurnitureOriginalPos = ownedFurnitureRect.anchoredPosition;
        }

        if (buttonBarContainer != null)
            buttonBarOriginalPos = buttonBarContainer.GetComponent<RectTransform>().anchoredPosition;

        // 初始化网格布局
        InitializeGridLayout();

        // 更新按钮状态
        UpdateButtonStates();

        // 更新房间名称显示
        UpdateRoomNameDisplay();

        // 初始化编辑模式UI
        UpdateEditModeUI();

        // 加载已保存的布局
        LoadRoomLayouts();
        // 应用已保存的布局到格子
        ApplyRoomLayouts();

        // 订阅家具拾取事件
        if (GridSystemManager.Instance != null)
        {
            GridSystemManager.Instance.OnFurniturePickupEvent += OnFurniturePickup;
        }

        // 初始化垃圾桶
        if (trashCan != null)
        {
            trashCan.OnFurnitureDropped += OnFurnitureDroppedToTrash;
        }

        Debug.Log("家园系统初始化完成");
    }

    private void OnDestroy()
    {
        // 取消订阅
        if (GridSystemManager.Instance != null)
        {
            GridSystemManager.Instance.OnFurniturePickupEvent -= OnFurniturePickup;
        }

        if (trashCan != null)
        {
            trashCan.OnFurnitureDropped -= OnFurnitureDroppedToTrash;
        }
    }

    /// <summary>
    /// 家具被丢入垃圾桶事件
    /// </summary>
    private void OnFurnitureDroppedToTrash()
    {
        Debug.Log("家具被丢入垃圾桶");

        // 如果正在移动家具，取消移动并删除家具
        if (GridSystemManager.Instance != null && GridSystemManager.Instance.IsMovingFurniture())
        {
            var movingInfo = GridSystemManager.Instance.GetMovingFurnitureInfo();
            if (movingInfo != null)
            {
                // 从临时布局中移除
                if (tempLayouts.ContainsKey(currentRoomIndex))
                {
                    tempLayouts[currentRoomIndex].RemoveAll(p => p.furnitureId == movingInfo.furnitureId);
                }

                // 取消移动（不恢复原位置，直接删除）
                GridSystemManager.Instance.CancelMovingFurniture(true);

                Debug.Log($"删除家具: {movingInfo.furnitureData.name}");
            }
        }
    }

    /// <summary>
    /// 设置房间可见性（只显示当前房间）
    /// </summary>
    private void SetRoomVisibility()
    {
        if (roomViews != null)
        {
            for (int i = 0; i < roomViews.Length; i++)
            {
                if (roomViews[i] != null)
                {
                    roomViews[i].SetActive(i == currentRoomIndex);
                }
            }
        }

        for (int i = 0; i < roomGridContainers.Length; i++)
        {
            if (roomGridContainers[i] != null)
            {
                roomGridContainers[i].gameObject.SetActive(i == currentRoomIndex);
            }
        }

        for (int i = 0; i < roomFurnitureContainers.Length; i++)
        {
            if (roomFurnitureContainers[i] != null)
            {
                roomFurnitureContainers[i].gameObject.SetActive(i == currentRoomIndex);
            }
        }

        Debug.Log($"设置房间可见性: 显示房间 {currentRoomIndex}");
    }

    /// <summary>
    /// 初始化网格布局
    /// </summary>
    private void InitializeGridLayout()
    {
        if (ownedFurnitureGrid == null && ownedFurnitureContent != null)
        {
            ownedFurnitureGrid = ownedFurnitureContent.GetComponent<GridLayoutGroup>();
            if (ownedFurnitureGrid == null)
            {
                ownedFurnitureGrid = ownedFurnitureContent.gameObject.AddComponent<GridLayoutGroup>();
            }
        }

        if (ownedFurnitureGrid != null)
        {
            ownedFurnitureGrid.cellSize = new Vector2(150, 150);
            ownedFurnitureGrid.spacing = new Vector2(80, 55);
            ownedFurnitureGrid.padding = new RectOffset(40, 40, 0, 0);
            ownedFurnitureGrid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            ownedFurnitureGrid.constraintCount = 2;

            Debug.Log("持有家具网格布局初始化完成");
        }

        if (ownedFurnitureContent != null)
        {
            ContentSizeFitter fitter = ownedFurnitureContent.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = ownedFurnitureContent.gameObject.AddComponent<ContentSizeFitter>();
            }
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        }
    }

    /// <summary>
    /// 加载已持有家具
    /// </summary>
    private void LoadOwnedFurniture()
    {
        string ownedFurniture = PlayerPrefs.GetString("Owned_Furniture", "");
        if (!string.IsNullOrEmpty(ownedFurniture))
        {
            ownedFurnitureIds = new List<string>(ownedFurniture.Split(','));
        }

        if (ownedFurnitureIds.Count == 0)
        {
            ownedFurnitureIds.Add("town_default_chair");
            ownedFurnitureIds.Add("town_default_table");
            ownedFurnitureIds.Add("town_oak_cabinet");
            ownedFurnitureIds.Add("desert_aladdin_lamp");
            ownedFurnitureIds.Add("sky_star_lamp");
            ownedFurnitureIds.Add("ice_crystal_lamp");
            ownedFurnitureIds.Add("sky_cloud_dressing_table");
            ownedFurnitureIds.Add("desert_feather_bed");
            ownedFurnitureIds.Add("town_clock");
            ownedFurnitureIds.Add("ice_aurora_scroll");
        }

        Debug.Log($"加载持有家具: {ownedFurnitureIds.Count}件");
    }

    /// <summary>
    /// 填充持有家具栏
    /// </summary>
    private void PopulateOwnedFurniture()
    {
        if (ownedFurnitureContent == null || ownedFurniturePrefab == null || FurnitureDatabase.Instance == null)
            return;

        foreach (Transform child in ownedFurnitureContent)
        {
            Destroy(child.gameObject);
        }
        ownedFurnitureItems.Clear();

        foreach (string furnitureId in ownedFurnitureIds)
        {
            FurnitureData data = FurnitureDatabase.Instance.GetFurnitureById(furnitureId);
            if (data == null) continue;

            GameObject itemObj = Instantiate(ownedFurniturePrefab, ownedFurnitureContent);
            DraggableFurnitureItem dragItem = itemObj.GetComponent<DraggableFurnitureItem>();

            if (dragItem != null)
            {
                dragItem.Initialize(data);
                dragItem.OnDragStart += OnFurnitureDragStart;
                dragItem.OnDragEnd += OnFurnitureDragEnd;
                ownedFurnitureItems.Add(itemObj);
            }
        }

        Debug.Log($"填充持有家具栏: {ownedFurnitureItems.Count}件");
    }

    /// <summary>
    /// 家具拖拽开始事件（从持有家具栏）
    /// </summary>
    private void OnFurnitureDragStart(FurnitureData furniture, Vector2 position)
    {
        if (!isEditMode) return;

        Debug.Log($"家具拖拽开始: {furniture.name}");

        if (GridSystemManager.Instance != null)
        {
            GridSystemManager.Instance.StartDraggingFromInventory(furniture);
        }

        if (ownedFurnitureScrollRect != null)
        {
            ownedFurnitureScrollRect.vertical = false;
            ownedFurnitureScrollRect.horizontal = false;
        }

        StartCoroutine(MoveBars(true));

        // 垃圾桶向上移动
        if (trashCan != null && enableTrashCan)
        {
            trashCan.MoveUp();
        }
    }

    /// <summary>
    /// 家具拖拽结束事件（从持有家具栏）
    /// </summary>
    private void OnFurnitureDragEnd(FurnitureData furniture, Vector2 screenPosition)
    {
        if (!isEditMode) return;

        Debug.Log($"家具拖拽结束: {furniture.name}");

        bool success = false;

        // 检查是否在垃圾桶上松手
        if (trashCan != null && enableTrashCan)
        {
            // 使用射线检测检查鼠标位置是否有垃圾桶
            var pointerData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            pointerData.position = screenPosition;

            var results = new List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerData, results);

            bool onTrashCan = false;
            foreach (var result in results)
            {
                if (result.gameObject.GetComponent<TrashCanController>() != null)
                {
                    onTrashCan = true;
                    break;
                }
            }

            if (onTrashCan)
            {
                // 触碰到垃圾桶，执行删除
                trashCan.DeleteFurniture();
                Debug.Log($"家具 {furniture.name} 被丢入垃圾桶");

                // 清理选中状态
                if (GridSystemManager.Instance != null)
                {
                    GridSystemManager.Instance.SetSelectedFurniture(null);
                }

                StartCoroutine(MoveBars(false));

                // 垃圾桶向下移动
                if (trashCan != null)
                {
                    trashCan.MoveDown();
                }

                return;
            }
        }

        // 否则尝试放置到格子
        GridCellUI targetCell = FindGridCellAtPosition(screenPosition);

        if (targetCell != null)
        {
            Vector2Int gridPos = targetCell.GridPosition;

            success = GridSystemManager.Instance?.TryPlaceFromInventory(currentRoomIndex, furniture, gridPos) ?? false;

            if (success)
            {
                AddToTempLayout(furniture, gridPos);
                Debug.Log($"家具放置成功: {furniture.name} 在位置 {gridPos}");
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySuccessSound();
            }
            else
            {
                Debug.Log($"家具放置失败: {furniture.name}");
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayErrorSound();
            }
        }
        else
        {
            Debug.Log($"未找到目标格子，家具放置失败: {furniture.name}");
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayErrorSound();
        }

        if (GridSystemManager.Instance != null)
        {
            GridSystemManager.Instance.SetSelectedFurniture(null);
        }

        StartCoroutine(MoveBars(false));

        // 垃圾桶向下移动
        if (trashCan != null && enableTrashCan)
        {
            trashCan.MoveDown();
        }
    }

    /// <summary>
    /// 家具拾取事件（从格子拾取）- 订阅自GridSystemManager
    /// </summary>
    private void OnFurniturePickup(FurnitureData furniture, Vector2Int position)
    {
        if (!isEditMode) return;

        Debug.Log($"家具拾取开始: {furniture.name} 从位置 {position}");

        if (ownedFurnitureScrollRect != null)
        {
            ownedFurnitureScrollRect.vertical = false;
            ownedFurnitureScrollRect.horizontal = false;
        }

        StartCoroutine(MoveBars(true));

        // 垃圾桶向上移动
        if (trashCan != null && enableTrashCan)
        {
            trashCan.MoveUp();
        }
    }

    /// <summary>
    /// 复位UI栏
    /// </summary>
    public void ResetUIBars()
    {
        if (isEditMode)
        {
            StartCoroutine(MoveBars(false));

            if (ownedFurnitureScrollRect != null)
            {
                ownedFurnitureScrollRect.vertical = false;
                ownedFurnitureScrollRect.horizontal = true;
            }

            // 垃圾桶向下移动
            if (trashCan != null && enableTrashCan)
            {
                trashCan.MoveDown();
            }
        }
    }

    /// <summary>
    /// 查找屏幕位置对应的格子
    /// </summary>
    public GridCellUI FindGridCellAtPosition(Vector2 screenPosition)
    {
        var pointerData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        pointerData.position = screenPosition;

        var results = new List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            GridCellUI cell = result.gameObject.GetComponent<GridCellUI>();
            if (cell != null)
            {
                return cell;
            }
        }

        foreach (var result in results)
        {
            GridCellUI cell = result.gameObject.GetComponentInParent<GridCellUI>();
            if (cell != null)
            {
                return cell;
            }
        }

        return null;
    }

    /// <summary>
    /// 添加到临时布局
    /// </summary>
    private void AddToTempLayout(FurnitureData furniture, Vector2Int gridPos)
    {
        if (!tempLayouts.ContainsKey(currentRoomIndex))
        {
            tempLayouts[currentRoomIndex] = new List<FurniturePlacementData>();
        }

        FurniturePlacementData placement = new FurniturePlacementData
        {
            furnitureId = furniture.id,
            gridX = gridPos.x,
            gridY = gridPos.y,
            roomType = currentRoomIndex
        };

        bool exists = false;
        foreach (var existing in tempLayouts[currentRoomIndex])
        {
            if (existing.gridX == gridPos.x && existing.gridY == gridPos.y)
            {
                exists = true;
                Debug.LogWarning($"位置 ({gridPos.x}, {gridPos.y}) 已有家具");
                break;
            }
        }

        if (!exists)
        {
            tempLayouts[currentRoomIndex].Add(placement);
            Debug.Log($"添加到临时布局: {furniture.name} 在位置 ({gridPos.x}, {gridPos.y})");
        }
    }

    /// <summary>
    /// 移动持有家具栏和按钮栏
    /// </summary>
    private IEnumerator MoveBars(bool moveOut)
    {
        if (isBarMoving) yield break;

        isBarMoving = true;

        RectTransform furnitureRect = ownedFurnitureRect;
        RectTransform buttonRect = buttonBarContainer?.GetComponent<RectTransform>();

        if (furnitureRect == null || buttonRect == null) yield break;

        Vector2 furnitureTarget = moveOut ?
            ownedFurnitureOriginalPos + new Vector2(0, furnitureBarOffset) :
            ownedFurnitureOriginalPos;

        Vector2 buttonTarget = moveOut ?
            buttonBarOriginalPos + new Vector2(0, buttonBarOffset) :
            buttonBarOriginalPos;

        Vector2 furnitureStart = furnitureRect.anchoredPosition;
        Vector2 buttonStart = buttonRect.anchoredPosition;

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            float curvedT = moveCurve.Evaluate(t);

            furnitureRect.anchoredPosition = Vector2.Lerp(furnitureStart, furnitureTarget, curvedT);
            buttonRect.anchoredPosition = Vector2.Lerp(buttonStart, buttonTarget, curvedT);

            yield return null;
        }

        furnitureRect.anchoredPosition = furnitureTarget;
        buttonRect.anchoredPosition = buttonTarget;

        if (!moveOut && ownedFurnitureScrollRect != null)
        {
            ownedFurnitureScrollRect.vertical = false;
            ownedFurnitureScrollRect.horizontal = true;
        }

        isBarMoving = false;
    }

    /// <summary>
    /// 加载房间布局
    /// </summary>
    private void LoadRoomLayouts()
    {
        string json = PlayerPrefs.GetString("RoomLayouts", "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                var wrapper = JsonUtility.FromJson<RoomLayoutData>(json);
                if (wrapper != null && wrapper.rooms != null)
                {
                    roomLayouts.Clear();
                    foreach (var room in wrapper.rooms)
                    {
                        if (room != null && room.placements != null)
                        {
                            List<FurniturePlacementData> placements = new List<FurniturePlacementData>();
                            foreach (var placement in room.placements)
                            {
                                if (placement != null)
                                {
                                    placements.Add(placement);
                                }
                            }
                            roomLayouts[room.roomType] = placements;
                            Debug.Log($"加载房间 {room.roomType} 布局: {placements.Count} 件家具");
                        }
                    }
                    Debug.Log($"加载房间布局成功: {roomLayouts.Count}个房间");
                }
                else
                {
                    Debug.LogWarning("JSON解析失败，数据格式可能不正确");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"加载布局失败: {e.Message}\nJSON内容: {json}");
                roomLayouts.Clear();
                PlayerPrefs.DeleteKey("RoomLayouts");
            }
        }
        else
        {
            Debug.Log("没有找到已保存的房间布局");
        }

        tempLayouts.Clear();
    }

    /// <summary>
    /// 应用已保存的布局到格子
    /// </summary>
    private void ApplyRoomLayouts()
    {
        if (GridSystemManager.Instance == null) return;

        for (int i = 0; i < roomGridContainers.Length; i++)
        {
            GridSystemManager.Instance.RemoveAllFurnitureInRoom(i);
        }

        foreach (var kvp in roomLayouts)
        {
            int roomIndex = kvp.Key;
            foreach (var placement in kvp.Value)
            {
                FurnitureData furniture = FurnitureDatabase.Instance?.GetFurnitureById(placement.furnitureId);
                if (furniture != null)
                {
                    Vector2Int gridPos = new Vector2Int(placement.gridX, placement.gridY);
                    GridSystemManager.Instance.PlaceFurniture(roomIndex, furniture, gridPos);
                    Debug.Log($"应用家具: {furniture.name} 在房间 {roomIndex} 位置 ({gridPos.x}, {gridPos.y})");
                }
            }
        }

        Debug.Log("房间布局已应用");
    }

    /// <summary>
    /// 保存布局到JSON
    /// </summary>
    private void SaveLayoutToJson()
    {
        try
        {
            foreach (var kvp in tempLayouts)
            {
                if (!roomLayouts.ContainsKey(kvp.Key))
                {
                    roomLayouts[kvp.Key] = new List<FurniturePlacementData>();
                }

                foreach (var placement in kvp.Value)
                {
                    bool exists = false;
                    foreach (var existing in roomLayouts[kvp.Key])
                    {
                        if (existing.gridX == placement.gridX && existing.gridY == placement.gridY)
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                    {
                        roomLayouts[kvp.Key].Add(placement);
                    }
                }
            }

            List<RoomData> rooms = new List<RoomData>();
            foreach (var kvp in roomLayouts)
            {
                if (kvp.Value != null && kvp.Value.Count > 0)
                {
                    RoomData room = new RoomData
                    {
                        roomType = kvp.Key,
                        placements = kvp.Value.ToArray()
                    };
                    rooms.Add(room);
                }
            }

            RoomLayoutData wrapper = new RoomLayoutData();
            wrapper.rooms = rooms.ToArray();

            string json = JsonUtility.ToJson(wrapper, true);

            if (!string.IsNullOrEmpty(json))
            {
                PlayerPrefs.SetString("RoomLayouts", json);
                PlayerPrefs.Save();

                string path = Path.Combine(Application.persistentDataPath, $"room_layout_{currentRoomIndex}.json");
                File.WriteAllText(path, json);

                Debug.Log($"布局已保存到 PlayerPrefs 和文件: {path}");
            }
            else
            {
                Debug.LogError("生成的JSON为空，保存失败");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存布局失败: {e.Message}");
        }
    }

    /// <summary>
    /// 确认布局按钮点击
    /// </summary>
    private void OnConfirmLayoutButtonClicked()
    {
        Debug.Log("点击确认布局按钮");

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySuccessSound();

        SaveLayoutToJson();
        ExitEditMode(true);

        tempLayouts.Clear();
    }

    /// <summary>
    /// 取消布局按钮点击
    /// </summary>
    private void OnCancelLayoutButtonClicked()
    {
        Debug.Log("点击取消布局按钮");

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        ClearTempPlacements();
        tempLayouts.Clear();
        ApplyRoomLayouts();
        ExitEditMode(false);
    }

    /// <summary>
    /// 进入编辑模式
    /// </summary>
    private void EnterEditMode()
    {
        if (isEditMode) return;

        isEditMode = true;

        SetAllGridsAlpha(editModeGridAlpha);
        UpdateEditModeUI();
        UpdateButtonStates();
        OnEditModeChanged?.Invoke(true);

        Debug.Log($"进入编辑模式");
    }

    /// <summary>
    /// 退出编辑模式
    /// </summary>
    private void ExitEditMode(bool saveChanges)
    {
        if (!isEditMode) return;

        isEditMode = false;

        SetAllGridsAlpha(normalGridAlpha);
        UpdateEditModeUI();
        UpdateButtonStates();
        OnEditModeChanged?.Invoke(false);

        Debug.Log($"退出编辑模式，保存更改: {saveChanges}");
    }

    /// <summary>
    /// 更新编辑模式UI
    /// </summary>
    private void UpdateEditModeUI()
    {
        if (editLayoutButton != null)
            editLayoutButton.gameObject.SetActive(!isEditMode);

        if (layoutEditingPanel != null)
            layoutEditingPanel.SetActive(isEditMode);

        if (ownedFurnitureScrollRect != null)
        {
            ownedFurnitureScrollRect.gameObject.SetActive(isEditMode);

            if (ownedFurnitureContent != null)
            {
                foreach (Transform child in ownedFurnitureContent)
                {
                    child.gameObject.SetActive(isEditMode);
                }
            }
        }

        if (buttonBarContainer != null)
        {
            buttonBarContainer.gameObject.SetActive(isEditMode);

            foreach (Transform child in buttonBarContainer)
            {
                child.gameObject.SetActive(isEditMode);
            }
        }
    }

    /// <summary>
    /// 设置所有格子的透明度
    /// </summary>
    private void SetAllGridsAlpha(float alpha)
    {
        if (GridSystemManager.Instance == null) return;

        alpha = Mathf.Clamp01(alpha);

        for (int roomIndex = 0; roomIndex < roomGridContainers.Length; roomIndex++)
        {
            GridCellUI[,] grid = GridSystemManager.Instance.GetCurrentRoomGrid(roomIndex);
            if (grid == null) continue;

            int width = grid.GetLength(0);
            int height = grid.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y] != null)
                    {
                        grid[x, y].SetEditMode(isEditMode);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 编辑布局按钮点击
    /// </summary>
    private void OnEditLayoutButtonClicked()
    {
        Debug.Log("点击编辑布局按钮");

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        EnterEditMode();
    }

    /// <summary>
    /// 左按钮点击
    /// </summary>
    private void OnLeftButtonClicked()
    {
        if (isEditMode) return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        SwitchToPreviousRoom();
    }

    /// <summary>
    /// 右按钮点击
    /// </summary>
    private void OnRightButtonClicked()
    {
        if (isEditMode) return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        SwitchToNextRoom();
    }

    /// <summary>
    /// 切换到上一个房间
    /// </summary>
    public void SwitchToPreviousRoom()
    {
        if (roomViews == null || roomViews.Length == 0 || isEditMode) return;

        int targetIndex = currentRoomIndex - 1;
        if (targetIndex >= 0)
        {
            SwitchToRoom(targetIndex);
        }
    }

    /// <summary>
    /// 切换到下一个房间
    /// </summary>
    public void SwitchToNextRoom()
    {
        if (roomViews == null || roomViews.Length == 0 || isEditMode) return;

        int targetIndex = currentRoomIndex + 1;
        if (targetIndex < roomViews.Length)
        {
            SwitchToRoom(targetIndex);
        }
    }

    /// <summary>
    /// 切换到指定房间
    /// </summary>
    public void SwitchToRoom(int roomIndex)
    {
        if (roomViews == null || roomViews.Length == 0 || isEditMode) return;
        if (roomIndex < 0 || roomIndex >= roomViews.Length || roomIndex == currentRoomIndex) return;

        Debug.Log($"切换到房间: {roomIndex} ({GetRoomName(roomIndex)})");

        currentRoomIndex = roomIndex;

        SetRoomVisibility();
        UpdateButtonStates();
        UpdateRoomNameDisplay();

        OnRoomChanged?.Invoke(currentRoomIndex);
    }

    /// <summary>
    /// 更新按钮状态
    /// </summary>
    private void UpdateButtonStates()
    {
        if (roomViews == null || roomViews.Length == 0) return;

        if (leftButton != null)
        {
            bool canGoLeft = currentRoomIndex > 0 && !isEditMode;
            leftButton.interactable = canGoLeft;
            if (leftButtonImage != null)
                leftButtonImage.color = canGoLeft ? normalButtonColor : disabledButtonColor;
        }

        if (rightButton != null)
        {
            bool canGoRight = currentRoomIndex < roomViews.Length - 1 && !isEditMode;
            rightButton.interactable = canGoRight;
            if (rightButtonImage != null)
                rightButtonImage.color = canGoRight ? normalButtonColor : disabledButtonColor;
        }

        if (editLayoutButton != null)
            editLayoutButton.interactable = !isEditMode;
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
    /// 检查是否处于编辑模式
    /// </summary>
    public bool IsEditMode()
    {
        return isEditMode;
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

        if (roomContainer != null)
        {
            if (roomViews.Length == 0)
            {
                List<GameObject> views = new List<GameObject>();
                foreach (Transform child in roomContainer)
                {
                    views.Add(child.gameObject);
                }
                roomViews = views.ToArray();
            }

            foreach (GameObject view in roomViews)
            {
                if (view != null)
                {
                    view.transform.SetParent(roomContainer, true);

                    RectTransform viewRect = view.GetComponent<RectTransform>();
                }
            }
        }
    }

    /// <summary>
    /// 初始化格子系统
    /// </summary>
    private void InitializeGridSystem()
    {
        if (GridSystemManager.Instance == null)
        {
            GameObject gridManagerObj = new GameObject("GridSystemManager");
            GridSystemManager gridManager = gridManagerObj.AddComponent<GridSystemManager>();
            gridManager.roomGridContainers = roomGridContainers;
            gridManager.roomFurnitureContainers = roomFurnitureContainers;
            gridManager.gridCellPrefab = gridCellPrefab;
        }

        StartCoroutine(DelayedGridSetup());
    }

    /// <summary>
    /// 延迟格子设置
    /// </summary>
    private IEnumerator DelayedGridSetup()
    {
        yield return null;
        yield return null;
        SetAllGridsAlpha(normalGridAlpha);
    }

    /// <summary>
    /// 清空临时放置的家具
    /// </summary>
    private void ClearTempPlacements()
    {
        if (GridSystemManager.Instance == null) return;

        GridSystemManager.Instance.RemoveAllFurnitureInRoom(currentRoomIndex);
        Debug.Log($"清空房间 {currentRoomIndex} 的所有临时家具");
    }

    /// <summary>
    /// 测试保存布局
    /// </summary>
    private void TestSaveLayout()
    {
        Debug.Log("=== 测试保存布局 ===");

        if (!tempLayouts.ContainsKey(currentRoomIndex))
        {
            tempLayouts[currentRoomIndex] = new List<FurniturePlacementData>();
        }

        if (ownedFurnitureIds.Count > 0)
        {
            FurniturePlacementData testData = new FurniturePlacementData
            {
                furnitureId = ownedFurnitureIds[0],
                gridX = 1,
                gridY = 1,
                roomType = currentRoomIndex
            };
            tempLayouts[currentRoomIndex].Add(testData);
            Debug.Log($"添加测试数据: {testData.furnitureId} 在 ({testData.gridX}, {testData.gridY})");
        }

        SaveLayoutToJson();
        Debug.Log("=== 测试结束 ===");
    }

    /// <summary>
    /// 测试加载布局
    /// </summary>
    private void TestLoadLayout()
    {
        Debug.Log("=== 测试加载布局 ===");
        LoadRoomLayouts();
        ApplyRoomLayouts();
        Debug.Log("=== 测试结束 ===");
    }
}

[System.Serializable]
public class FurniturePlacementData
{
    public string furnitureId;
    public int gridX;
    public int gridY;
    public int roomType;
}

[System.Serializable]
public class RoomData
{
    public int roomType;
    public FurniturePlacementData[] placements;
}

[System.Serializable]
public class RoomLayoutData
{
    public RoomData[] rooms;
}