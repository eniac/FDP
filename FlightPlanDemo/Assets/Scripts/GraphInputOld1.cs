using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEngine;
using UnityEngine.Networking;

public class GraphInputOld1 : MonoBehaviour
{
    [SerializeField] GraphControl graph = default;
    [SerializeField] SliderControl sliderControl = default;
    [SerializeField] private ColorControl colorControl = default;
    string xLabel, yLabel, legend, title;
    float xMax=0, yMax=0, xMaxToScale=0;
    float nCurveMax;
    List<string> graphLogText = new List<string>();
    List<StringReader> graphLogReader = new List<StringReader>();
    List<string> lastData = new List<string>();
    List<float> relative_scale = new List<float>();
    List<float> timeOffset = new List<float>(){0, 0};
    List<float> comparisonOffset = new List<float>(){0,0};
    float animTime=0f, scale=1, rc=0;
    string expPktTargetNode=null, targetNode=null;
    bool show=true;
    float graphStartTime=-1f;
    bool isXtime=true;
    float packetTime=0;

    public IEnumerator Start(){
        string legendText = "";
        List<Color> color = new List<Color>(){Color.black};
        UpdateDisable();
        if(Global.chosanExperimentName == "1_complete_fec_e2e"){
            show = true;
            yield return StartCoroutine(GetGraphLogText("1_complete_fec_e2e/graph_log1.txt"));
            yield return StartCoroutine(GetGraphLogText("1_complete_fec_e2e/graph_log2.txt"));
            legendText = "Parity\nTCP p0h0->p1h0\nTCP p1h0->p0h0";
            color = GetColors(new List<string>(){"#ffffff", "#0000ff", "#ffff00"});
        }
        else if(Global.chosanExperimentName == "1_complete_mcd_e2e"){
            show = true;
            yield return StartCoroutine(GetGraphLogText("1_complete_mcd_e2e/graph_log1.txt"));
            yield return StartCoroutine(GetGraphLogText("1_complete_mcd_e2e/graph_log2.txt"));
            legendText = "MCD Request\nMCD Reply\nMCD Cached\nParity\nICMP Request";
            color = GetColors(new List<string>(){"#0EF3E1", "#61D612", "#FF8A00", "#ffffff", "#0000ff"});
        }
        else if(Global.chosanExperimentName == "1_complete_hc_e2e"){
            show = true;
            yield return StartCoroutine(GetGraphLogText("1_complete_hc_e2e/graph_log1.txt"));
            yield return StartCoroutine(GetGraphLogText("1_complete_hc_e2e/graph_log2.txt"));
            legendText = "Compressed\nParity\nTCP p0h0->p1h0\nTCP p1h0->p0h0";
            color = GetColors(new List<string>(){"#ff00ff", "#ffffff", "#0000ff", "#ffff00"});
        }
        else if(Global.chosanExperimentName == "2_complete_all_e2e"){
            animTime = 4000f;
            show = false;
            legendText = "TCP p0h0->p1h0\nTCP p1h0->p0h0\nMCD Request\nMCD Reply\nMCD Cached\nCompressed\nParity\nICMP Request";
            color = GetColors(new List<string>(){"#0EF3E1", "#61D612","#FF8A00", "#ff00ff", "#ffffff", "#0000ff", "#ffff00", "#ff0000"});
        }
        else if(Global.chosanExperimentName == "3_complete_e2e_1_hl3new"){
            animTime = 3954.756f;
            show = false;
            legendText = "TCP p0h0->p1h0\nTCP p1h0->p0h0\nCompressed\nMCD Request\nMCD Reply\nMCD Cached\nParity\nICMP Request";
            color = GetColors(new List<string>(){"#0000ff", "#ffff00", "#ff00ff","#0EF3E1", "#61D612","#FF8A00", "#ffffff", "#ff0000"});
        }
        else if(Global.chosanExperimentName == "3_complete_e2e_2_hl3new"){
            animTime = 4783.464f;
            show = false;
            legendText = "MCD Request\nMCD Reply\nMCD Cached\nParity\nICMP Request";
            color = GetColors(new List<string>(){"#0EF3E1", "#61D612","#FF8A00", "#ffffff", "#0000ff"});
        }
        else if(Global.chosanExperimentName == "5_complete_2_FW"){
            show = true;
            colorControl.SetColorPattern(Global.ColorPattern.None);
            yield return StartCoroutine(GetGraphLogText("5_complete_2_FW/graph_log1.txt"));
            yield return StartCoroutine(GetGraphLogText("5_complete_2_FW/graph_log2.txt"));
            legendText = "TCP Packets";
            color = GetColors(new List<string>(){"#ffff00"});
        }
        else if(Global.chosanExperimentName == "7_split1"){
            show = true;
            yield return StartCoroutine(GetGraphLogText("7_split1/graph_log1.txt"));
            yield return StartCoroutine(GetGraphLogText("7_split1/graph_log2.txt"));
            legendText = "ICMP Request\nICMP Reply\nFeedback";
            color = GetColors(new List<string>(){"#0000ff", "#ffff00", "#EC119D"});
        }
        else if(Global.chosanExperimentName == "Introduction"){
            show = true;
            yield return StartCoroutine(GetGraphLogText("Introduction/graph_log1.txt"));
            yield return StartCoroutine(GetGraphLogText("Introduction/graph_log2.txt"));
            legendText = "ICMP Request\nICMP Reply\nFeedback";
            color = GetColors(new List<string>(){"#0000ff", "#ffff00", "#EC119D"});
        }
        else if(Global.chosanExperimentName == "6_split2_all"){
            animTime = 4783.464f;
            show = false;
            legendText = "TCP p0h0->p1h0\nTCP p1h0->p0h0\nMCD Request\nMCD Reply\nMCD Cached\nParity\nICMP Request\nFeedback";
            color = GetColors(new List<string>(){"#0000ff", "#ffff00","#0EF3E1", "#61D612","#FF8A00", "#ffffff", "#ff0000", "#EC119D"});
        }
        graph.ShowLegendColor(legendText, color);
    }

