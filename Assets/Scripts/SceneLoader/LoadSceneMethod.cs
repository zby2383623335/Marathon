using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneMethod : MonoBehaviour
{
    [SerializeField]
    [Tooltip("要加载的下一个场景索引")]
    private int nextSceneIndex = 1;
    [SerializeField]
    [Tooltip("是否自动计算当前场景的下一个场景索引")]
    private bool autoLoadNextScene = true;
    [SerializeField]
    [Tooltip("是否在AnimController播完后自动加载场景")]
    private bool loadSceneAfterAnimation = true;

    /// <summary>
    /// 动画控制器引用
    /// </summary>
    private AnimController animController;

    void Start()
    {
        // 如果启用了自动计算下一个场景索引
        if (autoLoadNextScene)
        {
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            nextSceneIndex = currentSceneIndex + 1;
            Debug.Log($"当前场景索引: {currentSceneIndex}，自动设置下一个场景索引为: {nextSceneIndex}");
        }

        // 获取动画控制器
        animController = GetComponent<AnimController>();
        if (animController == null)
        {
            animController = FindObjectOfType<AnimController>();
        }

        // 如果启用了动画后自动加载场景，则订阅事件
        if (loadSceneAfterAnimation && animController != null)
        {
            animController.onPlaybackComplete += OnAnimationComplete;
        }
    }

    void OnDestroy()
    {
        // 取消订阅，防止内存泄漏
        if (animController != null)
        {
            animController.onPlaybackComplete -= OnAnimationComplete;
        }
    }

    /// <summary>
    /// 动画播放完成回调
    /// </summary>
    private void OnAnimationComplete()
    {
        Debug.Log("动画播放完成，准备加载下一场景");
        LoadNextScene();
    }

    /// <summary>
    /// 加载下一个场景
    /// </summary>
    public void LoadNextScene()
    {
        if (!SceneLoader.IsInitialized)
        {
            Debug.LogError("SceneLoader 未初始化!");
            return;
        }

        StartCoroutine(SceneLoader.Instance.LoadSceneAsync(nextSceneIndex));
    }

    /// <summary>
    /// 手动设置场景索引并加载
    /// </summary>
    public void LoadScene(int sceneIndex)
    {
        nextSceneIndex = sceneIndex;
        LoadNextScene();
    }
}
