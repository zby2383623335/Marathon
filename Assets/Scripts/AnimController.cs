using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnimController : MonoBehaviour
{
    /// <summary>
    /// 漫画页面数据：包含图片和播放时长
    /// </summary>
    [System.Serializable]
    public class ComicPage
    {
        [Tooltip("页面图片")]
        public Sprite sprite;
        [Tooltip("页面播放时长（秒）")]
        public float duration = 3f;
    }

    /// <summary>
    /// 动画播放完成事件委托
    /// </summary>
    public delegate void OnComicPlaybackComplete();

    /// <summary>
    /// 动画播放完成事件
    /// </summary>
    public event OnComicPlaybackComplete onPlaybackComplete;

    [Header("贴图Image")]
    [SerializeField]
    [Tooltip("黑屏")]
    private Image Black = null;
    [SerializeField]
    [Tooltip("过场动画")]
    private Image Anim = null;
    [SerializeField]
    [Tooltip("淡出淡入速度")]
    private float fadeDuration = 0.5f;
    [SerializeField]
    [Tooltip("过场动画集合（包含每页的时长）")]
    private List<ComicPage> comicPages = new List<ComicPage>();
    [SerializeField]
    [Tooltip("物体创建时是否自动开始播放")]
    private bool autoPlayOnStart = true;

    /// <summary>
    /// 当前播放的页面索引
    /// </summary>
    private int currentPageIndex = 0;
    /// <summary>
    /// 是否正在播放
    /// </summary>
    private bool isPlaying = false;

    void Start()
    {
        // 如果启用了自动播放，则在场景加载时自动开始
        if (autoPlayOnStart)
        {
            PlayComicTransition();
        }
    }

    /// <summary>
    /// 开始播放漫画过场
    /// </summary>
    public void PlayComicTransition()
    {
        if (isPlaying || comicPages.Count == 0)
            return;

        currentPageIndex = 0;
        isPlaying = true;
        StartCoroutine(PlayComicSequence());
    }

    /// <summary>
    /// 播放漫画序列
    /// </summary>
    private IEnumerator PlayComicSequence()
    {
        // 初始黑屏
        SetBlackScreenAlpha(1f);

        for (int i = 0; i < comicPages.Count; i++)
        {
            currentPageIndex = i;
            ComicPage page = comicPages[i];

            // 1. 更新Anim图片
            if (Anim != null && page.sprite != null)
            {
                Anim.sprite = page.sprite;
            }

            // 2. 黑屏淡出，显示当前页面
            yield return StartCoroutine(FadeBlackScreen(1f, 0f, fadeDuration));

            // 3. 播放当前页面的时长
            yield return new WaitForSeconds(page.duration);

            // 4. 不是最后一张图时，黑屏淡入
            if (i < comicPages.Count - 1)
            {
                yield return StartCoroutine(FadeBlackScreen(0f, 1f, fadeDuration));
            }
        }

        isPlaying = false;
        // 触发播放完成事件
        OnPlaybackComplete();
    }

    /// <summary>
    /// 触发播放完成事件
    /// </summary>
    private void OnPlaybackComplete()
    {
        onPlaybackComplete?.Invoke();
    }

    /// <summary>
    /// 黑屏淡出淡入效果
    /// </summary>
    /// <param name="startAlpha">起始透明度</param>
    /// <param name="endAlpha">目标透明度</param>
    /// <param name="duration">淡变时长（秒）</param>
    private IEnumerator FadeBlackScreen(float startAlpha, float endAlpha, float duration)
    {
        if (Black == null)
            yield break;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            SetBlackScreenAlpha(alpha);
            yield return null;
        }

        // 确保最终值精确
        SetBlackScreenAlpha(endAlpha);
    }

    /// <summary>
    /// 设置黑屏透明度
    /// </summary>
    private void SetBlackScreenAlpha(float alpha)
    {
        if (Black == null)
            return;

        Color color = Black.color;
        color.a = Mathf.Clamp01(alpha);
        Black.color = color;
    }

    /// <summary>
    /// 停止播放
    /// </summary>
    public void StopPlayback()
    {
        if (isPlaying)
        {
            StopAllCoroutines();
            isPlaying = false;
        }
    }

    /// <summary>
    /// 检查是否正在播放
    /// </summary>
    public bool IsPlaying => isPlaying;
}
