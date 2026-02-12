using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI容器")]
    [SerializeField] private GameObject loginContainer;
    [SerializeField] private GameObject mainContainer;
    [SerializeField] private CanvasGroup loginCanvasGroup;
    [SerializeField] private CanvasGroup mainCanvasGroup;

    [Header("登录界面元素")]
    [SerializeField] private Button loginButton;
    [SerializeField] private Button testMoveButton;

    [Header("动画设置")]
    [SerializeField] private float uiFadeDuration = 0.5f;

    [Header("个人设置")]
    [SerializeField] private ProfilePanelController profilePanelController;

    [Header("经验条系统")]
    [SerializeField] private GameObject expBarContainer;      // 经验条容器
    [SerializeField] private Image expBarBackground;          // 背景层
    [SerializeField] private Image expBarBuffer;              // 缓冲层
    [SerializeField] private Image expBarFill;                // 填充层
    [SerializeField] private Text levelText;                 // 等级文字
    [SerializeField] private Text expText;                   // 经验值文字（可选）

    [Header("经验条设置")]
    [SerializeField] private float expFillSpeed = 0.5f;      // 填充速度（秒）
    [SerializeField] private AnimationCurve expFillCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool showExpText = true;        // 是否显示经验值文字

    [Header("自律币系统")]
    [SerializeField] private Text coinsText;                 // 自律币显示文字
    [SerializeField] private float coinAnimationDuration = 1f; // 自律币数值动画持续时间
    [SerializeField] private AnimationCurve coinAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 动画曲线
    [SerializeField] private bool showCoinChangeEffect = true; // 是否显示变化效果
    [SerializeField] private Text coinChangePopupPrefab;     // 金币变化弹窗预制体（可选）
    [SerializeField] private Transform coinChangePopupParent; // 金币变化弹窗父节点

    // 经验系统变量
    private int currentLevel = 1;
    private int currentExp = 0;
    private int targetExp = 0;
    private int maxExpForCurrentLevel = 100;

    // 经验条动画状态
    private bool isExpBarAnimating = false;
    private Coroutine expFillCoroutine;
    private Queue<int> pendingExpQueue = new Queue<int>();   // 待处理经验队列

    // 自律币系统变量
    private int currentCoins = 0;            // 当前自律币数量
    private int targetCoins = 0;             // 目标自律币数量
    private bool isCoinAnimating = false;    // 是否正在动画
    private Coroutine coinAnimationCoroutine; // 自律币动画协程
    private Queue<int> pendingCoinQueue = new Queue<int>(); // 待处理自律币队列

    // 等级经验公式常量
    private const float EXP_FORMULA_A = 6.2917f;
    private const float EXP_FORMULA_B = 0.4539f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        InitializeUI();
        SetupEventListeners();
        InitializeExpSystem();
        InitializeCoinSystem();
    }

    private void Start()
    {
        // 绑定相机事件
        if (CameraController.Instance != null)
        {
            CameraController.Instance.OnMoveToMainComplete += OnCameraMoveToMainComplete;
            CameraController.Instance.OnMoveToLoginComplete += OnCameraMoveToLoginComplete;
        }
    }

    private void OnDestroy()
    {
        // 清理事件绑定
        if (CameraController.Instance != null)
        {
            CameraController.Instance.OnMoveToMainComplete -= OnCameraMoveToMainComplete;
            CameraController.Instance.OnMoveToLoginComplete -= OnCameraMoveToLoginComplete;
        }
    }

    /// <summary>
    /// 初始化UI状态
    /// </summary>
    private void InitializeUI()
    {
        // 显示登录界面，隐藏主界面
        if (loginContainer != null)
        {
            loginContainer.SetActive(true);
            if (loginCanvasGroup != null)
            {
                loginCanvasGroup.alpha = 1;
                loginCanvasGroup.interactable = true;
                loginCanvasGroup.blocksRaycasts = true;
            }
        }

        if (mainContainer != null)
        {
            mainContainer.SetActive(false);
            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.alpha = 0;
                mainCanvasGroup.interactable = false;
                mainCanvasGroup.blocksRaycasts = false;
            }
        }

        Debug.Log("UI初始化完成：显示登录界面");
    }

    /// <summary>
    /// 初始化经验系统
    /// </summary>
    private void InitializeExpSystem()
    {
        // 从PlayerPrefs加载等级和经验
        LoadExpData();

        // 初始化经验条UI
        InitializeExpBar();

        Debug.Log($"经验系统初始化完成: 等级{currentLevel}, 经验{currentExp}/{maxExpForCurrentLevel}");
    }

    /// <summary>
    /// 初始化自律币系统
    /// </summary>
    private void InitializeCoinSystem()
    {
        // 从PlayerPrefs加载自律币数据
        LoadCoinData();

        // 初始化自律币显示
        UpdateCoinsDisplay(currentCoins);

        Debug.Log($"自律币系统初始化完成: 当前自律币 {currentCoins}");
    }

    /// <summary>
    /// 初始化经验条UI
    /// </summary>
    private void InitializeExpBar()
    {
        // 确保经验条组件存在
        if (expBarContainer != null)
        {
            // 设置经验条初始状态
            float fillAmount = (float)currentExp / maxExpForCurrentLevel;

            if (expBarFill != null)
            {
                expBarFill.fillAmount = fillAmount;
            }

            if (expBarBuffer != null)
            {
                expBarBuffer.fillAmount = fillAmount;
            }

            // 更新等级显示
            UpdateLevelDisplay();

            // 更新经验值文字
            UpdateExpText();
        }
        else
        {
            Debug.LogWarning("经验条容器未设置，经验系统将不可见");
        }
    }

    /// <summary>
    /// 更新等级显示
    /// </summary>
    private void UpdateLevelDisplay()
    {
        if (levelText != null)
        {
            levelText.text = $"Lv.{currentLevel}";
        }
    }

    /// <summary>
    /// 更新经验值文字
    /// </summary>
    private void UpdateExpText()
    {
        if (expText != null && showExpText)
        {
            expText.text = $"{currentExp}/{maxExpForCurrentLevel}";
        }
    }

    /// <summary>
    /// 更新自律币显示
    /// </summary>
    private void UpdateCoinsDisplay(int coins)
    {
        if (coinsText != null)
        {
            coinsText.text = coins.ToString();
        }
    }

    /// <summary>
    /// 加载经验数据
    /// </summary>
    private void LoadExpData()
    {
        currentLevel = PlayerPrefs.GetInt("Player_Level", 1);
        currentExp = PlayerPrefs.GetInt("Player_Exp", 0);

        // 计算当前等级所需最大经验
        maxExpForCurrentLevel = CalculateExpForLevel(currentLevel);

        // 确保经验值不超过上限（防止数据错误）
        if (currentExp >= maxExpForCurrentLevel)
        {
            HandleLevelUp(currentExp);
        }
    }

    /// <summary>
    /// 加载自律币数据
    /// </summary>
    private void LoadCoinData()
    {
        currentCoins = PlayerPrefs.GetInt("Player_Coins", 0);
        targetCoins = currentCoins;
    }

    /// <summary>
    /// 保存经验数据
    /// </summary>
    private void SaveExpData()
    {
        PlayerPrefs.SetInt("Player_Level", currentLevel);
        PlayerPrefs.SetInt("Player_Exp", currentExp);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 保存自律币数据
    /// </summary>
    private void SaveCoinData()
    {
        PlayerPrefs.SetInt("Player_Coins", currentCoins);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 计算指定等级所需经验（去尾法）
    /// </summary>
    /// <param name="level">等级，从1开始</param>
    /// <returns>该等级所需最大经验值</returns>
    public int CalculateExpForLevel(int level)
    {
        if (level < 1) level = 1;

        // 公式: 6.2917 * e^(0.4539 * x)
        float exp = EXP_FORMULA_A * Mathf.Exp(EXP_FORMULA_B * level);

        // 去尾法取整（直接舍弃小数部分）
        int result = Mathf.FloorToInt(exp);

        // 确保至少为1
        return Mathf.Max(1, result);
    }

    /// <summary>
    /// 添加经验值（外部调用接口）
    /// </summary>
    /// <param name="amount">增加的经验值</param>
    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        Debug.Log($"添加经验: +{amount}");

        // 播放获得经验音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySuccessSound();
        }

        // 将经验加入队列
        pendingExpQueue.Enqueue(amount);

        // 如果没有正在进行的动画，开始处理
        if (!isExpBarAnimating)
        {
            ProcessNextExp();
        }
    }

    /// <summary>
    /// 处理下一个待添加的经验
    /// </summary>
    private void ProcessNextExp()
    {
        if (pendingExpQueue.Count == 0)
        {
            return;
        }

        int expToAdd = pendingExpQueue.Dequeue();
        targetExp = currentExp + expToAdd;

        // 设置缓冲层：直接一口气到目标位置
        if (expBarBuffer != null)
        {
            // 计算目标经验占当前等级最大经验的比例
            float bufferTargetAmount = (float)targetExp / maxExpForCurrentLevel;
            // 不能超过1
            bufferTargetAmount = Mathf.Min(bufferTargetAmount, 1f);

            // 缓冲层直接设置为目标位置（一口气）
            expBarBuffer.fillAmount = bufferTargetAmount;
        }

        // 开始填充动画
        StartExpFillAnimation();
    }

    /// <summary>
    /// 开始经验条填充动画
    /// </summary>
    private void StartExpFillAnimation()
    {
        if (expFillCoroutine != null)
        {
            StopCoroutine(expFillCoroutine);
        }

        expFillCoroutine = StartCoroutine(ExpFillCoroutine());
    }

    /// <summary>
    /// 经验条填充协程
    /// </summary>
    private IEnumerator ExpFillCoroutine()
    {
        isExpBarAnimating = true;

        float startFillAmount = expBarFill != null ? expBarFill.fillAmount : 0f;
        float targetFillAmount = (float)targetExp / maxExpForCurrentLevel;
        targetFillAmount = Mathf.Min(targetFillAmount, 1f);

        float elapsed = 0f;

        while (elapsed < expFillSpeed)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / expFillSpeed);
            float curvedT = expFillCurve.Evaluate(t);

            float currentFillAmount = Mathf.Lerp(startFillAmount, targetFillAmount, curvedT);

            if (expBarFill != null)
            {
                expBarFill.fillAmount = currentFillAmount;
            }

            yield return null;
        }

        // 确保精确到达目标值
        if (expBarFill != null)
        {
            expBarFill.fillAmount = targetFillAmount;
        }

        // 更新当前经验值
        currentExp = targetExp;

        // 检查是否升级
        if (currentExp >= maxExpForCurrentLevel)
        {
            HandleLevelUp(currentExp);
        }
        else
        {
            // 没有升级，直接完成
            CompleteExpAddition();
        }
    }

    /// <summary>
    /// 处理升级逻辑
    /// </summary>
    /// <param name="totalExp">总经验值（包含超出部分）</param>
    private void HandleLevelUp(int totalExp)
    {
        int remainingExp = totalExp - maxExpForCurrentLevel;

        Debug.Log($"升级！等级 {currentLevel} → {currentLevel + 1}, 剩余经验: {remainingExp}");

        // 播放升级音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayNotificationSound();
        }

        // 等级+1
        currentLevel++;

        // 计算新等级的最大经验
        maxExpForCurrentLevel = CalculateExpForLevel(currentLevel);

        // 重置经验条
        if (expBarFill != null)
        {
            expBarFill.fillAmount = 0f;
        }

        if (expBarBuffer != null)
        {
            expBarBuffer.fillAmount = 0f;
        }

        // 更新等级显示
        UpdateLevelDisplay();

        // 设置当前经验为剩余经验
        currentExp = Mathf.Max(0, remainingExp);
        targetExp = currentExp;

        // 如果有剩余经验，更新缓冲层和填充层
        if (currentExp > 0)
        {
            float fillAmount = (float)currentExp / maxExpForCurrentLevel;

            if (expBarBuffer != null)
            {
                expBarBuffer.fillAmount = fillAmount;
            }

            if (expBarFill != null)
            {
                expBarFill.fillAmount = fillAmount;
            }
        }

        // 更新经验值文字
        UpdateExpText();

        // 保存数据
        SaveExpData();

        // 检查是否还能继续升级（连续升级）
        if (currentExp >= maxExpForCurrentLevel)
        {
            HandleLevelUp(currentExp);
        }
        else
        {
            CompleteExpAddition();
        }
    }

    /// <summary>
    /// 完成经验添加
    /// </summary>
    private void CompleteExpAddition()
    {
        // 更新经验值文字
        UpdateExpText();

        // 保存数据
        SaveExpData();

        isExpBarAnimating = false;
        expFillCoroutine = null;

        // 处理队列中的下一个经验值
        if (pendingExpQueue.Count > 0)
        {
            ProcessNextExp();
        }
    }

    #region 自律币系统核心方法

    /// <summary>
    /// 添加自律币（外部调用接口）
    /// </summary>
    /// <param name="amount">增加的数量</param>
    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        Debug.Log($"添加自律币: +{amount}");

        // 播放获得自律币音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySuccessSound();
        }

        // 显示金币变化弹窗
        ShowCoinChangePopup(amount, true);

        // 将自律币加入队列
        pendingCoinQueue.Enqueue(amount);

        // 如果没有正在进行的动画，开始处理
        if (!isCoinAnimating)
        {
            ProcessNextCoin();
        }
    }

    /// <summary>
    /// 扣除自律币（外部调用接口）
    /// </summary>
    /// <param name="amount">扣除的数量</param>
    /// <returns>是否扣除成功</returns>
    public bool SpendCoins(int amount)
    {
        if (amount <= 0) return false;

        // 检查余额是否足够
        if (currentCoins < amount)
        {
            Debug.LogWarning($"自律币不足，需要 {amount}，当前 {currentCoins}");

            // 播放错误音效
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayErrorSound();
            }

            return false;
        }

        Debug.Log($"扣除自律币: -{amount}");

        // 播放扣除自律币音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        // 显示金币变化弹窗
        ShowCoinChangePopup(amount, false);

        // 将负值加入队列（表示扣除）
        pendingCoinQueue.Enqueue(-amount);

        // 如果没有正在进行的动画，开始处理
        if (!isCoinAnimating)
        {
            ProcessNextCoin();
        }

        return true;
    }

    /// <summary>
    /// 设置自律币（直接设置数值，不带动画）
    /// </summary>
    public void SetCoins(int amount)
    {
        amount = Mathf.Max(0, amount);

        currentCoins = amount;
        targetCoins = amount;

        // 清空待处理队列
        pendingCoinQueue.Clear();

        // 停止动画
        if (coinAnimationCoroutine != null)
        {
            StopCoroutine(coinAnimationCoroutine);
            coinAnimationCoroutine = null;
        }

        isCoinAnimating = false;

        // 更新显示
        UpdateCoinsDisplay(currentCoins);

        // 保存数据
        SaveCoinData();

        Debug.Log($"设置自律币: {amount}");
    }

    /// <summary>
    /// 处理下一个待处理的自律币
    /// </summary>
    private void ProcessNextCoin()
    {
        if (pendingCoinQueue.Count == 0)
        {
            return;
        }

        int coinChange = pendingCoinQueue.Dequeue();
        targetCoins = currentCoins + coinChange;

        // 确保目标值不为负数
        targetCoins = Mathf.Max(0, targetCoins);

        // 开始数值动画
        StartCoinAnimation();
    }

    /// <summary>
    /// 开始自律币数值动画
    /// </summary>
    private void StartCoinAnimation()
    {
        if (coinAnimationCoroutine != null)
        {
            StopCoroutine(coinAnimationCoroutine);
        }

        coinAnimationCoroutine = StartCoroutine(CoinAnimationCoroutine());
    }

    /// <summary>
    /// 自律币数值动画协程（1秒递增动画）
    /// </summary>
    private IEnumerator CoinAnimationCoroutine()
    {
        isCoinAnimating = true;

        int startCoins = currentCoins;
        int endCoins = targetCoins;

        float elapsed = 0f;

        while (elapsed < coinAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / coinAnimationDuration);
            float curvedT = coinAnimationCurve.Evaluate(t);

            // 线性插值计算当前显示的值
            int displayCoins = Mathf.RoundToInt(Mathf.Lerp(startCoins, endCoins, curvedT));

            // 更新显示
            UpdateCoinsDisplay(displayCoins);

            yield return null;
        }

        // 确保精确到达目标值
        UpdateCoinsDisplay(endCoins);

        // 更新当前自律币数值
        currentCoins = endCoins;

        // 保存数据
        SaveCoinData();

        isCoinAnimating = false;
        coinAnimationCoroutine = null;

        // 处理队列中的下一个自律币变化
        if (pendingCoinQueue.Count > 0)
        {
            ProcessNextCoin();
        }
    }

    /// <summary>
    /// 显示自律币变化弹窗
    /// </summary>
    private void ShowCoinChangePopup(int amount, bool isAdd)
    {
        if (!showCoinChangeEffect || coinChangePopupPrefab == null) return;

        // 创建弹窗实例
        Text popupText = Instantiate(coinChangePopupPrefab, coinChangePopupParent ?? coinsText.transform.parent);

        // 设置文本和颜色
        string sign = isAdd ? "+" : "-";
        popupText.text = $"{sign}{amount}";
        popupText.color = isAdd ? Color.green : Color.red;

        // 播放动画（如果有）
        StartCoroutine(CoinPopupAnimation(popupText));
    }

    /// <summary>
    /// 自律币弹窗动画
    /// </summary>
    private IEnumerator CoinPopupAnimation(Text popupText)
    {
        if (popupText == null) yield break;

        RectTransform rect = popupText.rectTransform;
        Vector3 startPos = rect.anchoredPosition;
        Vector3 endPos = startPos + new Vector3(0, 50, 0);

        float duration = 1.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 向上移动
            rect.anchoredPosition = Vector3.Lerp(startPos, endPos, t);

            // 淡出
            popupText.color = new Color(popupText.color.r, popupText.color.g, popupText.color.b, 1 - t);

            yield return null;
        }

        // 销毁弹窗
        Destroy(popupText.gameObject);
    }

    /// <summary>
    /// 立即完成所有待处理的自律币动画
    /// </summary>
    public void CompleteAllCoinAnimations()
    {
        // 计算所有待处理自律币的总和
        int totalChange = 0;
        while (pendingCoinQueue.Count > 0)
        {
            totalChange += pendingCoinQueue.Dequeue();
        }

        if (totalChange != 0)
        {
            currentCoins += totalChange;
            currentCoins = Mathf.Max(0, currentCoins);
            targetCoins = currentCoins;

            UpdateCoinsDisplay(currentCoins);
            SaveCoinData();
        }

        // 停止动画
        if (coinAnimationCoroutine != null)
        {
            StopCoroutine(coinAnimationCoroutine);
            coinAnimationCoroutine = null;
        }

        isCoinAnimating = false;
    }

    /// <summary>
    /// 获取当前自律币数量
    /// </summary>
    public int GetCurrentCoins()
    {
        return currentCoins;
    }

    /// <summary>
    /// 获取目标自律币数量（动画目标值）
    /// </summary>
    public int GetTargetCoins()
    {
        return targetCoins;
    }

    /// <summary>
    /// 检查是否正在播放自律币动画
    /// </summary>
    public bool IsCoinAnimating()
    {
        return isCoinAnimating;
    }

    /// <summary>
    /// 重置自律币数据（测试用）
    /// </summary>
    [ContextMenu("重置自律币数据")]
    public void ResetCoinData()
    {
        PlayerPrefs.DeleteKey("Player_Coins");
        PlayerPrefs.Save();

        SetCoins(0);

        Debug.Log("自律币数据已重置");
    }

    /// <summary>
    /// 添加测试自律币（测试用）
    /// </summary>
    [ContextMenu("添加100自律币")]
    public void AddTestCoins100()
    {
        AddCoins(100);
    }

    [ContextMenu("添加500自律币")]
    public void AddTestCoins500()
    {
        AddCoins(500);
    }

    [ContextMenu("扣除100自律币")]
    public void SpendTestCoins100()
    {
        SpendCoins(100);
    }

    #endregion

    /// <summary>
    /// 设置经验条组件（外部调用，用于动态绑定）
    /// </summary>
    public void SetExpBarComponents(GameObject container, Image background, Image buffer, Image fill, Text level, Text exp = null)
    {
        expBarContainer = container;
        expBarBackground = background;
        expBarBuffer = buffer;
        expBarFill = fill;
        levelText = level;
        expText = exp;

        InitializeExpBar();
    }

    /// <summary>
    /// 获取当前等级
    /// </summary>
    public int GetCurrentLevel()
    {
        return currentLevel;
    }

    /// <summary>
    /// 获取当前经验
    /// </summary>
    public int GetCurrentExp()
    {
        return currentExp;
    }

    /// <summary>
    /// 获取当前等级最大经验
    /// </summary>
    public int GetMaxExpForCurrentLevel()
    {
        return maxExpForCurrentLevel;
    }

    /// <summary>
    /// 获取经验进度（0-1）
    /// </summary>
    public float GetExpProgress()
    {
        return (float)currentExp / maxExpForCurrentLevel;
    }

    /// <summary>
    /// 重置经验系统（测试用）
    /// </summary>
    [ContextMenu("重置经验数据")]
    public void ResetExpData()
    {
        PlayerPrefs.DeleteKey("Player_Level");
        PlayerPrefs.DeleteKey("Player_Exp");
        PlayerPrefs.Save();

        currentLevel = 1;
        currentExp = 0;
        maxExpForCurrentLevel = CalculateExpForLevel(1);

        InitializeExpBar();

        Debug.Log("经验数据已重置");
    }

    /// <summary>
    /// 添加测试经验（测试用）
    /// </summary>
    [ContextMenu("添加100经验")]
    public void AddTestExp100()
    {
        AddExp(100);
    }

    [ContextMenu("添加500经验")]
    public void AddTestExp500()
    {
        AddExp(500);
    }

    [ContextMenu("添加1000经验")]
    public void AddTestExp1000()
    {
        AddExp(1000);
    }

    /// <summary>
    /// 设置事件监听
    /// </summary>
    private void SetupEventListeners()
    {
        // 登录按钮
        if (loginButton != null)
        {
            loginButton.onClick.RemoveAllListeners();
            loginButton.onClick.AddListener(OnLoginButtonClicked);
        }

        // 测试按钮（可选）
        if (testMoveButton != null)
        {
            testMoveButton.onClick.RemoveAllListeners();
            testMoveButton.onClick.AddListener(() =>
            {
                CameraController.Instance.MoveToMainView();
            });
        }
    }

    /// <summary>
    /// 登录按钮点击事件
    /// </summary>
    public void OnLoginButtonClicked()
    {
        Debug.Log("登录按钮被点击");

        // 模拟登录验证成功
        StartLoginTransition();
    }

    /// <summary>
    /// 开始登录过渡
    /// </summary>
    public void StartLoginTransition()
    {
        // 禁用登录按钮防止重复点击
        if (loginButton != null)
            loginButton.interactable = false;

        // 淡出登录界面
        if (loginContainer != null && loginCanvasGroup != null)
        {
            StartCoroutine(FadeOutLoginUI());
        }
        OnCameraMoveToMainComplete();
        // 开始相机移动
        CameraController.Instance.MoveToMainView();
    }

    /// <summary>
    /// 淡出登录界面UI
    /// </summary>
    private IEnumerator FadeOutLoginUI()
    {
        float elapsed = 0f;
        float startAlpha = loginCanvasGroup.alpha;

        while (elapsed < uiFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / uiFadeDuration;
            loginCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0, t);
            yield return null;
        }

        loginCanvasGroup.alpha = 0;
        loginCanvasGroup.interactable = false;
        loginCanvasGroup.blocksRaycasts = false;

        // 延迟隐藏登录容器
        yield return new WaitForSeconds(0.1f);
        loginContainer.SetActive(false);

        Debug.Log("登录界面已淡出");
    }

    /// <summary>
    /// 淡入登录界面UI
    /// </summary>
    private IEnumerator FadeInLoginUI()
    {
        loginContainer.SetActive(true);

        float elapsed = 0f;
        float startAlpha = loginCanvasGroup.alpha;
        if (loginButton != null)
            loginButton.interactable = true;

        while (elapsed < uiFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / uiFadeDuration;
            loginCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1, t);
            yield return null;
        }

        loginCanvasGroup.alpha = 1;
        loginCanvasGroup.interactable = true;
        loginCanvasGroup.blocksRaycasts = true;

        yield return null;


        Debug.Log("登录界面已淡入");
    }

    /// <summary>
    /// 淡入主界面UI
    /// </summary>
    private IEnumerator FadeInMainUI()
    {
        // 确保主容器激活
        if (mainContainer != null && !mainContainer.activeSelf)
        {
            mainContainer.SetActive(true);
        }

        float elapsed = 0f;
        float startAlpha = mainCanvasGroup != null ? mainCanvasGroup.alpha : 0;

        while (elapsed < uiFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / uiFadeDuration;

            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1, t);
            }

            yield return null;
        }

        if (mainCanvasGroup != null)
        {
            mainCanvasGroup.alpha = 1;
            mainCanvasGroup.interactable = true;
            mainCanvasGroup.blocksRaycasts = true;
        }

        Debug.Log("主界面已淡入");
    }

    /// <summary>
    /// 相机移动到主界面完成事件
    /// </summary>
    private void OnCameraMoveToMainComplete()
    {
        Debug.Log("相机移动完成，开始显示主界面");

        // 淡入主界面
        StartCoroutine(FadeInMainUI());
    }

    /// <summary>
    /// 相机移动到登录界面完成事件
    /// </summary>
    private void OnCameraMoveToLoginComplete()
    {
        Debug.Log("相机移动完成，返回登录界面");

        if (loginContainer != null && loginCanvasGroup != null)
        {
            StartCoroutine(FadeInLoginUI());
        }
    }

    /// <summary>
    /// 登出功能
    /// </summary>
    public void Logout()
    {

        // 移动相机回登录界面
        CameraController.Instance.MoveToLoginView();
    }

    /// <summary>
    /// 淡出主界面UI
    /// </summary>
    private IEnumerator FadeOutMainUI()
    {
        float elapsed = 0f;
        float startAlpha = mainCanvasGroup.alpha;

        while (elapsed < uiFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / uiFadeDuration;
            mainCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0, t);
            yield return null;
        }

        mainCanvasGroup.alpha = 0;
        mainCanvasGroup.interactable = false;
        mainCanvasGroup.blocksRaycasts = false;

        // 隐藏主容器
        mainContainer.SetActive(false);

        Debug.Log("主界面已淡出");
    }
}
