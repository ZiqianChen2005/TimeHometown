using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Collections;

public class FurnitureShopController : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Transform shopContent;
    [SerializeField] private GameObject furnitureItemPrefab;
    [SerializeField] private Text coinsText;
    [SerializeField] private Button backButton;

    [Header("家具详情面板")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private CanvasGroup detailCanvasGroup;
    [SerializeField] private Image detailIcon;
    [SerializeField] private Text detailName;
    [SerializeField] private Text detailPrice;
    [SerializeField] private Text detailDescription;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button closeDetailButton;

    [Header("家具数据")]
    [SerializeField] private List<FurnitureData> allFurnitureData;

    [Header("商店设置")]
    [SerializeField] private int gridCellSize = 100;
    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color unaffordableColor = Color.gray;

    [Header("动画设置")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // 当前选中的家具
    private FurnitureData selectedFurniture;

    // 已拥有家具列表
    private List<string> ownedFurnitureIds = new List<string>();

    // 动画状态
    private bool isDetailPanelAnimating = false;
    private Coroutine fadeCoroutine;

    private void Start()
    {
        InitializeShop();
        LoadOwnedFurniture();
        UpdateCoinsDisplay();
        PopulateShopItems();

        // 订阅金币变更事件
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.OnCoinsChanged += OnCoinsChanged;
        }
    }

    private void OnDestroy()
    {
        // 取消订阅
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.OnCoinsChanged -= OnCoinsChanged;
        }
    }

    /// <summary>
    /// 金币变更回调
    /// </summary>
    private void OnCoinsChanged(int newCoins)
    {
        UpdateCoinsDisplay();
        RefreshShopItemsAffordability();
    }

    /// <summary>
    /// 初始化商店
    /// </summary>
    private void InitializeShop()
    {
        // 设置按钮事件
        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClicked);

        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyButtonClicked);

        if (closeDetailButton != null)
            closeDetailButton.onClick.AddListener(OnCloseDetailButtonClicked);

        // 确保有CanvasGroup组件
        if (detailPanel != null && detailCanvasGroup == null)
        {
            detailCanvasGroup = detailPanel.GetComponent<CanvasGroup>();
            if (detailCanvasGroup == null)
            {
                detailCanvasGroup = detailPanel.AddComponent<CanvasGroup>();
            }
        }

        // 初始隐藏详情面板
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
            if (detailCanvasGroup != null)
            {
                detailCanvasGroup.alpha = 0;
                detailCanvasGroup.interactable = false;
                detailCanvasGroup.blocksRaycasts = false;
            }
        }

        Debug.Log("家具商店初始化完成");
    }

    /// <summary>
    /// 加载已拥有家具
    /// </summary>
    private void LoadOwnedFurniture()
    {
        string ownedFurniture = PlayerPrefs.GetString("Owned_Furniture", "");
        if (!string.IsNullOrEmpty(ownedFurniture))
        {
            ownedFurnitureIds = new List<string>(ownedFurniture.Split(','));
        }

        // 标记已拥有的家具
        foreach (var furniture in allFurnitureData)
        {
            furniture.isLocked = !ownedFurnitureIds.Contains(furniture.id);
        }
    }

    /// <summary>
    /// 更新金币显示
    /// </summary>
    private void UpdateCoinsDisplay()
    {
        if (coinsText != null && GameDataManager.Instance != null)
        {
            coinsText.text = GameDataManager.Instance.GetCoins().ToString();
        }
    }

    /// <summary>
    /// 刷新商店物品的可购买状态
    /// </summary>
    private void RefreshShopItemsAffordability()
    {
        if (GameDataManager.Instance == null) return;

        int playerCoins = GameDataManager.Instance.GetCoins();

        foreach (Transform child in shopContent)
        {
            FurnitureItemUI itemUI = child.GetComponent<FurnitureItemUI>();
            if (itemUI != null)
            {
                FurnitureData data = itemUI.GetFurnitureData();
                if (data != null)
                {
                    if (ownedFurnitureIds.Contains(data.id))
                    {
                        itemUI.SetAsOwned();
                    }
                    else if (playerCoins < data.price)
                    {
                        itemUI.SetAsUnaffordable();
                    }
                }
            }
        }
    }

    /// <summary>
    /// 填充商店物品
    /// </summary>
    private void PopulateShopItems()
    {
        if (shopContent == null || furnitureItemPrefab == null)
            return;

        // 清空现有物品
        foreach (Transform child in shopContent)
        {
            Destroy(child.gameObject);
        }

        // 按房间类型分组家具
        var furnitureByRoom = new Dictionary<int, List<FurnitureData>>();
        foreach (var furniture in allFurnitureData)
        {
            if (!furnitureByRoom.ContainsKey(furniture.requiredRoomType))
                furnitureByRoom[furniture.requiredRoomType] = new List<FurnitureData>();

            furnitureByRoom[furniture.requiredRoomType].Add(furniture);
        }

        // 按房间顺序创建（客厅->书房->卧室->阳台）
        int[] roomOrder = { 0, 1, 2, 3 };

        foreach (int roomType in roomOrder)
        {
            if (furnitureByRoom.ContainsKey(roomType) && furnitureByRoom[roomType].Count > 0)
            {
                // 添加房间标题 - 直接内嵌创建，不使用预制体
                AddRoomTitle(GetRoomName(roomType));

                // 添加该房间的家具
                foreach (var furniture in furnitureByRoom[roomType])
                {
                    CreateFurnitureItem(furniture);
                }
            }
        }
    }

    /// <summary>
    /// 获取房间名称
    /// </summary>
    private string GetRoomName(int roomType)
    {
        switch (roomType)
        {
            case 0: return "客厅家具";
            case 1: return "书房家具";
            case 2: return "卧室家具";
            case 3: return "阳台家具";
            default: return "其他家具";
        }
    }

    /// <summary>
    /// 添加房间标题 - 直接内嵌创建，不使用预制体
    /// </summary>
    private void AddRoomTitle(string title)
    {
        // 创建标题对象
        GameObject titleObj = new GameObject("RoomTitle_" + title);
        titleObj.transform.SetParent(shopContent, false);

        // 添加Image组件作为背景
        Image background = titleObj.AddComponent<Image>();
        background.color = new Color(0.95f, 0.95f, 0.95f, 1f); // 浅灰色背景
        background.raycastTarget = false; // 不需要响应点击

        // 添加Text组件
        GameObject textObj = new GameObject("TitleText");
        textObj.transform.SetParent(titleObj.transform, false);

        Text titleText = textObj.AddComponent<Text>();
        titleText.text = title;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 45;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = new Color(0.2f, 0.2f, 0.2f, 1f); // 深灰色
        titleText.alignment = TextAnchor.MiddleLeft;
        titleText.raycastTarget = false;

        // 设置Text的RectTransform
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.offsetMin = new Vector2(20, 5);  // 左20，下5
        textRect.offsetMax = new Vector2(-20, -5); // 右-20，上-5

        // 设置标题对象的RectTransform
        RectTransform rectTransform = titleObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(0.5f, 1);
        rectTransform.sizeDelta = new Vector2(0, 100); // 高度

        // 添加布局元素以固定高度
        LayoutElement layout = titleObj.AddComponent<LayoutElement>();
        layout.preferredHeight = 100;
        layout.minHeight = 100;
        layout.flexibleHeight = 0;

        Debug.Log($"添加房间标题: {title}");
    }

    /// <summary>
    /// 创建家具项
    /// </summary>
    private void CreateFurnitureItem(FurnitureData furniture)
    {
        GameObject itemObj = Instantiate(furnitureItemPrefab, shopContent);
        itemObj.name = "FurnitureItem_" + furniture.name;

        FurnitureItemUI itemUI = itemObj.GetComponent<FurnitureItemUI>();

        if (itemUI != null)
        {
            itemUI.Initialize(furniture, OnFurnitureItemClicked);

            if (ownedFurnitureIds.Contains(furniture.id))
            {
                itemUI.SetAsOwned();
            }
            else if (GameDataManager.Instance != null && GameDataManager.Instance.GetCoins() < furniture.price)
            {
                itemUI.SetAsUnaffordable();
            }
        }

        // 设置布局元素
        LayoutElement layout = itemObj.GetComponent<LayoutElement>();
        if (layout == null)
            layout = itemObj.AddComponent<LayoutElement>();

        layout.preferredHeight = 150;
        layout.minHeight = 120;
        layout.flexibleHeight = 0;
    }

    /// <summary>
    /// 家具项点击事件
    /// </summary>
    private void OnFurnitureItemClicked(FurnitureData furniture)
    {
        selectedFurniture = furniture;
        ShowFurnitureDetail(furniture);
    }

    /// <summary>
    /// 显示家具详情
    /// </summary>
    private void ShowFurnitureDetail(FurnitureData furniture)
    {
        if (detailPanel == null || detailCanvasGroup == null) return;

        if (isDetailPanelAnimating && fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            isDetailPanelAnimating = false;
        }

        if (detailIcon != null && furniture.icon != null)
            detailIcon.sprite = furniture.icon;

        if (detailName != null)
            detailName.text = furniture.name;

        if (detailPrice != null)
            detailPrice.text = $"价格: {furniture.price}金币";

        if (detailDescription != null)
            detailDescription.text = furniture.description;

        if (buyButton != null && GameDataManager.Instance != null)
        {
            bool canBuy = GameDataManager.Instance.GetCoins() >= furniture.price && !ownedFurnitureIds.Contains(furniture.id);
            buyButton.interactable = canBuy;

            Text buttonText = buyButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = ownedFurnitureIds.Contains(furniture.id) ? "已拥有" : "购买";
            }
        }

        detailPanel.SetActive(true);
        fadeCoroutine = StartCoroutine(FadeInDetailPanel());
    }

    /// <summary>
    /// 购买按钮点击事件
    /// </summary>
    private void OnBuyButtonClicked()
    {
        if (selectedFurniture == null || GameDataManager.Instance == null) return;

        if (ownedFurnitureIds.Contains(selectedFurniture.id))
        {
            Debug.LogWarning($"家具 {selectedFurniture.name} 已拥有");
            return;
        }

        // 使用GameDataManager扣除金币
        if (GameDataManager.Instance.SpendCoins(selectedFurniture.price))
        {
            ownedFurnitureIds.Add(selectedFurniture.id);
            selectedFurniture.isLocked = false;

            SaveOwnedFurniture();

            // 刷新商店物品显示
            PopulateShopItems();

            // 播放购买成功音效
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySuccessSound();

            Debug.Log($"购买家具成功: {selectedFurniture.name}");

            // 购买成功后自动关闭详情窗口
            CloseDetailPanelAfterPurchase();
        }
        else
        {
            Debug.LogWarning($"金币不足，需要 {selectedFurniture.price} 金币");
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayErrorSound();
        }
    }

    /// <summary>
    /// 保存已拥有家具
    /// </summary>
    private void SaveOwnedFurniture()
    {
        string ownedFurnitureStr = string.Join(",", ownedFurnitureIds);
        PlayerPrefs.SetString("Owned_Furniture", ownedFurnitureStr);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 购买成功后关闭详情面板
    /// </summary>
    private void CloseDetailPanelAfterPurchase()
    {
        if (detailPanel != null && detailCanvasGroup != null)
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(DelayedCloseDetailPanel());
        }
    }

    /// <summary>
    /// 延迟关闭详情面板
    /// </summary>
    private IEnumerator DelayedCloseDetailPanel()
    {
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(FadeOutDetailPanel());
    }

    /// <summary>
    /// 关闭详情按钮点击事件
    /// </summary>
    private void OnCloseDetailButtonClicked()
    {
        CloseDetailPanel();
    }

    /// <summary>
    /// 关闭详情面板
    /// </summary>
    private void CloseDetailPanel()
    {
        if (detailPanel != null && detailCanvasGroup != null && detailPanel.activeSelf)
        {
            if (isDetailPanelAnimating && fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            fadeCoroutine = StartCoroutine(FadeOutDetailPanel());
        }
    }

    /// <summary>
    /// 淡入详情面板
    /// </summary>
    private IEnumerator FadeInDetailPanel()
    {
        isDetailPanelAnimating = true;

        detailCanvasGroup.alpha = 0;
        detailCanvasGroup.interactable = false;
        detailCanvasGroup.blocksRaycasts = false;

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float curvedT = fadeCurve.Evaluate(t);

            detailCanvasGroup.alpha = curvedT;
            yield return null;
        }

        detailCanvasGroup.alpha = 1;
        detailCanvasGroup.interactable = true;
        detailCanvasGroup.blocksRaycasts = true;

        isDetailPanelAnimating = false;
        fadeCoroutine = null;
    }

    /// <summary>
    /// 淡出详情面板
    /// </summary>
    private IEnumerator FadeOutDetailPanel()
    {
        isDetailPanelAnimating = true;

        detailCanvasGroup.interactable = false;
        detailCanvasGroup.blocksRaycasts = false;

        float elapsed = 0f;
        float startAlpha = detailCanvasGroup.alpha;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float curvedT = fadeCurve.Evaluate(t);

            detailCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0, curvedT);
            yield return null;
        }

        detailCanvasGroup.alpha = 0;
        detailPanel.SetActive(false);

        isDetailPanelAnimating = false;
        fadeCoroutine = null;
    }

    /// <summary>
    /// 返回按钮点击事件
    /// </summary>
    private void OnBackButtonClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.GoToMainScene();
        }
    }
}