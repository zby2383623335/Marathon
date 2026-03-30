using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NarratorTrigger : MonoBehaviour
{
    [SerializeField]
    [Tooltip("旁白显示位置")]
    private Transform narratorPosition;

    [SerializeField]
    [Tooltip("旁白文字内容")]
    private string narratorContent = "这是旁白";

    [SerializeField]
    [Tooltip("是否只触发一次")]
    private bool onlyTriggerOnce = true;

    [SerializeField]
    [Tooltip("玩家Tag标签")]
    private string playerTag = "Player";

    [SerializeField]
    [Tooltip("找不到打字机时应用的打字机预制体")]
    private GameObject fallbackTyperPrefab;

    private bool hasTriggered = false;
    private Typer cachedTyper = null;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 检查是否是玩家
        if (!collision.CompareTag(playerTag))
        {
            return;
        }

        // 先尝试获取或创建Typer单例，确保Typer存在
        Typer typer = GetOrCreateTyper();
        if (typer == null)
        {
            Debug.LogError("无法获取或创建Typer");
            return;
        }

        // 如果设置了只触发一次，检查是否已经触发过
        if (onlyTriggerOnce && hasTriggered)
        {
            return;
        }

        // 标记为已触发
        if (onlyTriggerOnce)
        {
            hasTriggered = true;
        }

        // 显示旁白
        ShowNarrator();
    }

    /// <summary>
    /// 显示旁白
    /// </summary>
    private void ShowNarrator()
    {
        // 此时Typer已在OnTriggerEnter2D中确保存在
        Typer typer = cachedTyper;

        if (typer == null)
        {
            Debug.LogError("Typer为null，这不应该发生");
            return;
        }

        // 检查位置是否设置
        if (narratorPosition == null)
        {
            Debug.LogError("narratorPosition 未设置");
            return;
        }

        // 设置旁白位置和内容
        typer.SetNarratorPosition(narratorPosition);
        typer.SetNarratorContent(narratorContent);

        // 显示旁白
        typer.ShowNarrator();
    }

    /// <summary>
    /// 获取或创建Typer单例
    /// </summary>
    private Typer GetOrCreateTyper()
    {
        // 如果有缓存，直接返回
        if (cachedTyper != null)
        {
            return cachedTyper;
        }

        // 尝试获取已存在的单例
        Typer typer = Typer.Instance;
        if (typer != null)
        {
            cachedTyper = typer;
            return typer;
        }

        // 如果没有单例且没有预制体，返回null
        if (fallbackTyperPrefab == null)
        {
            Debug.LogError("找不到Typer单例，且fallbackTyperPrefab未设置");
            return null;
        }

        // 生成备用Typer预制体
        Debug.Log("自动生成Typer备用预制体");
        GameObject typerInstance = Instantiate(fallbackTyperPrefab);

        // 等待一帧让Awake/OnEnable执行，完成单例初始化
        // 注：Instantiate会立即调用Awake，但为了安全起见这里再获取一次
        typer = Typer.Instance;

        if (typer != null)
        {
            cachedTyper = typer;
            Debug.Log("Typer备用预制体生成成功，已注册为全局单例");
        }
        else
        {
            Debug.LogError("生成Typer预制体后仍无法获取单例，请检查预制体中的Globalizer初始化");
        }

        return typer;
    }

    /// <summary>
    /// 重置触发状态（如果需要再次触发）
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