    List<Color> GetColors(List<string> hexColor){
        List<Color> color = new List<Color>();
        Color outColor;
        foreach(var c in hexColor){
            if ( ColorUtility.TryParseHtmlString(c, out outColor)){
                color.Add(outColor);
            }
            else{
                color.Add(Color.black);
            }
        }
        return color;
    }

    IEnumerator GetGraphLogText(string fileName){
        if(show ==false){
            yield break;
        }
        string graphText="";
        StringReader reader;
        var filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        Debug.Log("Graph Log File = " + filePath);
        if (filePath.Contains ("://") || filePath.Contains (":///")) {
            // Using UnityWebRequest class
            var loaded = new UnityWebRequest(filePath);
            loaded.downloadHandler = new DownloadHandlerBuffer();
            yield return loaded.SendWebRequest();
            graphText = loaded.downloadHandler.text;
        }
        else{
            graphText = File.ReadAllText(filePath);
        }
        graphLogText.Add(graphText);
    }

    public void GraphInputInit(){
        if(show == false){
            graph.HideGraph();
            sliderControl.SetSliderMaxValue(animTime);
            return;
        }
        graphLogReader.Clear();
        for(int i=0; i<graphLogText.Count; i++){
            var reader = new StringReader(graphLogText[i]);
            graphLogReader.Add(reader);
        }
        GetGraphData();
        Debug.Log(xLabel + " - " + yLabel + " - " + legend + " - " + title + " - " + xMax + " - " + yMax + " - " + scale);

        graph.GraphParamInit(xLabel, yLabel, legend, title);
        if(nCurveMax > 0){
            graph.GraphInit(Global.GraphType.Type0, new Color(1f, 1f, 0f, 1f), new Color(1f, 1f, 0f, 0.5f), xMax, yMax);
        }
        if(nCurveMax > 1){
            if(Global.chosanExperimentName == "5_complete_2_FW"){
                graph.GraphInit(Global.GraphType.Type1, new Color(0f, 1f, 0.25f, 1f), new Color(0f, 1f, 0.25f, 0.5f), xMax, yMax, 4f );
            }
            else{
                graph.GraphInit(Global.GraphType.Type1, new Color(0f, 1f, 0.25f, 1f), new Color(0f, 1f, 0.25f, 0.5f), xMax, yMax );
            }
        }
        if(nCurveMax > 2){
            graph.GraphInit(Global.GraphType.Type1, new Color(0f, 1f, 1f, 1f), new Color(0f, 1f, 1f, 0.5f), xMax, yMax );
        }
        sliderControl.SetSliderMaxValue(animTime);
        UpdateEnable();
    }

