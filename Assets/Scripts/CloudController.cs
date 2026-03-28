using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CloudController : MonoBehaviour
{
    [Header("位置")]
    [Tooltip("相对于 transform.position 的偏移量，表示云的初始（顶部）位置")]
    public Vector3 initialOffset = Vector3.zero;

    [Header("下落参数")]
    public float minDrop = 0.2f;
    public float maxDrop = 3f;
    [Tooltip("冲击速度 * 系数 => 下坠距离")]
    public float impactToDropFactor = 0.2f;
    public float dropSpeed = 2f;
    public float riseSpeed = 1.5f;

    [Header("计时")]
    [Tooltip("到达最低点后等待多长时间再检查云上是否有木头（秒）")]
    public float bottomHoldTime = 0.5f;
    [Tooltip("云上升完成后冷却多长时间才允许再次响应木头（秒）")]
    public float cooldownAfterRise = 1.0f;

    private Vector3 topPosition;
    private Vector3 bottomPosition;
    private Rigidbody2D rb;

    // 跟踪当前接触云的唯一木头对象（使用根 GameObject）
    private HashSet<GameObject> woodsOnCloud = new HashSet<GameObject>();

    /// <summary>
    /// 当木头落在云上时，云会下降一定的距离，这个距离会依据木头与云碰撞时的速度而改变
    /// 在木头落在云上使云下降后，在云上升回到原来的位置前，不会再因为检测到与木头碰撞而下降第二次
    /// 在木头与云第一次碰撞后，不管木头的状态，先让云下降到最低点，在下降到最低点过一段时间后再持续检测是否有木头在云上，如果没有，则直接上升到最高点，不管木头的状态，过一段时间后再检测木头的状态来决定是否再次下降
    /// </summary>

    private enum State { Idle, Descending, AtBottom, Checking, Rising, Cooldown }
    private State state = State.Idle;

    private Coroutine moveRoutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        topPosition = transform.position + initialOffset;
        bottomPosition = topPosition; // initialize same, will be set when drop occurs
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Wood")) return;

        // 添加木头的根对象（去重）
        var root = collision.collider.transform.root.gameObject;
        woodsOnCloud.Add(root);

        // 仅在空闲状态时触发下落
        if (state == State.Idle)
        {
            float impactSpeed = Mathf.Abs(collision.relativeVelocity.y);
            float drop = Mathf.Clamp(impactSpeed * impactToDropFactor, minDrop, maxDrop);
            bottomPosition = topPosition + Vector3.down * drop;
            StartDescend(bottomPosition);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Wood")) return;
        var root = collision.collider.transform.root.gameObject;
        woodsOnCloud.Remove(root);
        // 在这里不立即上升；是否上升的判断在到达底部并等待后进行
    }

    private void StartDescend(Vector3 target)
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveToPositionRoutine(target, dropSpeed, true));
    }

    private void StartRise()
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveToPositionRoutine(topPosition, riseSpeed, false));
    }

    private IEnumerator MoveToPositionRoutine(Vector3 target, float speed, bool isDescending)
    {
        state = isDescending ? State.Descending : State.Rising;

        Vector3 start = transform.position;
        float distance = Vector3.Distance(start, target);
        if (distance <= 0.0001f)
        {
            // already at target
            if (isDescending) state = State.AtBottom;
            else state = State.Idle;
            yield break;
        }

        float duration = Mathf.Max(0.001f, distance / speed);
        float elapsed = 0f;

        // move using physics-safe MovePosition if available
        if (rb != null)
        {
            while (elapsed < duration)
            {
                elapsed += Time.fixedDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector3 next = Vector3.Lerp(start, target, t);
                rb.MovePosition(next);
                yield return new WaitForFixedUpdate();
            }
            rb.MovePosition(target);
        }
        else
        {
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(start, target, t);
                yield return null;
            }
            transform.position = target;
        }

        if (isDescending)
        {
            state = State.AtBottom;
            // 在底部停留一段时间
            yield return new WaitForSeconds(bottomHoldTime);

            // 进入检查阶段：若云上仍有木头则继续等待并周期性检查；若无则上升
            state = State.Checking;
            while (woodsOnCloud.Count > 0)
            {
                // 若仍有木头，继续等待并再次检查
                yield return new WaitForSeconds(bottomHoldTime);
            }

            // 无木头，开始上升
            state = State.Rising;
            if (moveRoutine != null) StopCoroutine(moveRoutine);
            moveRoutine = StartCoroutine(MoveToPositionRoutine(topPosition, riseSpeed, false));
            yield break;
        }
        else
        {
            // 上升完成后进入冷却期，冷却结束才允许下一次下落
            state = State.Cooldown;
            yield return new WaitForSeconds(cooldownAfterRise);
            state = State.Idle;
            yield break;
        }
    }

    // 可选：在编辑器中可视化顶部/底部位置
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 top = transform.position + initialOffset;
        Gizmos.DrawWireSphere(top, 0.1f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(bottomPosition, 0.1f);
    }
}
