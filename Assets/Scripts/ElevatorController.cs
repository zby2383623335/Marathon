using UnityEngine;
using System.Collections;

public class ElevatorController : MonoBehaviour
{
    public Vector3 downPosition;
    public Vector3 upPosition;
    public float moveTime = 1f;
    private Coroutine moving;

    public void MoveToUp() { StartMove(upPosition); }
    public void MoveToDown() { StartMove(downPosition); }
    public void Toggle()
    {
        Vector3 target = (Vector3.Distance(transform.position, upPosition) < 0.01f) ? downPosition : upPosition;
        StartMove(target);
    }

    private void StartMove(Vector3 target)
    {
        if (moving != null) StopCoroutine(moving);
        moving = StartCoroutine(MoveRoutine(target));
    }

    private IEnumerator MoveRoutine(Vector3 target)
    {
        Vector3 start = transform.position;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, moveTime);
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }
        transform.position = target;
        moving = null;
    }
}