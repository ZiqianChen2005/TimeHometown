using UnityEngine;
using UnityEngine.UI;

public class FocusHistoryItemUI : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private Image modeIconImage;
    [SerializeField] private Text modeText;
    [SerializeField] private Text timeRangeText;
    [SerializeField] private Text durationText;
    [SerializeField] private Text rewardText;
    [SerializeField] private Button itemButton;

    [Header("模式图标")]
    [SerializeField] private Sprite countdownIcon;
    [SerializeField] private Sprite countupIcon;
    [SerializeField] private Sprite tomatoIcon;

    private FocusHistoryData historyData;

    private void Awake()
    {
        if (itemButton != null)
        {
            itemButton.onClick.AddListener(OnItemClicked);
        }
    }

    /// <summary>
    /// 初始化历史记录项
    /// </summary>
    public void Initialize(FocusHistoryData data)
    {
        historyData = data;

        // 设置模式图标和文字
        if (modeIconImage != null)
        {
            switch (data.mode)
            {
                case "Countdown":
                    modeIconImage.sprite = countdownIcon;
                    break;
                case "Countup":
                    modeIconImage.sprite = countupIcon;
                    break;
                case "Tomato":
                    modeIconImage.sprite = tomatoIcon;
                    break;
            }
        }

        if (modeText != null)
        {
            modeText.text = data.GetModeDisplay();
        }

        // 设置时间范围
        if (timeRangeText != null)
        {
            timeRangeText.text = $"{data.GetFormattedStartTime()} - {data.GetFormattedEndTime()}";
        }

        // 设置专注时长
        if (durationText != null)
        {
            durationText.text = $"专注时长: {data.GetFormattedDuration()}";
        }

        // 设置奖励
        if (rewardText != null)
        {
            rewardText.text = $"获得: {data.earnedExp}经验 {data.earnedCoins}自律币";
        }
    }

    /// <summary>
    /// 点击历史记录项
    /// </summary>
    private void OnItemClicked()
    {
        if (historyData != null)
        {
            Debug.Log($"点击历史记录: {historyData.GetModeDisplay()} {historyData.GetFormattedDuration()}");
            // 可以在这里添加查看详情的逻辑
        }
    }

    /// <summary>
    /// 获取历史记录数据
    /// </summary>
    public FocusHistoryData GetHistoryData()
    {
        return historyData;
    }
}