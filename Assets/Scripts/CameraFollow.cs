using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 摄像头移动限制区域
/// </summary>
[System.Serializable]
public class CameraBoundArea
{
    [Tooltip("区域名称")]
    public string areaName = "Area";
    [Tooltip("区域最小X坐标")]
    public float minX = -5f;
    [Tooltip("区域最小Y坐标")]
    public float minY = -5f;
    [Tooltip("区域最大X坐标")]
    public float maxX = 5f;
    [Tooltip("区域最大Y坐标")]
    public float maxY = 5f;
    [Tooltip("区域颜色（用于Gizmo显示）")]
    public Color gizmoColor = new Color(0, 1, 0, 0.3f);

    /// <summary>
    /// 获取点在区域内的最近位置
    /// </summary>
    public Vector3 GetClosestPointInArea(Vector3 point)
    {
        return new Vector3(
            Mathf.Clamp(point.x, minX, maxX),
            Mathf.Clamp(point.y, minY, maxY),
            point.z
        );
    }

    /// <summary>
    /// 获取点到区域的最近距离
    /// </summary>
    public float GetDistanceToPoint(Vector3 point)
    {
        Vector3 closestPoint = GetClosestPointInArea(point);
        return Vector3.Distance(closestPoint, point);
    }

    /// <summary>
    /// 点是否在区域内
    /// </summary>
    public bool ContainsPoint(Vector3 point)
    {
        return point.x >= minX && point.x <= maxX &&
               point.y >= minY && point.y <= maxY;
    }

    /// <summary>
    /// 获取区域中心点
    /// </summary>
    public Vector3 GetCenter()
    {
        return new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0);
    }

    /// <summary>
    /// 获取区域大小
    /// </summary>
    public Vector3 GetSize()
    {
        return new Vector3(maxX - minX, maxY - minY, 0);
    }
}

