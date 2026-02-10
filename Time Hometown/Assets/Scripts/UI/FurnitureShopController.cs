using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Collections;

public class FurnitureShopController : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Transform shopContent;         // 商店内容容器
    [SerializeField] private GameObject furnitureItemPrefab; // 家具项预制体
    [SerializeField] private GameObject roomTitlePrefab;    // 房间标题预制体
    [SerializeField] private Text coinsText;                // 金币显示文本
    [SerializeField] private Button backButton;             // 返回按钮

    [Header("家具详情面板")]
    [SerializeField] private GameObject detailPanel;        // 详情面板
    [SerializeField] private CanvasGroup detailCanvasGroup; // 详情面板CanvasGroup
    [SerializeField] private Image detailIcon;              // 详情图标
    [SerializeField] private Text detailName;               // 详情名称
    [SerializeField] private Text detailPrice;              // 详情价格
    [SerializeField] private Text detailDescription;        // 详情描述
    [SerializeField] private Button buyButton;              // 购买按钮
    [SerializeField] private Button closeDetailButton;      // 关闭详情按钮

    [Header("家具数据")]
    [SerializeField] private List<FurnitureData> allFurnitureData; // 所有家具数据

    [Header("商店设置")]
    [SerializeField] private int gridCellSize = 100;        // 格子大小（像素）
    [SerializeField] private Color affordableColor = Color.white;     // 可购买颜色
    [SerializeField] private Color unaffordableColor = Color.gray;    // 不可购买颜色

    [Header("动画设置")]
    [SerializeField] private float fadeDuration = 0.3f;     // 淡入淡出持续时间
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 动画曲线

    // 当前选中的家具
    private FurnitureData selectedFurniture;

    // 玩家数据
    private int playerCoins = 0;
    private List<string> ownedFurnitureIds = new List<string>();

    // 动画状态
    private bool isDetailPanelAnimating = false;
    private Coroutine fadeCoroutine;

    private void Start()
    {
        InitializeShop();
        LoadPlayerData();
        UpdateCoinsDisplay();
        PopulateShopItems();
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
    /// 加载玩家数据
    /// </summary>
    private void LoadPlayerData()
    {
        // 从PlayerPrefs加载金币
        playerCoins = PlayerPrefs.GetInt("Player_Coins", 1000); // 默认1000金币

        // 从PlayerPrefs加载已拥有家具
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

        Debug.Log($"加载玩家数据: 金币={playerCoins}, 已拥有家具={ownedFurnitureIds.Count}件");
    }

    /// <summary>
    /// 更新金币显示
    /// </summary>
    private void UpdateCoinsDisplay()
    {
        if (coinsText != null)
        {
            coinsText.text = $"金币: {playerCoins}";
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
        int[] roomOrder = { 0, 1, 2, 3 }; // 按房间类型顺序

        foreach (int roomType in roomOrder)
        {
            if (furnitureByRoom.ContainsKey(roomType) && furnitureByRoom[roomType].Count > 0)
            {
                // 添加房间标题
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
    /// 添加房间标题
    /// </summary>
    private void AddRoomTitle(string title)
    {
        GameObject titleObj = new GameObject("RoomTitle");
        titleObj.transform.SetParent(shopContent);

        // 添加背景Panel
        Image background = titleObj.AddComponent<Image>();
        background.color = new Color(0.95f, 0.95f, 0.95f, 1f);

        // 添加Text组件
        GameObject textObj = new GameObject("TitleText");
        textObj.transform.SetParent(titleObj.transform);

        Text titleText = textObj.AddComponent<Text>();
        titleText.text = title;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 50;
        titleText.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontStyle = FontStyle.Bold;

        // 设置Text的RectTransform比例全部为1
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.localScale = Vector3.one;  // 比例设为1,1,1

        // 设置Text的RectTransform锚点为拉伸
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);  // 中心点
        textRect.offsetMin = new Vector2(40, 0);   // 左内边距40
        textRect.offsetMax = Vector2.zero;
        textRect.localPosition = Vector3.zero;

        // 设置标题的RectTransform比例全部为1
        RectTransform rectTransform = titleObj.GetComponent<RectTransform>();
        rectTransform.localScale = Vector3.one;  // 比例设为1,1,1

        // 设置标题的RectTransform为顶部拉伸
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(0.5f, 1);  // 顶部中心点
        rectTransform.anchoredPosition = new Vector2(0, 0);
        rectTransform.sizeDelta = new Vector2(0, 100);
        rectTransform.localPosition = Vector3.zero;

        // 确保位置正确
        rectTransform.anchoredPosition3D = Vector3.zero;
        rectTransform.localEulerAngles = Vector3.zero;

        // 添加布局元素
        LayoutElement layout = titleObj.AddComponent<LayoutElement>();
        layout.preferredHeight = 100;
        layout.flexibleHeight = 0;

        Debug.Log($"添加房间标题: {title}");
    }

    /// <summary>
    /// 创建家具项
    /// </summary>
    private void CreateFurnitureItem(FurnitureData furniture)
    {
        GameObject itemObj = Instantiate(furnitureItemPrefab, shopContent);
        FurnitureItemUI itemUI = itemObj.GetComponent<FurnitureItemUI>();

        if (itemUI != null)
        {
            itemUI.Initialize(furniture, OnFurnitureItemClicked);

            // 如果已拥有，标记为已购买
            if (ownedFurnitureIds.Contains(furniture.id))
            {
                itemUI.SetAsOwned();
            }
            // 如果金币不足，标记为不可购买
            else if (playerCoins < furniture.price)
            {
                itemUI.SetAsUnaffordable();
            }
        }

        // 设置家具项的布局
        RectTransform itemRect = itemObj.GetComponent<RectTransform>();
        if (itemRect != null)
        {
            itemRect.pivot = new Vector2(0.5f, 0.5f);
        }

        // 设置布局元素
        LayoutElement layout = itemObj.GetComponent<LayoutElement>();
        if (layout == null)
            layout = itemObj.AddComponent<LayoutElement>();

        layout.preferredHeight = 150; // 固定高度
        layout.minHeight = 120;

        Debug.Log($"创建家具项: {furniture.name}");
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
    /// 显示家具详情（带淡入效果）
    /// </summary>
    private void ShowFurnitureDetail(FurnitureData furniture)
    {
        if (detailPanel == null || detailCanvasGroup == null) return;

        // 如果正在动画中，先停止
        if (isDetailPanelAnimating && fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            isDetailPanelAnimating = false;
        }

        // 更新详情信息
        if (detailIcon != null && furniture.icon != null)
            detailIcon.sprite = furniture.icon;

        if (detailName != null)
            detailName.text = furniture.name;

        if (detailPrice != null)
            detailPrice.text = $"价格: {furniture.price}金币";

        if (detailDescription != null)
            detailDescription.text = "介绍：\n\n"+furniture.description;

        // 更新购买按钮状态
        if (buyButton != null)
        {
            bool canBuy = playerCoins >= furniture.price && !ownedFurnitureIds.Contains(furniture.id);
            buyButton.interactable = canBuy;

            Text buttonText = buyButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = ownedFurnitureIds.Contains(furniture.id) ? "已拥有" : "购买";
            }
        }

        // 激活面板并开始淡入动画
        detailPanel.SetActive(true);
        fadeCoroutine = StartCoroutine(FadeInDetailPanel());

        Debug.Log($"显示家具详情: {furniture.name}");
    }

    /// <summary>
    /// 购买按钮点击事件
    /// </summary>
    private void OnBuyButtonClicked()
    {
        if (selectedFurniture == null) return;

        // 检查是否已拥有
        if (ownedFurnitureIds.Contains(selectedFurniture.id))
        {
            Debug.LogWarning($"家具 {selectedFurniture.name} 已拥有");
            return;
        }

        // 检查金币是否足够
        if (playerCoins < selectedFurniture.price)
        {
            Debug.LogWarning($"金币不足，需要 {selectedFurniture.price} 金币，当前 {playerCoins} 金币");

            // 播放错误音效
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayErrorSound();

            return;
        }

        // 扣除金币
        playerCoins -= selectedFurniture.price;

        // 添加到已拥有列表
        ownedFurnitureIds.Add(selectedFurniture.id);
        selectedFurniture.isLocked = false;

        // 保存数据
        SavePlayerData();

        // 播放购买成功音效
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySuccessSound();

        Debug.Log($"购买家具成功: {selectedFurniture.name}, 剩余金币: {playerCoins}");

        // 更新UI
        UpdateCoinsDisplay();
        PopulateShopItems(); // 重新加载商店物品以更新状态

        // 购买成功后自动关闭详情窗口（带淡出效果）
        CloseDetailPanelAfterPurchase();
    }

    /// <summary>
    /// 购买成功后关闭详情面板
    /// </summary>
    private void CloseDetailPanelAfterPurchase()
    {
        if (detailPanel != null && detailCanvasGroup != null)
        {
            // 稍微延迟一下，让玩家看到购买成功的反馈
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
        // 等待一小段时间让用户看到购买成功
        yield return new WaitForSeconds(0.5f);

        // 淡出并关闭详情面板
        yield return StartCoroutine(FadeOutDetailPanel());

        // 购买完成后可以显示一个提示（可选）
        ShowPurchaseSuccessMessage(selectedFurniture.name);
    }

    /// <summary>
    /// 显示购买成功消息
    /// </summary>
    private void ShowPurchaseSuccessMessage(string furnitureName)
    {
        // 这里可以添加一个短暂的Toast提示
        Debug.Log($"成功购买: {furnitureName}");
        // 也可以在这里显示UI提示
    }

    /// <summary>
    /// 关闭详情按钮点击事件
    /// </summary>
    private void OnCloseDetailButtonClicked()
    {
        CloseDetailPanel();
    }

    /// <summary>
    /// 关闭详情面板（带淡出效果）
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
    /// 淡入详情面板协程
    /// </summary>
    private IEnumerator FadeInDetailPanel()
    {
        isDetailPanelAnimating = true;

        if (detailCanvasGroup != null)
        {
            detailCanvasGroup.alpha = 0;
            detailCanvasGroup.interactable = false;
            detailCanvasGroup.blocksRaycasts = false;
        }

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float curvedT = fadeCurve.Evaluate(t);

            if (detailCanvasGroup != null)
            {
                detailCanvasGroup.alpha = curvedT;
            }

            yield return null;
        }

        if (detailCanvasGroup != null)
        {
            detailCanvasGroup.alpha = 1;
            detailCanvasGroup.interactable = true;
            detailCanvasGroup.blocksRaycasts = true;
        }

        isDetailPanelAnimating = false;
        fadeCoroutine = null;
    }

    /// <summary>
    /// 淡出详情面板协程
    /// </summary>
    private IEnumerator FadeOutDetailPanel()
    {
        isDetailPanelAnimating = true;

        if (detailCanvasGroup != null)
        {
            detailCanvasGroup.interactable = false;
            detailCanvasGroup.blocksRaycasts = false;
        }

        float elapsed = 0f;
        float startAlpha = detailCanvasGroup != null ? detailCanvasGroup.alpha : 1f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float curvedT = fadeCurve.Evaluate(t);

            if (detailCanvasGroup != null)
            {
                detailCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0, curvedT);
            }

            yield return null;
        }

        if (detailCanvasGroup != null)
        {
            detailCanvasGroup.alpha = 0;
        }

        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }

        isDetailPanelAnimating = false;
        fadeCoroutine = null;
    }

    /// <summary>
    /// 返回按钮点击事件
    /// </summary>
    private void OnBackButtonClicked()
    {
        // 播放按钮音效
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        // 返回主场景
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.GoToMainScene();
        }
        else
        {
            Debug.LogWarning("SceneTransitionManager未找到，请手动返回");
            // UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
        }
    }

    /// <summary>
    /// 保存玩家数据
    /// </summary>
    private void SavePlayerData()
    {
        PlayerPrefs.SetInt("Player_Coins", playerCoins);

        // 保存已拥有家具ID（逗号分隔）
        string ownedFurnitureStr = string.Join(",", ownedFurnitureIds);
        PlayerPrefs.SetString("Owned_Furniture", ownedFurnitureStr);

        PlayerPrefs.Save();

        Debug.Log($"保存玩家数据: 金币={playerCoins}, 已拥有家具={ownedFurnitureIds.Count}件");
    }

    /// <summary>
    /// 获取格子大小
    /// </summary>
    public int GetGridCellSize()
    {
        return gridCellSize;
    }

    /// <summary>
    /// 获取玩家金币
    /// </summary>
    public int GetPlayerCoins()
    {
        return playerCoins;
    }

    /// <summary>
    /// 添加金币（测试用）
    /// </summary>
    public void AddCoins(int amount)
    {
        playerCoins += amount;
        UpdateCoinsDisplay();
        SavePlayerData();
    }

    /// <summary>
    /// 检查家具是否已拥有
    /// </summary>
    public bool IsFurnitureOwned(string furnitureId)
    {
        return ownedFurnitureIds.Contains(furnitureId);
    }

    /// <summary>
    /// 获取已拥有家具列表
    /// </summary>
    public List<string> GetOwnedFurnitureIds()
    {
        return ownedFurnitureIds;
    }

    /// <summary>
    /// 外部调用：强制关闭详情面板
    /// </summary>
    public void ForceCloseDetailPanel()
    {
        CloseDetailPanel();
    }
}