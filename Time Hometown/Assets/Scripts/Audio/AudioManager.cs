using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("音频源")]
    [SerializeField] private AudioSource bgmSource;      // 背景音乐音频源
    [SerializeField] private AudioSource sfxSource;      // 音效音频源

    [Header("默认音量")]
    [SerializeField] private float defaultBGMVolume = 0.5f;
    [SerializeField] private float defaultSFXVolume = 0.7f;

    [Header("音频剪辑")]
    [SerializeField] private AudioClip[] bgmClips;       // 背景音乐列表
    [SerializeField] private AudioClip buttonClickSFX;   // 按钮点击音效
    [SerializeField] private AudioClip successSFX;       // 成功音效
    [SerializeField] private AudioClip errorSFX;         // 错误音效
    [SerializeField] private AudioClip notificationSFX;  // 通知音效

    public float currentBGMVolume = 0.5f;
    public float currentSFXVolume = 0.7f;
    private bool isMuted = false;
    private int currentBGMIndex = 0;

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

        InitializeAudioSources();
        LoadSavedVolumeSettings();
        StartBackgroundMusic();
    }

    private void InitializeAudioSources()
    {
        // 确保音频源存在
        if (bgmSource == null)
        {
            GameObject bgmObj = new GameObject("BGM AudioSource");
            bgmObj.transform.SetParent(transform);
            bgmSource = bgmObj.AddComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFX AudioSource");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
        }

        // 配置BGM音频源
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = currentBGMVolume;

        // 配置SFX音频源
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = currentSFXVolume;
    }

    private void LoadSavedVolumeSettings()
    {
        // 从PlayerPrefs加载保存的音量设置
        if (PlayerPrefs.HasKey("BGM_Volume"))
        {
            currentBGMVolume = PlayerPrefs.GetFloat("BGM_Volume", defaultBGMVolume);
        }
        else
        {
            currentBGMVolume = defaultBGMVolume;
        }

        if (PlayerPrefs.HasKey("SFX_Volume"))
        {
            currentSFXVolume = PlayerPrefs.GetFloat("SFX_Volume", defaultSFXVolume);
        }
        else
        {
            currentSFXVolume = defaultSFXVolume;
        }

        // 应用加载的音量
        SetBGMVolume(currentBGMVolume);
        SetSFXVolume(currentSFXVolume);

        Debug.Log($"加载音量设置: BGM={currentBGMVolume}, SFX={currentSFXVolume}");
    }

    private void StartBackgroundMusic()
    {
        if (bgmClips != null && bgmClips.Length > 0)
        {
            PlayBGM(currentBGMIndex);
        }
    }

    #region 音量控制

    public void SetBGMVolume(float volume)
    {
        if (isMuted) return;

        currentBGMVolume = Mathf.Clamp01(volume);
        if (bgmSource != null)
        {
            bgmSource.volume = currentBGMVolume;
        }

        // 保存设置
        PlayerPrefs.SetFloat("BGM_Volume", currentBGMVolume);
        PlayerPrefs.Save();

        Debug.Log($"设置BGM音量: {currentBGMVolume}");
    }

    public void SetSFXVolume(float volume)
    {
        if (isMuted) return;

        currentSFXVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = currentSFXVolume;
        }

        // 保存设置
        PlayerPrefs.SetFloat("SFX_Volume", currentSFXVolume);
        PlayerPrefs.Save();

        Debug.Log($"设置SFX音量: {currentSFXVolume}");
    }

    public float GetBGMVolume()
    {
        return currentBGMVolume;
    }

    public float GetSFXVolume()
    {
        return currentSFXVolume;
    }

    // 音量预览（用于设置界面实时预览）
    public void PreviewBGMVolume(float volume)
    {
        if (bgmSource != null && !isMuted)
        {
            float previewVolume = Mathf.Clamp01(volume);
            bgmSource.volume = previewVolume;

            // 2秒后恢复原音量
            StartCoroutine(RestoreBGMVolumeAfterDelay(2f));
        }
    }

    private IEnumerator RestoreBGMVolumeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (bgmSource != null)
        {
            bgmSource.volume = currentBGMVolume;
        }
    }

    // 静音/取消静音
    public void ToggleMute()
    {
        isMuted = !isMuted;

        if (bgmSource != null)
        {
            bgmSource.volume = isMuted ? 0f : currentBGMVolume;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = isMuted ? 0f : currentSFXVolume;
        }

        Debug.Log($"静音状态: {isMuted}");
    }

    public bool IsMuted()
    {
        return isMuted;
    }

    #endregion

    #region 音乐播放控制

    public void PlayBGM(int index)
    {
        if (bgmClips == null || index < 0 || index >= bgmClips.Length)
        {
            Debug.LogWarning($"无效的BGM索引: {index}");
            return;
        }

        currentBGMIndex = index;

        if (bgmSource != null)
        {
            bgmSource.clip = bgmClips[index];
            bgmSource.Play();
            Debug.Log($"播放BGM: {bgmClips[index].name}");
        }
    }

    public void PlayNextBGM()
    {
        if (bgmClips == null || bgmClips.Length == 0) return;

        currentBGMIndex = (currentBGMIndex + 1) % bgmClips.Length;
        PlayBGM(currentBGMIndex);
    }

    public void PlayPreviousBGM()
    {
        if (bgmClips == null || bgmClips.Length == 0) return;

        currentBGMIndex = (currentBGMIndex - 1 + bgmClips.Length) % bgmClips.Length;
        PlayBGM(currentBGMIndex);
    }

    public void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }
    }

    public void PauseBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Pause();
        }
    }

    public void ResumeBGM()
    {
        if (bgmSource != null && !bgmSource.isPlaying)
        {
            bgmSource.UnPause();
        }
    }

    public bool IsBGMPlaying()
    {
        return bgmSource != null && bgmSource.isPlaying;
    }

    #endregion

    #region 音效播放

    public void PlaySFX(string sfxName)
    {
        AudioClip clip = GetSFXClipByName(sfxName);
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
            Debug.Log($"播放音效: {sfxName}");
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public void PlayButtonClick()
    {
        if (buttonClickSFX != null)
        {
            PlaySFX(buttonClickSFX);
        }
    }

    public void PlaySuccessSound()
    {
        if (successSFX != null)
        {
            PlaySFX(successSFX);
        }
    }

    public void PlayErrorSound()
    {
        if (errorSFX != null)
        {
            PlaySFX(errorSFX);
        }
    }

    public void PlayNotificationSound()
    {
        if (notificationSFX != null)
        {
            PlaySFX(notificationSFX);
        }
    }

    private AudioClip GetSFXClipByName(string name)
    {
        switch (name.ToLower())
        {
            case "click":
            case "button":
                return buttonClickSFX;
            case "success":
                return successSFX;
            case "error":
                return errorSFX;
            case "notification":
            case "notify":
                return notificationSFX;
            default:
                Debug.LogWarning($"未找到音效: {name}");
                return null;
        }
    }

    #endregion

    #region 工具方法

    public void ResetToDefaultVolume()
    {
        SetBGMVolume(defaultBGMVolume);
        SetSFXVolume(defaultSFXVolume);
        Debug.Log($"音量已重置为默认值: BGM={defaultBGMVolume}, SFX={defaultSFXVolume}");
    }

    public void FadeOutBGM(float duration)
    {
        StartCoroutine(FadeBGM(0f, duration));
    }

    public void FadeInBGM(float duration)
    {
        StartCoroutine(FadeBGM(currentBGMVolume, duration));
    }

    private IEnumerator FadeBGM(float targetVolume, float duration)
    {
        if (bgmSource == null) yield break;

        float startVolume = bgmSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            bgmSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        bgmSource.volume = targetVolume;
    }

    #endregion

    #region 音频资源管理

    public void AddBGMClip(AudioClip clip)
    {
        if (clip != null)
        {
            System.Array.Resize(ref bgmClips, bgmClips.Length + 1);
            bgmClips[bgmClips.Length - 1] = clip;
        }
    }

    public void SetButtonClickSFX(AudioClip clip)
    {
        buttonClickSFX = clip;
    }

    public void SetSuccessSFX(AudioClip clip)
    {
        successSFX = clip;
    }

    public void SetErrorSFX(AudioClip clip)
    {
        errorSFX = clip;
    }

    public void SetNotificationSFX(AudioClip clip)
    {
        notificationSFX = clip;
    }

    #endregion
}