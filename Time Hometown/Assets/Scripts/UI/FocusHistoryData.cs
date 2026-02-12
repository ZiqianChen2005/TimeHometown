using UnityEngine;
using System;

/// <summary>
/// 专注历史记录数据类
/// </summary>
[System.Serializable]
public class FocusHistoryData
{
    public string id;                       // 唯一ID
    public string mode;                    // 专注模式
    public DateTime startTime;            // 开始时间
    public DateTime endTime;              // 结束时间
    public int durationMinutes;           // 专注时长（分钟，向下取整）
    public int earnedExp;                // 获得经验
    public int earnedCoins;              // 获得自律币

    /// <summary>
    /// 构造函数
    /// </summary>
    public FocusHistoryData(string mode, DateTime startTime, DateTime endTime, int durationMinutes, int earnedExp, int earnedCoins)
    {
        this.id = Guid.NewGuid().ToString();
        this.mode = mode;
        this.startTime = startTime;
        this.endTime = endTime;
        this.durationMinutes = durationMinutes;
        this.earnedExp = earnedExp;
        this.earnedCoins = earnedCoins;
    }

    /// <summary>
    /// 获取格式化的开始时间
    /// </summary>
    public string GetFormattedStartTime()
    {
        return startTime.ToString("yyyy/MM/dd HH:mm:ss");
    }

    /// <summary>
    /// 获取格式化的结束时间
    /// </summary>
    public string GetFormattedEndTime()
    {
        return endTime.ToString("yyyy/MM/dd HH:mm:ss");
    }

    /// <summary>
    /// 获取格式化的专注时长
    /// </summary>
    public string GetFormattedDuration()
    {
        int hours = durationMinutes / 60;
        int minutes = durationMinutes % 60;

        if (hours > 0)
            return $"{hours}小时{minutes}分钟";
        else
            return $"{minutes}分钟";
    }

    /// <summary>
    /// 获取模式显示文本
    /// </summary>
    public string GetModeDisplay()
    {
        switch (mode)
        {
            case "Countdown":
                return "倒计时";
            case "Countup":
                return "正计时";
            case "Tomato":
                return "番茄钟";
            default:
                return mode;
        }
    }
}