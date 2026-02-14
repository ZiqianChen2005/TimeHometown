using System; // 新增：用于Action委托
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIMaskManager : MonoBehaviour
{
    [Header("遮罩组件")]
    public GameObject panelMask;        // 遮罩对象
    public Image maskImage;            // 遮罩图片
    public Button maskButton;          // 遮罩按钮

    [Header("遮罩设置")]
    public Color maskColor = new Color(0, 0, 0, 0.5f); // 半透明黑色
    public float animationDuration = 0.2f;            // 动画时长

    // 当前状态
    private GameObject currentPanel;
    private Action onMaskClick;

    void Start()
    {
        // 初始化遮罩
        if (maskImage != null)
            maskImage.color = maskColor;

        // 绑定点击事件
        if (maskButton != null)
        {
            maskButton.onClick.RemoveAllListeners();
            maskButton.onClick.AddListener(OnMaskClick);
        }

        // 初始关闭
        if (panelMask != null)
            panelMask.SetActive(false);
    }

    // 打开遮罩
    public void ShowMask(GameObject panel, Action onClickCallback)
    {
        currentPanel = panel;
        onMaskClick = onClickCallback;

        if (panelMask != null)
        {
            panelMask.SetActive(true);
            panelMask.transform.SetAsLastSibling();
            panel.transform.SetAsLastSibling();

            // 淡入动画
            StartCoroutine(FadeIn());
        }
    }

    // 关闭遮罩
    public void HideMask()
    {
        if (panelMask != null && panelMask.activeSelf)
        {
            // 淡出动画
            StartCoroutine(FadeOut());
        }

        currentPanel = null;
        onMaskClick = null;
    }

    // 遮罩点击处理
    private void OnMaskClick()
    {
        onMaskClick?.Invoke();
    }

    IEnumerator FadeIn()
    {
        if (maskImage == null) yield break;

        float elapsed = 0;
        Color color = maskColor;
        color.a = 0;
        maskImage.color = color;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(0, maskColor.a, elapsed / animationDuration);
            maskImage.color = color;
            yield return null;
        }

        maskImage.color = maskColor;
    }

    IEnumerator FadeOut()
    {
        if (maskImage == null) yield break;

        float elapsed = 0;
        Color color = maskImage.color;
        float startAlpha = color.a;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, 0, elapsed / animationDuration);
            maskImage.color = color;
            yield return null;
        }

        color.a = 0;
        maskImage.color = color;

        if (panelMask != null)
            panelMask.SetActive(false);
    }
}