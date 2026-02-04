using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProfilePanelController : MonoBehaviour
{
    [Header("音量控制")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Text bgmValueText;
    [SerializeField] private Text sfxValueText;

    [Header("退出登录")]
    [SerializeField] private Button logoutButton;

    private bool isInitialized = false;

    void Start()
    {
        InitializeVolumeControls();
        InitializeLogoutButton();
    }

    void Update()
    {

    }

    /// <summary>
    /// 初始化音量控制
    /// </summary>
    private void InitializeVolumeControls()
    {
        if (isInitialized) return;

        // 设置BGM滑块
        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0f;
            bgmSlider.maxValue = 1f;
            bgmSlider.value = AudioManager.Instance?.GetBGMVolume() ?? 0.5f;
            bgmSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);

            // 更新显示值
            UpdateBGMValueDisplay(bgmSlider.value);
        }

        // 设置SFX滑块
        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.value = AudioManager.Instance?.GetSFXVolume() ?? 0.7f;
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

            // 更新显示值
            UpdateSFXValueDisplay(sfxSlider.value);
        }

        isInitialized = true;
        Debug.Log("音量控制初始化完成");
    }

    /// <summary>
    /// 初始化退出登录按钮
    /// </summary>
    private void InitializeLogoutButton()
    {
        if (logoutButton != null)
        {
            // 添加事件监听
            logoutButton.onClick.RemoveAllListeners();
            logoutButton.onClick.AddListener(OnLogoutButtonClicked);

            Debug.Log("退出登录按钮初始化完成");
        }
        else
        {
            Debug.LogWarning("未找到退出登录按钮，请检查UI配置");
        }
    }

    /// <summary>
    /// 退出登录按钮点击事件
    /// </summary>
    public void OnLogoutButtonClicked()
    {
        Debug.Log("退出登录按钮被点击");

        // 播放音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        // 直接执行退出登录
        PerformLogout();
    }

    /// <summary>
    /// 执行退出登录
    /// </summary>
    private void PerformLogout()
    {
        Debug.Log("正在退出登录...");

        // 重置音量滑块（可选）
        //ResetVolumeSlidersToDefault();

        // 调用UIManager的登出功能
        if (UIManager.Instance != null)
        {
            UIManager.Instance.Logout();
        }
        else
        {
            Debug.LogError("UIManager.Instance为null，无法执行登出");
            // 备用方案：直接重新加载场景或返回到登录界面
            FallbackLogout();
        }
    }

    /// <summary>
    /// 备用登出方案
    /// </summary>
    private void FallbackLogout()
    {
        Debug.Log("使用备用登出方案");

        // 如果有相机控制器，直接移动相机
        if (CameraController.Instance != null)
        {
            CameraController.Instance.MoveToLoginView();
        }

        // 这里可以添加更多清理逻辑
        // 例如：重置游戏状态、清理数据等
    }

    /// <summary>
    /// 重置音量滑块为默认值
    /// </summary>
    private void ResetVolumeSlidersToDefault()
    {
        // 这里可以根据需要重置滑块
        // 默认不重置，保持用户设置
    }

    /// <summary>
    /// BGM音量改变事件
    /// </summary>
    private void OnBGMVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBGMVolume(value);
            UpdateBGMValueDisplay(value);

            // 播放预览音效（可选）
            AudioManager.Instance.PreviewBGMVolume(value);
        }
    }

    /// <summary>
    /// SFX音量改变事件
    /// </summary>
    private void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
            UpdateSFXValueDisplay(value);

            // 播放按钮点击音效作为预览
            if (sfxSlider != null && Mathf.Approximately(sfxSlider.value, value))
            {
                AudioManager.Instance.PlayButtonClick();
            }
        }
    }

    /// <summary>
    /// 更新BGM值显示
    /// </summary>
    private void UpdateBGMValueDisplay(float value)
    {
        if (bgmValueText != null)
        {
            bgmValueText.text = Mathf.RoundToInt(value * 100) + "%";
        }
    }

    /// <summary>
    /// 更新SFX值显示
    /// </summary>
    private void UpdateSFXValueDisplay(float value)
    {
        if (sfxValueText != null)
        {
            sfxValueText.text = Mathf.RoundToInt(value * 100) + "%";
        }
    }

    /// <summary>
    /// 面板打开时调用
    /// </summary>
    public void OnPanelOpened()
    {
        RefreshVolumeSettings();
    }

    /// <summary>
    /// 面板关闭时调用
    /// </summary>
    public void OnPanelClosed()
    {
        // 可以在这里保存设置或进行清理
    }

    /// <summary>
    /// 刷新音量设置（当从其他界面返回时使用）
    /// </summary>
    public void RefreshVolumeSettings()
    {
        if (AudioManager.Instance != null)
        {
            // 更新滑块值
            if (bgmSlider != null)
            {
                float bgmVolume = AudioManager.Instance.GetBGMVolume();
                bgmSlider.value = bgmVolume;
                UpdateBGMValueDisplay(bgmVolume);
            }

            if (sfxSlider != null)
            {
                float sfxVolume = AudioManager.Instance.GetSFXVolume();
                sfxSlider.value = sfxVolume;
                UpdateSFXValueDisplay(sfxVolume);
            }
        }
    }

    /// <summary>
    /// 重置为默认音量
    /// </summary>
    public void ResetToDefaultVolume()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ResetToDefaultVolume();
            RefreshVolumeSettings();
        }
    }
}

/// <summary>
/// 按钮悬停效果组件（用于改变按钮颜色）
/// </summary>
public class ButtonHoverEffect : MonoBehaviour
{
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    public float transitionDuration = 0.1f;

    private Image buttonImage;
    private Coroutine colorTransitionCoroutine;

    private void Awake()
    {
        buttonImage = GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = normalColor;
        }
    }

    private void OnEnable()
    {
        if (buttonImage != null)
        {
            buttonImage.color = normalColor;
        }
    }

    public void OnPointerEnter()
    {
        if (buttonImage != null)
        {
            StartColorTransition(hoverColor);
        }
    }

    public void OnPointerExit()
    {
        if (buttonImage != null)
        {
            StartColorTransition(normalColor);
        }
    }

    private void StartColorTransition(Color targetColor)
    {
        if (colorTransitionCoroutine != null)
        {
            StopCoroutine(colorTransitionCoroutine);
        }
        colorTransitionCoroutine = StartCoroutine(TransitionColor(targetColor));
    }

    private IEnumerator TransitionColor(Color targetColor)
    {
        Color startColor = buttonImage.color;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            buttonImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        buttonImage.color = targetColor;
        colorTransitionCoroutine = null;
    }
}