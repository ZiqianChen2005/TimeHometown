using UnityEngine;
using UnityEngine.UI;
using System;

public class EditProfileDialog : MonoBehaviour
{
    public static EditProfileDialog Instance { get; private set; }

    [Header("UI引用")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private CanvasGroup dialogCanvasGroup;
    [SerializeField] private Text titleText;
    [SerializeField] private InputField contentInputField;
    [SerializeField] private Text placeholderText;
    [SerializeField] private Text characterCountText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Text confirmButtonText;
    [SerializeField] private Text cancelButtonText;

    [Header("设置")]
    [SerializeField] private string userNameTitle = "修改用户名";
    [SerializeField] private string signatureTitle = "修改个性签名";
    [SerializeField] private string userNamePlaceholder = "请输入用户名 (2-20个字符)";
    [SerializeField] private string signaturePlaceholder = "请输入个性签名 (最多50个字符)";
    [SerializeField] private int userNameMaxLength = 20;
    [SerializeField] private int signatureMaxLength = 50;
    [SerializeField] private float dialogFadeDuration = 0.3f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("按钮颜色")]
    [SerializeField] private Color confirmButtonNormalColor = new Color(0f, 0.6f, 1f, 1f); // 蓝色
    [SerializeField] private Color confirmButtonDisabledColor = new Color(0.5f, 0.5f, 0.5f, 1f); // 灰色
    [SerializeField] private Color cancelButtonNormalColor = new Color(0.8f, 0.8f, 0.8f, 1f); // 浅灰色

    private EditMode currentMode = EditMode.None;
    private Action<string> onConfirmCallback;
    private Action onCancelCallback;
    private bool isShowing = false;

