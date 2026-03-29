using UnityEngine;
using UnityEngine.Events;

public class ButtonTrigger : MonoBehaviour
{
    public UnityEvent onPressed;
    public UnityEvent onReleased;

    // 标记按钮是否已经被触发过一次（按下一次后保持触发状态）
    private bool triggered = false;

    // 使用普通碰撞检测（非 Trigger）来检测木头压下按钮
    private void OnCollisionEnter2D(Collision2D collision)
    {
        var other = collision.collider;
        if (other != null && other.CompareTag("Wood") && !triggered)
        {
            triggered = true;
            onPressed?.Invoke(); // 第一次被按下时触发一次，并保持状态
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // 不再在离开时触发恢复，按钮按下后保持触发状态
        // 保留该方法以便未来扩展
    }
}