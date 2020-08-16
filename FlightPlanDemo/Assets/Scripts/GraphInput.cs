using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEngine;
using UnityEngine.Networking;

public class GraphInput : MonoBehaviour
{
    [SerializeField] GraphControl graph = default;
    [SerializeField] SliderControl sliderControl = default;
    string xLabel, yLabel, legend, title;
    float xMax, yMax;
    float nCurveMax;
    List<string> graphLogText = new List<string>();
    List<StringReader> graphLogReader = new List<StringReader>();
    List<string> lastData = new List<string>();
    List<float> relative_scale = new List<float>();
    float animTime=0, scale=1, rc=0;
    string expPktTargetNode=null, targetNode=null;
    bool show=true;
    float graphStartTime=-1f;

    public IEnumerator Start(){
        UpdateDisable();
        if(Global.chosanExperimentName == "complete_fec_e2e"){
            show = true;
            yield return StartCoroutine(GetGraphLogText("complete_fec_e2e/graph_log1.txt"));
            yield return StartCoroutine(GetGraphLogText("complete_fec_e2e/graph_log2.txt"));
        }
        else if(Global.chosanExperimentName == "complete_mcd_e2e"){
            show = true;
            yield return StartCoroutine(GetGraphLogText("complete_mcd_e2e/graph_log1.txt"));
            yield return StartCoroutine(GetGraphLogText("complete_mcd_e2e/graph_log2.txt"));
        }
        else if(Global.chosanExperimentName == "complete_hc_e2e"){
            show = true;
            yield return StartCoroutine(GetGraphLogText("complete_hc_e2e/graph_log1.txt"));
            yield return StartCoroutine(GetGraphLogText("complete_hc_e2e/graph_log2.txt"));
        }
        else if(Global.chosanExperimentName == "complete_all_e2e"){
            animTime = 80f;
            show = false;
        }
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
            sliderControl.SetSliderMaxValue(animTime);
            graph.HideGraph();
            return;
        }
        graphLogReader.Clear();
        for(int i=0; i<graphLogText.Count; i++){
            var reader = new StringReader(graphLogText[i]);
            graphLogReader.Add(reader);
        }
        GetGraphData();
        Debug.Log(xLabel + " - " + yLabel + " - " + legend + " - " + title + " - " + xMax + " - " + yMax + " - " + scale);

        sliderControl.SetSliderMaxValue(animTime);

        graph.GraphParamInit(xLabel, yLabel, legend, title);
        if(nCurveMax > 0){
            graph.GraphInit(Global.GraphType.Type0, new Color(1f, 1f, 0f, 1f), new Color(1f, 1f, 0f, 0.5f), xMax, yMax);
        }
        if(nCurveMax > 1){
            graph.GraphInit(Global.GraphType.Type1, new Color(0f, 1f, 0.25f, 1f), new Color(0f, 1f, 0.25f, 0.5f), xMax, yMax );
        }
        if(nCurveMax > 2){
            graph.GraphInit(Global.GraphType.Type1, new Color(0f, 1f, 1f, 1f), new Color(0f, 1f, 1f, 0.5f), xMax, yMax );
        }
        UpdateEnable();
    }

    void GetGraphData(){
        if(Global.chosanExperimentName == "complete_fec_e2e"){
            xLabel = "time (sec)";
            yLabel = "# packets received at receiver";
            legend = "<color=#ffff00>---- No FEC</color>\n <color=#00ff40>---- With FEC</color>";
            title = "FEC Effectiveness";
            nCurveMax = 2;
            animTime = 121f;
            targetNode = "p3h0";
            GetCoordinates();
        }
        else if(Global.chosanExperimentName == "complete_mcd_e2e"){
            xLabel = "time (sec)";
            yLabel = "# packets received at receiver";
            legend = "<color=#ffff00>---- No MCD</color>\n <color=#00ff40>---- With MCD</color>";
            title = "MCD Effectiveness";
            nCurveMax = 2;
            animTime = 76f;
            targetNode = "p1h0";
            GetCoordinates();
        }
        else if(Global.chosanExperimentName == "complete_hc_e2e"){
            xLabel = "time (sec)";
            yLabel = "# bytes";
            legend = "<color=#ffff00>---- Before Header Compression</color>\n <color=#00ff40>---- After Header Compression</color>";
            title = "HC Effectiveness";
            nCurveMax = 2;
            animTime = 34f;
            targetNode = "p0e0";
            GetCoordinates();
        }
    }

    void GetCoordinates(){
        float xmax=0f, ymax=0f;
        for(int i=0; i<graphLogText.Count; i++){
            string[] lines = graphLogText[i].Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            // Debug.Log("last Line = " + lines[lines.Length - 1].ToString() + " - " + lines.Length + " - " + lines.GetType());
            
            string[] data = lines[lines.Length - 1].Split(' ');
            relative_scale.Add(float.Parse(data[0])/Global.U_SEC);
            if(xmax<float.Parse(data[0])){
                xmax = float.Parse(data[0])/Global.U_SEC;
            }
            if(ymax<float.Parse(data[1])){
                ymax = float.Parse(data[1]);
            }
            lastData.Add(null);
        }
        for(int i=0; i<graphLogText.Count; i++){
            relative_scale[i] = xmax/relative_scale[i];
        }

        xMax = xmax;
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

    void FixedUpdate(){
        if(graphStartTime==-1f){
            // Debug.Log("GRAPH = " + graphStartTime + " : " + targetNode + " : " + expPktTargetNode);
            if(targetNode==null || expPktTargetNode==null || targetNode != expPktTargetNode){
                return;
            }
            else{
                scale = (animTime-rc)/xMax;
                graphStartTime = rc;
            }
        }
        // nextPacketInfo = graphLogReader.ReadLine().Split(' ');
        string[] coord;
        for(int i=0; i<graphLogText.Count; i++){
            // Debug.Log("[" + i + "] " + lastData[i] + rc);
            if(lastData.Count>0 && lastData[i] != null){
                coord = lastData[i].Split(' ');
                var xVal = float.Parse(coord[0])*scale*relative_scale[i]/Global.U_SEC;
                if(xVal+graphStartTime <= rc){
                    float x = float.Parse(coord[0])/Global.U_SEC*relative_scale[i];
                    graph.ShowPlot((Global.GraphType)i, x, float.Parse(coord[1]));
                    lastData[i] = graphLogReader[i].ReadLine();
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
