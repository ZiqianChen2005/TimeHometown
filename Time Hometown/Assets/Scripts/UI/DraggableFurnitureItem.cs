using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class DraggableFurnitureItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI组件")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("拖拽设置")]
    [SerializeField] private float dragAlpha = 0.8f; // 拖拽时的透明度
    [SerializeField] private Vector2 dragOffset = new Vector2(50, -50); // 拖拽偏移量

    // 家具数据
    private FurnitureData furnitureData;
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private Vector2 originalPosition;
    private bool isDragging = false;

    // 事件
    public event Action<FurnitureData, Vector2> OnDragStart;
    public event Action<FurnitureData, Vector2> OnDragEnd;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        parentCanvas = GetComponentInParent<Canvas>();
    }

    public void Initialize(FurnitureData data)
    {
        furnitureData = data;

        if (iconImage != null && data.icon != null)
        {
            iconImage.sprite = data.icon;

            float maxSize = 100f;
            float aspectRatio = (float)data.width / data.height;

            RectTransform iconRect = iconImage.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);

            if (aspectRatio >= 1f)
            {
                float finalWidth = maxSize;
                float finalHeight = maxSize / aspectRatio;
                iconRect.sizeDelta = new Vector2(finalWidth, finalHeight);
            }
            else
            {
                float finalHeight = maxSize;
                float finalWidth = maxSize * aspectRatio;
                iconRect.sizeDelta = new Vector2(finalWidth, finalHeight);
            }

            iconImage.preserveAspect = true;
        }

        if (nameText != null)
            nameText.text = data.name;

        gameObject.name = $"Drag_{data.name}";
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        originalPosition = rectTransform.anchoredPosition;

        canvasGroup.alpha = dragAlpha;
        canvasGroup.blocksRaycasts = false;

        OnDragStart?.Invoke(furnitureData, rectTransform.anchoredPosition);

        Debug.Log($"开始拖拽: {furnitureData.name}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            eventData.position,
            parentCanvas.worldCamera,
            out localPoint
        );

        rectTransform.anchoredPosition = localPoint + dragOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // 触发拖拽结束事件 - 松手时立即触发
        OnDragEnd?.Invoke(furnitureData, eventData.position);

        // 回到原位
        rectTransform.anchoredPosition = originalPosition;

        Debug.Log($"结束拖拽: {furnitureData.name}");
    }

    public FurnitureData GetFurnitureData()
    {
        return furnitureData;
    }
}