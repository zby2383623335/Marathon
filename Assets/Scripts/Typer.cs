using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Typer : Globalizer<Typer>
{
    [SerializeField]
    [Tooltip("旁白位置控制物体")]
    private Transform narratorPosition;
    [SerializeField]
    [Tooltip("旁白位置偏移（相对于控制物体的偏移）")]
    private Vector2 narratorPositionOffset;
    [SerializeField]
    [Tooltip("旁白文字物体")]
    private GameObject narratorText;
    [SerializeField]
    [Tooltip("旁白底图物体列表")]
    private List<GameObject> narratorBackgrounds;
    [SerializeField]
    [Tooltip("旁白内容")]
    private string narratorContent;
    [SerializeField]
    [Tooltip("打字速度（字符/秒）")]
    private float typingSpeed = 50f;

    private TextMeshProUGUI textComponent;
    private RectTransform narratorRectTransform;
    private List<RectTransform> backgroundRectTransforms;
    private Canvas canvas;
    private Coroutine typingCoroutine;

    private void OnEnable()
    {
        // 获取TextMeshProUGUI组件和RectTransform
        if (narratorText != null)
        {
            if (textComponent == null)
            {
                textComponent = narratorText.GetComponent<TextMeshProUGUI>();
            }
            if (narratorRectTransform == null)
            {
                narratorRectTransform = narratorText.GetComponent<RectTransform>();
            }
        }

        // 获取背景的RectTransform列表
        if (narratorBackgrounds != null && narratorBackgrounds.Count > 0)
        {
            backgroundRectTransforms = new List<RectTransform>();
            foreach (GameObject background in narratorBackgrounds)
            {
                if (background != null)
                {
                    RectTransform rectTransform = background.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        backgroundRectTransforms.Add(rectTransform);
                    }
                }
            }
        }
    }

    public void Start()
    {
        // 初始状态隐藏旁白
        HideNarrator();
    }

    //public void Update()
    //{
    //    // 调试功能：按下空格键显示旁白
    //    if(Input.GetKeyDown(KeyCode.Space))
    //    {
    //        ShowNarrator();
    //    }
    //}

    /// <summary>
    /// 显示旁白文本
    /// </summary>
    public void ShowNarrator()
    {
        ShowNarrator(narratorContent);
    }

    /// <summary>
    /// 显示旁白文本（可指定内容）
    /// </summary>
    public void ShowNarrator(string content)
    {
        if (narratorText == null)
        {
            Debug.LogError("narratorText 未设置");
            return;
        }

        if (textComponent == null)
        {
            textComponent = narratorText.GetComponent<TextMeshProUGUI>();
        }

        if (textComponent == null)
        {
            Debug.LogError("narratorText 中没有 TextMeshProUGUI 组件");
            return;
        }

        if (narratorRectTransform == null)
        {
            narratorRectTransform = narratorText.GetComponent<RectTransform>();
        }

        // 停止之前的打字协程
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // 同步位置：将世界坐标转换为UI坐标
        if (narratorPosition != null)
        {
            SyncWorldPositionToUI();
        }

        // 激活文本物体
        narratorText.SetActive(true);

        // 同时激活所有背景物体
        if (narratorBackgrounds != null)
        {
            foreach (GameObject background in narratorBackgrounds)
            {
                if (background != null)
                {
                    background.SetActive(true);
                }
            }
        }

        // 开始打字效果
        typingCoroutine = StartCoroutine(TypeText(content));
    }

    /// <summary>
    /// 立即显示完整文本（不带打字效果）
    /// </summary>
    public void ShowNarratorImmediate(string content = null)
    {
        if (narratorText == null)
        {
            Debug.LogError("narratorText 未设置");
            return;
        }

        if (textComponent == null)
        {
            textComponent = narratorText.GetComponent<TextMeshProUGUI>();
        }

        if (textComponent == null)
        {
            Debug.LogError("narratorText 中没有 TextMeshProUGUI 组件");
            return;
        }

        if (narratorRectTransform == null)
        {
            narratorRectTransform = narratorText.GetComponent<RectTransform>();
        }

        // 停止打字协程
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // 同步位置：将世界坐标转换为UI坐标
        if (narratorPosition != null)
        {
            SyncWorldPositionToUI();
        }

        // 激活文本物体并显示完整文本
        narratorText.SetActive(true);
        textComponent.text = content ?? narratorContent;

        // 同时激活所有背景物体
        if (narratorBackgrounds != null)
        {
            foreach (GameObject background in narratorBackgrounds)
            {
                if (background != null)
                {
                    background.SetActive(true);
                }
            }
        }
    }

    /// <summary>
    /// 隐藏旁白
    /// </summary>
    public void HideNarrator()
    {
        if (narratorText != null)
        {
            // 停止打字协程
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            narratorText.SetActive(false);
        }

        // 同时隐藏所有背景
        if (narratorBackgrounds != null)
        {
            foreach (GameObject background in narratorBackgrounds)
            {
                if (background != null)
                {
                    background.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// 设置旁白位置控制物体
    /// </summary>
    public void SetNarratorPosition(Transform position)
    {
        narratorPosition = position;
    }

    /// <summary>
    /// 设置旁白位置偏移
    /// </summary>
    public void SetNarratorPositionOffset(Vector2 offset)
    {
        narratorPositionOffset = offset;
    }

    /// <summary>
    /// 设置旁白文本内容
    /// </summary>
    public void SetNarratorContent(string content)
    {
        narratorContent = content;
    }

    /// <summary>
    /// 立即重新显示当前旁白文本（带打字效果）
    /// </summary>
    public void RefreshNarratorDisplay()
    {
        ShowNarrator(narratorContent);
    }

    /// <summary>
    /// 将世界坐标同步到UI坐标
    /// </summary>
    private void SyncWorldPositionToUI()
    {
        if (narratorRectTransform == null)
        {
            narratorRectTransform = narratorText.GetComponent<RectTransform>();
        }

        // 获取Canvas
        if (canvas == null)
        {
            canvas = narratorRectTransform.GetComponentInParent<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogError("找不到Canvas，旁白文本物体必须在Canvas的子物体中");
            return;
        }

        // 将世界坐标转换为屏幕坐标
        Vector3 screenPos = Camera.main.WorldToScreenPoint(narratorPosition.position);

        // 将屏幕坐标转换为UI本地坐标
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            screenPos,
            canvas.worldCamera,
            out Vector2 localPoint
        );

        // 应用位置偏移
        localPoint += narratorPositionOffset;

        // 设置文字位置
        narratorRectTransform.anchoredPosition = localPoint;

        // 同时设置所有背景位置
        if (narratorBackgrounds != null && narratorBackgrounds.Count > 0)
        {
            if (backgroundRectTransforms == null || backgroundRectTransforms.Count == 0)
            {
                backgroundRectTransforms = new List<RectTransform>();
                foreach (GameObject background in narratorBackgrounds)
                {
                    if (background != null)
                    {
                        RectTransform rectTransform = background.GetComponent<RectTransform>();
                        if (rectTransform != null)
                        {
                            backgroundRectTransforms.Add(rectTransform);
                        }
                    }
                }
            }

            foreach (RectTransform backgroundRect in backgroundRectTransforms)
            {
                backgroundRect.anchoredPosition = localPoint;
            }
        }
    }

    /// <summary>
    /// 打字效果协程
    /// </summary>
    private IEnumerator TypeText(string content)
    {
        textComponent.text = "";
        float timeBetweenCharacters = 1f / typingSpeed;

        foreach (char character in content)
        {
            textComponent.text += character;
            yield return new WaitForSeconds(timeBetweenCharacters);
        }

        typingCoroutine = null;
    }
}
