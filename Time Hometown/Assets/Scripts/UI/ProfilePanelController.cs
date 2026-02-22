using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProfilePanelController : MonoBehaviour
{
    [Header("用户信息显示")]
    [SerializeField] private Text userNameText;
    [SerializeField] private Button editUserNameButton;
    [SerializeField] private Text signatureText;
    [SerializeField] private Button editSignatureButton;

    [Header("音量控制")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Text bgmValueText;
    [SerializeField] private Text sfxValueText;

    [Header("退出登录")]
    [SerializeField] private Button logoutButton;
    // 默认用户数据
    private string currentUserName = "时光旅人";
    private string currentSignature = "自律让我更自由！";

    private bool isInitialized = false;

    // 用户名变更事件
    public event System.Action<string> OnUserNameChanged;

    void Start()
    {
        InitializeProfileInfo();
        InitializeVolumeControls();
        InitializeLogoutButton();
        LoadUserData();
    }

    void Update()
    {

    }

    /// <summary>
    /// 初始化用户信息显示
    /// </summary>
    private void InitializeProfileInfo()
    {
        // 用户名编辑按钮
        if (editUserNameButton != null)
        {
            editUserNameButton.onClick.RemoveAllListeners();
            editUserNameButton.onClick.AddListener(OnEditUserNameClicked);

            // 添加悬停效果
            SetupButtonHoverEffect(editUserNameButton, Color.white, new Color(0.9f, 0.9f, 0.9f, 1f));
        }

        // 个性签名编辑按钮
        if (editSignatureButton != null)
        {
            editSignatureButton.onClick.RemoveAllListeners();
            editSignatureButton.onClick.AddListener(OnEditSignatureClicked);

            // 添加悬停效果
            SetupButtonHoverEffect(editSignatureButton, Color.white, new Color(0.9f, 0.9f, 0.9f, 1f));
        }

        Debug.Log("用户信息显示初始化完成");
    }

    /// <summary>
    /// 加载用户数据
    /// </summary>
    private void LoadUserData()
    {
        // 从PlayerPrefs加载保存的用户数据
        currentUserName = PlayerPrefs.GetString("User_Name", "时光旅人");
        currentSignature = PlayerPrefs.GetString("User_Signature", "自律让我更自由！");

        // 更新UI显示
        UpdateProfileDisplay();

        Debug.Log($"加载用户数据: 用户名={currentUserName}, 签名={currentSignature}");
    }

    /// <summary>
    /// 保存用户数据
    /// </summary>
    private void SaveUserData()
    {
        PlayerPrefs.SetString("User_Name", currentUserName);
        PlayerPrefs.SetString("User_Signature", currentSignature);
        PlayerPrefs.Save();

        Debug.Log($"保存用户数据: 用户名={currentUserName}, 签名={currentSignature}");

        // 触发用户名变更事件，通知UIManager更新顶部栏
        OnUserNameChanged?.Invoke(currentUserName);
    }

    /// <summary>
    /// 更新用户信息显示
    /// </summary>
    private void UpdateProfileDisplay()
    {
        // 更新用户名显示
        if (userNameText != null)
        {
            userNameText.text = currentUserName;
        }

        // 更新个性签名显示
        if (signatureText != null)
        {
            // 如果签名为空，显示默认提示
            if (string.IsNullOrWhiteSpace(currentSignature))
            {
                signatureText.text = "点击编辑个性签名";
                signatureText.color = new Color(0.6f, 0.6f, 0.6f, 1f); // 灰色
            }
            else
            {
                signatureText.text = currentSignature;
                signatureText.color = Color.black; // 黑色
            }
        }
    }

    /// <summary>
    /// 编辑用户名按钮点击事件
    /// </summary>
    private void OnEditUserNameClicked()
    {
        Debug.Log("点击编辑用户名按钮");

        // 播放音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        // 显示修改用户名对话框
        if (EditProfileDialog.Instance != null)
        {
            EditProfileDialog.Instance.ShowUserNameDialog(
                currentUserName,
                OnUserNameConfirmed,
                OnEditCancelled
            );
        }
        else
        {
            Debug.LogError("EditProfileDialog实例未找到！");
        }
    }

    /// <summary>
    /// 编辑个性签名按钮点击事件
    /// </summary>
    private void OnEditSignatureClicked()
    {
        Debug.Log("点击编辑个性签名按钮");

        // 播放音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        // 显示修改个性签名对话框
        if (EditProfileDialog.Instance != null)
        {
            EditProfileDialog.Instance.ShowSignatureDialog(
                currentSignature,
                OnSignatureConfirmed,
                OnEditCancelled
            );
        }
        else
        {
            Debug.LogError("EditProfileDialog实例未找到！");
        }
    }

    /// <summary>
    /// 用户名修改确认回调
    /// </summary>
    private void OnUserNameConfirmed(string newUserName)
    {
        Debug.Log($"用户名修改确认: {newUserName}");

        // 更新用户名
        currentUserName = newUserName.Trim();

        // 保存到PlayerPrefs
        SaveUserData();

        // 更新UI显示
        UpdateProfileDisplay();

        // 播放成功音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySuccessSound();
        }
    }

    /// <summary>
    /// 个性签名修改确认回调
    /// </summary>
    private void OnSignatureConfirmed(string newSignature)
    {
        Debug.Log($"个性签名修改确认: {newSignature}");

        // 更新个性签名
        currentSignature = newSignature.Trim();

        // 保存到PlayerPrefs
        SaveUserData();

        // 更新UI显示
        UpdateProfileDisplay();

        // 播放成功音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySuccessSound();
        }
    }

    /// <summary>
    /// 编辑取消回调
    /// </summary>
    private void OnEditCancelled()
    {
        Debug.Log("编辑已取消");

        // 播放音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }
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
        }
        else
        {
            Debug.LogWarning("未找到退出登录按钮，请检查UI配置");
        }
    }

    /// <summary>
    /// 设置按钮悬停效果
    /// </summary>
    private void SetupButtonHoverEffect(Button button, Color normalColor, Color hoverColor)
    {
        if (button == null) return;

        // 添加悬停效果组件
        ButtonHoverEffect hoverEffect = button.gameObject.GetComponent<ButtonHoverEffect>();
        if (hoverEffect == null)
        {
            hoverEffect = button.gameObject.AddComponent<ButtonHoverEffect>();
        }

        hoverEffect.normalColor = normalColor;
        hoverEffect.hoverColor = hoverColor;
        hoverEffect.transitionDuration = 0.2f;
    }

    /// <summary>
    /// 退出登录按钮点击事件
    /// </summary>
    private void OnLogoutButtonClicked()
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

        // 调用UIManager的登出功能
        if (UIManager.Instance != null)
        {
            // 调用快速版本
            UIManager.Instance.LogoutImmediate();
        }
        else
        {
            Debug.LogError("UIManager.Instance为null，无法执行登出");
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
        RefreshProfileInfo();
    }

    /// <summary>
    /// 面板关闭时调用
    /// </summary>
    public void OnPanelClosed()
    {
        // 如果编辑对话框还在显示，强制关闭
        if (EditProfileDialog.Instance != null && EditProfileDialog.Instance.IsShowing())
        {
            EditProfileDialog.Instance.ForceClose();
        }
    }

    /// <summary>
    /// 刷新用户信息
    /// </summary>
    private void RefreshProfileInfo()
    {
        // 重新加载数据并更新显示
        LoadUserData();
    }

    /// <summary>
    /// 刷新音量设置
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

    /// <summary>
    /// 获取当前用户名
    /// </summary>
    public string GetUserName()
    {
        return currentUserName;
    }

    /// <summary>
    /// 获取当前个性签名
    /// </summary>
    public string GetSignature()
    {
        return currentSignature;
    }

    /// <summary>
    /// 设置用户名（外部调用）
    /// </summary>
    public void SetUserName(string userName)
    {
        if (!string.IsNullOrWhiteSpace(userName))
        {
            currentUserName = userName.Trim();
            SaveUserData();
            UpdateProfileDisplay();
        }
    }

    /// <summary>
    /// 设置个性签名（外部调用）
    /// </summary>
    public void SetSignature(string signature)
    {
        currentSignature = signature?.Trim() ?? "";
        SaveUserData();
        UpdateProfileDisplay();
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