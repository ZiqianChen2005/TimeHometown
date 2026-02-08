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
    [SerializeField] private Text timerModeLabel;

    [Header("小时设置")]
    [SerializeField] private Button hourIncreaseButton;
    [SerializeField] private Button hourDecreaseButton;
    [SerializeField] private Text hourLabelText;

    [Header("分钟设置")]
    [SerializeField] private Button minuteIncreaseButton;
    [SerializeField] private Button minuteDecreaseButton;
    [SerializeField] private Text minuteLabelText;

    [Header("控制按钮")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button stopButton;

    [Header("模式选择")]
    [SerializeField] private Dropdown modeDropdown;

    [Header("计时器设置")]
    [SerializeField] private int maxHours = 12;      // 最大小时数
    [SerializeField] private int maxMinutes = 59;    // 最大分钟数
    [SerializeField] private int minHours = 0;       // 最小小时数
    [SerializeField] private int minMinutes = 5;     // 最小分钟数
    [SerializeField] private int hourStep = 1;       // 小时增减步长
    [SerializeField] private int minuteStep = 5;     // 分钟增减步长

    [Header("闪烁效果")]
    [SerializeField] private float blinkInterval = 0.5f; // 闪烁间隔（秒）
    [SerializeField] private Color workBlinkColor1 = new Color(0.1f, 0.3f, 0.1f, 1f); // 工作状态颜色1（绿色）
    [SerializeField] private Color workBlinkColor2 = new Color(0.1f, 0.5f, 0.1f, 1f);  // 工作状态颜色2（亮绿色）
    [SerializeField] private Color completeBlinkColor1 = new Color(0.5f, 0.25f, 0f, 1f);  // 完成状态颜色1（橙色）
    [SerializeField] private Color completeBlinkColor2 = new Color(0.75f, 0.375f, 0f, 1f); // 完成状态颜色2（亮橙色）
    [SerializeField] private Color pausedColor = new Color(0.5f, 0.5f, 0.5f, 1f); // 暂停状态颜色（灰色）

    // 计时器模式
    public enum TimerMode
    {
        Countdown = 0,  // 倒计时
        Countup = 1,    // 正计时
        Tomato = 2      // 番茄钟
    }

    private TimerMode currentMode = TimerMode.Countdown;

    // 当前设置的时间
    private int currentHours = 0;
    private int currentMinutes = 25; // 默认25分钟（番茄工作法）

    // 计时器状态
    private bool isTimerActive = false;
    private bool isTimerPaused = false;
    private float currentSeconds = 0f;
    private float targetSeconds = 0f; // 目标时间（秒）

    // 番茄钟相关
    private bool isTomatoWorkPhase = true; // true: 工作阶段, false: 休息阶段
    private int tomatoWorkMinutes = 25; // 番茄工作时长（分钟）
    private int tomatoRestMinutes = 5;  // 番茄休息时长（分钟）

    // 闪烁相关
    private Coroutine blinkCoroutine;
    private bool shouldBlink = false;
    private bool isWorkStateBlink = true; // true: 工作状态闪烁, false: 完成状态闪烁

    private void Awake()
    {
        InitializeTimer();
    }

    private void Start()
    {
        UpdateTimeDisplay();
        UpdateButtonStates();
        InitializeModeDropdown();
    }

    private void Update()
    {
        if (isTimerActive && !isTimerPaused)
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
        }

        if (hourDecreaseButton != null)
        {
            hourDecreaseButton.onClick.RemoveAllListeners();
            hourDecreaseButton.onClick.AddListener(DecreaseHours);
        }

        if (minuteIncreaseButton != null)
        {
            minuteIncreaseButton.onClick.RemoveAllListeners();
            minuteIncreaseButton.onClick.AddListener(IncreaseMinutes);
        }

        if (minuteDecreaseButton != null)
        {
            minuteDecreaseButton.onClick.RemoveAllListeners();
            minuteDecreaseButton.onClick.AddListener(DecreaseMinutes);
        }

        // 设置控制按钮事件
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartButtonClicked);
        }

        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(OnPauseButtonClicked);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(OnResumeButtonClicked);
        }

        if (stopButton != null)
        {
            stopButton.onClick.RemoveAllListeners();
            stopButton.onClick.AddListener(OnStopButtonClicked);
        }

        // 设置标签文本
        if (hourLabelText != null)
            hourLabelText.text = "小时";

        if (minuteLabelText != null)
            minuteLabelText.text = "分钟";

        if (colonText != null)
            colonText.text = ":";

        // 初始化时间
        targetSeconds = GetTotalSeconds();
        SetTimeColors(workBlinkColor1);
        currentSeconds = 0f;

        Debug.Log("专注计时器初始化完成");
    }

    /// <summary>
    /// 初始化模式下拉框
    /// </summary>
    private void InitializeModeDropdown()
    {
        if (modeDropdown != null)
        {
            // 清空现有选项
            modeDropdown.ClearOptions();

            // 添加模式选项
            modeDropdown.options.Add(new Dropdown.OptionData("倒计时"));
            modeDropdown.options.Add(new Dropdown.OptionData("正计时"));
            modeDropdown.options.Add(new Dropdown.OptionData("番茄钟"));

            // 设置默认值
            modeDropdown.value = (int)currentMode;

            // 添加事件监听
            modeDropdown.onValueChanged.RemoveAllListeners();
            modeDropdown.onValueChanged.AddListener(OnModeChanged);

            // 更新模式标签
            UpdateTimerModeLabel();
        }
    }

    /// <summary>
    /// 模式改变事件
    /// </summary>
    private void OnModeChanged(int modeIndex)
    {
        if (isTimerActive)
        {
            // 如果计时器正在运行，先停止
            StopTimer();
        }

        currentMode = (TimerMode)modeIndex;
        UpdateTimerModeLabel();

        // 根据模式重置计时器
        ResetTimerForMode();

        Debug.Log($"切换计时模式: {currentMode}");
    }

    /// <summary>
    /// 根据模式重置计时器
    /// </summary>
    private void ResetTimerForMode()
    {
        switch (currentMode)
        {
            case TimerMode.Countdown:
                // 倒计时：从设定时间开始倒计时
                targetSeconds = GetTotalSeconds();
                currentSeconds = targetSeconds;
                break;

            case TimerMode.Countup:
                // 正计时：从0开始
                targetSeconds = GetTotalSeconds();
                currentSeconds = 0f;
                break;

            case TimerMode.Tomato:
                // 番茄钟：使用默认的25分钟工作+5分钟休息
                tomatoWorkMinutes = Mathf.Clamp(currentMinutes, 5, 60);
                tomatoRestMinutes = 5; // 固定5分钟休息
                targetSeconds = tomatoWorkMinutes * 60;
                currentSeconds = targetSeconds;
                isTomatoWorkPhase = true;
                break;
        }

        UpdateTimeDisplay();
        UpdateButtonStates();
        StopBlinkEffect(); // 重置时停止闪烁
    }

    /// <summary>
    /// 更新计时器模式标签
    /// </summary>
    private void UpdateTimerModeLabel()
    {
        if (timerModeLabel != null)
        {
            string modeText = "";
            switch (currentMode)
            {
                case TimerMode.Countdown:
                    modeText = "当前模式：倒计时模式";
                    break;
                case TimerMode.Countup:
                    modeText = "当前模式：正计时模式";
                    break;
                case TimerMode.Tomato:
                    modeText = "当前模式：番茄钟模式";
                    break;
            }
            timerModeLabel.text = modeText;
        }
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
            ResetTimerForMode();
        }
        else
        {
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
            // 检查减少后总分钟数是否不低于5分钟
            int totalMinutesAfterDecrease = (newHours * 60) + currentMinutes;

            if (totalMinutesAfterDecrease >= minMinutes)
            {
                currentHours = newHours;
                UpdateTimeDisplay();
                UpdateButtonStates();
                ResetTimerForMode();
            }
            else
            {
                PlayLimitReachedFeedback();
            }
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
        ResetTimerForMode();
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
                ResetTimerForMode();
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
                ResetTimerForMode();
                return;
            }
        }

        PlayLimitReachedFeedback();
    }

    /// <summary>
    /// 更新时间显示
    /// </summary>
    private void UpdateTimeDisplay()
    {
        string hourString = "";
        string minuteString = "";

        if (isTimerActive)
        {
            // 计时器运行中，显示当前时间
            int displayHours, displayMinutes;

            if (currentMode == TimerMode.Countdown || currentMode== TimerMode.Tomato && isTomatoWorkPhase)
            {
                // 倒计时/番茄钟工作模式：向上取整（Mathf.CeilToInt）
                displayHours = Mathf.FloorToInt(currentSeconds / 3600f);
                displayMinutes = Mathf.CeilToInt((currentSeconds % 3600f) / 60f);

                // 确保不超过上限
                if (displayMinutes >= 60)
                {
                    displayHours += displayMinutes / 60;
                    displayMinutes = displayMinutes % 60;
                }
            }
            else
            {
                // 正计时/番茄钟休息模式：向下取整（Mathf.FloorToInt）
                displayHours = Mathf.FloorToInt(currentSeconds / 3600f);
                displayMinutes = Mathf.FloorToInt((currentSeconds % 3600f) / 60f);
            }

            hourString = displayHours.ToString("D2");
            minuteString = displayMinutes.ToString("D2");
        }
        else
        {
            // 计时器未运行，显示设定时间
            hourString = currentHours.ToString("D2");
            minuteString = currentMinutes.ToString("D2");
        }

        // 更新整体时间显示
        if (timeDisplayText != null)
        {
            timeDisplayText.text = $"{hourString}:{minuteString}";
        }

        // 更新单独的小时和分钟显示
        if (hourDisplayText != null)
        {
            hourDisplayText.text = hourString;
        }

        if (minuteDisplayText != null)
        {
            minuteDisplayText.text = minuteString;
        }
    }

    /// <summary>
    /// 更新按钮状态
    /// </summary>
    private void UpdateButtonStates()
    {
        // 时间调整按钮：计时器工作时禁用，但保持显示
        // 需要根据实际限制条件设置可交互性
        if (hourIncreaseButton != null)
        {
            hourIncreaseButton.gameObject.SetActive(true);
            bool canIncrease = (currentHours + hourStep <= maxHours) && !isTimerActive;
            hourIncreaseButton.interactable = canIncrease;
        }

        if (hourDecreaseButton != null)
        {
            hourDecreaseButton.gameObject.SetActive(true);
            bool canDecrease = (currentHours - hourStep >= minHours) && !isTimerActive;
            if (canDecrease)
            {
                // 检查减少后总分钟数是否不低于5分钟
                int totalMinutesAfterDecrease = ((currentHours - hourStep) * 60) + currentMinutes;
                canDecrease = totalMinutesAfterDecrease >= minMinutes;
            }
            hourDecreaseButton.interactable = canDecrease;
        }

        if (minuteIncreaseButton != null)
        {
            minuteIncreaseButton.gameObject.SetActive(true);
            // 计算增加后的总分钟数
            int totalMinutesAfterIncrease = (currentHours * 60) + currentMinutes + minuteStep;
            int maxTotalMinutes = maxHours * 60 + maxMinutes;
            bool canIncrease = (totalMinutesAfterIncrease <= maxTotalMinutes) && !isTimerActive;
            minuteIncreaseButton.interactable = canIncrease;
        }

        if (minuteDecreaseButton != null)
        {
            minuteDecreaseButton.gameObject.SetActive(true);
            // 计算减少后的总分钟数
            int totalMinutesAfterDecrease = (currentHours * 60) + currentMinutes - minuteStep;
            bool canDecrease = (totalMinutesAfterDecrease >= minMinutes) && !isTimerActive;
            minuteDecreaseButton.interactable = canDecrease;
        }

        // 控制按钮状态
        if (startButton != null)
        {
            startButton.gameObject.SetActive(!isTimerActive);
        }

        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(isTimerActive && !isTimerPaused);
        }

        if (resumeButton != null)
        {
            resumeButton.gameObject.SetActive(isTimerActive && isTimerPaused);
        }

        if (stopButton != null)
        {
            stopButton.gameObject.SetActive(isTimerActive);
        }

        // 模式选择器
        if (modeDropdown != null)
        {
            modeDropdown.gameObject.SetActive(!isTimerActive);
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
    }

    /// <summary>
    /// 开始按钮点击事件
    /// </summary>
    private void OnStartButtonClicked()
    {
        PlayButtonClickSound();
        StartTimer();
    }

    /// <summary>
    /// 暂停按钮点击事件
    /// </summary>
    private void OnPauseButtonClicked()
    {
        PlayButtonClickSound();
        PauseTimer();
    }

    /// <summary>
    /// 继续按钮点击事件
    /// </summary>
    private void OnResumeButtonClicked()
    {
        PlayButtonClickSound();
        ResumeTimer();
    }

    /// <summary>
    /// 停止按钮点击事件
    /// </summary>
    private void OnStopButtonClicked()
    {
        PlayButtonClickSound();
        StopTimer();
    }

    /// <summary>
    /// 获取总秒数
    /// </summary>
    public float GetTotalSeconds()
    {
        return (currentHours * 3600f) + (currentMinutes * 60f);
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

        // 确保不低于下限（至少5分钟）
        int totalMinutes = (hours * 60) + minutes;
        if (totalMinutes < minMinutes)
        {
            // 调整到最小允许值
            hours = minMinutes / 60;
            minutes = minMinutes % 60;
            if (minutes == 0 && hours == 0)
            {
                minutes = minMinutes;
            }
        }

        currentHours = hours;
        currentMinutes = minutes;

        UpdateTimeDisplay();
        UpdateButtonStates();
        ResetTimerForMode();
    }

    /// <summary>
    /// 更新计时器
    /// </summary>
    private void UpdateTimer()
    {
        if (!isTimerActive || isTimerPaused) return;

        bool blinkStateChanged = false;
        bool newShouldBlink = false;
        bool newIsWorkStateBlink = true;

        switch (currentMode)
        {
            case TimerMode.Countdown:
                // 倒计时
                currentSeconds -= Time.deltaTime;
                if (currentSeconds <= 0)
                {
                    currentSeconds = 0;
                    // 倒计时结束，停止计时
                    OnTimerComplete();
                    // 停止后显示工作状态颜色（不是完成状态）
                    newShouldBlink = true;
                    newIsWorkStateBlink = true;
                }
                else
                {
                    // 倒计时未结束，工作状态闪烁
                    newShouldBlink = true;
                    newIsWorkStateBlink = true;
                }
                break;

            case TimerMode.Countup:
                // 正计时
                currentSeconds += Time.deltaTime;
                if (currentSeconds >= targetSeconds)
                {
                    // 正计时完成，停止计时
                    OnTimerComplete();
                    // 停止后显示工作状态颜色（不是完成状态）
                    newShouldBlink = true;
                    newIsWorkStateBlink = true;
                }
                else
                {
                    // 正计时未完成，工作状态闪烁
                    newShouldBlink = true;
                    newIsWorkStateBlink = true;
                }
                break;

            case TimerMode.Tomato:
                // 番茄钟
                currentSeconds -= Time.deltaTime;
                if (currentSeconds <= 0)
                {
                    // 番茄钟阶段切换
                    SwitchTomatoPhase();
                    // 阶段切换后继续计时，显示对应状态颜色
                    newShouldBlink = true;
                    newIsWorkStateBlink = isTomatoWorkPhase;
                }
                else
                {
                    // 番茄钟阶段进行中
                    newShouldBlink = true;
                    newIsWorkStateBlink = isTomatoWorkPhase;
                }
                break;
        }

        UpdateTimeDisplay();

        // 检查是否需要更新闪烁状态
        if (newShouldBlink != shouldBlink || newIsWorkStateBlink != isWorkStateBlink)
        {
            shouldBlink = newShouldBlink;
            isWorkStateBlink = newIsWorkStateBlink;
            blinkStateChanged = true;
        }

        // 更新闪烁效果
        UpdateBlinkEffect(blinkStateChanged);

        // 播放提示音（可选，如最后1分钟提示）
        PlayTimerAlerts();
    }



    /// <summary>
    /// 更新闪烁效果
    /// </summary>
    private void UpdateBlinkEffect(bool forceRestart = false)
    {
        if (isTimerPaused)
        {
            // 暂停状态：显示暂停颜色，停止闪烁
            StopBlinkEffect();
            SetTimeColors(pausedColor);
            return;
        }

        if (!shouldBlink)
        {
            // 不需要闪烁：停止闪烁，恢复默认颜色
            StopBlinkEffect();
            SetTimeColors(workBlinkColor1);
            return;
        }

        // 需要闪烁：确保闪烁协程运行
        if (blinkCoroutine == null || forceRestart)
        {
            // 停止现有的闪烁协程
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
            }

            // 启动新的闪烁协程
            blinkCoroutine = StartCoroutine(BlinkTimerCoroutine());
        }
    }

    /// <summary>
    /// 停止闪烁效果
    /// </summary>
    private void StopBlinkEffect()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
    }

    /// <summary>
    /// 设置时间颜色
    /// </summary>
    private void SetTimeColors(Color color)
    {
        if (timeDisplayText != null)
        {
            timeDisplayText.color = color;
        }
        if (colonText != null)
        {
            colonText.color = color;
        }
        if (hourDisplayText != null)
        {
            hourDisplayText.color = color;
        }
        if (minuteDisplayText != null)
        {
            minuteDisplayText.color = color;
        }
    }

    /// <summary>
    /// 闪烁协程
    /// </summary>
    private IEnumerator BlinkTimerCoroutine()
    {
        Color color1, color2;

        if (isWorkStateBlink)
        {
            // 工作状态闪烁（绿色系）
            color1 = workBlinkColor1;
            color2 = workBlinkColor2;
        }
        else
        {
            // 完成状态闪烁（橙色系）
            color1 = completeBlinkColor1;
            color2 = completeBlinkColor2;
        }

        while (isTimerActive && !isTimerPaused && shouldBlink)
        {
            // 切换到颜色1
            SetTimeColors(color1);
            yield return new WaitForSeconds(blinkInterval);

            if (!isTimerActive || isTimerPaused || !shouldBlink) break;

            // 切换到颜色2
            SetTimeColors(color2);
            yield return new WaitForSeconds(blinkInterval);
        }

        blinkCoroutine = null;
    }

    /// <summary>
    /// 播放计时器提示音
    /// </summary>
    private void PlayTimerAlerts()
    {
        // 可以根据需要添加提示音
        // 例如：最后1分钟、最后30秒等
    }

    /// <summary>
    /// 计时器完成回调
    /// </summary>
    private void OnTimerComplete()
    {
        Debug.Log("计时器完成！");

        // 停止计时器
        isTimerActive = false;
        isTimerPaused = false;

        // 播放完成音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySuccessSound();
        }

        // 显示完成提示（可以扩展）
        ShowTimerCompleteMessage();

        // 更新按钮状态
        UpdateButtonStates();

        // 【新增】强制恢复默认颜色（工作状态颜色）
        SetTimeColors(workBlinkColor1);

        // 注意：这里不调用 StopBlinkEffect()，而是让闪烁继续显示工作状态颜色
        // 因为在 UpdateTimer() 中已经设置了 shouldBlink = true 和 isWorkStateBlink = true
    }

    /// <summary>
    /// 显示计时器完成消息
    /// </summary>
    private void ShowTimerCompleteMessage()
    {
        string message = "";
        switch (currentMode)
        {
            case TimerMode.Countdown:
                message = "倒计时结束！";
                break;
            case TimerMode.Countup:
                message = "正计时达到目标时间！";
                break;
            case TimerMode.Tomato:
                message = isTomatoWorkPhase ? "番茄工作时间结束！" : "番茄休息时间结束！";
                break;
        }

        Debug.Log(message);
        // 这里可以显示UI提示
    }

    /// <summary>
    /// 切换番茄钟阶段
    /// </summary>
    private void SwitchTomatoPhase()
    {
        if (isTomatoWorkPhase)
        {
            // 工作阶段结束，切换到休息阶段
            isTomatoWorkPhase = false;
            targetSeconds = tomatoRestMinutes * 60;
            currentSeconds = targetSeconds;
            Debug.Log("切换到番茄休息阶段");

            // 播放提示音
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayNotificationSound();
            }
        }
        else
        {
            // 休息阶段结束，切换到工作阶段
            isTomatoWorkPhase = true;
            targetSeconds = tomatoWorkMinutes * 60;
            currentSeconds = targetSeconds;
            Debug.Log("切换到番茄工作阶段");

            // 播放提示音
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayNotificationSound();
            }
        }

        UpdateTimeDisplay();
    }

    /// <summary>
    /// 开始计时
    /// </summary>
    public void StartTimer()
    {
        if (!isTimerActive)
        {
            isTimerActive = true;
            isTimerPaused = false;

            // 根据模式初始化时间
            ResetTimerForMode();

            Debug.Log($"开始计时：{currentMode} 模式");
            UpdateButtonStates();
        }
    }

    /// <summary>
    /// 暂停计时
    /// </summary>
    public void PauseTimer()
    {
        if (isTimerActive && !isTimerPaused)
        {
            isTimerPaused = true;
            Debug.Log("计时器已暂停");

            // 更新闪烁效果（显示暂停颜色）
            UpdateBlinkEffect();

            UpdateButtonStates();
        }
    }

    /// <summary>
    /// 继续计时
    /// </summary>
    public void ResumeTimer()
    {
        if (isTimerActive && isTimerPaused)
        {
            isTimerPaused = false;
            Debug.Log("计时器已恢复");

            // 重新评估并更新闪烁效果
            UpdateBlinkEffect(true); // 强制重启闪烁

            UpdateButtonStates();
        }
    }

    /// <summary>
    /// 停止计时
    /// </summary>
    public void StopTimer()
    {
        if (isTimerActive)
        {
            isTimerActive = false;
            isTimerPaused = false;

            // 停止闪烁
            StopBlinkEffect();

            // 重置为初始状态
            ResetTimerForMode();

            // 【新增】强制恢复默认颜色（工作状态颜色）
            SetTimeColors(workBlinkColor1);

            Debug.Log("计时器已停止");
            UpdateButtonStates();
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
    /// 检查计时器是否暂停
    /// </summary>
    public bool IsTimerPaused()
    {
        return isTimerPaused;
    }

    /// <summary>
    /// 获取当前模式
    /// </summary>
    public TimerMode GetCurrentMode()
    {
        return currentMode;
    }

    /// <summary>
    /// 设置番茄钟工作时间
    /// </summary>
    public void SetTomatoWorkMinutes(int minutes)
    {
        tomatoWorkMinutes = Mathf.Clamp(minutes, 5, 60);
        if (currentMode == TimerMode.Tomato)
        {
            ResetTimerForMode();
        }
    }

    /// <summary>
    /// 设置番茄钟休息时间
    /// </summary>
    public void SetTomatoRestMinutes(int minutes)
    {
        tomatoRestMinutes = Mathf.Clamp(minutes, 1, 30);
        if (currentMode == TimerMode.Tomato && !isTomatoWorkPhase)
        {
            ResetTimerForMode();
        }
    }
}
