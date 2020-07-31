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

        // List<int> valueList = new List<int>(){5, 98, 56, 45, 30, 22, 17, 15, 13, 17, 25, 37, 40, 36, 33};
        // ShowGraph(valueList);
    }

    public void Show(Dictionary<float, int> requestData, Dictionary<float, int> replyData){
        if(requestData.Count!=0){
            foreach(var data in requestData){
                Debug.Log(data.Key + " : " + data.Value);
            }
        }
        if(replyData.Count!=0){
            foreach(var data in replyData){
                Debug.Log(data.Key + " : " + data.Value);
            }
        }

        float graphHeight = graphContainer.sizeDelta.y;
        float graphWidth = graphContainer.sizeDelta.x;
        float yMax = 110f;               //22f;      
        float xMax = 120f;               //30f;
        Color color;
        GameObject lastCircleGameObject = null;
        foreach(var data in requestData){
            float xPos = (data.Key/xMax) * graphWidth;     // Normalized x position
            float yPos = (data.Value/yMax) * graphHeight;   // Normalized y position
            color = new Color(1f, 0f, 0f, 1f);
            GameObject goCircle = CreateCircle(new Vector2(xPos, yPos), color);
            if(lastCircleGameObject != null){
                color = new Color(1f, 0f, 0f, 0.5f);
                CreateDotConnection(lastCircleGameObject.GetComponent<RectTransform>().anchoredPosition, goCircle.GetComponent<RectTransform>().anchoredPosition, color);
            }
            lastCircleGameObject = goCircle;
        }

        lastCircleGameObject = null;
        foreach(var data in replyData){
            float xPos = (data.Key/xMax) * graphWidth;     // Normalized x position
            float yPos = (data.Value/yMax) * graphHeight;   // Normalized y position
            color = new Color(0f, 0f, 1f, 1f);
            GameObject goCircle = CreateCircle(new Vector2(xPos, yPos), color);
            if(lastCircleGameObject != null){
                color = new Color(0f, 0f, 1f, 0.5f);
                CreateDotConnection(lastCircleGameObject.GetComponent<RectTransform>().anchoredPosition, goCircle.GetComponent<RectTransform>().anchoredPosition, color);
            }
            lastCircleGameObject = goCircle;
        }

        // Labeling X axis
        int separatorCount = 10;
        for(int i=0; i<=separatorCount; i++){
            RectTransform labelX = Instantiate(labelTemplateX);
            labelX.SetParent(graphContainer);
            labelX.gameObject.SetActive(true);
            float normalizedValue = i * 1f / separatorCount;
            labelX.anchoredPosition = new Vector2(normalizedValue*graphWidth, -3f);
            labelX.GetComponent<Text>().text = Mathf.RoundToInt(normalizedValue * xMax).ToString();
        }

        // Labeling Y axis
        separatorCount = 10;
        for(int i=0; i<=separatorCount; i++){
            RectTransform labelY = Instantiate(labelTemplateY);
            labelY.SetParent(graphContainer);
            labelY.gameObject.SetActive(true);
            float normalizedValue = i * 1f / separatorCount;
            labelY.anchoredPosition = new Vector2(-3f, normalizedValue*graphHeight);
            labelY.GetComponent<Text>().text = Mathf.RoundToInt(normalizedValue * yMax).ToString();
        }
    }

    private GameObject CreateCircle(Vector2 anchoredPosition, Color color){
        // create a circle to show the values
        GameObject go = new GameObject("circle", typeof(Image));
        // Set the parent of circles to graphContainer
        go.transform.SetParent(graphContainer, false);
        go.GetComponent<Image>().sprite = circleSprite;
        go.GetComponent<Image>().color = color;
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
            GameObject goCircle = CreateCircle(new Vector2(xPos, yPos), new Color(1f, 1f, 1f, 1f));
            if(lastCircleGameObject != null){
                CreateDotConnection(lastCircleGameObject.GetComponent<RectTransform>().anchoredPosition, goCircle.GetComponent<RectTransform>().anchoredPosition, new Color(1f, 1f, 1f, 0.5f));
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

    private void CreateDotConnection(Vector2 posA, Vector2 posB, Color color){
        GameObject go = new GameObject("dotConnection", typeof(Image));
        go.transform.SetParent(graphContainer, false);
        go.GetComponent<Image>().color = color;

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
