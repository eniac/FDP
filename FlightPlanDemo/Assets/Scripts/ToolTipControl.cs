using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToolTipControl : MonoBehaviour
{
    private static ToolTipControl instance;
    [SerializeField] private Camera cam = default;
    Text toolTipText;
    RectTransform backgroundRectTransform;

    void Start(){
        instance = this;
        backgroundRectTransform = transform.Find("Background").GetComponent<RectTransform>();
        toolTipText = backgroundRectTransform.Find("Text").GetComponent<Text>();
        HideToolTip();
        // ShowToolTip("Random Text on");
    }
    void ShowToolTip(string toolTipString){
        gameObject.SetActive(true);
        toolTipText.text = toolTipString;   
        float textPaddingSize = 4f;     
        Vector2 backgroundSize = new Vector2(toolTipText.preferredWidth + textPaddingSize*2f, toolTipText.preferredHeight + textPaddingSize*2f);
        backgroundRectTransform.sizeDelta = backgroundSize;
    }

    void HideToolTip(){
        gameObject.SetActive(false);
    }

    public static void ShowToolTip_Static(string toolTipString){
        instance.ShowToolTip(toolTipString);
    }

    public static void HideToolTip_Static(){
        instance.HideToolTip();
    }
    void Update(){
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(transform.parent.GetComponent<RectTransform>(), Input.mousePosition, null, out localPoint);
        transform.localPosition = localPoint;
    }
}
