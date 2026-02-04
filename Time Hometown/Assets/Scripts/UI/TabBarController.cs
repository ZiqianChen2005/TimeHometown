using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class TabBarController : MonoBehaviour
{
    public static TabBarController Instance;

    [System.Serializable]
    public class TabInfo
    {
        public Button tabButton;
        public Color activeColor = new Color(0f, 1f, 1f, 1f);   // 青色 #00FFFF
        public Color inactiveColor = Color.white;               // 白色 #FFFFFF
        public GameObject contentPanel;
        public string tabName;

        // 淡入淡出设置
        [Range(0.1f, 2f)]
        public float fadeDuration = 0.3f;
        public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }

    [Header("标签配置")]
    [SerializeField] private TabInfo[] tabs;
    [SerializeField] private int defaultTabIndex = 2; // 默认选中家园标签

    [Header("全局淡入淡出设置")]
    [SerializeField] private bool useGlobalFade = true;
    [SerializeField] private float globalFadeDuration = 0.3f;
    [SerializeField] private AnimationCurve globalFadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("选中指示器（可选）")]
    [SerializeField] private Image[] selectionIndicators; // 可选的下划线或背景高亮
    [SerializeField] private float indicatorFadeDuration = 0.2f;

    [Header("默认状态设置")]
    [SerializeField] private bool highlightDefaultOnStart = true; // 启动时高亮默认标签
    [SerializeField] private bool skipInitialFadeForDefault = false; // 跳过默认标签的初始淡入，直接显示

    private int currentTabIndex = -1;
    public Image[] buttonImages; // 缓存按钮的Image组件
    private Coroutine[] fadeCoroutines; // 跟踪淡入淡出协程

    // 事件
    public event Action<int> OnTabChanged;
    public event Action<string> OnTabNameChanged;
    public event Action<int, bool> OnTabFadeComplete; // 淡入淡出完成事件

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeTabs();
        CacheButtonImages();
        InitializeFadeSystem();
    }

    private void Start()
    {
        // 默认选中家园标签（带淡入效果）
        StartCoroutine(DelayedInitialSelection());
    }

    private IEnumerator DelayedInitialSelection()
    {
        // 等待一帧确保所有组件初始化完成
        yield return null;

        // 初始淡入所有标签
        if (highlightDefaultOnStart)
        {
            // 如果有跳过初始淡入的设置，先直接显示默认标签
            if (skipInitialFadeForDefault && defaultTabIndex >= 0 && defaultTabIndex < tabs.Length)
            {
                // 直接设置默认标签为亮起状态（不淡入）
                if (buttonImages[defaultTabIndex] != null)
                {
                    buttonImages[defaultTabIndex].color = tabs[defaultTabIndex].activeColor;
                }
                // 设置默认标签按钮为选中状态
                if (defaultTabIndex >= 0 && defaultTabIndex < tabs.Length && tabs[defaultTabIndex].tabButton != null)
                {
                    tabs[defaultTabIndex].tabButton.Select();
                }

                // 其他标签淡入
                for (int i = 0; i < tabs.Length; i++)
                {
                    if (i != defaultTabIndex && buttonImages[i] != null)
                    {
                        Color transparent = tabs[i].inactiveColor;
                        transparent.a = 0;
                        buttonImages[i].color = transparent;
                        FadeButtonColor(i, tabs[i].inactiveColor);
                    }
                }

                // 等待淡入完成
                yield return new WaitForSeconds(useGlobalFade ? globalFadeDuration : GetMaxFadeDuration());
            }
            else
            {
                // 所有标签都淡入
                FadeInAllTabs(() => {
                    // 然后切换到默认标签
                    SwitchTabWithFade(defaultTabIndex);
                });
            }

            // 切换到默认标签
            if (skipInitialFadeForDefault)
            {
                // 如果跳过了淡入，直接切换
                SwitchToDefaultImmediate();
            }
            else
            {
                // 正常淡入切换
                SwitchTabWithFade(defaultTabIndex);
            }
        }
        else
        {
            // 不默认高亮，只淡入所有标签
            FadeInAllTabs();
        }
    }

    private void SwitchToDefaultImmediate()
    {
        // 直接切换到默认标签，无淡入效果
        if (defaultTabIndex >= 0 && defaultTabIndex < tabs.Length)
        {
            currentTabIndex = defaultTabIndex;

            // 显示默认标签内容
            if (tabs[defaultTabIndex].contentPanel != null)
            {
                tabs[defaultTabIndex].contentPanel.SetActive(true);
                CanvasGroup cg = tabs[defaultTabIndex].contentPanel.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
            }

            // 触发事件
            OnTabChanged?.Invoke(currentTabIndex);
            OnTabNameChanged?.Invoke(tabs[defaultTabIndex].tabName);

            Debug.Log($"直接切换到默认标签: {tabs[defaultTabIndex].tabName}");
        }
    }

    private void CacheButtonImages()
    {
        // 缓存所有按钮的Image组件
        buttonImages = new Image[tabs.Length];
        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i].tabButton != null)
            {
                buttonImages[i] = tabs[i].tabButton.GetComponent<Image>();
                // 初始设置为透明
                if (buttonImages[i] != null)
                {
                    // 如果是默认标签且设置了跳过淡入，初始就不透明
                    if (skipInitialFadeForDefault && i == defaultTabIndex)
                    {
                        buttonImages[i].color = tabs[i].activeColor;
                    }
                    else
                    {
                        Color transparent = buttonImages[i].color;
                        transparent.a = 0;
                        buttonImages[i].color = transparent;
                    }
                }
            }
        }
    }

    private void InitializeFadeSystem()
    {
        fadeCoroutines = new Coroutine[tabs.Length];

        // 初始化内容面板为透明
        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i].contentPanel != null)
            {
                CanvasGroup cg = tabs[i].contentPanel.GetComponent<CanvasGroup>();
                if (cg == null)
                {
                    cg = tabs[i].contentPanel.AddComponent<CanvasGroup>();
                }

                // 如果是默认标签且设置了跳过淡入，初始就不透明
                if (skipInitialFadeForDefault && i == defaultTabIndex)
                {
                    cg.alpha = 1;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                    tabs[i].contentPanel.SetActive(true);
                }
                else
                {
                    cg.alpha = 0;
                    cg.interactable = false;
                    cg.blocksRaycasts = false;
                    tabs[i].contentPanel.SetActive(false);
                }
            }
        }
    }

    private void InitializeTabs()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            int tabIndex = i;
            tabs[i].tabButton.onClick.RemoveAllListeners();
            tabs[i].tabButton.onClick.AddListener(() => OnTabClicked(tabIndex));

            // 初始化按钮为未选中状态
            if (buttonImages[i] != null)
            {
                // 如果是默认标签且设置了跳过淡入，初始就是选中颜色
                if (skipInitialFadeForDefault && i == defaultTabIndex)
                {
                    buttonImages[i].color = tabs[i].activeColor;
                }
                else
                {
                    Color color = tabs[i].inactiveColor;
                    color.a = 0; // 初始透明
                    buttonImages[i].color = color;
                }
            }
        }

        // 初始化选中指示器
        if (selectionIndicators != null)
        {
            for (int i = 0; i < selectionIndicators.Length; i++)
            {
                if (selectionIndicators[i] != null)
                {
                    // 如果是默认标签，初始显示选中指示器
                    if (skipInitialFadeForDefault && i == defaultTabIndex)
                    {
                        selectionIndicators[i].color = new Color(
                            selectionIndicators[i].color.r,
                            selectionIndicators[i].color.g,
                            selectionIndicators[i].color.b,
                            1
                        );
                        selectionIndicators[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        selectionIndicators[i].color = new Color(
                            selectionIndicators[i].color.r,
                            selectionIndicators[i].color.g,
                            selectionIndicators[i].color.b,
                            0
                        );
                        selectionIndicators[i].gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    private void OnTabClicked(int tabIndex)
    {
        Debug.Log($"点击标签: {tabs[tabIndex].tabName}");
        SwitchTabWithFade(tabIndex);
    }

    #region 标签切换（带淡入淡出）

    public void SwitchTabWithFade(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= tabs.Length || tabIndex == currentTabIndex)
            return;

        StartCoroutine(SwitchTabWithFadeCoroutine(tabIndex));
    }

    private IEnumerator SwitchTabWithFadeCoroutine(int tabIndex)
    {
        int previousTabIndex = currentTabIndex;

        // 淡出当前标签内容
        if (previousTabIndex >= 0)
        {
            yield return StartCoroutine(FadeOutTab(previousTabIndex));
        }

        // 切换颜色（当前标签变白，新标签变青）
        if (previousTabIndex >= 0)
        {
            FadeButtonColor(previousTabIndex, tabs[previousTabIndex].inactiveColor);
        }

        // 更新当前索引
        currentTabIndex = tabIndex;

        // 淡入新标签
        FadeButtonColor(currentTabIndex, tabs[currentTabIndex].activeColor);
        yield return StartCoroutine(FadeInTab(currentTabIndex));

        // 触发事件
        OnTabChanged?.Invoke(currentTabIndex);
        OnTabNameChanged?.Invoke(tabs[tabIndex].tabName);

        Debug.Log($"切换到标签: {tabs[tabIndex].tabName}");
    }

    #endregion

    #region 淡入淡出动画

    // 淡入单个标签内容
    private IEnumerator FadeInTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= tabs.Length)
            yield break;

        // 显示内容面板
        if (tabs[tabIndex].contentPanel != null)
        {
            // 检查是否已经是显示状态（默认标签跳过淡入的情况）
            CanvasGroup cg = tabs[tabIndex].contentPanel.GetComponent<CanvasGroup>();
            bool alreadyActive = cg != null && cg.alpha > 0.9f;

            if (!alreadyActive)
            {
                tabs[tabIndex].contentPanel.SetActive(true);

                if (cg != null)
                {
                    float duration = useGlobalFade ? globalFadeDuration : tabs[tabIndex].fadeDuration;
                    AnimationCurve curve = useGlobalFade ? globalFadeCurve : tabs[tabIndex].fadeCurve;

                    yield return StartCoroutine(FadeCanvasGroup(cg, 0, 1, duration, curve));

                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
            }
        }

        // 淡入选中指示器
        if (selectionIndicators != null && tabIndex < selectionIndicators.Length)
        {
            Image indicator = selectionIndicators[tabIndex];
            if (indicator != null && !indicator.gameObject.activeSelf)
            {
                indicator.gameObject.SetActive(true);
                yield return StartCoroutine(FadeImage(indicator, 0, 1, indicatorFadeDuration));
            }
        }

        OnTabFadeComplete?.Invoke(tabIndex, true);
    }

    // 淡出单个标签内容
    private IEnumerator FadeOutTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= tabs.Length)
            yield break;

        // 如果是默认标签且设置了跳过淡入，检查是否需要淡出
        bool isDefaultSkipped = skipInitialFadeForDefault && tabIndex == defaultTabIndex;

        if (isDefaultSkipped)
        {
            // 对于跳过了淡入的默认标签，只需要淡出指示器
            if (selectionIndicators != null && tabIndex < selectionIndicators.Length)
            {
                Image indicator = selectionIndicators[tabIndex];
                if (indicator != null && indicator.gameObject.activeSelf)
                {
                    yield return StartCoroutine(FadeImage(indicator, 1, 0, indicatorFadeDuration));
                    indicator.gameObject.SetActive(false);
                }
            }

            // 直接隐藏内容面板而不淡出
            if (tabs[tabIndex].contentPanel != null)
            {
                CanvasGroup cg = tabs[tabIndex].contentPanel.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.interactable = false;
                    cg.blocksRaycasts = false;
                }
                tabs[tabIndex].contentPanel.SetActive(false);
            }
        }
        else
        {
            // 正常淡出流程
            // 淡出选中指示器
            if (selectionIndicators != null && tabIndex < selectionIndicators.Length)
            {
                Image indicator = selectionIndicators[tabIndex];
                if (indicator != null && indicator.gameObject.activeSelf)
                {
                    yield return StartCoroutine(FadeImage(indicator, 1, 0, indicatorFadeDuration));
                    indicator.gameObject.SetActive(false);
                }
            }

            // 淡出内容面板
            if (tabs[tabIndex].contentPanel != null)
            {
                CanvasGroup cg = tabs[tabIndex].contentPanel.GetComponent<CanvasGroup>();

                if (cg != null)
                {
                    cg.interactable = false;
                    cg.blocksRaycasts = false;

                    float duration = useGlobalFade ? globalFadeDuration : tabs[tabIndex].fadeDuration;
                    AnimationCurve curve = useGlobalFade ? globalFadeCurve : tabs[tabIndex].fadeCurve;

                    yield return StartCoroutine(FadeCanvasGroup(cg, 1, 0, duration, curve));

                    tabs[tabIndex].contentPanel.SetActive(false);
                }
            }
        }

        OnTabFadeComplete?.Invoke(tabIndex, false);
    }

    // 按钮颜色渐变
    private void FadeButtonColor(int tabIndex, Color targetColor)
    {
        if (tabIndex < 0 || tabIndex >= tabs.Length || buttonImages[tabIndex] == null)
            return;

        // 如果是默认标签且设置了跳过淡入，检查是否已经是目标颜色
        bool isDefaultSkipped = skipInitialFadeForDefault && tabIndex == defaultTabIndex;
        if (isDefaultSkipped && buttonImages[tabIndex].color == targetColor)
            return;

        // 停止之前的淡入淡出协程
        if (fadeCoroutines[tabIndex] != null)
        {
            StopCoroutine(fadeCoroutines[tabIndex]);
        }

        // 开始新的颜色渐变
        fadeCoroutines[tabIndex] = StartCoroutine(FadeButtonColorCoroutine(
            buttonImages[tabIndex],
            targetColor,
            useGlobalFade ? globalFadeDuration : tabs[tabIndex].fadeDuration
        ));
    }

    private IEnumerator FadeButtonColorCoroutine(Image image, Color targetColor, float duration)
    {
        if (image == null) yield break;

        Color startColor = image.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            AnimationCurve curve = useGlobalFade ? globalFadeCurve : tabs[currentTabIndex].fadeCurve;
            float curvedT = curve.Evaluate(t);

            image.color = Color.Lerp(startColor, targetColor, curvedT);
            yield return null;
        }

        image.color = targetColor;
        fadeCoroutines[Array.IndexOf(buttonImages, image)] = null;
    }

    #endregion

    #region 工具方法

    // CanvasGroup淡入淡出
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float fromAlpha, float toAlpha, float duration, AnimationCurve curve)
    {
        if (cg == null) yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = curve.Evaluate(t);

            cg.alpha = Mathf.Lerp(fromAlpha, toAlpha, curvedT);
            yield return null;
        }

        cg.alpha = toAlpha;
    }

    // Image淡入淡出
    private IEnumerator FadeImage(Image image, float fromAlpha, float toAlpha, float duration)
    {
        if (image == null) yield break;

        Color startColor = image.color;
        Color endColor = image.color;
        endColor.a = toAlpha;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            image.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        image.color = endColor;
    }

    #endregion

    #region 公共接口

    // 淡入所有标签（初始化用）
    public void FadeInAllTabs(Action onComplete = null)
    {
        StartCoroutine(FadeInAllTabsCoroutine(onComplete));
    }

    private IEnumerator FadeInAllTabsCoroutine(Action onComplete)
    {
        // 淡入所有按钮
        for (int i = 0; i < tabs.Length; i++)
        {
            if (buttonImages[i] != null)
            {
                // 如果是默认标签且设置了跳过淡入，已经是亮起状态
                if (skipInitialFadeForDefault && i == defaultTabIndex)
                {
                    // 已经是亮起状态，不需要淡入
                    continue;
                }

                Color targetColor = (i == defaultTabIndex) ? tabs[i].activeColor : tabs[i].inactiveColor;
                Color startColor = buttonImages[i].color;
                startColor.a = 0;
                buttonImages[i].color = startColor;
                FadeButtonColor(i, targetColor);
            }
        }

        // 等待淡入完成
        yield return new WaitForSeconds(useGlobalFade ? globalFadeDuration : GetMaxFadeDuration());

        onComplete?.Invoke();
    }

    private float GetMaxFadeDuration()
    {
        float max = 0;
        foreach (var tab in tabs)
        {
            if (tab.fadeDuration > max) max = tab.fadeDuration;
        }
        return max;
    }

    // 强制高亮默认标签（外部调用）
    public void HighlightDefaultTab(bool withFade = false)
    {
        if (defaultTabIndex >= 0 && defaultTabIndex < tabs.Length)
        {
            if (withFade)
            {
                SwitchTabWithFade(defaultTabIndex);
            }
            else
            {
                SwitchTabImmediate(defaultTabIndex);
            }
        }
    }

    // 重置到默认标签
    public void ResetToDefaultTab()
    {
        if (skipInitialFadeForDefault)
        {
            // 直接切换到默认标签
            SwitchToDefaultImmediate();
        }
        else
        {
            SwitchTabWithFade(defaultTabIndex);
        }
    }

    // 淡出所有标签（登出时用）
    public void FadeOutAllTabs(Action onComplete = null)
    {
        StartCoroutine(FadeOutAllTabsCoroutine(onComplete));
    }

    private IEnumerator FadeOutAllTabsCoroutine(Action onComplete)
    {
        // 淡出当前标签内容
        if (currentTabIndex >= 0)
        {
            yield return StartCoroutine(FadeOutTab(currentTabIndex));
        }

        // 淡出所有按钮
        for (int i = 0; i < tabs.Length; i++)
        {
            if (buttonImages[i] != null)
            {
                // 跳过已经淡出的默认标签
                if (skipInitialFadeForDefault && i == defaultTabIndex && buttonImages[i].color.a == 0)
                    continue;

                Color transparent = buttonImages[i].color;
                transparent.a = 0;
                FadeButtonColor(i, transparent);
            }
        }

        // 等待淡出完成
        yield return new WaitForSeconds(useGlobalFade ? globalFadeDuration : GetMaxFadeDuration());

        currentTabIndex = -1;
        onComplete?.Invoke();
    }

    // 快速切换（无淡入淡出）
    public void SwitchTabImmediate(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= tabs.Length || tabIndex == currentTabIndex)
            return;

        // 停止所有淡入淡出协程
        StopAllCoroutines();
        foreach (var coroutine in fadeCoroutines)
        {
            if (coroutine != null) StopCoroutine(coroutine);
        }

        // 隐藏之前的内容
        if (currentTabIndex >= 0)
        {
            if (tabs[currentTabIndex].contentPanel != null)
            {
                CanvasGroup cg = tabs[currentTabIndex].contentPanel.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 0;
                    cg.interactable = false;
                    cg.blocksRaycasts = false;
                }
                tabs[currentTabIndex].contentPanel.SetActive(false);
            }

            // 恢复按钮颜色
            if (buttonImages[currentTabIndex] != null)
            {
                buttonImages[currentTabIndex].color = tabs[currentTabIndex].inactiveColor;
            }
        }

        // 显示新的内容
        currentTabIndex = tabIndex;

        if (tabs[currentTabIndex].contentPanel != null)
        {
            tabs[currentTabIndex].contentPanel.SetActive(true);
            CanvasGroup cg = tabs[currentTabIndex].contentPanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
        }

        // 设置按钮颜色
        if (buttonImages[currentTabIndex] != null)
        {
            buttonImages[currentTabIndex].color = tabs[currentTabIndex].activeColor;
        }

        OnTabChanged?.Invoke(currentTabIndex);
    }

    // 获取当前选中的标签索引
    public int GetCurrentTabIndex()
    {
        return currentTabIndex;
    }

    // 获取当前选中的标签名称
    public string GetCurrentTabName()
    {
        if (currentTabIndex >= 0 && currentTabIndex < tabs.Length)
        {
            return tabs[currentTabIndex].tabName;
        }
        return "";
    }

    // 获取默认标签索引
    public int GetDefaultTabIndex()
    {
        return defaultTabIndex;
    }

    // 是否默认标签已亮起
    public bool IsDefaultTabActive()
    {
        return currentTabIndex == defaultTabIndex;
    }

    #endregion
}