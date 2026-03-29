using UnityEngine;
using UnityEngine.SceneManagement;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    private Vector3 respawnPosition;
    private bool hasCheckpoint = false;

/// <summary>
/// 新增公有布尔字段 reloadSceneOnRespawn（Inspector 可切换），开启后按 R 或调用复活会重载当前场景以完全重置关卡。
/// 如果 reloadSceneOnRespawn 为 true，复活会触发场景重载，并在场景加载完成后自动把玩家移动到记录点并恢复木头数量（通过订阅 SceneManager.sceneLoaded 来执行延后应用）。
/// 如果 reloadSceneOnRespawn 为 false，则按原逻辑直接在当前场景内移动并重置玩家状态。
/// </summary>

    [Tooltip("场景中要被复活的玩家对象（可选，若为空会在运行时搜索带有 'player' 脚本的对象）")]
    public GameObject playerObject;
    [Tooltip("复活时是否重载场景以完全重置关卡（启用后会在场景加载完成后把玩家移动到重生点并恢复木头数量）")]
    public bool reloadSceneOnRespawn = false;

    private bool pendingRespawnAfterLoad = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (playerObject == null)
        {
            var p = FindObjectOfType<player>();
            if (p != null) playerObject = p.gameObject;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RespawnPlayer();
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (pendingRespawnAfterLoad)
        {
            // apply respawn after scene reload
            pendingRespawnAfterLoad = false;
            ApplyRespawn();
        }
    }

    public void SetCheckpoint(Vector3 worldPosition)
    {
        respawnPosition = worldPosition;
        hasCheckpoint = true;
    }

    public bool HasCheckpoint() => hasCheckpoint;

    public void RespawnPlayer()
    {
        if (!hasCheckpoint) return;

        if (reloadSceneOnRespawn)
        {
            // mark pending and reload current scene
            pendingRespawnAfterLoad = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        // immediate apply
        if (playerObject == null)
        {
            var p = FindObjectOfType<player>();
            if (p != null) playerObject = p.gameObject;
        }

        if (playerObject == null) return;

        ApplyRespawn();
    }

    private void ApplyRespawn()
    {
        if (playerObject == null)
        {
            var p = FindObjectOfType<player>();
            if (p != null) playerObject = p.gameObject;
        }

        if (playerObject == null) return;

        var pComp = playerObject.GetComponent<player>();
        var rb = playerObject.GetComponent<Rigidbody2D>();

        // reset position
        playerObject.transform.position = respawnPosition;

        // reset velocity
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (pComp != null)
        {
            // 复活时固定持有 4 根木头
            const int targetWoodCount = 4;
            int current = pComp.GetWoodCount();
            if (targetWoodCount > current)
            {
                int toAdd = targetWoodCount - current;
                for (int i = 0; i < toAdd; i++)
                {
                    if (!pComp.AddWood()) break;
                }
            }
            else if (targetWoodCount < current)
            {
                int toRemove = current - targetWoodCount;
                for (int i = 0; i < toRemove; i++)
                {
                    if (!pComp.PickWood()) break;
                }
            }
        }
    }
}
