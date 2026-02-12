using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class FocusHistoryManager : MonoBehaviour
{
    public static FocusHistoryManager Instance { get; private set; }

    [Header("历史记录设置")]
    [SerializeField] private int maxHistoryRecords = 100; // 最大保存记录数

    private List<FocusHistoryData> historyRecords = new List<FocusHistoryData>();
    private const string HISTORY_SAVE_KEY = "FocusHistoryRecords";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadHistory();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 添加新的专注记录
    /// </summary>
    public void AddHistoryRecord(FocusHistoryData record)
    {
        historyRecords.Insert(0, record); // 最新的记录放在最前面

        // 限制最大记录数
        if (historyRecords.Count > maxHistoryRecords)
        {
            historyRecords.RemoveAt(historyRecords.Count - 1);
        }

        SaveHistory();
        Debug.Log($"添加专注历史记录: {record.GetModeDisplay()} {record.GetFormattedDuration()}");
    }

    /// <summary>
    /// 获取所有历史记录
    /// </summary>
    public List<FocusHistoryData> GetAllHistoryRecords()
    {
        return new List<FocusHistoryData>(historyRecords);
    }

    /// <summary>
    /// 获取最近N条历史记录
    /// </summary>
    public List<FocusHistoryData> GetRecentHistoryRecords(int count)
    {
        return historyRecords.Take(Mathf.Min(count, historyRecords.Count)).ToList();
    }

    /// <summary>
    /// 根据模式筛选历史记录
    /// </summary>
    public List<FocusHistoryData> GetHistoryRecordsByMode(string mode)
    {
        return historyRecords.Where(r => r.mode == mode).ToList();
    }

    /// <summary>
    /// 获取今日专注总时长（分钟）
    /// </summary>
    public int GetTodayTotalMinutes()
    {
        DateTime today = DateTime.Today;
        return historyRecords
            .Where(r => r.startTime.Date == today)
            .Sum(r => r.durationMinutes);
    }

    /// <summary>
    /// 获取本周专注总时长（分钟）
    /// </summary>
    public int GetWeekTotalMinutes()
    {
        DateTime weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
        return historyRecords
            .Where(r => r.startTime.Date >= weekStart)
            .Sum(r => r.durationMinutes);
    }

    /// <summary>
    /// 获取本月专注总时长（分钟）
    /// </summary>
    public int GetMonthTotalMinutes()
    {
        DateTime monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        return historyRecords
            .Where(r => r.startTime.Date >= monthStart)
            .Sum(r => r.durationMinutes);
    }

    /// <summary>
    /// 清空所有历史记录
    /// </summary>
    public void ClearAllHistory()
    {
        historyRecords.Clear();
        SaveHistory();
        Debug.Log("已清空所有专注历史记录");
    }

    /// <summary>
    /// 保存历史记录到PlayerPrefs
    /// </summary>
    private void SaveHistory()
    {
        string json = JsonHelper.ToJson(historyRecords.ToArray());
        PlayerPrefs.SetString(HISTORY_SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 从PlayerPrefs加载历史记录
    /// </summary>
    private void LoadHistory()
    {
        if (PlayerPrefs.HasKey(HISTORY_SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(HISTORY_SAVE_KEY);
            FocusHistoryData[] records = JsonHelper.FromJson<FocusHistoryData>(json);
            historyRecords = new List<FocusHistoryData>(records);
            Debug.Log($"加载专注历史记录: {historyRecords.Count}条");
        }
        else
        {
            historyRecords = new List<FocusHistoryData>();
            Debug.Log("没有找到专注历史记录");
        }
    }

    /// <summary>
    /// 获取总记录数
    /// </summary>
    public int GetRecordCount()
    {
        return historyRecords.Count;
    }
}

/// <summary>
/// JSON辅助类（处理数组序列化）
/// </summary>
public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
        return wrapper.Items;
    }

    public static string ToJson<T>(T[] array)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.Items = array;
        return JsonUtility.ToJson(wrapper);
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }
}