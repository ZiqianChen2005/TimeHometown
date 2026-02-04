using UnityEngine;
using UnityEngine.UI;

public class SignatureEditDialog : MonoBehaviour
{
    [Header("对话框UI")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private CanvasGroup dialogCanvasGroup;
    [SerializeField] private InputField signatureInputField;   // 输入框
    [SerializeField] private Text charCountText;               // 字符计数
    [SerializeField] private Button confirmButton;             // 确认按钮
    [SerializeField] private Button cancelButton;              // 取消按钮
    [SerializeField] private Text titleText;                   // 标题文本

    [Header("设置")]
    [SerializeField] private int maxSignatureLength = 50;      // 最大字符数
    [SerializeField] private float fadeDuration = 0.2f;

    // 回调函数
    private System.Action<string> onSignatureUpdated;
    private string originalSignature;

    /// <summary>
    /// 初始化对话框
    /// </summary>
    public void Initialize(string currentSignature, System.Action<string> callback)
    {
        originalSignature = currentSignature;
        onSignatureUpdated = callback;

        InitializeUI();
        ShowDialog();
    }

    private void InitializeUI()
    {
        if (dialogPanel != null && !dialogPanel.activeSelf)
        {
            dialogPanel.SetActive(true);
        }

        if (dialogCanvasGroup != null)
        {
            dialogCanvasGroup.alpha = 0;
            dialogCanvasGroup.interactable = false;
        }

        // 设置输入框
        if (signatureInputField != null)
        {
            signatureInputField.text = originalSignature;
            signatureInputField.characterLimit = maxSignatureLength;
            signatureInputField.onValueChanged.AddListener(OnInputValueChanged);

            // 设置占位符
            Text placeholder = signatureInputField.placeholder as Text;
            if (placeholder != null)
            {
                placeholder.text = "请输入个性签名...";
            }
        }

        // 绑定按钮事件
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelClicked);
        }

        // 设置标题
        if (titleText != null)
        {
            titleText.text = "编辑个性签名";
        }

        // 更新字符计数
        UpdateCharacterCount();
    }

    /// <summary>
    /// 显示对话框
    /// </summary>
    private void ShowDialog()
    {
        StartCoroutine(FadeInDialog());
    }

    private System.Collections.IEnumerator FadeInDialog()
    {
        float elapsed = 0f;

        dialogCanvasGroup.alpha = 0;
        dialogCanvasGroup.interactable = false;
        dialogCanvasGroup.blocksRaycasts = false;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            dialogCanvasGroup.alpha = Mathf.Lerp(0, 1, t);
            yield return null;
        }

        dialogCanvasGroup.alpha = 1;
        dialogCanvasGroup.interactable = true;
        dialogCanvasGroup.blocksRaycasts = true;

        // 自动聚焦输入框
        if (signatureInputField != null)
        {
            signatureInputField.ActivateInputField();
            signatureInputField.Select();
        }
    }

    /// <summary>
    /// 隐藏对话框
    /// </summary>
    private void HideDialog()
    {
        StartCoroutine(FadeOutDialog());
    }

    private System.Collections.IEnumerator FadeOutDialog()
    {
        float elapsed = 0f;
        float startAlpha = dialogCanvasGroup.alpha;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            dialogCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0, t);
            yield return null;
        }

        dialogCanvasGroup.alpha = 0;
        dialogCanvasGroup.interactable = false;
        dialogCanvasGroup.blocksRaycasts = false;

        Destroy(gameObject);
    }

    /// <summary>
    /// 更新字符计数
    /// </summary>
    private void UpdateCharacterCount()
    {
        if (signatureInputField != null && charCountText != null)
        {
            int currentLength = signatureInputField.text.Length;
            charCountText.text = $"{currentLength}/{maxSignatureLength}";

            // 根据长度改变颜色
            if (currentLength >= maxSignatureLength)
            {
                charCountText.color = Color.red;
            }
            else if (currentLength > maxSignatureLength * 0.8f)
            {
                charCountText.color = Color.yellow;
            }
            else
            {
                charCountText.color = Color.gray;
            }
        }
    }

    #region 事件处理

    /// <summary>
    /// 输入框内容变化
    /// </summary>
    private void OnInputValueChanged(string text)
    {
        UpdateCharacterCount();
    }

    /// <summary>
    /// 确认按钮点击
    /// </summary>
    private void OnConfirmClicked()
    {
        AudioManager.Instance?.PlayButtonClick();

        string newSignature = signatureInputField.text.Trim();

        if (string.IsNullOrEmpty(newSignature))
        {
            // 空签名，使用默认值
            newSignature = "记录每一刻的成长";
        }

        // 检查长度
        if (newSignature.Length > maxSignatureLength)
        {
            newSignature = newSignature.Substring(0, maxSignatureLength);
            AudioManager.Instance?.PlayErrorSound();
        }
        else
        {
            AudioManager.Instance?.PlaySuccessSound();
        }

        onSignatureUpdated?.Invoke(newSignature);
        HideDialog();
    }

    /// <summary>
    /// 取消按钮点击
    /// </summary>
    private void OnCancelClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        HideDialog();
    }

    #endregion
}