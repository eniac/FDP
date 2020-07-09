using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GraphControl : MonoBehaviour
{
    [SerializeField] private Sprite circleSprite = default; 
    private RectTransform graphContainer;
    private RectTransform labelTemplateX;
    private RectTransform labelTemplateY;

    private void Awake(){
        graphContainer = transform.Find("GraphContainer").GetComponent<RectTransform>();
        labelTemplateX = graphContainer.Find("LabelTemplateX").GetComponent<RectTransform>();
        labelTemplateY = graphContainer.Find("LabelTemplateY").GetComponent<RectTransform>();

        List<int> valueList = new List<int>(){5, 98, 56, 45, 30, 22, 17, 15, 13, 17, 25, 37, 40, 36, 33};
        ShowGraph(valueList);
    }

    private GameObject CreateCircle(Vector2 anchoredPosition){
        // create a circle to show the values
        GameObject go = new GameObject("circle", typeof(Image));
        // Set the parent of circles to graphContainer
        go.transform.SetParent(graphContainer, false);
        go.GetComponent<Image>().sprite = circleSprite;
        // Change the position and size of circle
        RectTransform rectTransform = go.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(2,2);
        rectTransform.anchorMin = new Vector2(0,0);
        rectTransform.anchorMax = new Vector2(0,0);
        return go;
    }

    private void ShowGraph(List<int> valueList){
        float graphHeight = graphContainer.sizeDelta.y;
        float yMax = 100f;      // Maximum y value
        float xSize = 7f;       // Horizontal spacing between points
        GameObject lastCircleGameObject = null;
        for(int i=0; i<valueList.Count; i++){
            float xPos = xSize + i * xSize;
            // Normalized y position
            float yPos = ((valueList[i])/yMax) * graphHeight;
            GameObject goCircle = CreateCircle(new Vector2(xPos, yPos));
            if(lastCircleGameObject != null){
                CreateDotConnection(lastCircleGameObject.GetComponent<RectTransform>().anchoredPosition, goCircle.GetComponent<RectTransform>().anchoredPosition);
            }
            lastCircleGameObject = goCircle;

            // Labeling X axis
            RectTransform labelX = Instantiate(labelTemplateX);
            labelX.SetParent(graphContainer);
            labelX.gameObject.SetActive(true);
            labelX.anchoredPosition = new Vector2(xPos, -3f);
            labelX.GetComponent<Text>().text = i.ToString();
        }

        // Labeling Y axis
        int separatorCount = 10;
        for(int i=0; i<=separatorCount; i++){
            RectTransform labelY = Instantiate(labelTemplateY);
            labelY.SetParent(graphContainer);
            labelY.gameObject.SetActive(true);
            float normalizedValue = i * 1f / separatorCount;
            labelY.anchoredPosition = new Vector2(-3f, normalizedValue*graphHeight);
            labelY.GetComponent<Text>().text = Mathf.RoundToInt(normalizedValue * yMax).ToString();
        }
    }

    private void CreateDotConnection(Vector2 posA, Vector2 posB){
        GameObject go = new GameObject("dotConnection", typeof(Image));
        go.transform.SetParent(graphContainer, false);
        go.GetComponent<Image>().color = new Color(1f,1f,1f,0.5f);

        Vector2 direction = (posB - posA).normalized;
        float distance = Vector2.Distance(posA, posB);
        float angle = (direction.y < 0 ? -Mathf.Acos(direction.x) : Mathf.Acos(direction.x)) * Mathf.Rad2Deg;

        RectTransform rectTransform = go.GetComponent<RectTransform>();

        rectTransform.anchoredPosition = posA + direction * distance * 0.5f;
        rectTransform.sizeDelta = new Vector2(distance,1f);  // Horizontal bar
        rectTransform.anchorMin = new Vector2(0,0);
        rectTransform.anchorMax = new Vector2(0,0);
        rectTransform.localEulerAngles = new Vector3(0, 0, angle);
    }
}
