using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GridCellUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("格子组件")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image highlightImage;
    [SerializeField] private Image furnitureIcon;      // 放置家具后的图标
    [SerializeField] private Text gridInfoText;        // 调试用，可隐藏

    [Header("格子颜色")]
    [SerializeField] private Color floorColor = new Color(1f, 0.92f, 0.016f, 1f);      // 黄色 #FFFF00
    [SerializeField] private Color tableColor = new Color(0.5f, 0.5f, 0.5f, 1f);       // 灰色 #808080
    [SerializeField] private Color wallColor = new Color(0f, 0.5f, 1f, 1f);            // 蓝色 #0080FF
    [SerializeField] private Color outdoorColor = new Color(0f, 0.8f, 0f, 1f);         // 绿色 #00CC00
    [SerializeField] private Color decorationColor = new Color(0.8f, 0f, 0.8f, 1f);    // 紫色 #CC00CC
    [SerializeField] private Color forbiddenColor = new Color(1f, 0f, 0f, 0.5f);       // 红色半透明

    [Header("高亮颜色")]
    [SerializeField] private Color highlightValidColor = new Color(1f, 1f, 1f, 0.3f);  // 白色半透明 - 可放置
    [SerializeField] private Color highlightInvalidColor = new Color(1f, 0f, 0f, 0.3f); // 红色半透明 - 不可放置
    [SerializeField] private Color highlightSelectedColor = new Color(0f, 1f, 0f, 0.3f); // 绿色半透明 - 选中

    [Header("编辑模式")]
    [SerializeField] private float normalAlpha = 0f;           // 正常模式透明度
    [SerializeField] private float editModeAlpha = 1f;       // 编辑模式透明度

    // 格子数据
    private GridType gridType;
    private Vector2Int gridPosition;
    private bool isOccupied = false;
    private string placedFurnitureId;
    private bool isSelectable = true;
    private bool isHighlighted = false;

    // 状态
    private bool isSelected = false;
    private bool isValidPlacement = false;
    private bool isEditMode = false;        // 是否处于编辑模式
    private Color originalColor;             // 原始颜色（不包含透明度）

    private void Awake()
    {
        // 确保必要组件存在
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (backgroundImage == null)
            backgroundImage = gameObject.AddComponent<Image>();

        // 初始化高亮图片
        if (highlightImage == null)
        {
            GameObject highlightObj = new GameObject("Highlight");
            highlightObj.transform.SetParent(transform, false);
            highlightImage = highlightObj.AddComponent<Image>();

            RectTransform highlightRect = highlightImage.rectTransform;
            highlightRect.anchorMin = Vector2.zero;
            highlightRect.anchorMax = Vector2.one;
            highlightRect.offsetMin = Vector2.zero;
            highlightRect.offsetMax = Vector2.zero;
        }

        highlightImage.gameObject.SetActive(false);

        // 初始化家具图标
        if (furnitureIcon == null)
        {
            GameObject iconObj = new GameObject("FurnitureIcon");
            iconObj.transform.SetParent(transform, false);
            furnitureIcon = iconObj.AddComponent<Image>();

            RectTransform iconRect = furnitureIcon.rectTransform;
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(5, 5);
            iconRect.offsetMax = new Vector2(-5, -5);
        }

        furnitureIcon.gameObject.SetActive(false);
    }

    /// <summary>
    /// 初始化格子
    /// </summary>
    public void Initialize(GridType type, Vector2Int pos, bool selectable = true)
    {
        gridType = type;
        gridPosition = pos;
        isSelectable = selectable;

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
    /// 放置家具
    /// </summary>
    public void PlaceFurniture(string furnitureId, Sprite icon)
    {
        isOccupied = true;
        placedFurnitureId = furnitureId;

        if (furnitureIcon != null && icon != null)
        {
            furnitureIcon.sprite = icon;
            furnitureIcon.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 移除家具
    /// </summary>
    public void RemoveFurniture()
    {
        isOccupied = false;
        placedFurnitureId = null;

        if (furnitureIcon != null)
        {
            furnitureIcon.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 设置高亮状态
    /// </summary>
    public void SetHighlight(bool valid, bool selected = false)
    {
        if (!isSelectable) return;

        isHighlighted = true;
        isValidPlacement = valid;
        isSelected = selected;

        highlightImage.gameObject.SetActive(true);

        if (selected)
        {
            highlightImage.color = highlightSelectedColor;
        }
        else if (valid)
        {
            highlightImage.color = highlightValidColor;
        }
        else
        {
            highlightImage.color = highlightInvalidColor;
        }
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
    /// 点击事件
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isSelectable) return;

        Debug.Log($"点击格子: {gridPosition}, 类型: {gridType}, 已占用: {isOccupied}");

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
    }

    /// <summary>
    /// 鼠标退出事件
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelectable) return;

        // 触发格子悬停结束事件
        GridSystemManager.Instance?.OnGridHoverEnd();
    }

    // 属性访问
    public GridType GridType => gridType;
    public Vector2Int GridPosition => gridPosition;
    public bool IsOccupied => isOccupied;
    public string PlacedFurnitureId => placedFurnitureId;
    public bool IsSelectable => isSelectable;
}
