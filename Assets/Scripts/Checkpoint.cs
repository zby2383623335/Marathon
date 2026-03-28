using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("是否在玩家经过时自动设置为重生点（勾选则会在触发时调用 SetCheckpoint）")]
    public bool autoActivate = true;

    private void Reset()
    {
        // 确保 collider 为 trigger
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!autoActivate) return;
        var p = other.GetComponentInParent<player>();
        if (p != null)
        {
            // 设为当前重生点
            Vector3 respPos = transform.position;
            RespawnManager.Instance?.SetCheckpoint(respPos, p.GetWoodCount());
            Debug.Log($"Checkpoint set at {respPos} with wood {p.GetWoodCount()}");
        }
    }

    // 可被其它脚本调用以手动激活
    public void ActivateCheckpoint()
    {
        var p = FindObjectOfType<player>();
        if (p != null)
        {
            Vector3 respPos = transform.position;
            RespawnManager.Instance?.SetCheckpoint(respPos, p.GetWoodCount());
        }
    }
}
