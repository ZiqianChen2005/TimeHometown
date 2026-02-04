using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
        CameraController.Instance.OnMoveToMainComplete += OnCameraMoveToMainComplete;
        CameraController.Instance.OnMoveToLoginComplete += OnCameraMoveToLoginComplete;
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
        // 淡出主界面
        if (mainCanvasGroup != null)
        {
            StartCoroutine(FadeOutMainUI());
        }

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