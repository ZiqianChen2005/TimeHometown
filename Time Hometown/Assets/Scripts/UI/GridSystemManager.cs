using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GridSystemManager : MonoBehaviour
{
    public static GridSystemManager Instance { get; private set; }

    [Header("格子预制体")]
    [SerializeField] public GameObject gridCellPrefab;

    [Header("房间视图")]
    [SerializeField] public Transform[] roomGridContainers; // 每个房间的格子容器
    [SerializeField] public Transform[] roomFurnitureContainers; // 每个房间的家具容器

    [Header("格子尺寸")]
    [SerializeField] private int cellSize = 100;              // 格子大小 100x100
    [SerializeField] private int columns = 10;                // 列数
    [SerializeField] private int rows = 17;                   // 行数

    [Header("边距")]
    [SerializeField] private int leftMargin = 40;              // 左边距
    [SerializeField] private int rightMargin = 40;             // 右边距
    [SerializeField] private int topMargin = 110;              // 上边距
    [SerializeField] private int bottomMargin = 110;           // 下边距

    [Header("高亮设置")]
    [SerializeField] private bool highlightOnHover = true;     // 悬停时高亮

    // 格子数据
    private GridCellUI[][,] roomGrids;                         // 每个房间的格子数组

    // 当前选中的家具和格子
    private FurnitureData currentSelectedFurniture;
    private Vector2Int? hoveredGridPosition;
    private GridCellUI currentlyHighlightedCell;               // 当前高亮的格子
    private List<Vector2Int> currentPlacementCells = new List<Vector2Int>();

    // 家具移动相关
    private bool isMovingFurniture = false;
    private FurniturePlacementInfo movingFurnitureInfo;
    private Vector2Int originalTopLeftCell;                     // 原始位置

    // 事件
    public event System.Action<Vector2Int, GridType, bool, string> OnGridClickedEvent;
    public event System.Action<Vector2Int, GridType, bool> OnGridHoveredEvent;
    public event System.Action OnGridHoverEndEvent;
    public event System.Action<FurnitureData, Vector2Int> OnFurniturePickupEvent; // 家具拾取事件

    // 标记是否已初始化
    private bool isInitialized = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        InitializeAllRoomGrids();
    }

    /// <summary>
    /// 初始化所有房间的格子（公开方法，供外部调用）
    /// </summary>
    public void InitializeAllRoomGrids()
    {
        if (isInitialized) return;

        if (roomGridContainers == null || roomGridContainers.Length == 0)
        {
            Debug.LogError("未设置房间格子容器");
            return;
        }

        roomGrids = new GridCellUI[roomGridContainers.Length][,];

        for (int roomIndex = 0; roomIndex < roomGridContainers.Length; roomIndex++)
        {
            InitializeRoomGrid(roomIndex);
        }

        isInitialized = true;
        Debug.Log($"已初始化 {roomGridContainers.Length} 个房间的格子系统");
    }

    /// <summary>
    /// 初始化指定房间的格子
    /// </summary>
    private void InitializeRoomGrid(int roomIndex)
    {
        if (roomIndex < 0 || roomIndex >= roomGridContainers.Length) return;

        Transform container = roomGridContainers[roomIndex];
        if (container == null) return;

        RectTransform containerRect = container.GetComponent<RectTransform>();
        if (containerRect != null)
        {
            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(0, 1);
            containerRect.pivot = new Vector2(0, 1);
            containerRect.anchoredPosition = Vector2.zero;
        }

        if (roomIndex < roomFurnitureContainers.Length && roomFurnitureContainers[roomIndex] != null)
        {
            RectTransform furnitureContainerRect = roomFurnitureContainers[roomIndex].GetComponent<RectTransform>();
            if (furnitureContainerRect != null)
            {
                furnitureContainerRect.anchorMin = new Vector2(0, 1);
                furnitureContainerRect.anchorMax = new Vector2(0, 1);
                furnitureContainerRect.pivot = new Vector2(0, 1);
                furnitureContainerRect.anchoredPosition = Vector2.zero;
            }
        }

        foreach (Transform child in container)
        {
            DestroyImmediate(child.gameObject);
        }

        roomGrids[roomIndex] = new GridCellUI[columns, rows];

        float startX = leftMargin;
        float startY = -topMargin;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                GameObject cellObj = Instantiate(gridCellPrefab, container);
                cellObj.name = $"GridCell_{roomIndex}_{x}_{y}";

                RectTransform rect = cellObj.GetComponent<RectTransform>();

                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);

                rect.anchoredPosition = new Vector2(
                    startX + x * cellSize,
                    startY - y * cellSize
                );
                rect.sizeDelta = new Vector2(cellSize, cellSize);

                GridType gridType = GetGridTypeForPosition(roomIndex, x, y);

                GridCellUI cell = cellObj.GetComponent<GridCellUI>();
                if (cell == null)
                    cell = cellObj.AddComponent<GridCellUI>();

                cell.Initialize(gridType, new Vector2Int(x, y), gridType != GridType.Forbidden);

                roomGrids[roomIndex][x, y] = cell;
            }
        }

        Debug.Log($"房间 {roomIndex} 格子初始化完成: {columns}x{rows}");
    }

    /// <summary>
    /// 创建家具图标
    /// </summary>
    private GameObject CreateFurnitureIcon(int roomIndex, FurnitureData furniture, Vector2Int topLeftCell, string instanceId)
    {
        if (roomIndex < 0 || roomIndex >= roomFurnitureContainers.Length) return null;

        Transform container = roomFurnitureContainers[roomIndex];
        if (container == null) return null;

        // 使用实例ID确保每个家具图标有唯一名称
        GameObject iconObj = new GameObject($"Furniture_{furniture.id}_{instanceId}");
        iconObj.transform.SetParent(container, false);

        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.sprite = furniture.icon;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        RectTransform rect = iconObj.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);

        float posX = leftMargin + topLeftCell.x * cellSize;
        float posY = -topMargin - topLeftCell.y * cellSize;
        rect.anchoredPosition = new Vector2(posX, posY);

        rect.sizeDelta = new Vector2(furniture.width * cellSize, furniture.height * cellSize);

        return iconObj;
    }

    private GridType GetGridTypeForPosition(int roomIndex, int x, int y)
    {
        switch (roomIndex)
        {
            case 0: // 户外
                if (y < 7)
                    return GridType.Forbidden;
                if (x >= 4 && x <= 5)
                    return GridType.Forbidden;
                return GridType.Outdoor;

            case 1: // 书房
            case 2: // 客厅
            case 3: // 卧室
            default:
                if (y < 7)
                    return GridType.Wall;
                return GridType.Floor;
        }
    }

    public GridCellUI[,] GetCurrentRoomGrid(int roomIndex)
    {
        if (!isInitialized)
        {
            InitializeAllRoomGrids();
        }

        if (roomGrids == null || roomIndex < 0 || roomIndex >= roomGrids.Length)
        {
            Debug.LogError($"获取格子失败: roomGrids is null 或 roomIndex {roomIndex} 超出范围");
            return null;
        }

        return roomGrids[roomIndex];
    }

    public bool CanPlaceFurniture(int roomIndex, FurnitureData furniture, Vector2Int topLeftCell)
    {
        if (furniture == null) return false;

        GridCellUI[,] grid = GetCurrentRoomGrid(roomIndex);
        if (grid == null) return false;

        int width = furniture.width;
        int height = furniture.height;

        if (topLeftCell.x < 0 || topLeftCell.y < 0 ||
            topLeftCell.x + width > columns || topLeftCell.y + height > rows)
        {
            return false;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int checkX = topLeftCell.x + x;
                int checkY = topLeftCell.y + y;

                GridCellUI cell = grid[checkX, checkY];

                if (cell == null || !cell.IsSelectable)
                    return false;

                // 移动模式下的特殊处理
                if (isMovingFurniture && movingFurnitureInfo != null)
                {
                    // 检查是否在原始位置范围内
                    bool isOriginalCell = false;
                    foreach (var pos in movingFurnitureInfo.occupiedCells)
                    {
                        if (pos.x == checkX && pos.y == checkY)
                        {
                            isOriginalCell = true;
                            break;
                        }
                    }

                    // 如果不是原始位置的格子且被占用，则不可放置
                    if (!isOriginalCell && cell.IsOccupied)
                        return false;
                }
                else if (cell.IsOccupied)
                {
                    return false;
                }

                bool typeValid = false;
                foreach (var req in furniture.gridRequirements)
                {
                    if (cell.GridType == req.requiredType)
                    {
                        typeValid = true;
                        break;
                    }
                }

                if (!typeValid)
                    return false;
            }
        }

        return true;
    }

    public void HighlightSingleCell(int roomIndex, Vector2Int cellPos)
    {
        ClearAllHighlights();

        GridCellUI[,] grid = GetCurrentRoomGrid(roomIndex);
        if (grid == null) return;

        if (cellPos.x >= 0 && cellPos.x < columns && cellPos.y >= 0 && cellPos.y < rows)
        {
            GridCellUI cell = grid[cellPos.x, cellPos.y];
            if (cell != null)
            {
                cell.SetHighlight(true);
                currentlyHighlightedCell = cell;
            }
        }
    }

    public void HighlightPlacementArea(int roomIndex, FurnitureData furniture, Vector2Int hoverCell)
    {
        ClearAllHighlights();

        if (furniture == null) return;

        GridCellUI[,] grid = GetCurrentRoomGrid(roomIndex);
        if (grid == null) return;

        int startX = hoverCell.x;
        int startY = hoverCell.y;

        bool canPlace = CanPlaceFurniture(roomIndex, furniture, hoverCell);

        for (int x = 0; x < furniture.width; x++)
        {
            for (int y = 0; y < furniture.height; y++)
            {
                int checkX = startX + x;
                int checkY = startY + y;

                if (checkX >= 0 && checkX < columns && checkY >= 0 && checkY < rows)
                {
                    grid[checkX, checkY].SetHighlight(canPlace);
                    currentPlacementCells.Add(new Vector2Int(checkX, checkY));
                }
            }
        }
    }

    public void ClearAllHighlights()
    {
        if (currentlyHighlightedCell != null)
        {
            currentlyHighlightedCell.ClearHighlight();
            currentlyHighlightedCell = null;
        }

        foreach (var pos in currentPlacementCells)
        {
            for (int r = 0; r < roomGrids.Length; r++)
            {
                if (roomGrids[r] != null &&
                    pos.x >= 0 && pos.x < columns &&
                    pos.y >= 0 && pos.y < rows)
                {
                    roomGrids[r][pos.x, pos.y]?.ClearHighlight();
                }
            }
        }
        currentPlacementCells.Clear();
    }

    public void SetSelectedFurniture(FurnitureData furniture)
    {
        currentSelectedFurniture = furniture;
    }

    public void OnGridClicked(Vector2Int position, GridType type, bool occupied, string furnitureId)
    {
        OnGridClickedEvent?.Invoke(position, type, occupied, furnitureId);
    }

    public void OnGridHovered(Vector2Int position, GridType type, bool occupied)
    {
        hoveredGridPosition = position;
        OnGridHoveredEvent?.Invoke(position, type, occupied);

        HomeSystemController homeSystem = FindObjectOfType<HomeSystemController>();
        int currentRoom = homeSystem?.GetCurrentRoomIndex() ?? 0;
        bool isEditMode = homeSystem?.IsEditMode() ?? false;

        if (!isEditMode) return;

        if (isMovingFurniture && movingFurnitureInfo != null)
        {
            // 移动模式下，显示放置预览
            HighlightPlacementArea(currentRoom, movingFurnitureInfo.furnitureData, position);
        }
        else if (currentSelectedFurniture != null)
        {
            HighlightPlacementArea(currentRoom, currentSelectedFurniture, position);
        }
        else if (highlightOnHover)
        {
            HighlightSingleCell(currentRoom, position);
        }
    }

    public void OnGridHoverEnd()
    {
        hoveredGridPosition = null;
        OnGridHoverEndEvent?.Invoke();

        HomeSystemController homeSystem = FindObjectOfType<HomeSystemController>();
        bool isEditMode = homeSystem?.IsEditMode() ?? false;

        if (isEditMode)
        {
            ClearAllHighlights();
        }
    }

    /// <summary>
    /// 家具拾取事件（按下时触发）
    /// </summary>
    public void OnFurniturePickup(Vector2Int gridPos, string furnitureIdWithInstance)
    {
        // 如果已经在移动中，不重复触发
        if (isMovingFurniture) return;

        HomeSystemController homeSystem = FindObjectOfType<HomeSystemController>();
        int currentRoom = homeSystem?.GetCurrentRoomIndex() ?? 0;

        // 解析实例ID
        string[] parts = furnitureIdWithInstance.Split('_');
        if (parts.Length < 2) return;

        string instanceId = parts[parts.Length - 1];
        string furnitureId = furnitureIdWithInstance.Substring(0, furnitureIdWithInstance.Length - instanceId.Length - 1);

        // 查找家具实例信息
        if (!placedFurnitureInfo.ContainsKey(furnitureId)) return;

        FurniturePlacementInfo infoToMove = null;
        foreach (var info in placedFurnitureInfo[furnitureId])
        {
            if (info.instanceId == instanceId && info.roomIndex == currentRoom)
            {
                infoToMove = info;
                break;
            }
        }

        if (infoToMove == null) return;

        // 按下时立即开始移动家具
        StartMovingFurniture(currentRoom, infoToMove);

        OnFurniturePickupEvent?.Invoke(infoToMove.furnitureData, infoToMove.topLeftCell);
    }

    /// <summary>
    /// 开始移动家具（从格子拾取）
    /// </summary>
    private void StartMovingFurniture(int roomIndex, FurniturePlacementInfo info)
    {
        if (isMovingFurniture) return;

        isMovingFurniture = true;
        movingFurnitureInfo = info;
        originalTopLeftCell = info.topLeftCell;

        // 移除原家具图标
        if (info.furnitureIcon != null)
        {
            Destroy(info.furnitureIcon);
        }

        // 从字典中移除该实例（暂时）
        placedFurnitureInfo[info.furnitureId].Remove(info);
        if (placedFurnitureInfo[info.furnitureId].Count == 0)
        {
            placedFurnitureInfo.Remove(info.furnitureId);
        }

        // 清除格子占用
        GridCellUI[,] grid = GetCurrentRoomGrid(roomIndex);
        if (grid != null)
        {
            foreach (var pos in info.occupiedCells)
            {
                if (pos.x >= 0 && pos.x < columns && pos.y >= 0 && pos.y < rows)
                {
                    GridCellUI cell = grid[pos.x, pos.y];
                    if (cell != null)
                    {
                        cell.RemoveFurniture(info.originalGridTypes[pos]);
                    }
                }
            }
        }

        Debug.Log($"开始移动家具: {info.furnitureData.name} (实例 {info.instanceId})");
    }

    /// <summary>
    /// 尝试完成移动家具（松手时调用）
    /// </summary>
    public bool TryFinishMovingFurniture(int roomIndex, Vector2Int targetCell)
    {
        if (!isMovingFurniture || movingFurnitureInfo == null) return false;

        HomeSystemController homeSystem = FindObjectOfType<HomeSystemController>();
        bool isEditMode = homeSystem?.IsEditMode() ?? false;

        if (!isEditMode)
        {
            CancelMovingFurniture();
            return false;
        }

        // 检查新位置是否可放置
        if (!CanPlaceFurniture(roomIndex, movingFurnitureInfo.furnitureData, targetCell))
        {
            Debug.Log("新位置不可放置，取消移动");
            CancelMovingFurniture();
            return false;
        }

        // 在新位置放置家具
        GridCellUI[,] grid = GetCurrentRoomGrid(roomIndex);
        if (grid == null) return false;

        List<Vector2Int> newOccupiedCells = new List<Vector2Int>();
        Dictionary<Vector2Int, GridType> newOriginalGridTypes = new Dictionary<Vector2Int, GridType>();

        for (int x = 0; x < movingFurnitureInfo.furnitureData.width; x++)
        {
            for (int y = 0; y < movingFurnitureInfo.furnitureData.height; y++)
            {
                int placeX = targetCell.x + x;
                int placeY = targetCell.y + y;

                if (placeX >= 0 && placeX < columns && placeY >= 0 && placeY < rows)
                {
                    GridCellUI cell = grid[placeX, placeY];
                    if (cell != null)
                    {
                        Vector2Int pos = new Vector2Int(placeX, placeY);
                        newOriginalGridTypes[pos] = cell.GetOriginalGridType();

                        if (movingFurnitureInfo.furnitureData.providesNewGrids)
                        {
                            cell.PlaceFurniture(movingFurnitureInfo.furnitureData.id + "_" + movingFurnitureInfo.instanceId,
                                true, movingFurnitureInfo.furnitureData.providedGridType);
                        }
                        else
                        {
                            cell.PlaceFurniture(movingFurnitureInfo.furnitureData.id + "_" + movingFurnitureInfo.instanceId, false);
                        }

                        newOccupiedCells.Add(new Vector2Int(placeX, placeY));
                    }
                }
            }
        }

        // 创建新图标
        GameObject newIcon = CreateFurnitureIcon(roomIndex, movingFurnitureInfo.furnitureData, targetCell, movingFurnitureInfo.instanceId);

        // 更新家具信息
        movingFurnitureInfo.topLeftCell = targetCell;
        movingFurnitureInfo.occupiedCells = newOccupiedCells;
        movingFurnitureInfo.originalGridTypes = newOriginalGridTypes;
        movingFurnitureInfo.furnitureIcon = newIcon;

        // 重新添加到字典
        if (!placedFurnitureInfo.ContainsKey(movingFurnitureInfo.furnitureId))
        {
            placedFurnitureInfo[movingFurnitureInfo.furnitureId] = new List<FurniturePlacementInfo>();
        }
        placedFurnitureInfo[movingFurnitureInfo.furnitureId].Add(movingFurnitureInfo);

        Debug.Log($"完成移动家具: {movingFurnitureInfo.furnitureData.name} 到位置 {targetCell}");

        // 重置移动状态
        isMovingFurniture = false;
        movingFurnitureInfo = null;

        return true;
    }

    /// <summary>
    /// 取消移动家具（可选项：是否删除）
    /// </summary>
    public void CancelMovingFurniture(bool delete = false)
    {
        if (!isMovingFurniture || movingFurnitureInfo == null) return;

        if (!delete)
        {
            // 恢复原家具
            HomeSystemController homeSystem = FindObjectOfType<HomeSystemController>();
            int currentRoom = homeSystem?.GetCurrentRoomIndex() ?? 0;

            // 重新放置到原位置
            PlaceFurniture(currentRoom, movingFurnitureInfo.furnitureData, originalTopLeftCell);

            Debug.Log($"取消移动家具: {movingFurnitureInfo.furnitureData.name}，回到原位");
        }
        else
        {
            Debug.Log($"删除家具: {movingFurnitureInfo.furnitureData.name}");
            // 不需要恢复，直接丢弃
        }

        // 重置移动状态
        isMovingFurniture = false;
        movingFurnitureInfo = null;
    }

    /// <summary>
    /// 开始从持有家具栏拖拽
    /// </summary>
    public void StartDraggingFromInventory(FurnitureData furniture)
    {
        // 从持有家具栏拖拽不需要移动状态，只需要记录选中家具用于预览
        currentSelectedFurniture = furniture;
        Debug.Log($"开始从持有家具栏拖拽: {furniture.name}");
    }

    /// <summary>
    /// 尝试从持有家具栏放置
    /// </summary>
    public bool TryPlaceFromInventory(int roomIndex, FurnitureData furniture, Vector2Int targetCell)
    {
        if (furniture == null) return false;

        // 检查新位置是否可放置
        if (!CanPlaceFurniture(roomIndex, furniture, targetCell))
        {
            Debug.Log("位置不可放置");
            return false;
        }

        // 创建新家具实例
        string instanceId = System.Guid.NewGuid().ToString();

        GridCellUI[,] grid = GetCurrentRoomGrid(roomIndex);
        if (grid == null) return false;

        List<Vector2Int> occupiedCells = new List<Vector2Int>();
        Dictionary<Vector2Int, GridType> originalGridTypes = new Dictionary<Vector2Int, GridType>();

        for (int x = 0; x < furniture.width; x++)
        {
            for (int y = 0; y < furniture.height; y++)
            {
                int placeX = targetCell.x + x;
                int placeY = targetCell.y + y;

                if (placeX >= 0 && placeX < columns && placeY >= 0 && placeY < rows)
                {
                    GridCellUI cell = grid[placeX, placeY];
                    if (cell != null)
                    {
                        Vector2Int pos = new Vector2Int(placeX, placeY);
                        originalGridTypes[pos] = cell.GetOriginalGridType();

                        if (furniture.providesNewGrids)
                        {
                            cell.PlaceFurniture(furniture.id + "_" + instanceId, true, furniture.providedGridType);
                        }
                        else
                        {
                            cell.PlaceFurniture(furniture.id + "_" + instanceId, false);
                        }

                        occupiedCells.Add(new Vector2Int(placeX, placeY));
                    }
                }
            }
        }

        GameObject newIcon = CreateFurnitureIcon(roomIndex, furniture, targetCell, instanceId);

        FurniturePlacementInfo placementInfo = new FurniturePlacementInfo
        {
            instanceId = instanceId,
            furnitureId = furniture.id,
            furnitureData = furniture,
            topLeftCell = targetCell,
            occupiedCells = occupiedCells,
            originalGridTypes = originalGridTypes,
            furnitureIcon = newIcon,
            roomIndex = roomIndex
        };

        if (!placedFurnitureInfo.ContainsKey(furniture.id))
        {
            placedFurnitureInfo[furniture.id] = new List<FurniturePlacementInfo>();
        }
        placedFurnitureInfo[furniture.id].Add(placementInfo);

        Debug.Log($"从持有家具栏放置成功: {furniture.name} (实例 {instanceId}) 在位置 {targetCell}");

        return true;
    }

    /// <summary>
    /// 获取当前是否在移动家具
    /// </summary>
    public bool IsMovingFurniture()
    {
        return isMovingFurniture;
    }

    /// <summary>
    /// 获取当前移动的家具信息
    /// </summary>
    public FurniturePlacementInfo GetMovingFurnitureInfo()
    {
        return movingFurnitureInfo;
    }

    public bool IsInitialized()
    {
        return isInitialized;
    }

    public FurnitureData GetFurnitureData(string furnitureId)
    {
        if (FurnitureDatabase.Instance != null)
        {
            return FurnitureDatabase.Instance.GetFurnitureById(furnitureId);
        }
        return null;
    }

    public GridRequirement[] GetFurnitureGridRequirements(string furnitureId)
    {
        FurnitureData data = GetFurnitureData(furnitureId);
        if (data != null)
        {
            return data.gridRequirements;
        }
        return null;
    }

    private Dictionary<string, List<FurniturePlacementInfo>> placedFurnitureInfo = new Dictionary<string, List<FurniturePlacementInfo>>();

    /// <summary>
    /// 放置家具 - 允许同一家具重复放置
    /// </summary>
    public bool PlaceFurniture(int roomIndex, FurnitureData furniture, Vector2Int topLeftCell)
    {
        if (!CanPlaceFurniture(roomIndex, furniture, topLeftCell))
        {
            Debug.LogWarning($"无法放置家具: {furniture.name} 在位置 ({topLeftCell.x}, {topLeftCell.y})");
            return false;
        }

        GridCellUI[,] grid = GetCurrentRoomGrid(roomIndex);
        if (grid == null)
        {
            Debug.LogError($"房间 {roomIndex} 的格子数组为空");
            return false;
        }

        List<Vector2Int> occupiedCells = new List<Vector2Int>();
        string furnitureId = furniture.id;
        string instanceId = System.Guid.NewGuid().ToString();

        Dictionary<Vector2Int, GridType> originalGridTypes = new Dictionary<Vector2Int, GridType>();

        for (int x = 0; x < furniture.width; x++)
        {
            for (int y = 0; y < furniture.height; y++)
            {
                int placeX = topLeftCell.x + x;
                int placeY = topLeftCell.y + y;

                if (placeX >= 0 && placeX < columns && placeY >= 0 && placeY < rows)
                {
                    GridCellUI cell = grid[placeX, placeY];
                    if (cell != null)
                    {
                        Vector2Int pos = new Vector2Int(placeX, placeY);
                        originalGridTypes[pos] = cell.GetOriginalGridType();

                        if (furniture.providesNewGrids)
                        {
                            cell.PlaceFurniture(furnitureId + "_" + instanceId, true, furniture.providedGridType);
                        }
                        else
                        {
                            cell.PlaceFurniture(furnitureId + "_" + instanceId, false);
                        }

                        occupiedCells.Add(new Vector2Int(placeX, placeY));
                    }
                }
            }
        }

        GameObject furnitureIcon = CreateFurnitureIcon(roomIndex, furniture, topLeftCell, instanceId);

        FurniturePlacementInfo placementInfo = new FurniturePlacementInfo
        {
            instanceId = instanceId,
            furnitureId = furnitureId,
            furnitureData = furniture,
            topLeftCell = topLeftCell,
            occupiedCells = occupiedCells,
            originalGridTypes = originalGridTypes,
            furnitureIcon = furnitureIcon,
            roomIndex = roomIndex
        };

        if (!placedFurnitureInfo.ContainsKey(furnitureId))
        {
            placedFurnitureInfo[furnitureId] = new List<FurniturePlacementInfo>();
        }
        placedFurnitureInfo[furnitureId].Add(placementInfo);

        Debug.Log($"放置家具成功: {furniture.name} (实例 {instanceId}) 在房间 {roomIndex} 位置 {topLeftCell}");
        return true;
    }

    /// <summary>
    /// 移除家具的特定实例
    /// </summary>
    public void RemoveFurnitureInstance(int roomIndex, string furnitureId, string instanceId)
    {
        if (!placedFurnitureInfo.ContainsKey(furnitureId))
        {
            Debug.LogWarning($"找不到家具: {furnitureId}");
            return;
        }

        GridCellUI[,] grid = GetCurrentRoomGrid(roomIndex);
        if (grid == null) return;

        FurniturePlacementInfo infoToRemove = null;
        foreach (var info in placedFurnitureInfo[furnitureId])
        {
            if (info.instanceId == instanceId && info.roomIndex == roomIndex)
            {
                infoToRemove = info;
                break;
            }
        }

        if (infoToRemove == null)
        {
            Debug.LogWarning($"找不到家具实例: {furnitureId}_{instanceId} 在房间 {roomIndex}");
            return;
        }

        // 删除家具图标
        if (infoToRemove.furnitureIcon != null)
        {
            Destroy(infoToRemove.furnitureIcon);
        }

        // 恢复格子
        foreach (var pos in infoToRemove.occupiedCells)
        {
            if (pos.x >= 0 && pos.x < columns && pos.y >= 0 && pos.y < rows)
            {
                GridCellUI cell = grid[pos.x, pos.y];
                if (cell != null)
                {
                    GridType originalType = infoToRemove.originalGridTypes[pos];
                    cell.RemoveFurniture(originalType);
                }
            }
        }

        placedFurnitureInfo[furnitureId].Remove(infoToRemove);
        if (placedFurnitureInfo[furnitureId].Count == 0)
        {
            placedFurnitureInfo.Remove(furnitureId);
        }

        Debug.Log($"移除家具实例: {furnitureId}_{instanceId} 在房间 {roomIndex}");
    }

    /// <summary>
    /// 移除房间内的所有家具（用于取消编辑）
    /// </summary>
    public void RemoveAllFurnitureInRoom(int roomIndex)
    {
        List<string> furnitureIdsToRemove = new List<string>();
        List<FurniturePlacementInfo> instancesToRemove = new List<FurniturePlacementInfo>();

        foreach (var kvp in placedFurnitureInfo)
        {
            foreach (var info in kvp.Value)
            {
                if (info.roomIndex == roomIndex)
                {
                    instancesToRemove.Add(info);
                }
            }
        }

        foreach (var info in instancesToRemove)
        {
            RemoveFurnitureInstance(roomIndex, info.furnitureId, info.instanceId);
        }
    }

    [System.Serializable]
    public class FurniturePlacementInfo
    {
        public string instanceId;           // 实例ID
        public string furnitureId;          // 家具ID
        public FurnitureData furnitureData; // 家具数据
        public Vector2Int topLeftCell;      // 左上角格子位置
        public List<Vector2Int> occupiedCells; // 占用的格子列表
        public Dictionary<Vector2Int, GridType> originalGridTypes; // 每个格子的原始类型
        public GameObject furnitureIcon;    // 家具图标
        public int roomIndex;                // 所在房间
    }
}