    void GetGraphData(){
        if(Global.chosanExperimentName == "1_complete_fec_e2e"){
            xLabel = "time (sec)";
            yLabel = "# packets received at receiver";
            legend = "<color=#ffff00>---- No FEC</color>\n <color=#00ff40>---- With FEC (k=5, h=1)</color>";
            title = "FEC Effectiveness";
            nCurveMax = 2;
            animTime = 515;
            targetNode = "p1h0";
            timeOffset[0] = 9515f;
            timeOffset[1] = 8618f;
            GetCoordinates();
        }
        else if(Global.chosanExperimentName == "1_complete_mcd_e2e"){
            xLabel = "time (sec)";
            yLabel = "# packets received at receiver";
            legend = "<color=#ffff00>---- No MCD</color>\n <color=#00ff40>---- With MCD</color>";
            title = "MCD Effectiveness";
            nCurveMax = 2;
            animTime = 2400f;
            targetNode = "p1h0";
            GetCoordinates();
        }
        else if(Global.chosanExperimentName == "1_complete_hc_e2e"){
            xLabel = "time (sec)";
            yLabel = "# bytes";
            legend = "<color=#ffff00>---- Before Header Compression</color>\n <color=#00ff40>---- After Header Compression</color>";
            title = "HC Effectiveness";
            nCurveMax = 2;
            animTime = 342f;
            targetNode = "p0e0";
            GetCoordinates();
        }
        else if(Global.chosanExperimentName == "5_complete_2_FW"){
            xLabel = "% test completed";
            yLabel = "% success rate";
            legend = "<color=#ffff00>---- Positive Test</color>\n <color=#00ff40>---- Negative Test</color>";
            title = "Firewall Effectiveness";
            nCurveMax = 2;
            animTime = 82.418f;
            targetNode = "p1e1";
            isXtime = false;
            GetCoordinates();
        }
        else if(Global.chosanExperimentName == "7_split1"){
            xLabel = "time (sec)";
            yLabel = "# bytes passing through the devices";
            legend = "<color=#ffff00>---- SA_1</color>\n <color=#00ff40>---- SA_2</color>";
            title = "Failover Mechanism";
            nCurveMax = 2;
            animTime = 121.012f;
            targetNode = "p0e0";
            GetCoordinates();
            relative_scale[0] = 1;
            relative_scale[1] = 1;
        }
        else if(Global.chosanExperimentName == "Introduction"){
            xLabel = "time (sec)";
            yLabel = "# bytes passing through the devices";
            legend = "<color=#ffff00>---- SA_1</color>\n <color=#00ff40>---- SA_2</color>";
            title = "Failover Mechanism";
            nCurveMax = 2;
            animTime = 121.012f;
            targetNode = "p0e0";
            GetCoordinates();
            relative_scale[0] = 1;
            relative_scale[1] = 1;
        }
    }

