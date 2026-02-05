using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FocusTimerController : MonoBehaviour
{
    [Header("时间显示")]
    [SerializeField] private Text timeDisplayText;
    [SerializeField] private Text hourDisplayText;
    [SerializeField] private Text minuteDisplayText;
    [SerializeField] private Text colonText;

    [Header("小时设置")]
    [SerializeField] private Button hourIncreaseButton;
    [SerializeField] private Button hourDecreaseButton;
    [SerializeField] private Text hourLabelText;

    [Header("分钟设置")]
    [SerializeField] private Button minuteIncreaseButton;
    [SerializeField] private Button minuteDecreaseButton;
    [SerializeField] private Text minuteLabelText;

    [Header("计时器设置")]
    [SerializeField] private int maxHours = 12;      // 最大小时数
    [SerializeField] private int maxMinutes = 59;    // 最大分钟数
    [SerializeField] private int minHours = 0;       // 最小小时数
    [SerializeField] private int minMinutes = 5;     // 最小分钟数
    [SerializeField] private int hourStep = 1;       // 小时增减步长
    [SerializeField] private int minuteStep = 5;     // 分钟增减步长（推荐5分钟步长）

    [Header("按钮颜色")]
    [SerializeField] private Color normalButtonColor = new Color(0.7f, 0.8f, 1f, 1f);     // 蓝色
    [SerializeField] private Color disabledButtonColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 灰色半透明

    [Header("时间颜色")]
    [SerializeField] private Color normalTimeColor = new Color(0.1f, 0.63f, 0.2f);
    // 当前设置的时间
    private int currentHours = 0;
    private int currentMinutes = 25; // 默认25分钟（番茄工作法）

    // 计时器状态
    private bool isTimerActive = false;
    private float remainingSeconds = 0f;

    private void Awake()
    {
        InitializeTimer();
    }

    private void Start()
    {
        UpdateTimeDisplay();
        UpdateButtonStates();
    }

    private void Update()
    {
        if (isTimerActive)
        {
            UpdateTimer();
        }
    }

    /// <summary>
    /// 初始化计时器
    /// </summary>
    private void InitializeTimer()
    {
        // 设置按钮事件
        if (hourIncreaseButton != null)
        {
            hourIncreaseButton.onClick.RemoveAllListeners();
            hourIncreaseButton.onClick.AddListener(IncreaseHours);
            SetupButtonHoverEffect(hourIncreaseButton);
        }

        if (hourDecreaseButton != null)
        {
            hourDecreaseButton.onClick.RemoveAllListeners();
            hourDecreaseButton.onClick.AddListener(DecreaseHours);
            SetupButtonHoverEffect(hourDecreaseButton);
        }

        if (minuteIncreaseButton != null)
        {
            minuteIncreaseButton.onClick.RemoveAllListeners();
            minuteIncreaseButton.onClick.AddListener(IncreaseMinutes);
            SetupButtonHoverEffect(minuteIncreaseButton);
        }

        if (minuteDecreaseButton != null)
        {
            minuteDecreaseButton.onClick.RemoveAllListeners();
            minuteDecreaseButton.onClick.AddListener(DecreaseMinutes);
            SetupButtonHoverEffect(minuteDecreaseButton);
        }

        // 设置标签文本
        if (hourLabelText != null)
            hourLabelText.text = "小时";

        if (minuteLabelText != null)
            minuteLabelText.text = "分钟";

        if (colonText != null)
            colonText.text = ":";

        Debug.Log("专注计时器初始化完成");
    }

    /// <summary>
    /// 设置按钮悬停效果
    /// </summary>
    private void SetupButtonHoverEffect(Button button)
    {
        if (button == null) return;

        // 添加悬停效果组件
        ButtonHoverEffect hoverEffect = button.gameObject.GetComponent<ButtonHoverEffect>();
        if (hoverEffect == null)
        {
            hoverEffect = button.gameObject.AddComponent<ButtonHoverEffect>();
        }

        hoverEffect.normalColor = normalButtonColor;
        hoverEffect.hoverColor = new Color(
            normalButtonColor.r * 1.2f,
            normalButtonColor.g * 1.2f,
            normalButtonColor.b * 1.2f,
            normalButtonColor.a
        );
        hoverEffect.transitionDuration = 0.1f;
    }

    /// <summary>
    /// 增加小时数
    /// </summary>
    public void IncreaseHours()
    {
        PlayButtonClickSound();

        int newHours = currentHours + hourStep;

        if (newHours <= maxHours)
        {
            currentHours = newHours;
            UpdateTimeDisplay();
            UpdateButtonStates();
        }
        else
        {
            // 播放错误音效或提供视觉反馈
            PlayLimitReachedFeedback();
        }
    }

    /// <summary>
    /// 减少小时数
    /// </summary>
    public void DecreaseHours()
    {
        PlayButtonClickSound();

        int newHours = currentHours - hourStep;

        if (newHours >= minHours)
        {
            currentHours = newHours;
            UpdateTimeDisplay();
            UpdateButtonStates();
        }
        else
        {
            PlayLimitReachedFeedback();
        }
    }

    /// <summary>
    /// 增加分钟数
    /// </summary>
    public void IncreaseMinutes()
    {
        PlayButtonClickSound();

        int newMinutes = currentMinutes + minuteStep;

        if (newMinutes <= maxMinutes)
        {
            currentMinutes = newMinutes;
        }
        else
        {
            // 如果分钟超过59，进位到小时
            int extraHours = (newMinutes) / 60;
            int remainingMinutes = newMinutes % 60;

            if (currentHours + extraHours <= maxHours)
            {
                currentHours += extraHours;
                currentMinutes = remainingMinutes;
            }
            else
            {
                // 达到上限
                PlayLimitReachedFeedback();
                return;
            }
        }

        UpdateTimeDisplay();
        UpdateButtonStates();
    }

    /// <summary>
    /// 减少分钟数
    /// </summary>
    public void DecreaseMinutes()
    {
        PlayButtonClickSound();

        int newMinutes = currentMinutes - minuteStep;

        if (newMinutes >= 0)
        {
            // 检查减少后的总分钟数是否不低于5分钟
            int totalMinutesAfterDecrease = (currentHours * 60) + newMinutes;

            if (totalMinutesAfterDecrease >= minMinutes)
            {
                currentMinutes = newMinutes;
                UpdateTimeDisplay();
                UpdateButtonStates();
                return;
            }
        }
        else
        {
            // 如果分钟小于0，向小时借位
            int borrowHours = Mathf.CeilToInt(Mathf.Abs(newMinutes) / 60f);
            int remainingMinutes = 60 - (Mathf.Abs(newMinutes) % 60);

            if (remainingMinutes == 60)
            {
                remainingMinutes = 0;
                borrowHours--;
            }

            // 检查借位后总分钟数是否不低于5分钟
            int newHours = currentHours - borrowHours;
            int totalMinutesAfterDecrease = (newHours * 60) + remainingMinutes;

            if (newHours >= minHours && totalMinutesAfterDecrease >= minMinutes)
            {
                currentHours = newHours;
                currentMinutes = remainingMinutes;
                UpdateTimeDisplay();
                UpdateButtonStates();
                return;
            }
        }
    }

    /// <summary>
    /// 更新时间显示
    /// </summary>
    private void UpdateTimeDisplay()
    {
        // 格式化显示：时时：分分
        string hourString = currentHours.ToString("D2");
        string minuteString = currentMinutes.ToString("D2");

        // 更新整体时间显示
        if (timeDisplayText != null)
        {
            timeDisplayText.text = $"{hourString}:{minuteString}";
            timeDisplayText.color = normalTimeColor;
        }

        // 更新单独的小时和分钟显示
        if (hourDisplayText != null)
        {
            hourDisplayText.text = hourString;
            hourDisplayText.color = normalTimeColor;
        }

        if (minuteDisplayText != null)
        {
            minuteDisplayText.text = minuteString;
            minuteDisplayText.color = normalTimeColor;
        }

        // 更新冒号颜色
        if (colonText != null)
        {
            colonText.color = normalTimeColor;
        }

        // 更新标签颜色
        if (hourLabelText != null)
        {
            hourLabelText.color = normalTimeColor;
        }

        if (minuteLabelText != null)
        {
            minuteLabelText.color = normalTimeColor;
        }
    }

    /// <summary>
    /// 更新按钮状态
    /// </summary>
    private void UpdateButtonStates()
    {
        // 小时增加按钮
        if (hourIncreaseButton != null)
        {
            bool canIncrease = (currentHours + hourStep <= maxHours);
            hourIncreaseButton.interactable = canIncrease;
            UpdateButtonColor(hourIncreaseButton, canIncrease);
        }

        // 小时减少按钮
        if (hourDecreaseButton != null)
        {
            bool canDecrease = (currentHours - hourStep >= minHours);
            hourDecreaseButton.interactable = canDecrease;
            UpdateButtonColor(hourDecreaseButton, canDecrease);
        }

        // 分钟增加按钮
        if (minuteIncreaseButton != null)
        {
            // 计算增加后的总分钟数
            int totalMinutesAfterIncrease = (currentHours * 60) + currentMinutes + minuteStep;
            bool canIncrease = (totalMinutesAfterIncrease <= maxHours * 60 + maxMinutes);
            minuteIncreaseButton.interactable = canIncrease;
            UpdateButtonColor(minuteIncreaseButton, canIncrease);
        }

        // 分钟减少按钮
        if (minuteDecreaseButton != null)
        {
            // 计算减少后的总分钟数
            int totalMinutesAfterDecrease = (currentHours * 60) + currentMinutes - minuteStep;
            bool canDecrease = (totalMinutesAfterDecrease >= 5);
            minuteDecreaseButton.interactable = canDecrease;
            UpdateButtonColor(minuteDecreaseButton, canDecrease);
        }
    }

    /// <summary>
    /// 更新按钮颜色
    /// </summary>
    private void UpdateButtonColor(Button button, bool isEnabled)
    {
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = isEnabled ? normalButtonColor : disabledButtonColor;
        }
    }

    /// <summary>
    /// 播放按钮点击音效
    /// </summary>
    private void PlayButtonClickSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }
    }

    /// <summary>
    /// 播放达到限制的反馈
    /// </summary>
    private void PlayLimitReachedFeedback()
    {
        // 播放错误音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayErrorSound();
        }

        // 可以添加视觉反馈，比如按钮抖动
        // StartCoroutine(ShakeButton(...));
    }

    /// <summary>
    /// 获取总秒数
    /// </summary>
    public float GetTotalSeconds()
    {
        return (currentHours * 3600f) + (currentMinutes * 60f);
    }

    /// <summary>
    /// 获取格式化时间字符串
    /// </summary>
    public string GetFormattedTime()
    {
        return $"{currentHours:D2}:{currentMinutes:D2}";
    }

    /// <summary>
    /// 获取小时数
    /// </summary>
    public int GetHours()
    {
        return currentHours;
    }

    /// <summary>
    /// 获取分钟数
    /// </summary>
    public int GetMinutes()
    {
        return currentMinutes;
    }

    /// <summary>
    /// 设置时间（外部调用）
    /// </summary>
    public void SetTime(int hours, int minutes)
    {
        // 验证输入
        hours = Mathf.Clamp(hours, minHours, maxHours);
        minutes = Mathf.Clamp(minutes, minMinutes, maxMinutes);

        // 处理分钟进位
        if (minutes >= 60)
        {
            hours += minutes / 60;
            minutes = minutes % 60;
        }

        // 确保不超过上限
        if (hours > maxHours)
        {
            hours = maxHours;
            minutes = maxMinutes;
        }

        currentHours = hours;
        currentMinutes = minutes;

        UpdateTimeDisplay();
        UpdateButtonStates();
    }

    /// <summary>
    /// 重置为默认时间（25分钟）
    /// </summary>
    public void ResetToDefault()
    {
        SetTime(0, 25);
    }

    /// <summary>
    /// 重置为最大值（12小时）
    /// </summary>
    public void ResetToMax()
    {
        SetTime(maxHours, maxMinutes);
    }

    /// <summary>
    /// 重置为最小值（0分钟）
    /// </summary>
    public void ResetToMin()
    {
        SetTime(minHours, minMinutes);
    }

    /// <summary>
    /// 更新计时器（当计时器运行时调用）
    /// </summary>
    private void UpdateTimer()
    {
        if (!isTimerActive || remainingSeconds <= 0) return;

        remainingSeconds -= Time.deltaTime;

        if (remainingSeconds <= 0)
        {
            remainingSeconds = 0;
            isTimerActive = false;
            OnTimerComplete();
        }

        // 更新显示（如果需要实时显示剩余时间）
        // UpdateRemainingTimeDisplay();
    }

    /// <summary>
    /// 计时器完成回调
    /// </summary>
    private void OnTimerComplete()
    {
        Debug.Log("计时器完成！");
        // 这里可以添加完成音效或通知
    }

    /// <summary>
    /// 公共接口：开始计时
    /// </summary>
    public void StartTimer()
    {
        if (!isTimerActive)
        {
            remainingSeconds = GetTotalSeconds();
            isTimerActive = true;
            Debug.Log($"开始计时：{GetFormattedTime()}");
        }
    }

    /// <summary>
    /// 公共接口：停止计时
    /// </summary>
    public void StopTimer()
    {
        isTimerActive = false;
        Debug.Log("计时器已停止");
    }

    /// <summary>
    /// 公共接口：暂停计时
    /// </summary>
    public void PauseTimer()
    {
        isTimerActive = false;
        Debug.Log("计时器已暂停");
    }

    /// <summary>
    /// 公共接口：恢复计时
    /// </summary>
    public void ResumeTimer()
    {
        if (remainingSeconds > 0)
        {
            isTimerActive = true;
            Debug.Log("计时器已恢复");
        }
    }

    /// <summary>
    /// 检查计时器是否在运行
    /// </summary>
    public bool IsTimerActive()
    {
        return isTimerActive;
    }

    /// <summary>
    /// 获取剩余秒数
    /// </summary>
    public float GetRemainingSeconds()
    {
        return remainingSeconds;
    }

    /// <summary>
    /// 获取剩余时间（格式化为字符串）
    /// </summary>
    public string GetRemainingTimeFormatted()
    {
        int remainingHours = Mathf.FloorToInt(remainingSeconds / 3600f);
        int remainingMinutes = Mathf.FloorToInt((remainingSeconds % 3600f) / 60f);
        return $"{remainingHours:D2}:{remainingMinutes:D2}";
    }
}