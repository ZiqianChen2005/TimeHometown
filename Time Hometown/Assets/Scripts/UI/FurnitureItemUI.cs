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
            priceText.text = $"价格：{furnitureData.price}自律币";
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