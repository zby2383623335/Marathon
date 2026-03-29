using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelEndTrigger : MonoBehaviour
{
    [Header("将要加载的场景名")]
    [Tooltip("若留空则加载当前场景的下一个 buildIndex")]
    public string nextSceneName = "";

    [Header("加载后场景内玩家位置")]
    public Vector2 playerSpawnPosition = Vector2.zero;

    private bool loading = false;

    /// <summary>
    /// 静态字段，用于在场景加载时传递玩家生成位置
    /// </summary>
    public static Vector2 NextScenePlayerSpawnPosition { get; set; } = Vector2.zero;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (loading) return;
        if (!other.CompareTag("Player")) return;

        loading = true;
        // 保存玩家生成位置到静态字段
        NextScenePlayerSpawnPosition = playerSpawnPosition;

        if (!string.IsNullOrEmpty(nextSceneName))
            StartCoroutine(SceneLoader.Instance.LoadSceneAsync(nextSceneName));
        else
            StartCoroutine(SceneLoader.Instance.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex + 1));
    }
}