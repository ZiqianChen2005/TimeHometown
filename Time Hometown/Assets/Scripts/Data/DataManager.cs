using UnityEngine;
using System;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    // 数据变更事件
    public event Action<int> OnCoinsChanged;
    public event Action<int, int> OnExpChanged; // 参数：当前经验, 等级
    public event Action<int> OnLevelChanged;

    // 玩家数据
    private int currentCoins = 0;
    private int currentExp = 0;
    private int currentLevel = 1;
    private int maxExpForCurrentLevel = 100;

    // 等级经验公式常量
    private const float EXP_FORMULA_A = 6.2917f;
    private const float EXP_FORMULA_B = 0.4539f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAllData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 确保UIManager和FocusHistoryManager都能接收到初始数据
        BroadcastData();
    }

    /// <summary>
    /// 加载所有数据
    /// </summary>
    private void LoadAllData()
    {
        // 加载自律币
        currentCoins = PlayerPrefs.GetInt("Player_Coins", 0);

        // 加载经验数据
        currentLevel = PlayerPrefs.GetInt("Player_Level", 1);
        currentExp = PlayerPrefs.GetInt("Player_Exp", 0);

        // 计算当前等级所需最大经验
        maxExpForCurrentLevel = CalculateExpForLevel(currentLevel);

        // 确保经验值不超过上限（防止数据错误）
        if (currentExp >= maxExpForCurrentLevel)
        {
            HandleLevelUp(currentExp);
        }

        Debug.Log($"GameDataManager加载数据: 金币={currentCoins}, 等级={currentLevel}, 经验={currentExp}/{maxExpForCurrentLevel}");
    }

    /// <summary>
    /// 保存所有数据
    /// </summary>
    private void SaveAllData()
    {
        PlayerPrefs.SetInt("Player_Coins", currentCoins);
        PlayerPrefs.SetInt("Player_Level", currentLevel);
        PlayerPrefs.SetInt("Player_Exp", currentExp);
        PlayerPrefs.Save();

        Debug.Log($"GameDataManager保存数据: 金币={currentCoins}, 等级={currentLevel}, 经验={currentExp}");
    }

    /// <summary>
    /// 广播数据到所有监听器
    /// </summary>
    private void BroadcastData()
    {
        OnCoinsChanged?.Invoke(currentCoins);
        OnExpChanged?.Invoke(currentExp, currentLevel);
        OnLevelChanged?.Invoke(currentLevel);
    }

    /// <summary>
    /// 计算指定等级所需经验（去尾法）
    /// </summary>
    public int CalculateExpForLevel(int level)
    {
        if (level < 1) level = 1;

        float exp = EXP_FORMULA_A * Mathf.Exp(EXP_FORMULA_B * level);
        return Mathf.Max(1, Mathf.FloorToInt(exp));
    }

    /// <summary>
    /// 获取当前等级最大经验
    /// </summary>
    public int GetMaxExpForCurrentLevel()
    {
        return maxExpForCurrentLevel;
    }

    /// <summary>
    /// 处理升级逻辑
    /// </summary>
    private void HandleLevelUp(int totalExp)
    {
        int remainingExp = totalExp - maxExpForCurrentLevel;

        Debug.Log($"GameDataManager升级！等级 {currentLevel} → {currentLevel + 1}, 剩余经验: {remainingExp}");

        currentLevel++;
        maxExpForCurrentLevel = CalculateExpForLevel(currentLevel);
        currentExp = Mathf.Max(0, remainingExp);

        // 触发等级变更事件
        OnLevelChanged?.Invoke(currentLevel);

        // 如果还有剩余经验，继续升级
        if (currentExp >= maxExpForCurrentLevel)
        {
            HandleLevelUp(currentExp);
        }
    }

    #region 自律币相关方法

    /// <summary>
    /// 获取当前自律币
    /// </summary>
    public int GetCoins()
    {
        return currentCoins;
    }

    /// <summary>
    /// 添加自律币
    /// </summary>
    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        currentCoins += amount;
        SaveAllData();
        OnCoinsChanged?.Invoke(currentCoins);

        Debug.Log($"GameDataManager添加自律币: +{amount}, 当前: {currentCoins}");
    }

    /// <summary>
    /// 扣除自律币
    /// </summary>
    public bool SpendCoins(int amount)
    {
        if (amount < 0) return false;

        if (currentCoins < amount)
        {
            Debug.LogWarning($"GameDataManager自律币不足: 需要{amount}, 当前{currentCoins}");
            return false;
        }

        currentCoins -= amount;
        SaveAllData();
        OnCoinsChanged?.Invoke(currentCoins);

        Debug.Log($"GameDataManager扣除自律币: -{amount}, 当前: {currentCoins}");
        return true;
    }

    /// <summary>
    /// 设置自律币（直接设置）
    /// </summary>
    public void SetCoins(int amount)
    {
        amount = Mathf.Max(0, amount);
        currentCoins = amount;
        SaveAllData();
        OnCoinsChanged?.Invoke(currentCoins);
    }

    #endregion

    #region 经验相关方法

    /// <summary>
    /// 获取当前经验
    /// </summary>
    public int GetExp()
    {
        return currentExp;
    }

    /// <summary>
    /// 获取当前等级
    /// </summary>
    public int GetLevel()
    {
        return currentLevel;
    }

    /// <summary>
    /// 获取经验进度（0-1）
    /// </summary>
    public float GetExpProgress()
    {
        return (float)currentExp / maxExpForCurrentLevel;
    }

    /// <summary>
    /// 添加经验
    /// </summary>
    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        currentExp += amount;

        // 触发经验变更事件
        OnExpChanged?.Invoke(currentExp, currentLevel);

        // 检查是否升级
        if (currentExp >= maxExpForCurrentLevel)
        {
            HandleLevelUp(currentExp);
        }

        SaveAllData();

        Debug.Log($"GameDataManager添加经验: +{amount}, 当前: {currentExp}/{maxExpForCurrentLevel}, 等级: {currentLevel}");
    }

    /// <summary>
    /// 设置经验（直接设置）
    /// </summary>
    public void SetExp(int exp)
    {
        exp = Mathf.Max(0, exp);
        currentExp = exp;

        // 检查是否升级
        while (currentExp >= maxExpForCurrentLevel)
        {
            HandleLevelUp(currentExp);
        }

        SaveAllData();
        OnExpChanged?.Invoke(currentExp, currentLevel);
    }

    #endregion

    #region 数据重置方法（测试用）

    [ContextMenu("重置所有数据")]
    public void ResetAllData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        currentCoins = 0;
        currentLevel = 1;
        currentExp = 0;
        maxExpForCurrentLevel = CalculateExpForLevel(1);

        BroadcastData();

        Debug.Log("GameDataManager重置所有数据");
    }

    [ContextMenu("添加1000测试金币")]
    public void AddTestCoins()
    {
        AddCoins(1000);
    }

    [ContextMenu("添加500测试经验")]
    public void AddTestExp()
    {
        AddExp(500);
    }

    #endregion
}