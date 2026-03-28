using UnityEngine;
using UnityEngine.Events;

public class ButtonTrigger : MonoBehaviour
{
    public UnityEvent onPressed;
    public UnityEvent onReleased;

    /// <summary>
    /// 记录按压在按钮上的木头数量，用于支持多个木头同时压在按钮上
    /// </summary>
    private int woodCount = 0;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Wood"))
        {
            woodCount++;
            if (woodCount == 1) onPressed?.Invoke(); //只有当 woodCount 从 0 变为 1 时才调用 onPressed（即“第一次被压下”）
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Wood"))
        {
            woodCount = Mathf.Max(0, woodCount - 1);
            if (woodCount == 0) onReleased?.Invoke(); //只有当 woodCount 从 1 变为 0 时才调用 onReleased（即“最后一根离开”）
        }
    }
}