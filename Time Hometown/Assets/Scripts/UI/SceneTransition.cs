using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("场景名称")]
    [SerializeField] private string mainSceneName = "MainScene";
    [SerializeField] private string shopSceneName = "Shop";

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
    }

    /// <summary>
    /// 跳转到家具商店
    /// </summary>
    public void GoToFurnitureShop()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        SceneManager.LoadScene(shopSceneName);
        Debug.Log($"跳转到家具商店: {shopSceneName}");
    }

    /// <summary>
    /// 返回主场景
    /// </summary>
    public void GoToMainScene()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        SceneManager.LoadScene(mainSceneName);
        Debug.Log($"返回主场景: {mainSceneName}");
    }

    /// <summary>
    /// 获取当前场景名称
    /// </summary>
    public string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    /// <summary>
    /// 检查是否在商店场景
    /// </summary>
    public bool IsInShopScene()
    {
        return GetCurrentSceneName() == shopSceneName;
    }

    /// <summary>
    /// 检查是否在主场景
    /// </summary>
    public bool IsInMainScene()
    {
        return GetCurrentSceneName() == mainSceneName;
    }
}