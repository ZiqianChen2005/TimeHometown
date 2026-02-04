using UnityEngine;
using UnityEngine.UI;
using System;

public class AvatarSelectionDialog : MonoBehaviour
{
    [Header("对话框UI")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private CanvasGroup dialogCanvasGroup;
    [SerializeField] private Transform avatarGrid;            // 头像网格容器
    [SerializeField] private GameObject avatarItemPrefab;     // 头像项预制体
    [SerializeField] private Button confirmButton;            // 确认按钮
    [SerializeField] private Button cancelButton;             // 取消按钮
    [SerializeField] private Text titleText;                  // 标题文本

    [Header("设置")]
    [SerializeField] private float fadeDuration = 0.2f;

    // 回调函数
    private Action<int> onAvatarSelected;

    // 数据
    private Sprite[] avatarSprites;
    private int selectedIndex = -1;
    private AvatarItem[] avatarItems;

    /// <summary>
    /// 初始化对话框
    /// </summary>
    public void Initialize(int currentIndex, Sprite[] sprites, Action<int> callback)
    {
        avatarSprites = sprites;
        selectedIndex = currentIndex;
        onAvatarSelected = callback;

        InitializeUI();
        PopulateAvatarGrid();
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
            titleText.text = "选择头像";
        }
    }

    /// <summary>
    /// 填充头像网格
    /// </summary>
    private void PopulateAvatarGrid()
    {
        if (avatarGrid == null || avatarItemPrefab == null || avatarSprites == null)
            return;

        // 清空现有项
        foreach (Transform child in avatarGrid)
        {
            Destroy(child.gameObject);
        }

        avatarItems = new AvatarItem[avatarSprites.Length];

        // 创建头像项
        for (int i = 0; i < avatarSprites.Length; i++)
        {
            GameObject itemObj = Instantiate(avatarItemPrefab, avatarGrid);
            AvatarItem item = itemObj.GetComponent<AvatarItem>();

            if (item != null)
            {
                int index = i;
                item.Initialize(avatarSprites[i], i == selectedIndex, () => OnAvatarItemClicked(index));
                avatarItems[i] = item;
            }
        }
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

    #region 事件处理

    /// <summary>
    /// 头像项点击事件
    /// </summary>
    private void OnAvatarItemClicked(int index)
    {
        // 播放音效
        AudioManager.Instance?.PlayButtonClick();

        // 更新选择状态
        selectedIndex = index;

        for (int i = 0; i < avatarItems.Length; i++)
        {
            if (avatarItems[i] != null)
            {
                avatarItems[i].SetSelected(i == index);
            }
        }

        Debug.Log($"选择头像: {index}");
    }

    /// <summary>
    /// 确认按钮点击
    /// </summary>
    private void OnConfirmClicked()
    {
        AudioManager.Instance?.PlayButtonClick();

        if (selectedIndex >= 0)
        {
            onAvatarSelected?.Invoke(selectedIndex);
            AudioManager.Instance?.PlaySuccessSound();
        }
        else
        {
            AudioManager.Instance?.PlayErrorSound();
        }

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

/// <summary>
/// 头像项组件
/// </summary>
public class AvatarItem : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private Image selectionFrame;
    [SerializeField] private Button selectButton;

    private Action onClick;

    public void Initialize(Sprite avatarSprite, bool isSelected, Action clickCallback)
    {
        if (avatarImage != null)
        {
            avatarImage.sprite = avatarSprite;
        }

        SetSelected(isSelected);

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => onClick?.Invoke());
        }

        onClick = clickCallback;
    }

    public void SetSelected(bool selected)
    {
        if (selectionFrame != null)
        {
            selectionFrame.gameObject.SetActive(selected);
        }
    }
}