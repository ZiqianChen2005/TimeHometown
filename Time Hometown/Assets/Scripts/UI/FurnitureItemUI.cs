using UnityEngine;
using UnityEngine.UI;

public class FurnitureItemUI : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Text priceText;
    [SerializeField] private Button itemButton;
    [SerializeField] private GameObject ownedTag;
    [SerializeField] private GameObject lockedTag;

    [Header("颜色设置")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color ownedColor = new Color(0.8f, 1f, 0.8f, 1f); // 浅绿色
    [SerializeField] private Color lockedColor = new Color(0.7f, 0.7f, 0.7f, 0.5f); // 灰色半透明

    [Header("尺寸设置")]
    [SerializeField] private float maxSize = 150f; // 最大边长为150像素
    [SerializeField] private float iconPadding = 10f; // 图标内边距

    private FurnitureData furnitureData;
    private System.Action<FurnitureData> onClickCallback;

    private void Awake()
    {
        if (itemButton != null)
        {
            itemButton.onClick.AddListener(OnItemClicked);
        }
    }

    /// <summary>
    /// 初始化家具项
    /// </summary>
    public void Initialize(FurnitureData data, System.Action<FurnitureData> onClick)
    {
        furnitureData = data;
        onClickCallback = onClick;

        // 更新UI显示
        UpdateUI();

        // 根据家具尺寸调整图标大小
        AdjustIconSize();
    }

    /// <summary>
    /// 根据家具宽高比调整图标大小
    /// </summary>
    private void AdjustIconSize()
    {
        if (furnitureData == null || iconImage == null) return;

        // 获取家具宽高
        int width = furnitureData.width;
        int height = furnitureData.height;

        // 计算宽高比
        float aspectRatio = (float)width / height;

        // 获取图标RectTransform
        RectTransform iconRect = iconImage.rectTransform;

        // 重置锚点和中心点
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);

        // 根据宽高比设置尺寸
        if (aspectRatio >= 1f)
        {
            // 宽度 >= 高度：宽度为maxSize，高度按比例计算
            float finalWidth = maxSize - iconPadding * 2;
            float finalHeight = finalWidth / aspectRatio;
            iconRect.sizeDelta = new Vector2(finalWidth, finalHeight);
        }
        else
        {
            // 高度 > 宽度：高度为maxSize，宽度按比例计算
            float finalHeight = maxSize - iconPadding * 2;
            float finalWidth = finalHeight * aspectRatio;
            iconRect.sizeDelta = new Vector2(finalWidth, finalHeight);
        }

        // 确保图标不变形
        iconImage.preserveAspect = true;

        Debug.Log($"家具 {furnitureData.name} 图标尺寸调整为: {iconRect.sizeDelta} (宽高比: {aspectRatio:F2})");
    }

    /// <summary>
    /// 更新UI显示
    /// </summary>
    private void UpdateUI()
    {
        if (furnitureData == null) return;

        // 设置图标
        if (iconImage != null && furnitureData.icon != null)
        {
            iconImage.sprite = furnitureData.icon;
        }

        // 设置名称
        if (nameText != null)
        {
            nameText.text = furnitureData.name;
        }

        // 设置价格
        if (priceText != null)
        {
            priceText.text = $"{furnitureData.price}金币";
        }

        // 设置初始颜色
        if (itemButton != null)
        {
            Image buttonImage = itemButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = normalColor;
            }
        }

        // 隐藏所有标签
        if (ownedTag != null)
            ownedTag.SetActive(false);

        if (lockedTag != null)
            lockedTag.SetActive(false);
    }

    /// <summary>
    /// 标记为已拥有
    /// </summary>
    public void SetAsOwned()
    {
        if (itemButton != null)
        {
            itemButton.interactable = true; // 仍然可以点击查看

            Image buttonImage = itemButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = ownedColor;
            }
        }

        if (ownedTag != null)
            ownedTag.SetActive(true);

        if (lockedTag != null)
            lockedTag.SetActive(false);
    }

    /// <summary>
    /// 标记为不可购买（金币不足）
    /// </summary>
    public void SetAsUnaffordable()
    {
        if (itemButton != null)
        {
            itemButton.interactable = true; // 仍然可以点击查看

            Image buttonImage = itemButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = lockedColor;
            }
        }

        if (lockedTag != null)
        {
            lockedTag.SetActive(true);
            Text lockedText = lockedTag.GetComponentInChildren<Text>();
            if (lockedText != null)
            {
                lockedText.text = "金币不足";
            }
        }
    }

    /// <summary>
    /// 标记为未解锁（等级不足）
    /// </summary>
    public void SetAsLocked(int requiredLevel)
    {
        if (itemButton != null)
        {
            itemButton.interactable = false;

            Image buttonImage = itemButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = lockedColor;
            }
        }

        if (lockedTag != null)
        {
            lockedTag.SetActive(true);
            Text lockedText = lockedTag.GetComponentInChildren<Text>();
            if (lockedText != null)
            {
                lockedText.text = $"Lv.{requiredLevel}解锁";
            }
        }
    }

    /// <summary>
    /// 家具项点击事件
    /// </summary>
    private void OnItemClicked()
    {
        if (furnitureData != null && onClickCallback != null)
        {
            onClickCallback?.Invoke(furnitureData);

            // 播放点击音效
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayButtonClick();
        }
    }

    /// <summary>
    /// 获取家具数据
    /// </summary>
    public FurnitureData GetFurnitureData()
    {
        return furnitureData;
    }
}
