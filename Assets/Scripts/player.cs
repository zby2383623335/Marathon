using UnityEngine;

public class player : MonoBehaviour
{
    public int DebugGrongundCheck = 0;


    /// <summary>
    /// 2D刚体组件，用于处理物理运动
    /// </summary>
    private Rigidbody2D rb;

    [Header("Normal")]
    [SerializeField]
    [Tooltip("移动速度")]
    private float moveSpeed;
    [SerializeField]
    [Tooltip("跳跃速度")]
    private float jumpV_instant;
    /// <summary>
    /// 面向方向（1为右，-1为左）
    /// </summary>
    private int facingDir = 1;
    /// <summary>
    /// 是否面向右边
    /// </summary>
    private bool facingRight = true;

    [Header("Collision info")]
    [SerializeField]
    [Tooltip("控制台显示检测是否有地面")]
    private LayerMask groundLayer;
    [SerializeField]
    [Tooltip("地面检测距离")]
    private float groundCheckDistance;
    [SerializeField]
    [Tooltip("地面检测区域宽度")]
    private float groundCheckWidth = 0.8f;
    /// <summary>
    /// 是否在地面上
    /// </summary>
    private bool isGrounded;

    [Header("Backpack")]
    [SerializeField]
    [Tooltip("背包容量")]
    private int backpackCapacity = 4;
    [SerializeField]
    [Tooltip("背包中的木材数量")]
    private int woodCount = 4;
    [SerializeField]
    [Tooltip("背包满时的横向移动速度倍率")]
    private float moveSpeedRateWhenFull = 0.2f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        MoveUnderControl(); 
        GroundCheck();
        CheckInputToJump();

        FlipController();
    }
    /// <summary>
    /// 在Scene视图中绘制地面检测射线
    /// </summary>
    private void OnDrawGizmos()
    {
        // 在Scene视图中绘制地面检测区域
        Vector3 checkCenter = transform.position + Vector3.down * (groundCheckDistance / 2f);
        Vector3 checkSize = new Vector3(groundCheckWidth, groundCheckDistance, 0f);

        // 绘制矩形框表示检测区域
        Gizmos.color = Color.green;
        DrawWireBox(checkCenter, checkSize);
    }

    /// <summary>
    /// 绘制线框矩形（用于Gizmo显示）
    /// </summary>
    private void DrawWireBox(Vector3 center, Vector3 size)
    {
        Vector3 halfSize = size / 2f;

        // 8个顶点
        Vector3[] corners = new Vector3[8]
        {
            center + new Vector3(-halfSize.x, -halfSize.y, 0),
            center + new Vector3(halfSize.x, -halfSize.y, 0),
            center + new Vector3(halfSize.x, halfSize.y, 0),
            center + new Vector3(-halfSize.x, halfSize.y, 0),
            center + new Vector3(-halfSize.x, -halfSize.y, 0),
            center + new Vector3(halfSize.x, -halfSize.y, 0),
            center + new Vector3(halfSize.x, halfSize.y, 0),
            center + new Vector3(-halfSize.x, halfSize.y, 0)
        };

        // 绘制底面
        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[1], corners[5]);

        // 绘制顶面
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.DrawLine(corners[3], corners[7]);

        // 绘制竖线
        Gizmos.DrawLine(corners[0], corners[3]);
        Gizmos.DrawLine(corners[1], corners[2]);
    }

    /// <summary>
    /// 根据玩家输入控制移动，背包越满移动速度越慢
    /// </summary>
    private void MoveUnderControl()
    {
        // 计算速度倍率：背包满时速度为moveSpeedRateWhenFull倍，背包空时速度为1倍
        var speedMultiplier = 1 - (woodCount / (float)backpackCapacity) * (1 - moveSpeedRateWhenFull);
        // 获取水平输入，乘以移动速度和倍率，保持垂直速度不变
        rb.velocity = new Vector2(Input.GetAxis("Horizontal") * moveSpeed * speedMultiplier, rb.velocity.y);
    }

    /// <summary>
    /// 检测角色是否与地面接触
    /// </summary>
    private void GroundCheck()
    {
        // 计算检测区域的中心（玩家下方）
        Vector3 checkCenter = transform.position + Vector3.down * (groundCheckDistance / 2f);
        Vector3 checkSize = new Vector3(groundCheckWidth, groundCheckDistance, 0f);

        // 使用OverlapBox在下方区域检测是否与groundLayer相碰撞
        Collider2D[] colliders = Physics2D.OverlapBoxAll(checkCenter, checkSize, 0f, groundLayer);

        //忽略玩家自身碰撞箱
        colliders = System.Array.FindAll(colliders, col => col.gameObject != gameObject);

        // 如果检测到任何collider，说明接触到地面
        isGrounded = colliders.Length > 0;

        //地板检测测试
        if(DebugGrongundCheck > 0)
        {
            isGrounded = colliders.Length > 0;
            Debug.Log($"地面检测到的碰撞体数量：{colliders.Length}，是否在地面：{isGrounded}");
        }
            
        
        

    }

    /// <summary>
    /// 检查是否按下跳跃键，背包越满跳跃高度越低
    /// </summary>
    private void CheckInputToJump()
    {
        // 检测空格键是否被按下
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 只有在地面上时才能跳跃
            if (isGrounded)
            {
                // 计算跳跃速度：背包满时跳跃速度降低，背包空时为基础跳跃速度
                rb.velocity = new Vector2(rb.velocity.x, jumpV_instant * Mathf.Sqrt(1 - (woodCount / (float)backpackCapacity)));
            }
        }
    }
    /// <summary>
    /// 根据速度方向控制角色翻转
    /// </summary>
    private void FlipController()
    {
        // 向右移动但角色面向左时，翻转角色
        if (rb.velocity.x > 0 && !facingRight)
        {
            Flip();
        }
        // 向左移动但角色面向右时，翻转角色
        else if (rb.velocity.x < 0 && facingRight)
        {
            Flip();
        }
    }

    /// <summary>
    /// 翻转角色（改变面向方向）
    /// </summary>
    private void Flip()
    {
        // 改变面向方向的标记
        facingDir = -facingDir;
        facingRight = !facingRight;
        // 绕Y轴旋转180度
        transform.Rotate(0, 180, 0);
    }

    /// <summary>
    /// 向背包中添加木材
    /// </summary>
    /// <returns>添加成功返回true，背包满时返回false</returns>
    public bool AddWood()
    {
        // 如果背包已满，返回false
        if (woodCount >= backpackCapacity)
            return false;

        // 增加木材计数
        woodCount++;
        return true;
    }

    /// <summary>
    /// 从背包中取出一个木材
    /// </summary>
    /// <returns>取出成功返回true，背包为空时返回false</returns>
    public bool PickWood()
    {
        // 如果背包为空，返回false
        if (woodCount <= 0)
            return false;

        // 减少木材计数
        woodCount--;
        return true;
    }

    /// <summary>
    /// 获取背包中的木材数量
    /// </summary>
    /// <returns>返回背包中的木材数量</returns>
    public int GetWoodCount()
    {
        return woodCount;
    }

    /// <summary>
    /// 检查背包是否已满
    /// </summary>
    /// <returns>背包满时返回true，否则返回false</returns>
    public bool IsBackpackFull()
    {
        return woodCount >= backpackCapacity;
    }


}
