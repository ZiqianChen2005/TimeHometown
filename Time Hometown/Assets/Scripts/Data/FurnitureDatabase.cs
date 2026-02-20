using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class FurnitureDatabase : MonoBehaviour
{
    public static FurnitureDatabase Instance { get; private set; }

    [Header("JSON配置")]
    [SerializeField] private string jsonFilePath = "Data/furniture_data"; // Resources路径，不需要扩展名

    [Header("贴图配置")]
    [SerializeField] private string iconPath = "Image/UI/Furniture"; // 贴图Resources路径

    [Header("调试")]
    [SerializeField] private bool loadIconsOnStartup = true; // 启动时加载贴图
    [SerializeField] private bool logLoadingDetails = false;  // 是否打印详细日志

    // 家具数据
    private List<FurnitureData> allFurniture = new List<FurnitureData>();

    // 按房间类型分组的家具
    private Dictionary<int, List<FurnitureData>> furnitureByRoom = new Dictionary<int, List<FurnitureData>>();

    // 按ID索引的家具
    private Dictionary<string, FurnitureData> furnitureById = new Dictionary<string, FurnitureData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadFurnitureData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 加载家具数据
    /// </summary>
    private void LoadFurnitureData()
    {
        allFurniture.Clear();
        furnitureByRoom.Clear();
        furnitureById.Clear();

        // 从Resources加载JSON文件
        LoadFromResources();

        // 如果没有数据，输出错误
        if (allFurniture.Count == 0)
        {
            Debug.LogError($"无法加载家具数据：在 Resources/{jsonFilePath}.json 中未找到有效数据");
        }

        // 构建索引
        BuildIndices();

        Debug.Log($"家具数据库加载完成，共 {allFurniture.Count} 件家具");
    }

    /// <summary>
    /// 从Resources加载JSON
    /// </summary>
    private void LoadFromResources()
    {
        try
        {
            // 加载JSON文件
            TextAsset jsonAsset = Resources.Load<TextAsset>(jsonFilePath);

            if (jsonAsset == null)
            {
                Debug.LogError($"找不到JSON文件: Resources/{jsonFilePath}.json");
                return;
            }

            // 解析JSON
            FurnitureDataArray wrapper = JsonUtility.FromJson<FurnitureDataArray>(jsonAsset.text);

            if (wrapper != null && wrapper.furniture != null)
            {
                allFurniture.AddRange(wrapper.furniture);
                Debug.Log($"从 Resources/{jsonFilePath}.json 加载了 {wrapper.furniture.Length} 件家具");

                // 如果启用了启动时加载贴图
                if (loadIconsOnStartup)
                {
                    LoadAllFurnitureIcons();
                }
            }
            else
            {
                Debug.LogError("JSON解析失败：数据格式错误");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载家具数据失败: {e.Message}");
        }
    }

    /// <summary>
    /// 加载所有家具的贴图
    /// </summary>
    private void LoadAllFurnitureIcons()
    {
        int loadedCount = 0;
        int missingCount = 0;

        foreach (var furniture in allFurniture)
        {
            if (LoadFurnitureIcon(furniture))
            {
                loadedCount++;
            }
            else
            {
                missingCount++;
                if (logLoadingDetails)
                {
                    Debug.LogWarning($"未找到家具贴图: {furniture.id}，路径: {iconPath}/{furniture.id}");
                }
            }
        }

        Debug.Log($"家具贴图加载完成: {loadedCount} 个成功, {missingCount} 个缺失");
    }

    /// <summary>
    /// 加载单个家具的贴图
    /// </summary>
    private bool LoadFurnitureIcon(FurnitureData furniture)
    {
        if (furniture == null) return false;

        // 构建贴图路径：iconPath + 家具ID
        string fullPath = $"{iconPath}/{furniture.id}";

        // 加载贴图
        Sprite icon = Resources.Load<Sprite>(fullPath);

        if (icon != null)
        {
            furniture.icon = icon;

            // 如果启用了详细日志
            if (logLoadingDetails)
            {
                Debug.Log($"加载家具贴图成功: {furniture.id}, 尺寸: {icon.rect.width}x{icon.rect.height}");
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// 重新加载指定家具的贴图
    /// </summary>
    public bool ReloadFurnitureIcon(string furnitureId)
    {
        FurnitureData furniture = GetFurnitureById(furnitureId);
        if (furniture != null)
        {
            return LoadFurnitureIcon(furniture);
        }
        return false;
    }

    /// <summary>
    /// 重新加载所有家具贴图
    /// </summary>
    [ContextMenu("重新加载所有家具贴图")]
    public void ReloadAllFurnitureIcons()
    {
        LoadAllFurnitureIcons();
    }

    /// <summary>
    /// 构建索引
    /// </summary>
    private void BuildIndices()
    {
        furnitureById.Clear();
        furnitureByRoom.Clear();

        foreach (var furniture in allFurniture)
        {
            // 按ID索引
            if (!furnitureById.ContainsKey(furniture.id))
            {
                furnitureById[furniture.id] = furniture;
            }
            else
            {
                Debug.LogWarning($"重复的家具ID: {furniture.id}");
            }

            // 按房间类型分组
            int roomType = furniture.requiredRoomType;
            if (!furnitureByRoom.ContainsKey(roomType))
            {
                furnitureByRoom[roomType] = new List<FurnitureData>();
            }
            furnitureByRoom[roomType].Add(furniture);
        }

        Debug.Log($"已建立索引: {furnitureById.Count}个ID, {furnitureByRoom.Count}个房间类型");
    }

    /// <summary>
    /// 获取所有家具
    /// </summary>
    public List<FurnitureData> GetAllFurniture()
    {
        return new List<FurnitureData>(allFurniture);
    }

    /// <summary>
    /// 根据房间类型获取家具
    /// </summary>
    public List<FurnitureData> GetFurnitureByRoom(int roomType)
    {
        if (furnitureByRoom.ContainsKey(roomType))
        {
            return new List<FurnitureData>(furnitureByRoom[roomType]);
        }
        return new List<FurnitureData>();
    }

    /// <summary>
    /// 根据ID获取家具
    /// </summary>
    public FurnitureData GetFurnitureById(string id)
    {
        if (furnitureById.ContainsKey(id))
        {
            return furnitureById[id];
        }
        return null;
    }

    /// <summary>
    /// 重新加载JSON数据（编辑器用）
    /// </summary>
    [ContextMenu("重新加载JSON数据")]
    public void ReloadFromJson()
    {
        LoadFurnitureData();
    }

    /// <summary>
    /// 获取家具贴图加载统计
    /// </summary>
    public string GetIconLoadStatistics()
    {
        int total = allFurniture.Count;
        int loaded = 0;

        foreach (var furniture in allFurniture)
        {
            if (furniture.icon != null)
            {
                loaded++;
            }
        }

        return $"贴图加载状态: {loaded}/{total}";
    }
}

/// <summary>
/// 家具数据数组包装器（用于JSON序列化）
/// </summary>
[System.Serializable]
public class FurnitureDataArray
{
    public FurnitureData[] furniture;
}