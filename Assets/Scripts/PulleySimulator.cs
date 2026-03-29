using UnityEngine;

/// <summary>
/// 简单的滑轮模拟器（不依赖 PulleyJoint2D）。
/// 将两个箱子分别连接到上方挂点（anchorA/anchorB），总绳长为 ropeLength。
/// 每帧根据箱子自身质量 + BoxWeightSensor 检测到的额外质量计算净力，并施加等效力；
/// 同时按质量比例修正位置以维持绳长约束。
/// 这是一个工程近似，实现上力求稳定且可调。
/// 使用方法：在场景中创建两个箱子（带 Rigidbody2D），创建两个挂点（空物体）并在 Inspector 赋值。
/// </summary>
public class PulleySimulator : MonoBehaviour
{
    [Header("References")]
    public Rigidbody2D boxA; // 左侧箱子
    public Rigidbody2D boxB; // 右侧箱子
    public Transform anchorA; // 左侧挂点（应在箱子上方）
    public Transform anchorB; // 右侧挂点

    [Header("Sensors (optional)")]
    public BoxWeightSensor sensorA;
    public BoxWeightSensor sensorB;

    [Header("Rope settings")]
    [Tooltip("如果想让脚本自动根据当前场景计算绳长，把 ropeLength 在 Inspector 设为 0（或负数）即可")]
    public float ropeLength = 0; // 兩段繩子總長

    [Header("Behavior tuning")]
    public float forceMultiplier = 1f; // 施加的力系数。控制重量差转为施加力的比例（默认 1）。遇到反应弱可放大到 2~3，抖动大则减小。
    public float correctionSpeed = 1f; // 位置修正速度系数。位置纠正速度（越大越快修正绳长，但易抖动）。
    public float maxCorrectionPerStep = 0.5f; // 单帧位置最大修正量，防止穿透。单帧最大位置修正，降低此值可以消抖（推荐 0.1~0.5）。

    [Header("Visualization")]
    [Tooltip("是否在场景中可视化绳子（使用 LineRenderer）")]
    public bool visualizeRope = true;
    [Tooltip("可视化用的 LineRenderer（可留空由脚本自动创建）")]
    public LineRenderer lineA;
    public LineRenderer lineB;
    [Tooltip("LineRenderer 线宽")]
    public float lineWidth = 0.05f;

    private GameObject createdLineA;
    private GameObject createdLineB;

    void Reset()
    {
        // 便于在 Inspector 中快速创建对象后看到警告
        if (boxA == null || boxB == null)
            Debug.LogWarning("PulleySimulator: 请在 Inspector 中设置 boxA/boxB。");
    }

    void Awake()
    {
        // 如果 ropeLength 未被设置或设置为 0，则基于当前位置初始化为两段竖直距离之和
        if (ropeLength <= 0f && anchorA != null && anchorB != null && boxA != null && boxB != null)
        {
            float lenA = anchorA.position.y - boxA.position.y;
            float lenB = anchorB.position.y - boxB.position.y;
            ropeLength = Mathf.Max(0.01f, lenA + lenB);
        }

        if (visualizeRope)
        {
            if (lineA == null) lineA = CreateLineRenderer(ref createdLineA, "RopeLineA");
            if (lineB == null) lineB = CreateLineRenderer(ref createdLineB, "RopeLineB");
        }
    }

    private LineRenderer CreateLineRenderer(ref GameObject holder, string name)
    {
        holder = new GameObject(name);
        holder.transform.parent = this.transform;
        var lr = holder.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = lr.endWidth = lineWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.numCapVertices = 2;
        lr.sortingOrder = 1000;
        return lr;
    }

    void OnDestroy()
    {
        if (createdLineA != null) Destroy(createdLineA);
        if (createdLineB != null) Destroy(createdLineB);
    }

    void FixedUpdate()
    {
        if (boxA == null || boxB == null || anchorA == null || anchorB == null) return;

        // 读取两侧的附加质量（传感器返回额外质量）
        float extraA = sensorA != null ? sensorA.GetDetectedMass() : 0f;
        float extraB = sensorB != null ? sensorB.GetDetectedMass() : 0f;

        float massA = boxA.mass + extraA;
        float massB = boxB.mass + extraB;

        // 计算净重差并施加等效力（近似处理）
        float g = Mathf.Abs(Physics2D.gravity.y);
        float net = (massA - massB);
        float force = net * g * forceMultiplier;

        if (force > 0f)
        {
            // A 较重：向下拉 A，向上拉 B
            boxA.AddForce(Vector2.down * force);
            boxB.AddForce(Vector2.up * force);
        }
        else if (force < 0f)
        {
            boxA.AddForce(Vector2.up * (-force));
            boxB.AddForce(Vector2.down * (-force));
        }

        // 维持绳长约束（两段竖直长度之和 = ropeLength）
        float lenA = anchorA.position.y - boxA.position.y;
        float lenB = anchorB.position.y - boxB.position.y;
        float currentLen = lenA + lenB;
        float err = currentLen - ropeLength; // 正表示当前绳子过长（箱子过低）

        if (Mathf.Abs(err) > 0.001f)
        {
            float totalMass = Mathf.Max(0.0001f, massA + massB);
            // 质量越大，位置修正时越不动（由对方承担更多位移）；按逆比例分配修正量
            float moveA = err * (massB / totalMass);
            float moveB = err * (massA / totalMass);

            // 限制单步修正量
            moveA = Mathf.Clamp(moveA * correctionSpeed, -maxCorrectionPerStep, maxCorrectionPerStep);
            moveB = Mathf.Clamp(moveB * correctionSpeed, -maxCorrectionPerStep, maxCorrectionPerStep);

            Vector2 targetA = boxA.position + Vector2.up * moveA; // err positive -> move up (reduce len)
            Vector2 targetB = boxB.position + Vector2.up * moveB;

            // 使用 MovePosition 保持与物理系统一致
            boxA.MovePosition(targetA);
            boxB.MovePosition(targetB);
        }

        // 更新绳索可视化
        if (visualizeRope && lineA != null && lineB != null && anchorA != null && anchorB != null && boxA != null && boxB != null)
        {
            // 绘制从 anchor 到箱子顶部（箱子 collider 顶部近似）
            Vector3 topA = boxA.transform.position + Vector3.up * (boxA.GetComponent<Collider2D>()?.bounds.extents.y ?? 0.5f);
            Vector3 topB = boxB.transform.position + Vector3.up * (boxB.GetComponent<Collider2D>()?.bounds.extents.y ?? 0.5f);

            lineA.SetPosition(0, anchorA.position);
            lineA.SetPosition(1, topA);

            lineB.SetPosition(0, anchorB.position);
            lineB.SetPosition(1, topB);
        }
    }
}
