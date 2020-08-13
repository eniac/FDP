using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

struct GraphAttributes{
    public GameObject lastCircleGameObject;
    public Color pointColor;
    public Color segmentColor;
    public float yMax;
    public float xMax;
    public List<GameObject> points;
    public List<GameObject> segments;
};
public class GraphControl : MonoBehaviour
{
    [SerializeField] private Sprite circleSprite = default; 
    private RectTransform graphContainer;
    private RectTransform labelTemplateX;
    private RectTransform labelTemplateY;
    private RectTransform labelAxisX;
    private RectTransform labelAxisY;
    private RectTransform legends;
    private RectTransform graphTitle;
    Dictionary<Global.GraphType, GraphAttributes> gAttr = new Dictionary<Global.GraphType, GraphAttributes>();
    RectTransform labelX, labelY, legend, title;

    float graphHeight;
    float graphWidth;
    private void Awake(){
        graphContainer = transform.Find("GraphContainer").GetComponent<RectTransform>();
        labelTemplateX = graphContainer.Find("LabelTemplateX").GetComponent<RectTransform>();
        labelTemplateY = graphContainer.Find("LabelTemplateY").GetComponent<RectTransform>();
        labelAxisX = graphContainer.Find("LabelAxisX").GetComponent<RectTransform>();
        labelAxisY = graphContainer.Find("LabelAxisY").GetComponent<RectTransform>();
        legends = graphContainer.Find("Legends").GetComponent<RectTransform>();
        graphTitle = graphContainer.Find("Title").GetComponent<RectTransform>();

        labelX = Instantiate(labelAxisX);
        labelX.SetParent(graphContainer);
        labelX.gameObject.SetActive(true);
        labelX.anchoredPosition = new Vector2(0, -15f);
        
        labelY = Instantiate(labelAxisY);
        labelY.SetParent(graphContainer);
        labelY.gameObject.SetActive(true);
        labelY.anchoredPosition = new Vector2(-25f, 0);
        
        legend = Instantiate(legends);
        legend.SetParent(graphContainer);
        legend.gameObject.SetActive(true);
        legend.anchoredPosition = new Vector2(60, -30f);

        title = Instantiate(graphTitle);
        title.SetParent(graphContainer);
        title.gameObject.SetActive(true);
        title.anchoredPosition = new Vector2(0, 15f);
        
        graphHeight = graphContainer.sizeDelta.y;
        graphWidth = graphContainer.sizeDelta.x;
    }

    public void GraphParamInit(string x, string y, string l, string t){
        labelX.GetComponent<Text>().text = x;
        labelY.GetComponent<Text>().text = y;
        legend.GetComponent<Text>().text = l;
        title.GetComponent<Text>().text = t;
    }

    public void GraphInit(Global.GraphType gType, Color pointColor, Color segmentColor, float xMax, float yMax){
        GraphAttributes gt = new GraphAttributes();
        gt.lastCircleGameObject = null;
        gt.pointColor = pointColor;
        gt.segmentColor = segmentColor;
        gt.xMax = xMax;
        gt.yMax = yMax;
        gt.points = new List<GameObject>();
        gt.segments = new List<GameObject>();
        if(gAttr.ContainsKey(gType)==true){
            gAttr[gType] = gt;
        }
        else{
            gAttr.Add(gType, gt);
        }

        // Labeling X axis
        float separatorCount = 3f;
        for(int i=0; i<=separatorCount; i++){
            RectTransform labelX = Instantiate(labelTemplateX);
            labelX.SetParent(graphContainer);
            labelX.gameObject.SetActive(true);
            float normalizedValue = i * 1f / separatorCount;
            labelX.anchoredPosition = new Vector2(normalizedValue*graphWidth, -5f);
            // labelX.GetComponent<Text>().text = Mathf.RoundToInt(normalizedValue * xMax).ToString();
            labelX.GetComponent<Text>().text = (normalizedValue * xMax).ToString();
        }

        // Labeling Y axis
        separatorCount = 5f;
        for(int i=0; i<=separatorCount; i++){
            RectTransform labelY = Instantiate(labelTemplateY);
            labelY.SetParent(graphContainer);
            labelY.gameObject.SetActive(true);
            float normalizedValue = i * 1f / separatorCount;
            labelY.anchoredPosition = new Vector2(-3f, normalizedValue*graphHeight);
            labelY.GetComponent<Text>().text = Mathf.RoundToInt(normalizedValue * yMax).ToString();
        }
    }
    public void ShowPlot(Global.GraphType gType, float x, float y){
        GraphAttributes gt = gAttr[gType];
        float xPos = (x/gt.xMax) * graphWidth;     // Normalized x position
        float yPos = (y/gt.yMax) * graphHeight;   // Normalized y position
        GameObject goCircle = CreateCircle(new Vector2(xPos, yPos), gt.pointColor);
        gt.points.Add(goCircle);
        if(gt.lastCircleGameObject != null){
            GameObject goConn = CreateDotConnection(gt.lastCircleGameObject.GetComponent<RectTransform>().anchoredPosition, goCircle.GetComponent<RectTransform>().anchoredPosition, gt.segmentColor);
            gt.segments.Add(goConn);
        }
        gt.lastCircleGameObject = goCircle;
        gAttr[gType] = gt;
    }

    public void ClearPlot(Global.GraphType gType){
        if(gAttr.ContainsKey(gType)==false){
            return;
        }
        GraphAttributes gt = gAttr[gType];
        foreach(var go in gt.points){
            Destroy(go);
        }
        foreach(var go in gt.segments){
            Destroy(go);
        }
    }

    private GameObject CreateCircle(Vector2 anchoredPosition, Color color){
        // create a circle to show the values
        GameObject go = new GameObject("circle", typeof(Image));
        // Set the parent of circles to graphContainer
        go.transform.SetParent(graphContainer, false);
        go.GetComponent<Image>().sprite = circleSprite;
        go.GetComponent<Image>().color = color;
        go.transform.localScale = new Vector3(0,0,0);
        // Change the position and size of circle
        RectTransform rectTransform = go.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(2,2);
        rectTransform.anchorMin = new Vector2(0,0);
        rectTransform.anchorMax = new Vector2(0,0);
        return go;
    }

    private GameObject CreateDotConnection(Vector2 posA, Vector2 posB, Color color){
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

        return go;
    }
}
