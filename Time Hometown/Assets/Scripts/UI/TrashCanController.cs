using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class TrashCanController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("图层组件")]
    [SerializeField] private Image foregroundImage;  // 前景图层
    [SerializeField] private Image backgroundImage;  // 背景图层

    [Header("颜色配置")]
    [SerializeField] private Color normalForegroundColor = Color.gray;    // 正常前景色（灰色）
    [SerializeField] private Color normalBackgroundColor = Color.blue;    // 正常背景色（蓝色）
    [SerializeField] private Color hoverForegroundColor = Color.white;    // 悬停前景色（白色）
    [SerializeField] private Color hoverBackgroundColor = Color.red;      // 悬停背景色（红色）

    [Header("动画设置")]
    [SerializeField] private float moveDuration = 0.3f;                   // 移动动画时间
    [SerializeField] private float moveOffset = 200f;                     // 移动距离（向上为正）
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector2 originalPosition;
    private RectTransform rectTransform;
    private bool isHovered = false;
    private Coroutine moveCoroutine;

    // 事件
    public event System.Action OnFurnitureDropped;  // 家具被丢入垃圾桶事件

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;

        // 设置初始颜色
        UpdateColors(false);
    }

    /// <summary>
    /// 更新颜色
    /// </summary>
    private void UpdateColors(bool isHover)
    {
        if (foregroundImage != null)
        {
            foregroundImage.color = isHover ? hoverForegroundColor : normalForegroundColor;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = isHover ? hoverBackgroundColor : normalBackgroundColor;
        }
    }

    /// <summary>
    /// 鼠标进入事件
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        UpdateColors(true);
    }

    /// <summary>
    /// 鼠标退出事件
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateColors(false);
    }

    /// <summary>
    /// 检查鼠标是否悬停在垃圾桶上
    /// </summary>
    public bool IsHovered()
    {
        return isHovered;
    }

    /// <summary>
    /// 垃圾桶向上移动（拖拽开始时）
    /// </summary>
    public void MoveUp()
    {
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(MoveToPosition(originalPosition + new Vector2(0, moveOffset)));
    }

    /// <summary>
    /// 垃圾桶向下移动（拖拽结束时）
    /// </summary>
    public void MoveDown()
    {
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(MoveToPosition(originalPosition));
    }

    /// <summary>
    /// 移动协程
    /// </summary>
    private IEnumerator MoveToPosition(Vector2 targetPosition)
    {
        Vector2 startPosition = rectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            float curvedT = moveCurve.Evaluate(t);

            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, curvedT);

            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
        moveCoroutine = null;
    }

    /// <summary>
    /// 触发删除
    /// </summary>
    public void DeleteFurniture()
    {
        OnFurnitureDropped?.Invoke();

        // 播放删除动画或音效
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySuccessSound(); // 可以使用专门的删除音效

        // 可以添加一个短暂的闪烁效果
        StartCoroutine(DeleteFlashEffect());
    }

    /// <summary>
    /// 删除时的闪烁效果
    /// </summary>
    private IEnumerator DeleteFlashEffect()
    {
        if (foregroundImage != null && backgroundImage != null)
        {
            Color originalFgColor = foregroundImage.color;
            Color originalBgColor = backgroundImage.color;

            // 闪烁白色
            foregroundImage.color = Color.white;
            backgroundImage.color = Color.white;

            yield return new WaitForSeconds(0.1f);

            // 恢复悬停状态的颜色
            UpdateColors(isHovered);
        }

        yield return null;
    }


    /// <summary>
    /// 重置到原始位置（用于场景初始化）
    /// </summary>
    public void ResetPosition()
    {
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        rectTransform.anchoredPosition = originalPosition;
        moveCoroutine = null;
    }
}