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

    [Header("格子尺寸")]
    [SerializeField] private int cellSize = 100;              // 格子大小 100x100
    [SerializeField] private int columns = 10;                // 列数
    [SerializeField] private int rows = 17;                   // 行数

    [Header("边距")]
    [SerializeField] private int leftMargin = 40;              // 左边距
    [SerializeField] private int rightMargin = 40;             // 右边距
    [SerializeField] private int topMargin = 110;              // 上边距
    [SerializeField] private int bottomMargin = 110;           // 下边距

    // 格子数据
    private GridCellUI[][,] roomGrids;                         // 每个房间的格子数组
    private Dictionary<string, List<Vector2Int>> placedFurniture = new Dictionary<string, List<Vector2Int>>();

    // 当前选中的家具和格子
    private FurnitureData currentSelectedFurniture;
    private Vector2Int? hoveredGridPosition;
    private List<Vector2Int> currentPlacementCells = new List<Vector2Int>();

    // 事件
    public event System.Action<Vector2Int, GridType, bool, string> OnGridClickedEvent;
    public event System.Action<Vector2Int, GridType, bool> OnGridHoveredEvent;
    public event System.Action OnGridHoverEndEvent;

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

        // 确保容器有正确的锚点设置
        RectTransform containerRect = container.GetComponent<RectTransform>();
        if (containerRect != null)
        {
            // 设置容器锚点为左上角
            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(0, 1);
            containerRect.pivot = new Vector2(0, 1);
        }

        // 清空现有格子
        foreach (Transform child in container)
        {
            DestroyImmediate(child.gameObject); // 使用DestroyImmediate确保立即清除
        }

        // 创建格子数组
        roomGrids[roomIndex] = new GridCellUI[columns, rows];

        // 计算格子起始位置（左上角）
        float startX = leftMargin;
        float startY = -topMargin; // Y轴向下（因为左上角为原点，向下为负）

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                // 创建格子
                GameObject cellObj = Instantiate(gridCellPrefab, container);
                cellObj.name = $"GridCell_{roomIndex}_{x}_{y}";

                RectTransform rect = cellObj.GetComponent<RectTransform>();

                // 设置格子的锚点为左上角
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1); // 左上角为中心点

                // 设置位置（左上角坐标）
                rect.anchoredPosition = new Vector2(
                    startX + x * cellSize,
                    startY - y * cellSize
                );
                rect.sizeDelta = new Vector2(cellSize, cellSize);

                // 获取格子类型
                GridType gridType = GetGridTypeForPosition(roomIndex, x, y);

                // 初始化格子
                GridCellUI cell = cellObj.GetComponent<GridCellUI>();
                if (cell == null)
                    cell = cellObj.AddComponent<GridCellUI>();

                cell.Initialize(gridType, new Vector2Int(x, y), gridType != GridType.Forbidden);

                // 存储格子引用
                roomGrids[roomIndex][x, y] = cell;
            }
        }

        Debug.Log($"房间 {roomIndex} 格子初始化完成: {columns}x{rows}，左上角起始位置 ({startX}, {startY})");
    }

    /// <summary>
    /// 获取指定位置的格子类型
    /// </summary>
    private GridType GetGridTypeForPosition(int roomIndex, int x, int y)
    {
        // 根据房间索引设置格子类型
        // roomIndex: 0=户外, 1=书房, 2=客厅, 3=卧室
        switch (roomIndex)
        {
            case 0: // 户外
                // 上方7行禁止
                if (y < 7)
                    return GridType.Forbidden;

                // 中间两列禁止（列4和5，索引从0开始）
                if (x >= 4 && x <= 5)
                    return GridType.Forbidden;

                // 其余为户外格
                return GridType.Outdoor;

            case 1: // 书房
            case 2: // 客厅
            case 3: // 卧室
            default:
                // 其他房间
                // 上方7行为墙面格
                if (y < 7)
                    return GridType.Wall;

                // 下方10行为地板格
                return GridType.Floor;
        }
    }

    /// <summary>
    /// 获取指定房间的格子
    /// </summary>
    public GridCellUI[,] GetCurrentRoomGrid(int roomIndex)
    {
        // 如果还没初始化，先初始化
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

    /// <summary>
    /// 检查家具是否可以放置在指定位置
    /// </summary>
    public bool CanPlaceFurniture(int roomIndex, FurnitureData furniture, Vector2Int topLeftCell)
    {
        if (furniture == null) return false;

        GridCellUI[,] grid = GetCurrentRoomGrid(roomIndex);
        if (grid == null) return false;

        int width = furniture.width;
        int height = furniture.height;

        // 检查是否超出边界
        if (topLeftCell.x < 0 || topLeftCell.y < 0 ||
            topLeftCell.x + width > columns || topLeftCell.y + height > rows)
        {
            return false;
        }

        // 检查每个格子
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int checkX = topLeftCell.x + x;
                int checkY = topLeftCell.y + y;

                GridCellUI cell = grid[checkX, checkY];

                // 格子必须存在且可放置
                if (cell == null || !cell.IsSelectable)
                    return false;

                // 格子不能被占用
                if (cell.IsOccupied)
                    return false;

                // 检查格子类型需求
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

    /// <summary>
    /// 放置家具
    /// </summary>
    public bool PlaceFurniture(int roomIndex, FurnitureData furniture, Vector2Int topLeftCell)
    {
        if (!CanPlaceFurniture(roomIndex, furniture, topLeftCell))
            return false;

        GridCellUI[,] grid = GetCurrentRoomGrid(roomIndex);
        List<Vector2Int> occupiedCells = new List<Vector2Int>();

        // 占用格子
        for (int x = 0; x < furniture.width; x++)
        {
            for (int y = 0; y < furniture.height; y++)
            {
                int placeX = topLeftCell.x + x;
                int placeY = topLeftCell.y + y;

                grid[placeX, placeY].PlaceFurniture(furniture.id, furniture.icon);
                occupiedCells.Add(new Vector2Int(placeX, placeY));
            }
        }

        // 记录放置的家具
        placedFurniture[furniture.id] = occupiedCells;

        Debug.Log($"放置家具: {furniture.name} 在房间 {roomIndex} 位置 {topLeftCell}");
        return true;
    }

    /// <summary>
    /// 移除家具
    /// </summary>
    public void RemoveFurniture(int roomIndex, string furnitureId)
    {
        if (!placedFurniture.ContainsKey(furnitureId)) return;

        GridCellUI[,] grid = GetCurrentRoomGrid(roomIndex);

        foreach (var pos in placedFurniture[furnitureId])
        {
            grid[pos.x, pos.y].RemoveFurniture();
        }

        placedFurniture.Remove(furnitureId);
        Debug.Log($"移除家具: {furnitureId}");
    }

    /// <summary>
    /// 高亮显示家具放置区域
    /// </summary>
    public void HighlightPlacementArea(int roomIndex, FurnitureData furniture, Vector2Int hoverCell)
    {
        // 清除之前的高亮
        ClearHighlight();

        if (furniture == null) return;

        GridCellUI[,] grid = GetCurrentRoomGrid(roomIndex);
        if (grid == null) return;

        // 计算以hoverCell为左上角的家具覆盖区域
        int startX = hoverCell.x;
        int startY = hoverCell.y;

        bool canPlace = CanPlaceFurniture(roomIndex, furniture, hoverCell);

        // 高亮所有相关格子
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

    /// <summary>
    /// 清除所有高亮
    /// </summary>
    public void ClearHighlight()
    {
        foreach (var pos in currentPlacementCells)
        {
            // 需要知道是哪个房间...
            // 简化处理：遍历所有房间
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

    /// <summary>
    /// 设置当前选中的家具
    /// </summary>
    public void SetSelectedFurniture(FurnitureData furniture)
    {
        currentSelectedFurniture = furniture;
    }

    // 事件触发方法
    public void OnGridClicked(Vector2Int position, GridType type, bool occupied, string furnitureId)
    {
        OnGridClickedEvent?.Invoke(position, type, occupied, furnitureId);
    }

    public void OnGridHovered(Vector2Int position, GridType type, bool occupied)
    {
        hoveredGridPosition = position;
        OnGridHoveredEvent?.Invoke(position, type, occupied);

        // 如果有选中的家具，显示放置预览
        if (currentSelectedFurniture != null)
        {
            // 需要知道当前房间索引
            int currentRoom = FindObjectOfType<HomeSystemController>()?.GetCurrentRoomIndex() ?? 0;
            HighlightPlacementArea(currentRoom, currentSelectedFurniture, position);
        }
    }

    public void OnGridHoverEnd()
    {
        hoveredGridPosition = null;
        OnGridHoverEndEvent?.Invoke();

        // 清除放置预览
        ClearHighlight();
    }

    /// <summary>
    /// 检查是否已初始化
    /// </summary>
    public bool IsInitialized()
    {
        return isInitialized;
    }

    /// <summary>
    /// 根据家具ID获取家具数据
    /// </summary>
    public FurnitureData GetFurnitureData(string furnitureId)
    {
        if (FurnitureDatabase.Instance != null)
        {
            return FurnitureDatabase.Instance.GetFurnitureById(furnitureId);
        }
        return null;
    }

    /// <summary>
    /// 获取家具的格子需求
    /// </summary>
    public GridRequirement[] GetFurnitureGridRequirements(string furnitureId)
    {
        FurnitureData data = GetFurnitureData(furnitureId);
        if (data != null)
        {
            return data.gridRequirements;
        }
        return null;
    }
}