    public enum EditMode
    {
        None,
        UserName,
        Signature
    }

    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeDialog();
    }

    /// <summary>
    /// 初始化对话框
    /// </summary>
    private void InitializeDialog()
    {
        // 确保初始状态为隐藏
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
        }

        if (dialogCanvasGroup != null)
        {
            dialogCanvasGroup.alpha = 0;
            dialogCanvasGroup.interactable = false;
            dialogCanvasGroup.blocksRaycasts = false;
        }

        // 设置按钮事件
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);

            // 设置按钮颜色
            Image confirmImage = confirmButton.GetComponent<Image>();
            if (confirmImage != null)
            {
                confirmImage.color = confirmButtonNormalColor;
            }
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelButtonClicked);

            // 设置按钮颜色
            Image cancelImage = cancelButton.GetComponent<Image>();
            if (cancelImage != null)
            {
                cancelImage.color = cancelButtonNormalColor;
            }
        }

        // 设置输入框事件
        if (contentInputField != null)
        {
            contentInputField.onValueChanged.RemoveAllListeners();
            contentInputField.onValueChanged.AddListener(OnInputFieldValueChanged);
        }

        Debug.Log("修改资料对话框初始化完成");
    }

    /// <summary>
    /// 显示修改用户名对话框
    /// </summary>
    public void ShowUserNameDialog(string currentName, Action<string> onConfirm, Action onCancel = null)
    {
        ShowDialog(EditMode.UserName, currentName, onConfirm, onCancel);
    }

    /// <summary>
    /// 显示修改个性签名对话框
    /// </summary>
    public void ShowSignatureDialog(string currentSignature, Action<string> onConfirm, Action onCancel = null)
    {
        ShowDialog(EditMode.Signature, currentSignature, onConfirm, onCancel);
    }

    /// <summary>
    /// 显示对话框
    /// </summary>
    private void ShowDialog(EditMode mode, string currentContent, Action<string> onConfirm, Action onCancel)
    {
        if (isShowing) return;
        isShowing = true;

        currentMode = mode;
        onConfirmCallback = onConfirm;
        onCancelCallback = onCancel;

        // 根据模式设置UI
        switch (mode)
        {
            case EditMode.UserName:
                if (titleText != null) titleText.text = userNameTitle;
                if (placeholderText != null) placeholderText.text = userNamePlaceholder;
                if (contentInputField != null)
                {
                    contentInputField.characterLimit = userNameMaxLength;
                    contentInputField.text = currentContent;
                    contentInputField.contentType = InputField.ContentType.Standard;
                }
                break;

            case EditMode.Signature:
                if (titleText != null) titleText.text = signatureTitle;
                if (placeholderText != null) placeholderText.text = signaturePlaceholder;
                if (contentInputField != null)
                {
                    contentInputField.characterLimit = signatureMaxLength;
                    contentInputField.text = currentContent;
                    contentInputField.contentType = InputField.ContentType.Standard;
                    contentInputField.lineType = InputField.LineType.MultiLineNewline; // 多行支持
                }
                break;
        }

        // 更新字符计数
        UpdateCharacterCount(currentContent?.Length ?? 0);

        // 激活面板
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(true);
        }

        // 淡入动画
        StartCoroutine(FadeInDialog());

        // 选中输入框
        if (contentInputField != null)
        {
            contentInputField.Select();
            contentInputField.ActivateInputField();
        }

        // 播放音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }
    }

    /// <summary>
    /// 淡入对话框
    /// </summary>
    private System.Collections.IEnumerator FadeInDialog()
    {
        if (dialogCanvasGroup == null) yield break;

        float elapsed = 0f;
        dialogCanvasGroup.alpha = 0;
        dialogCanvasGroup.interactable = false;
        dialogCanvasGroup.blocksRaycasts = false;

        while (elapsed < dialogFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dialogFadeDuration;
            float curvedT = fadeCurve.Evaluate(t);
            dialogCanvasGroup.alpha = curvedT;
            yield return null;
        }

        dialogCanvasGroup.alpha = 1;
        dialogCanvasGroup.interactable = true;
        dialogCanvasGroup.blocksRaycasts = true;
    }

    /// <summary>
    /// 隐藏对话框
    /// </summary>
    public void HideDialog()
    {
        if (!isShowing) return;

        StartCoroutine(FadeOutDialog());
    }

    /// <summary>
    /// 淡出对话框
    /// </summary>
    private System.Collections.IEnumerator FadeOutDialog()
    {
        if (dialogCanvasGroup == null) yield break;

        float elapsed = 0f;
        float startAlpha = dialogCanvasGroup.alpha;

        while (elapsed < dialogFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dialogFadeDuration;
            float curvedT = fadeCurve.Evaluate(t);
            dialogCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0, curvedT);
            yield return null;
        }

        dialogCanvasGroup.alpha = 0;
        dialogCanvasGroup.interactable = false;
        dialogCanvasGroup.blocksRaycasts = false;

        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
        }

        isShowing = false;
        currentMode = EditMode.None;
        onConfirmCallback = null;
        onCancelCallback = null;
    }

    /// <summary>
    /// 确认按钮点击事件
    /// </summary>
    private void OnConfirmButtonClicked()
    {
        if (!isShowing) return;

        string inputText = contentInputField != null ? contentInputField.text.Trim() : "";

        // 验证输入
        if (!ValidateInput(inputText))
        {
            // 播放错误音效
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayErrorSound();
            }
            return;
        }

        // 播放成功音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySuccessSound();
        }

        // 调用确认回调
        onConfirmCallback?.Invoke(inputText);

        // 隐藏对话框
        HideDialog();
    }

    /// <summary>
    /// 取消按钮点击事件
    /// </summary>
    private void OnCancelButtonClicked()
    {
        if (!isShowing) return;

        // 播放音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        // 调用取消回调
        onCancelCallback?.Invoke();

        // 隐藏对话框
        HideDialog();
    }

    /// <summary>
    /// 输入框内容改变事件
    /// </summary>
    private void OnInputFieldValueChanged(string text)
    {
        // 更新字符计数
        UpdateCharacterCount(text.Length);

        // 验证输入并更新确认按钮状态
        bool isValid = ValidateInput(text);
        UpdateConfirmButtonState(isValid);
    }

    /// <summary>
    /// 更新字符计数显示
    /// </summary>
    private void UpdateCharacterCount(int count)
    {
        if (characterCountText == null) return;

        int maxLength = currentMode == EditMode.UserName ? userNameMaxLength : signatureMaxLength;
        characterCountText.text = $"{count}/{maxLength}";

        // 颜色提示（接近上限时变红）
        if (count > maxLength * 0.8f)
        {
            characterCountText.color = Color.red;
        }
        else
        {
            characterCountText.color = Color.gray;
        }
    }

    /// <summary>
    /// 更新确认按钮状态
    /// </summary>
    private void UpdateConfirmButtonState(bool enabled)
    {
        if (confirmButton == null) return;

        confirmButton.interactable = enabled;

        // 更新按钮颜色
        Image confirmImage = confirmButton.GetComponent<Image>();
        if (confirmImage != null)
        {
            confirmImage.color = enabled ? confirmButtonNormalColor : confirmButtonDisabledColor;
        }
    }

    /// <summary>
    /// 验证输入
    /// </summary>
    private bool ValidateInput(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        switch (currentMode)
        {
            case EditMode.UserName:
                // 用户名：2-20个字符，不能全为空格
                text = text.Trim();
                if (text.Length < 2 || text.Length > userNameMaxLength)
                {
                    return false;
                }

                // 检查是否包含非法字符（可选）
                // 这里可以添加更多验证逻辑
                break;

            case EditMode.Signature:
                // 个性签名：1-50个字符，可以为空（如果为空则显示默认提示）
                if (text.Length > signatureMaxLength)
                {
                    return false;
                }
                break;

            default:
                return false;
        }

        return true;
    }

    /// <summary>
    /// 获取当前输入内容
    /// </summary>
    public string GetCurrentInput()
    {
        return contentInputField != null ? contentInputField.text : "";
    }

    /// <summary>
    /// 获取当前模式
    /// </summary>
    public EditMode GetCurrentMode()
    {
        return currentMode;
    }

    /// <summary>
    /// 是否正在显示
    /// </summary>
    public bool IsShowing()
    {
        return isShowing;
    }

    /// <summary>
    /// 外部调用：强制关闭
    /// </summary>
    public void ForceClose()
    {
        HideDialog();
    }
}