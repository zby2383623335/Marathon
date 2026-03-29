using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WoodUIControler : Globalizer<WoodUIControler>
{
    [Header("5种UI的贴图")]
    [SerializeField]
    [Tooltip("0个木头时的贴图")]
    private Sprite woodSprite0 = null;
    [SerializeField]
    [Tooltip("1个木头时的贴图")]
    private Sprite woodSprite1 = null;
    [SerializeField]
    [Tooltip("2个木头时的贴图")]
    private Sprite woodSprite2 = null;
    [SerializeField]
    [Tooltip("3个木头时的贴图")]
    private Sprite woodSprite3 = null;
    [SerializeField]
    [Tooltip("4个木头时的贴图")]
    private Sprite woodSprite4 = null;
    [SerializeField]
    [Tooltip("木头UI的Image组件")]
    private UnityEngine.UI.Image woodImage = null;

    public void UpdateWoodUI(int woodCount)
    {
        switch (woodCount)
        {
            case 0:
                woodImage.sprite = woodSprite0;
                break;
            case 1:
                woodImage.sprite = woodSprite1;
                break;
            case 2:
                woodImage.sprite = woodSprite2;
                break;
            case 3:
                woodImage.sprite = woodSprite3;
                break;
            case 4:
                woodImage.sprite = woodSprite4;
                break;
            default:
                Debug.LogError("木头数量超出范围！");
                break;
        }
    }
}
