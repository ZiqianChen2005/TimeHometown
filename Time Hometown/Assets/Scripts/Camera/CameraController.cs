using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;

    [Header("相机设置")]
    private float moveDuration = 1.5f;      // 移动持续时间
    public AnimationCurve moveCurve;      // 移动曲线
    private float distance = 1920f;         // 移动距离 (1920/2 ÷ 100)

    [Header("状态")]
    [SerializeField] private bool isAtMainView = false;     // 是否在主视图
    private Vector3 loginViewPosition;                      // 登录视图位置
    private Vector3 mainViewPosition;                       // 主视图位置

    [Header("事件")]
    public System.Action OnMoveToMainStart;                 // 移动到主界面开始事件
    public System.Action OnMoveToMainComplete;              // 移动到主界面完成事件
    public System.Action OnMoveToLoginStart;                // 移动到登录界面开始事件
    public System.Action OnMoveToLoginComplete;             // 移动到登录界面完成事件

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

        InitializePositions();

        // 默认移动曲线（缓入缓出）
        if (moveCurve == null || moveCurve.keys.Length == 0)
        {
            moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        }
    }

    private void InitializePositions()
    {
        // 当前相机位置作为登录视图位置
        loginViewPosition = transform.position;

        // 计算主视图位置（向下移动一个屏幕高度的一半）
        // 注意：Orthographic相机Size为8时，屏幕高度为16个单位
        mainViewPosition = new Vector3(
            loginViewPosition.x,
            loginViewPosition.y - distance,  // 9.6 = 1920/2 ÷ 100 ÷ 2
            loginViewPosition.z
        );

        Debug.Log($"相机位置初始化完成:");
        Debug.Log($"- 登录视图: {loginViewPosition}");
        Debug.Log($"- 主视图: {mainViewPosition}");
    }

    /// <summary>
    /// 移动到主界面
    /// </summary>
    public void MoveToMainView()
    {
        if (isAtMainView) return;

        StartCoroutine(MoveCoroutine(mainViewPosition, true));
    }

    /// <summary>
    /// 移动到登录界面
    /// </summary>
    public void MoveToLoginView()
    {
        if (!isAtMainView) return;

        StartCoroutine(MoveCoroutine(loginViewPosition, false));
    }

    /// <summary>
    /// 协程：执行相机移动
    /// </summary>
    private IEnumerator MoveCoroutine(Vector3 targetPosition, bool movingToMain)
    {
        // 触发开始事件
        if (movingToMain)
        {
            OnMoveToMainStart?.Invoke();
            Debug.Log("开始移动到主界面...");
        }
        else
        {
            OnMoveToLoginStart?.Invoke();
            Debug.Log("开始移动到登录界面...");
        }

        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        // 播放移动动画
        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / moveDuration);
            float curvedT = moveCurve.Evaluate(t);

            // 平滑移动
            transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                curvedT
            );

            yield return null;
        }

        // 确保到达精确位置
        transform.position = targetPosition;

        // 更新状态
        isAtMainView = movingToMain;

        // 触发完成事件
        if (movingToMain)
        {
            OnMoveToMainComplete?.Invoke();
            Debug.Log("已到达主界面");
        }
        else
        {
            OnMoveToLoginComplete?.Invoke();
            Debug.Log("已返回登录界面");
        }
    }

    /// <summary>
    /// 立即跳转到主界面（用于测试）
    /// </summary>
    public void JumpToMainView()
    {
        transform.position = mainViewPosition;
        isAtMainView = true;
        Debug.Log("已跳转到主界面");
    }

    /// <summary>
    /// 立即跳转到登录界面（用于测试）
    /// </summary>
    public void JumpToLoginView()
    {
        transform.position = loginViewPosition;
        isAtMainView = false;
        Debug.Log("已跳转到登录界面");
    }

    /// <summary>
    /// 设置移动速度
    /// </summary>
    public void SetMoveDuration(float duration)
    {
        if (duration > 0)
        {
            moveDuration = duration;
            Debug.Log($"移动持续时间已设置为: {duration}秒");
        }
    }

    /// <summary>
    /// 获取当前状态
    /// </summary>
    public bool IsAtMainView()
    {
        return isAtMainView;
    }
}