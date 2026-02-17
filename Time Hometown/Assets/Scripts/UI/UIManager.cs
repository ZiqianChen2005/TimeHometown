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
    [SerializeField] private GameObject expBarContainer;
    [SerializeField] private Image expBarBackground;
    [SerializeField] private Image expBarBuffer;
    [SerializeField] private Image expBarFill;
    [SerializeField] private Text levelText;
    [SerializeField] private Text expText;

    [Header("经验条设置")]
    [SerializeField] private float expFillSpeed = 0.5f;
    [SerializeField] private AnimationCurve expFillCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool showExpText = true;

    [Header("自律币系统")]
    [SerializeField] private Text coinsText;
    [SerializeField] private float coinAnimationDuration = 1f;
    [SerializeField] private AnimationCurve coinAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool showCoinChangeEffect = true;
    [SerializeField] private Text coinChangePopupPrefab;
    [SerializeField] private Transform coinChangePopupParent;

    // 经验条动画状态
    private bool isExpBarAnimating = false;
    private Coroutine expFillCoroutine;
    private Queue<int> pendingExpQueue = new Queue<int>();

    // 自律币系统变量
    private int currentCoins = 0;
    private int targetCoins = 0;
    private bool isCoinAnimating = false;
    private Coroutine coinAnimationCoroutine;
    private Queue<int> pendingCoinQueue = new Queue<int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        InitializeUI();
        SetupEventListeners();
    }

    private void Start()
    {
        // 绑定相机事件
        if (CameraController.Instance != null)
        {
            CameraController.Instance.OnMoveToMainComplete += OnCameraMoveToMainComplete;
            CameraController.Instance.OnMoveToLoginComplete += OnCameraMoveToLoginComplete;
        }

        // 确保GameDataManager存在
        EnsureGameDataManager();

        // 订阅数据变更事件
        SubscribeToDataEvents();

        // 初始化UI显示
        InitializeUIData();
    }

    private void OnDestroy()
    {
        // 清理事件绑定
        if (CameraController.Instance != null)
        {
            CameraController.Instance.OnMoveToMainComplete -= OnCameraMoveToMainComplete;
            CameraController.Instance.OnMoveToLoginComplete -= OnCameraMoveToLoginComplete;
        }

        // 取消订阅数据事件
        UnsubscribeFromDataEvents();
    }

    /// <summary>
    /// 确保GameDataManager存在
    /// </summary>
    private void EnsureGameDataManager()
    {
        if (GameDataManager.Instance == null)
        {
            GameObject managerObj = new GameObject("GameDataManager");
            managerObj.AddComponent<GameDataManager>();
            Debug.Log("自动创建GameDataManager");
        }
    }

    /// <summary>
    /// 订阅数据变更事件
    /// </summary>
    private void SubscribeToDataEvents()
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.OnCoinsChanged += OnCoinsDataChanged;
            GameDataManager.Instance.OnExpChanged += OnExpDataChanged;
            GameDataManager.Instance.OnLevelChanged += OnLevelDataChanged;
        }
    }

    /// <summary>
    /// 取消订阅数据事件
    /// </summary>
    private void UnsubscribeFromDataEvents()
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.OnCoinsChanged -= OnCoinsDataChanged;
            GameDataManager.Instance.OnExpChanged -= OnExpDataChanged;
            GameDataManager.Instance.OnLevelChanged -= OnLevelDataChanged;
        }
    }

    /// <summary>
    /// 初始化UI数据
    /// </summary>
    private void InitializeUIData()
    {
        if (GameDataManager.Instance != null)
        {
            // 初始化自律币
            currentCoins = GameDataManager.Instance.GetCoins();
            targetCoins = currentCoins;
            UpdateCoinsDisplay(currentCoins);

            // 初始化经验条
            InitializeExpBar();
        }
    }

    /// <summary>
    /// 初始化经验条
    /// </summary>
    private void InitializeExpBar()
    {
        if (expBarContainer != null && GameDataManager.Instance != null)
        {
            float fillAmount = GameDataManager.Instance.GetExpProgress();

            if (expBarFill != null)
                expBarFill.fillAmount = fillAmount;

            if (expBarBuffer != null)
                expBarBuffer.fillAmount = fillAmount;

            UpdateLevelDisplay(GameDataManager.Instance.GetLevel());
            UpdateExpText(GameDataManager.Instance.GetExp(), GameDataManager.Instance.GetMaxExpForCurrentLevel());
        }
    }

    /// <summary>
    /// 自律币数据变更回调
    /// </summary>
    private void OnCoinsDataChanged(int newCoins)
    {
        // 将变化加入队列
        int change = newCoins - currentCoins;
        if (change != 0)
        {
            pendingCoinQueue.Enqueue(change);

            if (!isCoinAnimating)
            {
                ProcessNextCoin();
            }
        }
    }

    /// <summary>
    /// 经验数据变更回调
    /// </summary>
    private void OnExpDataChanged(int newExp, int newLevel)
    {
        // 更新等级显示
        UpdateLevelDisplay(newLevel);

        // 计算经验变化
        if (GameDataManager.Instance != null)
        {
            int maxExp = GameDataManager.Instance.GetMaxExpForCurrentLevel();
            UpdateExpText(newExp, maxExp);

            // 将经验变化加入队列
            int change = newExp - (int)(expBarFill != null ? expBarFill.fillAmount * maxExp : 0);
            if (change > 0)
            {
                pendingExpQueue.Enqueue(change);

                if (!isExpBarAnimating)
                {
                    ProcessNextExp();
                }
            }
        }
    }

    /// <summary>
    /// 等级数据变更回调
    /// </summary>
    private void OnLevelDataChanged(int newLevel)
    {
        UpdateLevelDisplay(newLevel);
    }

    /// <summary>
    /// 更新等级显示
    /// </summary>
    private void UpdateLevelDisplay(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"Lv.{level}";
        }
    }

    /// <summary>
    /// 更新经验值文字
    /// </summary>
    private void UpdateExpText(int exp, int maxExp)
    {
        if (expText != null && showExpText)
        {
            expText.text = $"{exp}/{maxExp}";
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
    /// 处理下一个待添加的经验
    /// </summary>
    private void ProcessNextExp()
    {
        if (pendingExpQueue.Count == 0 || GameDataManager.Instance == null)
        {
            return;
        }

        int expToAdd = pendingExpQueue.Dequeue();
        int currentExp = GameDataManager.Instance.GetExp();
        int maxExp = GameDataManager.Instance.GetMaxExpForCurrentLevel();

        // 设置缓冲层
        if (expBarBuffer != null)
        {
            float bufferTargetAmount = Mathf.Min((float)currentExp / maxExp, 1f);
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
        float targetFillAmount = GameDataManager.Instance != null ?
            GameDataManager.Instance.GetExpProgress() : startFillAmount;

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

        if (expBarFill != null)
        {
            expBarFill.fillAmount = targetFillAmount;
        }

        isExpBarAnimating = false;
        expFillCoroutine = null;

        // 处理队列中的下一个经验值
        if (pendingExpQueue.Count > 0)
        {
            ProcessNextExp();
        }
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
        targetCoins = Mathf.Max(0, targetCoins);

        // 显示金币变化弹窗
        if (showCoinChangeEffect && coinChange != 0)
        {
            ShowCoinChangePopup(coinChange, coinChange > 0);
        }

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
    /// 自律币数值动画协程
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

            int displayCoins = Mathf.RoundToInt(Mathf.Lerp(startCoins, endCoins, curvedT));
            UpdateCoinsDisplay(displayCoins);

            yield return null;
        }

        UpdateCoinsDisplay(endCoins);
        currentCoins = endCoins;

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

        Text popupText = Instantiate(coinChangePopupPrefab, coinChangePopupParent ?? coinsText.transform.parent);

        string sign = isAdd ? "+" : "-";
        popupText.text = $"{sign}{amount}";
        popupText.color = isAdd ? Color.green : Color.red;

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

            rect.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
            popupText.color = new Color(popupText.color.r, popupText.color.g, popupText.color.b, 1 - t);

            yield return null;
        }

        Destroy(popupText.gameObject);
    }

    /// <summary>
    /// 添加经验（外部调用接口）
    /// </summary>
    public void AddExp(int amount)
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.AddExp(amount);
        }
    }

    /// <summary>
    /// 添加自律币（外部调用接口）
    /// </summary>
    public void AddCoins(int amount)
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.AddCoins(amount);
        }
    }

    /// <summary>
    /// 扣除自律币
    /// </summary>
    public bool SpendCoins(int amount)
    {
        return GameDataManager.Instance != null && GameDataManager.Instance.SpendCoins(amount);
    }

    /// <summary>
    /// 获取当前自律币
    /// </summary>
    public int GetCurrentCoins()
    {
        return GameDataManager.Instance != null ? GameDataManager.Instance.GetCoins() : 0;
    }

    /// <summary>
    /// 获取当前等级
    /// </summary>
    public int GetCurrentLevel()
    {
        return GameDataManager.Instance != null ? GameDataManager.Instance.GetLevel() : 1;
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
    /// 设置事件监听
    /// </summary>
    private void SetupEventListeners()
    {
        if (loginButton != null)
        {
            loginButton.onClick.RemoveAllListeners();
            loginButton.onClick.AddListener(OnLoginButtonClicked);
        }

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
        StartLoginTransition();
    }

    /// <summary>
    /// 开始登录过渡
    /// </summary>
    public void StartLoginTransition()
    {
        if (loginButton != null)
            loginButton.interactable = false;

        if (loginContainer != null && loginCanvasGroup != null)
        {
            StartCoroutine(FadeOutLoginUI());
        }
        OnCameraMoveToMainComplete();
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

        Debug.Log("登录界面已淡入");
    }

    /// <summary>
    /// 淡入主界面UI
    /// </summary>
    private IEnumerator FadeInMainUI()
    {
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

        mainContainer.SetActive(false);

        Debug.Log("主界面已淡出");
    }

    /// <summary>
    /// 当相机移动到主界面完成时调用（供SceneTransitionManager调用）
    /// </summary>
    public void OnMainSceneReady()
    {
        Debug.Log("SceneTransitionManager通知：主场景已就绪");

        // 可以在这里执行任何需要在主场景显示后进行的操作
        // 比如刷新数据等

        // 触发相机移动完成事件（如果需要）
        OnCameraMoveToMainComplete();
    }

    /// <summary>
    /// 获取主容器对象
    /// </summary>
    public GameObject GetMainContainer()
    {
        return mainContainer;
    }

    /// <summary>
    /// 获取登录容器对象
    /// </summary>
    public GameObject GetLoginContainer()
    {
        return loginContainer;
    }
}