public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    [Tooltip("玩家对象")]
    private GameObject player;

    [Header("Settings")]
    [SerializeField]
    [Tooltip("摄像头平滑跟随的时间（秒）")]
    private float smoothTime = 0.3f;
    [SerializeField]
    [Tooltip("摄像头追踪的最大速度")]
    private float maxSpeed = 15f;
    [SerializeField]
    [Tooltip("摄像头Z轴偏移")]
    private float zOffset = -10f;

    [Header("Camera Bounds")]
    [SerializeField]
    [Tooltip("是否启用区域限制")]
    private bool enableBounds = false;
    [SerializeField]
    [Tooltip("摄像头移动限制区域列表")]
    private CameraBoundArea[] boundAreas = new CameraBoundArea[0];

    /// <summary>
    /// 用于SmoothDamp的速度参考
    /// </summary>
    private Vector3 smoothDampVelocity = Vector3.zero;
    /// <summary>
    /// 当前所在的区域索引
    /// </summary>
    private int currentBoundAreaIndex = -1;

    void Start()
    {
        // 通过Tag获取玩家
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj;
            }
            else
            {
                Debug.LogWarning("找不到标签为'Player'的游戏对象！");
            }
        }

        // 初始化区域
        if (enableBounds && boundAreas.Length > 0)
        {
            currentBoundAreaIndex = 0;
        }
    }

    void Update()
    {
        if (player == null)
            return;

        // 获取玩家位置
        Vector3 playerPos = player.transform.position;
        // 设置摄像头的目标位置（添加Z轴偏移）
        Vector3 targetPos = new Vector3(playerPos.x, playerPos.y, zOffset);

        // 如果启用了区域限制，应用限制逻辑
        if (enableBounds && boundAreas.Length > 0)
        {
            targetPos = ApplyBoundsConstraint(targetPos, playerPos);
        }

        // 使用SmoothDamp实现平滑跟随
        // 允许速度突变，但位置变化平滑
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref smoothDampVelocity,
            smoothTime,
            maxSpeed,
            Time.deltaTime
        );
    }

    /// <summary>
    /// 应用区域约束逻辑
    /// </summary>
    private Vector3 ApplyBoundsConstraint(Vector3 targetPos, Vector3 playerPos)
    {
        // 找到距离玩家最近的区域
        int closestAreaIndex = FindClosestAreaToPlayer(playerPos);

        // 如果找到了更近的区域，转移到该区域
        if (closestAreaIndex != currentBoundAreaIndex && closestAreaIndex >= 0)
        {
            currentBoundAreaIndex = closestAreaIndex;
            // 重置速度以实现平滑过渡
            smoothDampVelocity = Vector3.zero;
        }

        // 在当前区域内限制目标位置
        if (currentBoundAreaIndex >= 0 && currentBoundAreaIndex < boundAreas.Length)
        {
            CameraBoundArea currentArea = boundAreas[currentBoundAreaIndex];
            targetPos = currentArea.GetClosestPointInArea(targetPos);
        }

        return targetPos;
    }

    /// <summary>
    /// 找到距离玩家最近的区域索引
    /// </summary>
    private int FindClosestAreaToPlayer(Vector3 playerPos)
    {
        float closestDistance = float.MaxValue;
        int closestAreaIndex = currentBoundAreaIndex >= 0 ? currentBoundAreaIndex : 0;

        for (int i = 0; i < boundAreas.Length; i++)
        {
            float distance = boundAreas[i].GetDistanceToPoint(playerPos);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestAreaIndex = i;
            }
        }

        return closestAreaIndex;
    }

    /// <summary>
    /// 在Scene视图中绘制所有限制区域
    /// </summary>
    void OnDrawGizmos()
    {
        if (!enableBounds || boundAreas == null || boundAreas.Length == 0)
            return;

        for (int i = 0; i < boundAreas.Length; i++)
        {
            DrawBoundAreaGizmo(boundAreas[i], i == currentBoundAreaIndex);
        }
    }

    /// <summary>
    /// 绘制单个区域的Gizmo
    /// </summary>
    private void DrawBoundAreaGizmo(CameraBoundArea area, bool isCurrentArea)
    {
        // 设置颜色（当前区域显示不同颜色）
        if (isCurrentArea)
        {
            Gizmos.color = new Color(area.gizmoColor.r, area.gizmoColor.g, area.gizmoColor.b, 0.6f);
        }
        else
        {
            Gizmos.color = area.gizmoColor;
        }

        // 计算区域的四个角
        Vector3 min = new Vector3(area.minX, area.minY, 0);
        Vector3 max = new Vector3(area.maxX, area.maxY, 0);
        Vector3 topLeft = new Vector3(area.minX, area.maxY, 0);
        Vector3 topRight = new Vector3(area.maxX, area.maxY, 0);
        Vector3 bottomLeft = new Vector3(area.minX, area.minY, 0);
        Vector3 bottomRight = new Vector3(area.maxX, area.minY, 0);

        // 绘制矩形边框
        Gizmos.DrawLine(bottomLeft, bottomRight);  // 下边
        Gizmos.DrawLine(bottomRight, topRight);    // 右边
        Gizmos.DrawLine(topRight, topLeft);        // 上边
        Gizmos.DrawLine(topLeft, bottomLeft);      // 左边

        // 绘制区域中心点
        Vector3 center = area.GetCenter();
        Gizmos.color = new Color(1, 0.5f, 0, 1);
        Gizmos.DrawSphere(center, 0.2f);

        // 绘制填充矩形（用于3D视图中的可视化）
        DrawFilledRect(bottomLeft, bottomRight, topRight, topLeft, area.gizmoColor);
    }

    /// <summary>
    /// 绘制填充矩形
    /// </summary>
    private void DrawFilledRect(Vector3 bottomLeft, Vector3 bottomRight, Vector3 topRight, Vector3 topLeft, Color color)
    {
        // 绘制两个三角形以形成填充矩形
        Vector3[] vertices = new Vector3[] { bottomLeft, bottomRight, topRight, topLeft };

        // 调整Z轴位置，使其在摄像头前面显示
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].z = -1;
        }

        Gizmos.color = color;
        // 绘制两个三角形
        Gizmos.DrawLine(vertices[0], vertices[1]);
        Gizmos.DrawLine(vertices[1], vertices[2]);
        Gizmos.DrawLine(vertices[2], vertices[3]);
        Gizmos.DrawLine(vertices[3], vertices[0]);
    }
}