    void GetCoordinates(){
        float xmax=0f, ymax=0f, div=1f;
        if(isXtime){
            div = Global.U_SEC;
        }
        for(int i=0; i<graphLogText.Count; i++){
            string[] lines = graphLogText[i].Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            // Debug.Log("last Line = " + lines[lines.Length - 1].ToString() + " - " + lines.Length + " - " + lines.GetType());
            
            string[] data = lines[lines.Length - 1].Split(' ');
            relative_scale.Add(float.Parse(data[0])/div);
            if(xmax<float.Parse(data[0])){
                xmax = float.Parse(data[0]);
            }
            if(ymax<float.Parse(data[1])){
                ymax = float.Parse(data[1]);
            }
            lastData.Add(null);
        }
        xmax = xmax/div;
        
        
        for(int i=0; i<graphLogText.Count; i++){
                relative_scale[i] = xmax/relative_scale[i];
                // relative_scale[i] = relative_scale[0]/relative_scale[i];
        }

        xMax = xmax;
        xMaxToScale = xmax;
        yMax = ymax;
        // scale = animTime/xMax;
        for(int i=0; i<graphLogText.Count; i++){
            lastData[i] = graphLogReader[i].ReadLine();
        }
    }
    
    public void ClearPlot(){
        for(int i=0; i<graphLogText.Count; i++){
            graph.ClearPlot((Global.GraphType)i);
        }
        graphLogReader.Clear();
        lastData.Clear();
        relative_scale.Clear();
        expPktTargetNode = null;
        targetNode = null;
        graphStartTime = -1f;
    }

    public void ExpiredPacketTargetNode(string expPktTargetNode){
        this.expPktTargetNode = expPktTargetNode;
    }

    public void ReferenceCounterValue(float rc){
        this.rc = rc;
    }

    public void SetAnimTime(float t){
        animTime = t - 1f;
    }

    void FixedUpdate(){
        if(graphStartTime==-1f){
            // Debug.Log("GRAPH = " + graphStartTime + " : " + targetNode + " : " + expPktTargetNode);
            if(targetNode==null || expPktTargetNode==null || targetNode != expPktTargetNode || animTime == 0f){
                return;
            }
            else{
                scale = (animTime-rc)/xMaxToScale;
                graphStartTime = rc;
            }
        }   
        UpdateGraph();     
    }

    void DispatchedPacketTime(float time){
        packetTime = time;
    }

    void UpdateGraph(){
        string[] coord;
        float div=1;
        if(isXtime){
            div = Global.U_SEC;
        }

        if(Global.chosanExperimentName == "5_complete_2_FW"){
            for(int i=0; i<graphLogText.Count; i++){
                if(lastData.Count>0 && lastData[i] != null){
                    coord = lastData[i].Split(' ');
                    var xVal = (( float.Parse(coord[0]) / 2f) + 50f * i) * scale;
                    if(xVal <= rc){
                        float x = ( float.Parse(coord[0]) ) ;
                        graph.ShowPlot((Global.GraphType)i, x, float.Parse(coord[1]));
                        lastData[i] = graphLogReader[i].ReadLine();
                    }
                }
            }
        }
        else{
            for(int i=0; i<graphLogText.Count; i++){
                // Debug.Log("[" + i + "] " + lastData[i] + rc);
                if(lastData.Count>0 && lastData[i] != null){
                    coord = lastData[i].Split(' ');
                    var xVal = ( float.Parse(coord[0]) - timeOffset[i]) * scale * relative_scale[i] / div;
                    if(xVal+graphStartTime <= rc){
                        float x = ( float.Parse(coord[0]) - timeOffset[i]) / div * relative_scale[i];
                        graph.ShowPlot((Global.GraphType)i, x, float.Parse(coord[1]));
                        lastData[i] = graphLogReader[i].ReadLine();
                    }
                }
            }
        }
    }

    void UpdateEnable(){
        enabled = true;
    }

    void UpdateDisable(){
        enabled = false;
    }
}
