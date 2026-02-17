using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("场景名称")]
    [SerializeField] private string mainSceneName = "MainScene";
    [SerializeField] private string shopSceneName = "Shop";

    [Header("按钮自动设置")]
    [SerializeField] private bool autoFindButtons = true; // 是否自动查找按钮
    [SerializeField] private string shopButtonTag = "ShopButton"; // 商店按钮Tag
    [SerializeField] private string backButtonTag = "BackButton"; // 返回按钮Tag
    [SerializeField] private string mainCanvasName = "Canvas"; // 主Canvas名称

    [Header("主界面容器")]
    [SerializeField] private string mainContainerName = "MainContainer"; // 主容器名称
    [SerializeField] private string loginContainerName = "LoginContainer"; // 登录容器名称
    [SerializeField] private float uiFadeDuration = 0.5f; // UI淡入淡出时间

    // 记录上一个场景，用于判断是否从商店返回
    private string previousSceneName = "";

    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        // 订阅场景加载完成事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // 取消订阅
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 场景加载完成回调
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"场景加载完成: {scene.name}, 上一个场景: {previousSceneName}");

        // 等待一帧确保场景完全加载
        StartCoroutine(DelayedSceneSetup(scene.name));
    }

    /// <summary>
    /// 延迟场景设置（等待一帧）
    /// </summary>
    private IEnumerator DelayedSceneSetup(string sceneName)
    {
        yield return null; // 等待一帧

        if (sceneName == mainSceneName)
        {
            SetupMainScene();
        }
        else if (sceneName == shopSceneName)
        {
            SetupShopScene();
        }

        // 更新上一个场景记录
        previousSceneName = sceneName;
    }

    /// <summary>
    /// 设置主场景
    /// </summary>
    private void SetupMainScene()
    {
        Debug.Log("设置主场景");

        // 自动设置按钮
        if (autoFindButtons)
        {
            SetupMainSceneButtons();
        }

        // 【关键修复】只有当从商店返回时才自动显示MainContainer
        if (previousSceneName == shopSceneName)
        {
            Debug.Log("检测到从商店返回，自动显示主容器");
            StartCoroutine(ShowMainContainerCoroutine());
        }
        else
        {
            Debug.Log("首次启动或从其他场景进入，不自动显示主容器");
            // 首次启动时，让UIManager处理登录界面的显示
            EnsureLoginScreenVisible();
        }
    }

    /// <summary>
    /// 设置商店场景
    /// </summary>
    private void SetupShopScene()
    {
        Debug.Log("设置商店场景");

        // 自动设置按钮
        if (autoFindButtons)
        {
            SetupShopSceneButtons();
        }

        // 商店场景不需要显示主容器
    }

    /// <summary>
    /// 确保登录界面可见（首次启动时）
    /// </summary>
    private void EnsureLoginScreenVisible()
    {
        // 查找Canvas
        Canvas mainCanvas = FindCanvas(mainCanvasName);
        if (mainCanvas == null)
        {
            mainCanvas = FindObjectOfType<Canvas>();
        }

        if (mainCanvas == null) return;

        // 查找LoginContainer并确保它可见
        foreach (Transform child in mainCanvas.transform)
        {
            if (child.name == loginContainerName)
            {
                child.gameObject.SetActive(true);

                CanvasGroup cg = child.GetComponent<CanvasGroup>();
                if (cg == null)
                    cg = child.gameObject.AddComponent<CanvasGroup>();

                cg.alpha = 1;
                cg.interactable = true;
                cg.blocksRaycasts = true;

                Debug.Log("确保登录界面可见");
                break;
            }
        }
    }

    /// <summary>
    /// 显示主容器协程
    /// </summary>
    private IEnumerator ShowMainContainerCoroutine()
    {
        // 查找Canvas
        Canvas mainCanvas = FindCanvas(mainCanvasName);
        if (mainCanvas == null)
        {
            Debug.LogWarning("未找到主Canvas，尝试查找所有Canvas");
            mainCanvas = FindObjectOfType<Canvas>();
        }

        if (mainCanvas == null)
        {
            Debug.LogError("无法找到Canvas，无法显示MainContainer");
            yield break;
        }

        // 查找MainContainer
        Transform mainContainer = null;
        Transform loginContainer = null;

        // 在Canvas下查找
        foreach (Transform child in mainCanvas.transform)
        {
            if (child.name == mainContainerName)
            {
                mainContainer = child;
            }
            else if (child.name == loginContainerName)
            {
                loginContainer = child;
            }
        }

        if (mainContainer == null)
        {
            Debug.LogError($"未找到名为 {mainContainerName} 的容器");
            yield break;
        }

        // 获取或添加CanvasGroup
        CanvasGroup mainCanvasGroup = mainContainer.GetComponent<CanvasGroup>();
        if (mainCanvasGroup == null)
        {
            mainCanvasGroup = mainContainer.gameObject.AddComponent<CanvasGroup>();
        }

        // 隐藏登录容器（如果存在）
        if (loginContainer != null)
        {
            CanvasGroup loginCanvasGroup = loginContainer.GetComponent<CanvasGroup>();
            if (loginCanvasGroup == null)
            {
                loginCanvasGroup = loginContainer.gameObject.AddComponent<CanvasGroup>();
            }

            loginCanvasGroup.alpha = 0;
            loginCanvasGroup.interactable = false;
            loginCanvasGroup.blocksRaycasts = false;
            loginContainer.gameObject.SetActive(false);
        }

        // 激活主容器
        mainContainer.gameObject.SetActive(true);

        // 淡入主容器
        float elapsed = 0f;
        mainCanvasGroup.alpha = 0;
        mainCanvasGroup.interactable = false;
        mainCanvasGroup.blocksRaycasts = false;

        while (elapsed < uiFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / uiFadeDuration;
            mainCanvasGroup.alpha = Mathf.Lerp(0, 1, t);
            yield return null;
        }

        mainCanvasGroup.alpha = 1;
        mainCanvasGroup.interactable = true;
        mainCanvasGroup.blocksRaycasts = true;

        Debug.Log("主容器已显示");

        // 触发UIManager的相机移动完成事件（如果UIManager存在）
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnMainSceneReady();
        }
    }

    /// <summary>
    /// 跳转到家具商店
    /// </summary>
    public void GoToFurnitureShop()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        // 记录当前场景为上一个场景，然后跳转
        previousSceneName = GetCurrentSceneName();
        SceneManager.LoadScene(shopSceneName);
        Debug.Log($"跳转到家具商店: {shopSceneName}");
    }

    /// <summary>
    /// 返回主场景
    /// </summary>
    public void GoToMainScene()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        // 记录当前场景为上一个场景，然后跳转
        previousSceneName = GetCurrentSceneName();
        SceneManager.LoadScene(mainSceneName);
        Debug.Log($"返回主场景: {mainSceneName}");
    }

    /// <summary>
    /// 获取当前场景名称
    /// </summary>
    public string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    /// <summary>
    /// 检查是否在商店场景
    /// </summary>
    public bool IsInShopScene()
    {
        return GetCurrentSceneName() == shopSceneName;
    }

    /// <summary>
    /// 检查是否在主场景
    /// </summary>
    public bool IsInMainScene()
    {
        return GetCurrentSceneName() == mainSceneName;
    }

    #region 按钮自动设置方法

    /// <summary>
    /// 设置主场景的按钮（进入商店）
    /// </summary>
    public void SetupMainSceneButtons()
    {
        Debug.Log("自动设置主场景按钮");

        // 查找Canvas
        Canvas mainCanvas = FindCanvas(mainCanvasName);
        if (mainCanvas == null)
        {
            Debug.LogWarning("未找到主Canvas，尝试查找所有Canvas");
            mainCanvas = FindObjectOfType<Canvas>();
        }

        if (mainCanvas == null)
        {
            Debug.LogError("无法找到Canvas，按钮自动设置失败");
            return;
        }

        // 查找所有按钮
        Button[] allButtons = mainCanvas.GetComponentsInChildren<Button>(true);

        foreach (Button btn in allButtons)
        {
            // 根据Tag查找
            if (!string.IsNullOrEmpty(shopButtonTag) && btn.CompareTag(shopButtonTag))
            {
                SetupButton(btn, GoToFurnitureShop, "商店按钮");
            }
            // 根据按钮名称或文本查找
            else if (btn.name.Contains("Shop") || btn.name.Contains("商店"))
            {
                SetupButton(btn, GoToFurnitureShop, "商店按钮");
            }
            else
            {
                // 检查按钮上的文本
                Text btnText = btn.GetComponentInChildren<Text>();
                if (btnText != null && (btnText.text.Contains("商店") || btnText.text.Contains("商城")))
                {
                    SetupButton(btn, GoToFurnitureShop, "商店按钮");
                }
            }
        }
    }

    /// <summary>
    /// 设置商店场景的按钮（返回主界面）
    /// </summary>
    public void SetupShopSceneButtons()
    {
        Debug.Log("自动设置商店场景按钮");

        // 查找Canvas
        Canvas shopCanvas = FindCanvas(mainCanvasName);
        if (shopCanvas == null)
        {
            Debug.LogWarning("未找到主Canvas，尝试查找所有Canvas");
            shopCanvas = FindObjectOfType<Canvas>();
        }

        if (shopCanvas == null)
        {
            Debug.LogError("无法找到Canvas，按钮自动设置失败");
            return;
        }

        // 查找所有按钮
        Button[] allButtons = shopCanvas.GetComponentsInChildren<Button>(true);

        foreach (Button btn in allButtons)
        {
            // 根据Tag查找
            if (!string.IsNullOrEmpty(backButtonTag) && btn.CompareTag(backButtonTag))
            {
                SetupButton(btn, GoToMainScene, "返回按钮");
            }
            // 根据按钮名称或文本查找
            else if (btn.name.Contains("Back") || btn.name.Contains("返回"))
            {
                SetupButton(btn, GoToMainScene, "返回按钮");
            }
            else
            {
                // 检查按钮上的文本
                Text btnText = btn.GetComponentInChildren<Text>();
                if (btnText != null && (btnText.text.Contains("返回") || btnText.text.Contains("后退")))
                {
                    SetupButton(btn, GoToMainScene, "返回按钮");
                }
            }
        }
    }

    /// <summary>
    /// 查找Canvas
    /// </summary>
    private Canvas FindCanvas(string canvasName)
    {
        GameObject canvasObj = GameObject.Find(canvasName);
        if (canvasObj != null)
        {
            return canvasObj.GetComponent<Canvas>();
        }
        return null;
    }

    /// <summary>
    /// 设置按钮点击事件
    /// </summary>
    private void SetupButton(Button button, UnityEngine.Events.UnityAction action, string buttonName)
    {
        if (button != null)
        {
            // 清除现有监听器
            button.onClick.RemoveAllListeners();
            // 添加新监听器
            button.onClick.AddListener(action);
            Debug.Log($"已自动设置{buttonName}: {button.name}");
        }
    }

    #endregion

    #region 手动设置按钮的方法（供外部调用）

    /// <summary>
    /// 手动设置一个按钮为商店按钮
    /// </summary>
    public void SetButtonAsShopButton(Button button)
    {
        if (button != null)
        {
            SetupButton(button, GoToFurnitureShop, "商店按钮");
        }
    }

    /// <summary>
    /// 手动设置一个按钮为返回按钮
    /// </summary>
    public void SetButtonAsBackButton(Button button)
    {
        if (button != null)
        {
            SetupButton(button, GoToMainScene, "返回按钮");
        }
    }

    /// <summary>
    /// 批量设置所有符合Tag的按钮
    /// </summary>
    public void SetupAllButtonsByTag()
    {
        // 查找所有带商店Tag的按钮
        GameObject[] shopButtons = GameObject.FindGameObjectsWithTag(shopButtonTag);
        foreach (GameObject btnObj in shopButtons)
        {
            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                SetupButton(btn, GoToFurnitureShop, "商店按钮");
            }
        }

        // 查找所有带返回Tag的按钮
        GameObject[] backButtons = GameObject.FindGameObjectsWithTag(backButtonTag);
        foreach (GameObject btnObj in backButtons)
        {
            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                SetupButton(btn, GoToMainScene, "返回按钮");
            }
        }
    }

    /// <summary>
    /// 重置所有按钮的点击事件
    /// </summary>
    public void ResetAllButtons()
    {
        // 查找所有Canvas
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
            foreach (Button btn in buttons)
            {
                btn.onClick.RemoveAllListeners();
            }
        }
        Debug.Log("已重置所有按钮点击事件");
    }

    #endregion

    #region 公共方法 - 手动控制UI显示

    /// <summary>
    /// 手动显示主容器（公共方法）
    /// </summary>
    public void ShowMainContainer()
    {
        if (GetCurrentSceneName() == mainSceneName)
        {
            StartCoroutine(ShowMainContainerCoroutine());
        }
        else
        {
            Debug.LogWarning($"当前场景不是主场景（当前: {GetCurrentSceneName()}），无法显示主容器");
        }
    }

    /// <summary>
    /// 设置主容器名称
    /// </summary>
    public void SetMainContainerName(string name)
    {
        mainContainerName = name;
    }

    /// <summary>
    /// 设置登录容器名称
    /// </summary>
    public void SetLoginContainerName(string name)
    {
        loginContainerName = name;
    }

    /// <summary>
    /// 获取上一个场景名称
    /// </summary>
    public string GetPreviousSceneName()
    {
        return previousSceneName;
    }

    #endregion

    #region 测试方法

    [ContextMenu("测试进入商店")]
    public void TestGoToShop()
    {
        GoToFurnitureShop();
    }

    [ContextMenu("测试返回主界面")]
    public void TestGoToMain()
    {
        GoToMainScene();
    }

    [ContextMenu("自动设置当前场景按钮")]
    public void TestSetupCurrentSceneButtons()
    {
        string currentScene = GetCurrentSceneName();
        if (currentScene == mainSceneName)
        {
            SetupMainSceneButtons();
        }
        else if (currentScene == shopSceneName)
        {
            SetupShopSceneButtons();
        }
        else
        {
            Debug.LogWarning($"当前场景 {currentScene} 不是主场景或商店场景");
        }
    }

    [ContextMenu("测试显示主容器")]
    public void TestShowMainContainer()
    {
        ShowMainContainer();
    }

    #endregion
}