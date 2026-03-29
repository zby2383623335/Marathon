using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BoxWeightSensor : MonoBehaviour
{
    [Tooltip("检测区域相对于箱子中心的偏移（世界坐标系内相对）")]
    public Vector2 sensorOffset = new Vector2(0f, 0.6f);
    [Tooltip("检测区域大小（宽,高）")]
    public Vector2 sensorSize = new Vector2(1.0f, 0.3f);
    [Tooltip("哪些 Layer 会被计入重量（Player/Wood 等）")]
    public LayerMask weightLayers = ~0;
    [Tooltip("当检测到玩家但玩家没有刚体时使用的等效质量")]
    public float playerEffectiveMass = 1.0f;

    private Rigidbody2D rb;
    private float lastDetectedMass = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        // 仅统计，不在此处施加力（由 PulleyController 统一控制），但也可供独立使用
        lastDetectedMass = DetectMassOnTop();
    }

    /// <summary>
    /// 返回检测到的附加质量（单位：质量）
    /// </summary>
    public float GetDetectedMass()
    {
        return lastDetectedMass;
    }

    /// <summary>
    /// 立即检测并返回质量（不会依赖上一次 FixedUpdate）
    /// </summary>
    public float DetectMassOnTop()
    {
        Vector2 worldCenter = (Vector2)transform.position + sensorOffset;
        Collider2D[] hits = Physics2D.OverlapBoxAll(worldCenter, sensorSize, 0f, weightLayers);
        float totalMass = 0f;
        foreach (var c in hits)
        {
            if (c == null) continue;
            var otherRb = c.attachedRigidbody;
            if (otherRb != null)
            {
                // 不统计与自身相连的刚体
                if (otherRb == rb) continue;
                totalMass += otherRb.mass;
                continue;
            }

            var p = c.GetComponentInParent<player>();
            if (p != null)
            {
                totalMass += playerEffectiveMass;
            }
        }
        return totalMass;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector2 worldCenter = (Vector2)transform.position + sensorOffset;
        Gizmos.DrawWireCube(worldCenter, sensorSize);
    }
}
