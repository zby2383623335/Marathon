using UnityEngine;

public class MouseWoodController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    [Tooltip("木头的显示图标")]
    private SpriteRenderer woodSpriteRenderer;

    [Header("Settings")]
    [SerializeField]
    [Tooltip("鼠标持有木头时的透明度")]
    private float cursorWoodAlpha = 0.5f;
    [SerializeField]
    [Tooltip("木头旋转速率（度/秒）")]
    private float woodRotationSpeed = 90f;

    /// <summary>
    /// 是否当前持有木头
    /// </summary>
    private bool isHoldingWood = false;
    /// <summary>
    /// 主摄像机
    /// </summary>
    private Camera mainCam;
    /// <summary>
    /// 鼠标持有木头时展示的图标
    /// </summary>
    private GameObject cursorWoodInstance;
    /// <summary>
    /// 鼠标持有木头时展示的图标的精灵渲染器
    /// </summary>
    private SpriteRenderer cursorSpriteRenderer;
    /// <summary>
    /// 木头的显示图标
    /// </summary>
    private Sprite woodSprite;
    /// <summary>
    /// 木头预制体的Collider2D（用于放置检测）
    /// </summary>
    private Collider2D woodCollider;
    /// <summary>
    /// 木头当前的旋转角度（度）
    /// </summary>
    private float currentWoodRotation = 0f;
    /// <summary>
    /// 木头预制体的缩放
    /// </summary>
    private Vector3 woodScale = Vector3.one;

    void Start()
    {
        mainCam = Camera.main;
        woodSprite = woodSpriteRenderer != null ? woodSpriteRenderer.sprite : null;

        // 获取木头预制体的Collider2D，用于放置检测
        woodCollider = WoodPool.Instance.GetWoodCollider();

        // 获取木头预制体的缩放
        woodScale = WoodPool.Instance.GetWoodScale();
    }

    void Update()
    {
        // 检测是否生成木头图标
        if (isHoldingWood && cursorWoodInstance != null && mainCam != null)
        {
            Vector3 worldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
            worldPos.z = 0f;
            cursorWoodInstance.transform.position = worldPos;

            // 检测右键按住旋转木头
            if (Input.GetMouseButton(1))
            {
                // 按住右键时逆时针旋转（角度增加）
                currentWoodRotation += woodRotationSpeed * Time.deltaTime;
                // 保持角度在0-360之间
                if (currentWoodRotation >= 360f)
                    currentWoodRotation -= 360f;
            }

            // 更新预览的旋转
            cursorWoodInstance.transform.rotation = Quaternion.Euler(0, 0, currentWoodRotation);

            // 检测鼠标是否指向玩家
            bool pointingAtPlayer = IsPointingAtPlayer(worldPos);

            // 检测放置位置是否会发生碰撞
            bool hasCollision = CheckPlacementCollision(worldPos);
            UpdatePreviewColor(hasCollision, pointingAtPlayer);
        }

        if (Input.GetMouseButtonDown(0))
        {
            HandleLeftClick();
        }
    }

    private void HandleLeftClick()
    {
        Vector3 worldPos3 = mainCam.ScreenToWorldPoint(Input.mousePosition);
        worldPos3.z = 0f;
        Vector2 worldPos = worldPos3;

        // 先检测是否点击到带 Collider2D 的物体（例如 player）
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);
        Collider2D hit = null;
        player p = null;
        if (hits != null && hits.Length > 0)
        {
            // 优先找到属于 player 的 collider，避免 checkpoint/其他 trigger 覆盖玩家点击
            foreach (var h in hits)
            {
                p = h.GetComponentInParent<player>();
                if (p != null)
                {
                    hit = h;
                    break;
                }
            }

            // 若没有命中 player，则使用第一个命中的 collider
            if (hit == null)
                hit = hits[0];
        }

        if (hit != null)
        {
            // 如果点击到 player
            p = hit.GetComponentInParent<player>();
            if (p != null)
            {
                if (!isHoldingWood)
                {
                    // 尝试从 player 身上拿一根木头
                    bool ok = p.PickWood();
                    if (ok)
                    {
                        // 标记为持有木头
                        isHoldingWood = true;
                        // 重置旋转角度
                        currentWoodRotation = 0f;
                        CreateCursorVisual();
                        // 更新UI显示
                        if (WoodUIControler.IsInitialized)
                            WoodUIControler.Instance.UpdateWoodUI(p.GetWoodCount());
                    }
                }
                else
                {
                    // 将手上的木头放回 player 背包
                    bool ok = p.AddWood();
                    if (ok)
                    {
                        isHoldingWood = false;
                        DestroyCursorVisual();
                        // 更新UI显示
                        if (WoodUIControler.IsInitialized)
                            WoodUIControler.Instance.UpdateWoodUI(p.GetWoodCount());
                    }
                    // 如果放回失败（背包满），不做处理
                }

                return;
            }

            // 如果没点击到player，尝试拾起场景中的木头
            if (!isHoldingWood && WoodPool.Instance.IsActiveWood(hit.gameObject))
            {
                // 拾起场景中的木头
                isHoldingWood = true;
                // 重置旋转角度
                currentWoodRotation = 0f;
                // 将木头归还到对象池
                WoodPool.Instance.ReturnWood(hit.gameObject);
                CreateCursorVisual();
                return;
            }
        }

        // 若没点击到 player 或 木头，且当前持有木头，则在点击位置放置木头
        if (isHoldingWood)
        {
            // 检测放置位置是否会发生碰撞
            if (CheckPlacementCollision(worldPos3))
            {
                // 位置有碰撞，不允许放置
                Debug.LogWarning("木头放置位置有碰撞，禁止放置！");
                return;
            }

            // 从对象池中获取木头对象并放置在点击位置
            GameObject wood = WoodPool.Instance.GetWood(worldPos3);
            // 应用旋转角度
            wood.transform.rotation = Quaternion.Euler(0, 0, currentWoodRotation);

            isHoldingWood = false;
            DestroyCursorVisual();
        }
    }

    private void CreateCursorVisual()
    {
        DestroyCursorVisual();

        // 创建半透明的木头贴图预览
        if (woodSprite != null)
        {
            cursorWoodInstance = new GameObject("CursorWood");
            cursorSpriteRenderer = cursorWoodInstance.AddComponent<SpriteRenderer>();
            cursorSpriteRenderer.sprite = woodSprite;
            cursorSpriteRenderer.sortingOrder = 1000;

            // 应用木头预制体的缩放
            cursorWoodInstance.transform.localScale = woodScale;

            // 设置为半透明
            Color color = cursorSpriteRenderer.color;
            color.a = cursorWoodAlpha;
            cursorSpriteRenderer.color = color;
        }
        else
        {
            // 创建一个空的占位符
            cursorWoodInstance = new GameObject("CursorWood");
            // 即使是占位符也应用缩放
            cursorWoodInstance.transform.localScale = woodScale;
        }

        if (cursorWoodInstance != null && mainCam != null)
        {
            Vector3 worldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
            worldPos.z = 0f;
            cursorWoodInstance.transform.position = worldPos;
        }
    }

    private void DestroyCursorVisual()
    {
        if (cursorWoodInstance != null)
        {
            Destroy(cursorWoodInstance);
            cursorWoodInstance = null;
            cursorSpriteRenderer = null;
        }
    }

    /// <summary>
    /// 检测放置位置是否存在碰撞体
    /// </summary>
    /// <param name="position">要检测的世界位置</param>
    /// <returns>如果存在碰撞返回true，否则返回false</returns>
    private bool CheckPlacementCollision(Vector3 position)
    {
        // 如果没有木头Collider2D，无法进行检测
        if (woodCollider == null)
            return false;

        // 创建临时的检测物体，用于进行碰撞检测
        GameObject tempCheckObject = new GameObject("_TempCollisionCheck");
        tempCheckObject.transform.position = position;
        // 应用木头预制体的缩放
        tempCheckObject.transform.localScale = woodScale;
        // 应用木头的旋转角度
        tempCheckObject.transform.rotation = Quaternion.Euler(0, 0, currentWoodRotation);

        // 复制木头的Collider2D到临时对象
        Collider2D tempCollider = tempCheckObject.AddComponent(woodCollider.GetType()) as Collider2D;

        // 复制碰撞体的属性
        if (tempCollider is BoxCollider2D boxSource && woodCollider is BoxCollider2D boxTarget)
        {
            boxSource.offset = boxTarget.offset;
            boxSource.size = boxTarget.size;
        }
        else if (tempCollider is CircleCollider2D circleSource && woodCollider is CircleCollider2D circleTarget)
        {
            circleSource.offset = circleTarget.offset;
            circleSource.radius = circleTarget.radius;
        }
        else if (tempCollider is PolygonCollider2D polySource && woodCollider is PolygonCollider2D polyTarget)
        {
            polySource.points = polyTarget.points;
            polySource.offset = polyTarget.offset;
        }

        // 使用OverlapCollider检测是否有碰撞，忽略触发器（例如检查点）或带有 Checkpoint 组件的碰撞体
        Collider2D[] colliders = new Collider2D[10];
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true; // we still collect triggers but will ignore checkpoints explicitly
        int colliderCount = Physics2D.OverlapCollider(tempCollider, filter, colliders);

        // 清理临时对象
        Destroy(tempCheckObject);

        // 检查碰撞结果
        for (int i = 0; i < colliderCount; i++)
        {
            Collider2D collider = colliders[i];
            if (collider == null) continue;

            // 忽略检查点触发器对放置判断的影响
            if (collider.GetComponent<Checkpoint>() != null) continue;

            // 如果有其他Rigidbody2D，说明会与动态物体发生物理冲突
            Collider2D rb = collider.GetComponent<Collider2D>();
            if (rb != null)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 更新预览贴图的颜色
    /// </summary>
    /// <param name="hasCollision">是否有碰撞</param>
    /// <param name="pointingAtPlayer">是否指向玩家</param>
    private void UpdatePreviewColor(bool hasCollision, bool pointingAtPlayer)
    {
        if (cursorSpriteRenderer == null)
            return;

        Color color = cursorSpriteRenderer.color;

        // 根据是否指向玩家来调整透明度
        float alpha = pointingAtPlayer ? cursorWoodAlpha / 2f : cursorWoodAlpha;

        if (hasCollision)
        {
            // 碰撞时显示红色
            color.r = 1f;
            color.g = 0f;
            color.b = 0f;
            color.a = alpha;
        }
        else
        {
            // 正常时显示白色（原始颜色）
            color.r = 1f;
            color.g = 1f;
            color.b = 1f;
            color.a = alpha;
        }

        cursorSpriteRenderer.color = color;
    }

    /// <summary>
    /// 供 UI 显示当前是否持有木头
    /// </summary>
    /// <returns>持有木头返回true，否则返回false</returns>
    public bool IsHoldingWood()
    {
        return isHoldingWood;
    }

    /// <summary>
    /// 检测鼠标是否指向玩家
    /// </summary>
    /// <param name="worldPos">鼠标的世界位置</param>
    /// <returns>如果指向玩家返回true，否则返回false</returns>
    private bool IsPointingAtPlayer(Vector3 worldPos)
    {
        // 使用OverlapPointAll优先查找玩家的碰撞体，避免被检查点等触发器遮挡
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);
        if (hits != null && hits.Length > 0)
        {
            foreach (var h in hits)
            {
                var p = h.GetComponentInParent<player>();
                if (p != null) return true;
            }
        }

        return false;
    }
}