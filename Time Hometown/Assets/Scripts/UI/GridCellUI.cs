using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class GridCellUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("格子组件")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image highlightImage;      // 高亮图片组件
    [SerializeField] private Text gridInfoText;         // 调试用，可隐藏

    [Header("格子颜色")]
    [SerializeField] private Color floorColor = new Color(1f, 0.92f, 0.016f, 1f);      // 黄色 #FFFF00
    [SerializeField] private Color tableColor = new Color(0.5f, 0.5f, 0.5f, 1f);       // 灰色 #808080
    [SerializeField] private Color wallColor = new Color(0f, 0.5f, 1f, 1f);            // 蓝色 #0080FF
    [SerializeField] private Color outdoorColor = new Color(0f, 0.8f, 0f, 1f);         // 绿色 #00CC00
    [SerializeField] private Color decorationColor = new Color(0.8f, 0f, 0.8f, 1f);    // 紫色 #CC00CC
    [SerializeField] private Color forbiddenColor = new Color(1f, 0f, 0f, 0.5f);       // 红色半透明

    [Header("编辑模式")]
    [SerializeField] private float normalAlpha = 0f;           // 正常模式透明度
    [SerializeField] private float editModeAlpha = 1f;         // 编辑模式透明度

    // 格子数据
    private GridType gridType;
    private Vector2Int gridPosition;
    private bool isOccupied = false;
    private string placedFurnitureId;
    private bool isSelectable = true;
    private bool isHighlighted = false;

    // 叠加状态
    [SerializeField] private bool canStack = true;              // 是否可以叠加放置（默认true）
    [SerializeField] private List<string> stackedFurnitureIds = new List<string>(); // 叠加的家具ID列表

    // 状态
    private bool isSelected = false;
    private bool isValidPlacement = false;
    private bool isEditMode = false;        // 是否处于编辑模式
    private Color originalColor;             // 原始颜色（不包含透明度）

    // 拖拽相关
    private bool isPointerDown = false;

    private void Awake()
    {
        // 确保必要组件存在
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (backgroundImage == null)
            backgroundImage = gameObject.AddComponent<Image>();

        // 确保高亮图片存在
        if (highlightImage == null)
        {
            // 尝试在子对象中查找
            highlightImage = GetComponentInChildren<Image>(true);

            if (highlightImage == null)
            {
                GameObject highlightObj = new GameObject("Highlight");
                highlightObj.transform.SetParent(transform, false);
                highlightImage = highlightObj.AddComponent<Image>();
            }
        }

        // 设置高亮图片的RectTransform铺满整个格子
        RectTransform highlightRect = highlightImage.rectTransform;
        highlightRect.anchorMin = Vector2.zero;
        highlightRect.anchorMax = Vector2.one;
        highlightRect.offsetMin = Vector2.zero;
        highlightRect.offsetMax = Vector2.zero;

        highlightImage.gameObject.SetActive(false);
        highlightImage.raycastTarget = false; // 确保高亮不阻挡点击
    }

    /// <summary>
    /// 初始化格子
    /// </summary>
    public void Initialize(GridType type, Vector2Int pos, bool selectable = true)
    {
        gridType = type;
        gridPosition = pos;
        isSelectable = selectable;
        canStack = true; // 默认可叠加

        // 获取原始颜色（不带透明度）
        originalColor = GetBaseColor();
        originalColor.a = 1f; // 确保原始颜色不透明

        // 设置格子颜色
        UpdateGridColor();

        // 设置格子名称（调试用）
        gameObject.name = $"Grid_{pos.x}_{pos.y}_{type}";

        // 添加RectTransform设置
        RectTransform rect = GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1); // 左上角为原点
    }

    /// <summary>
    /// 获取基础颜色（根据格子类型）
    /// </summary>
    private Color GetBaseColor()
    {
        switch (gridType)
        {
            case GridType.Floor:
                return floorColor;
            case GridType.Table:
                return tableColor;
            case GridType.Wall:
                return wallColor;
            case GridType.Outdoor:
                return outdoorColor;
            case GridType.Decoration:
                return decorationColor;
            case GridType.Forbidden:
                return forbiddenColor;
            default:
                return Color.white;
        }
    }

    /// <summary>
    /// 放置家具 - 现在支持叠加
    /// </summary>
    public void PlaceFurniture(string furnitureId, bool providesNewGrids = false, GridType newGridType = GridType.Forbidden, bool isStack = false)
    {
        // 如果是叠加放置，需要特殊处理
        if (isStack)
        {
            if (!canStack)
            {
                Debug.LogWarning($"格子 {gridPosition} 不可叠加，无法放置家具 {furnitureId}");
                return;
            }

            // 添加到叠加列表
            stackedFurnitureIds.Add(furnitureId);
            Debug.Log($"格子 {gridPosition} 叠加放置家具: {furnitureId}，当前叠加层数: {stackedFurnitureIds.Count}");

            // 叠加放置不改变格子类型和占用状态
            return;
        }

        // 如果格子已被占用，先记录警告
        if (isOccupied)
        {
            Debug.LogWarning($"格子 {gridPosition} 尝试放置家具 {furnitureId}，但已被 {placedFurnitureId} 占用");
            return;
        }

        isOccupied = true;
        placedFurnitureId = furnitureId;

        // 根据家具是否提供新格子来更新格子类型
        if (providesNewGrids)
        {
            // 家具提供新格子，更新为提供的类型，并且保持可叠加
            gridType = newGridType;
            canStack = true; // 提供新格子的家具仍然可以叠加
            Debug.Log($"格子 {gridPosition} 类型更新为: {gridType} (家具提供新格子)，可叠加: {canStack}");
        }
        else
        {
            // 家具不提供格子，变为禁止格，并且不可叠加
            gridType = GridType.Forbidden;
            canStack = false; // 不提供格子的家具不可叠加
            Debug.Log($"格子 {gridPosition} 类型更新为: Forbidden (家具不提供格子)，不可叠加");
        }

        // 更新格子颜色
        UpdateGridColor();

        Debug.Log($"格子 {gridPosition} 放置家具: {furnitureId}");
    }

    /// <summary>
    /// 移除家具 - 支持移除叠加的家具
    /// </summary>
    public void RemoveFurniture(GridType originalGridType, string furnitureId = null)
    {
        // 如果有指定家具ID，尝试从叠加列表中移除
        if (!string.IsNullOrEmpty(furnitureId))
        {
            if (stackedFurnitureIds.Contains(furnitureId))
            {
                stackedFurnitureIds.Remove(furnitureId);
                Debug.Log($"格子 {gridPosition} 移除叠加家具: {furnitureId}，剩余叠加层数: {stackedFurnitureIds.Count}");

                // 如果还有叠加家具，不改变格子状态
                if (stackedFurnitureIds.Count > 0)
                    return;
            }
            else if (placedFurnitureId == furnitureId)
            {
                // 移除主家具
                isOccupied = false;
                placedFurnitureId = null;

                // 恢复原始格子类型
                gridType = originalGridType;
                canStack = true; // 恢复可叠加

                Debug.Log($"格子 {gridPosition} 移除主家具，恢复为原始类型: {gridType}");
            }
            else
            {
                Debug.LogWarning($"格子 {gridPosition} 尝试移除家具 {furnitureId}，但未找到");
                return;
            }
        }
        else
        {
            // 没有指定ID，移除所有家具
            if (!isOccupied && stackedFurnitureIds.Count == 0)
            {
                Debug.LogWarning($"格子 {gridPosition} 尝试移除家具，但未被占用");
                return;
            }

            // 清空叠加列表
            stackedFurnitureIds.Clear();

            // 移除主家具
            isOccupied = false;
            placedFurnitureId = null;

            // 恢复原始格子类型
            gridType = originalGridType;
            canStack = true; // 恢复可叠加
        }

        // 更新格子颜色
        UpdateGridColor();

        Debug.Log($"格子 {gridPosition} 家具已移除，恢复为原始类型: {gridType}");
    }

    /// <summary>
    /// 获取格子原始类型（在放置家具前）
    /// </summary>
    public GridType GetOriginalGridType()
    {
        return gridType;
    }

    /// <summary>
    /// 更新格子颜色
    /// </summary>
    private void UpdateGridColor()
    {
        if (backgroundImage == null) return;

        // 获取基础颜色
        Color color = GetBaseColor();

        // 根据编辑模式设置透明度
        if (isEditMode)
        {
            color.a = editModeAlpha;
        }
        else
        {
            color.a = normalAlpha;
        }

        // 如果格子被占用且不可叠加，稍微调暗
        if (isOccupied && !canStack)
        {
            color.r *= 0.7f;
            color.g *= 0.7f;
            color.b *= 0.7f;
        }
        // 如果有叠加家具，稍微调亮
        else if (stackedFurnitureIds.Count > 0)
        {
            color.r *= 1.2f;
            color.g *= 1.2f;
            color.b *= 1.2f;
        }

        backgroundImage.color = color;
    }

    /// <summary>
    /// 设置编辑模式
    /// </summary>
    public void SetEditMode(bool editMode)
    {
        isEditMode = editMode;
        UpdateGridColor();
    }

    /// <summary>
    /// 设置格子透明度（直接设置）
    /// </summary>
    public void SetGridAlpha(float alpha)
    {
        if (backgroundImage == null) return;

        Color color = backgroundImage.color;
        color.a = Mathf.Clamp01(alpha);
        backgroundImage.color = color;
    }

    /// <summary>
    /// 设置高亮状态 - 使用高亮图片组件
    /// </summary>
    public void SetHighlight(bool valid, bool selected = false)
    {
        if (!isSelectable) return;

        isHighlighted = true;
        isValidPlacement = valid;
        isSelected = selected;

        highlightImage.gameObject.SetActive(true);
    }

    /// <summary>
    /// 清除高亮
    /// </summary>
    public void ClearHighlight()
    {
        isHighlighted = false;
        highlightImage.gameObject.SetActive(false);
    }

    /// <summary>
    /// 指针按下事件
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isSelectable || !isEditMode) return;

        isPointerDown = true;

        // 如果格子有家具，触发家具拾取（准备移动）
        if (isOccupied || stackedFurnitureIds.Count > 0)
        {
            // 如果有叠加家具，优先拾取最上层的家具
            string furnitureToPick = stackedFurnitureIds.Count > 0 ?
                stackedFurnitureIds[stackedFurnitureIds.Count - 1] : placedFurnitureId;

            GridSystemManager.Instance?.OnFurniturePickup(gridPosition, furnitureToPick);
            Debug.Log($"拾取家具: {furnitureToPick} 在位置 {gridPosition}");
        }
    }

    /// <summary>
    /// 指针抬起事件
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isSelectable || !isEditMode) return;

        // 如果正在移动家具，尝试完成移动
        if (GridSystemManager.Instance != null && GridSystemManager.Instance.IsMovingFurniture())
        {
            HomeSystemController homeSystem = FindObjectOfType<HomeSystemController>();

            // 检查是否在垃圾桶上
            bool onTrashCan = false;
            if (homeSystem != null && homeSystem.trashCan != null && homeSystem.enableTrashCan)
            {
                // 使用射线检测检查鼠标位置是否有垃圾桶
                var results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);
                foreach (var result in results)
                {
                    if (result.gameObject.GetComponent<TrashCanController>() != null)
                    {
                        onTrashCan = true;
                        break;
                    }
                }
            }

            if (onTrashCan)
            {
                // 触碰到垃圾桶，执行删除
                homeSystem.trashCan.DeleteFurniture();
                Debug.Log("家具被丢入垃圾桶");

                // 通知HomeSystemController复位UI栏
                homeSystem?.ResetUIBars();
            }
            else
            {
                // 否则尝试放置到格子
                Vector2 screenPosition = eventData.position;
                GridCellUI targetCell = homeSystem?.FindGridCellAtPosition(screenPosition);

                if (targetCell != null)
                {
                    // 尝试完成移动
                    bool success = GridSystemManager.Instance.TryFinishMovingFurniture(
                        homeSystem?.GetCurrentRoomIndex() ?? 0,
                        targetCell.GridPosition
                    );

                    if (success)
                    {
                        Debug.Log($"家具移动成功到位置: {targetCell.GridPosition}");
                        if (AudioManager.Instance != null)
                            AudioManager.Instance.PlaySuccessSound();
                    }
                    else
                    {
                        Debug.Log("家具移动失败");
                        if (AudioManager.Instance != null)
                            AudioManager.Instance.PlayErrorSound();
                        // 移动失败时取消移动（回到原位）
                        GridSystemManager.Instance.CancelMovingFurniture();
                    }
                }
                else
                {
                    // 没有找到目标格子，取消移动（回到原位）
                    GridSystemManager.Instance.CancelMovingFurniture();
                    Debug.Log("未找到目标格子，取消移动");
                }

                // 通知HomeSystemController复位UI栏
                homeSystem?.ResetUIBars();
            }
        }

        isPointerDown = false;
    }

    /// <summary>
    /// 点击事件
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isSelectable) return;

        Debug.Log($"点击格子: {gridPosition}, 类型: {gridType}, 已占用: {isOccupied}, 叠加层数: {stackedFurnitureIds.Count}");

        // 触发格子点击事件
        GridSystemManager.Instance?.OnGridClicked(gridPosition, gridType, isOccupied, placedFurnitureId);
    }

    /// <summary>
    /// 鼠标进入事件
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSelectable) return;

        // 触发格子悬停事件
        GridSystemManager.Instance?.OnGridHovered(gridPosition, gridType, isOccupied);

        // 只有在编辑模式下才高亮当前格子
        if (isEditMode)
        {
            SetHighlight(true);
        }
    }

    /// <summary>
    /// 鼠标退出事件
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelectable) return;

        // 触发格子悬停结束事件
        GridSystemManager.Instance?.OnGridHoverEnd();

        // 只有在编辑模式下才清除当前格子的高亮
        if (isEditMode)
        {
            ClearHighlight();
        }
    }

    // 属性访问
    public GridType GridType => gridType;
    public Vector2Int GridPosition => gridPosition;
    public bool IsOccupied => isOccupied;
    public string PlacedFurnitureId => placedFurnitureId;
    public bool IsSelectable => isSelectable;
    public bool CanStack => canStack;
    public int StackCount => stackedFurnitureIds.Count;
    public List<string> StackedFurnitureIds => stackedFurnitureIds;
}