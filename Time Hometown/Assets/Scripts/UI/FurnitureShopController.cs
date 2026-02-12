using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class FurnitureShopController : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Transform shopContent;         // 商店内容容器
    [SerializeField] private GameObject furnitureItemPrefab; // 家具项预制体
    [SerializeField] private Text coinsText;                // 金币显示文本
    [SerializeField] private Button backButton;             // 返回按钮

    [Header("家具详情面板")]
    [SerializeField] private GameObject detailPanel;        // 详情面板
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

    // 当前选中的家具
    private FurnitureData selectedFurniture;

    // 玩家数据
    private int playerCoins = 0;
    private List<string> ownedFurnitureIds = new List<string>();

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

        // 初始隐藏详情面板
        if (detailPanel != null)
            detailPanel.SetActive(false);

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

        // 创建家具项
        foreach (var roomGroup in furnitureByRoom)
        {
            // 添加房间标题
            AddRoomTitle(GetRoomName(roomGroup.Key));

            // 添加该房间的家具
            foreach (var furniture in roomGroup.Value)
            {
                CreateFurnitureItem(furniture);
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

        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = title;
        titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        titleText.fontSize = 24;
        titleText.color = Color.black;
        titleText.alignment = TextAnchor.MiddleLeft;

        // 添加布局元素
        LayoutElement layout = titleObj.AddComponent<LayoutElement>();
        layout.preferredHeight = 40;

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
    /// 显示家具详情
    /// </summary>
    private void ShowFurnitureDetail(FurnitureData furniture)
    {
        if (detailPanel == null) return;

        // 更新详情信息
        if (detailIcon != null && furniture.icon != null)
            detailIcon.sprite = furniture.icon;

        if (detailName != null)
            detailName.text = furniture.name;

        if (detailPrice != null)
            detailPrice.text = $"价格: {furniture.price}金币";

        if (detailDescription != null)
            detailDescription.text = furniture.description;

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

        // 显示详情面板
        detailPanel.SetActive(true);

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

        // 更新UI
        UpdateCoinsDisplay();
        PopulateShopItems(); // 重新加载商店物品以更新状态

        // 播放购买成功音效
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySuccessSound();

        Debug.Log($"购买家具成功: {selectedFurniture.name}, 剩余金币: {playerCoins}");
    }

    /// <summary>
    /// 关闭详情按钮点击事件
    /// </summary>
    private void OnCloseDetailButtonClicked()
    {
        if (detailPanel != null)
            detailPanel.SetActive(false);
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
        // SceneManager.LoadScene("MainScene"); // 需要根据实际场景名调整

        Debug.Log("返回主场景");
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
}