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

    public IEnumerator Start(){
        UpdateDisable();
        if(Global.chosanExperimentName == "complete_fec_e2e"){
            yield return StartCoroutine(GetGraphLogText("complete_fec_e2e/graph_log1.txt"));
            yield return StartCoroutine(GetGraphLogText("complete_fec_e2e/graph_log2.txt"));
        }
        else if(Global.chosanExperimentName == "complete_mcd_e2e"){
            yield return StartCoroutine(GetGraphLogText("complete_mcd_e2e/graph_log1.txt"));
            yield return StartCoroutine(GetGraphLogText("complete_mcd_e2e/graph_log2.txt"));
        }
        else if(Global.chosanExperimentName == "complete_hc_e2e"){
            yield return StartCoroutine(GetGraphLogText("complete_hc_e2e/graph_log1.txt"));
            yield return StartCoroutine(GetGraphLogText("complete_hc_e2e/graph_log2.txt"));
        }
    }

    IEnumerator GetGraphLogText(string fileName){
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
            graph.GraphInit(Global.GraphType.Type0, new Color(1f, 0f, 0f, 1f), new Color(1f, 0f, 0f, 0.5f), xMax, yMax);
        }
        if(nCurveMax > 1){
            graph.GraphInit(Global.GraphType.Type1, new Color(0f, 0f, 1f, 1f), new Color(0f, 0f, 1f, 0.5f), xMax, yMax );
        }
        if(nCurveMax > 2){
            graph.GraphInit(Global.GraphType.Type1, new Color(0f, 1f, 1f, 1f), new Color(0f, 1f, 1f, 0.5f), xMax, yMax );
        }
        UpdateEnable();
    }

    void GetGraphData(){
        if(Global.chosanExperimentName == "complete_fec_e2e"){
            xLabel = "time";
            yLabel = "# packets received at receiver";
            legend = "<color=red>---- No FEC</color>\n <color=blue>---- With FEC</color>";
            title = "FEC Effectiveness";
            nCurveMax = 2;
            animTime = 121f;
            // var coord = GetCoordinateMax(); 
            // xMax = coord[0];
            // yMax = coord[1];
            // scale = animTime/xMax;
            // for(int i=0; i<graphLogText.Count; i++){
            //     lastData[i] = graphLogReader[i].ReadLine();
            // }
            GetCoordinates();
        }
        else if(Global.chosanExperimentName == "complete_mcd_e2e"){
            xLabel = "time";
            yLabel = "# packets received at receiver";
            legend = "<color=red>---- No MCD</color>\n <color=blue>---- With MCD</color>";
            title = "MCD Effectiveness";
            nCurveMax = 2;
            animTime = 76f;
            // var coord = GetCoordinateMax();
            // xMax = coord[0];
            // yMax = coord[1];
            // scale = animTime/xMax;
            // for(int i=0; i<graphLogText.Count; i++){
            //     lastData[i] = graphLogReader[i].ReadLine();
            // }
            GetCoordinates();
        }
        else if(Global.chosanExperimentName == "complete_hc_e2e"){
            xLabel = "time";
            yLabel = "# bytes";
            legend = "<color=red>---- Before Header Compression</color>\n <color=blue>---- After Header Compression</color>";
            title = "HC Effectiveness";
            nCurveMax = 2;
            animTime = 34f;
            // var coord = GetCoordinateMax();
            // xMax = coord[0];
            // yMax = coord[1];
            // scale = animTime/xMax;
            // for(int i=0; i<graphLogText.Count; i++){
            //     lastData[i] = graphLogReader[i].ReadLine();
            // }
            GetCoordinates();
        }
    }

    List<float> GetCoordinateMax(){
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
        List<float> coord = new List<float>();
        coord.Add(xmax);
        coord.Add(ymax);
        return coord;
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
        scale = animTime/xMax;
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
    }

    public void ReferenceCounterValue(float rc){
        this.rc = rc;
    }

    void FixedUpdate(){
        // nextPacketInfo = graphLogReader.ReadLine().Split(' ');
        string[] coord;
        for(int i=0; i<graphLogText.Count; i++){
            // Debug.Log("[" + i + "] " + lastData[i] + rc);
            if(lastData[i] != null){
                coord = lastData[i].Split(' ');
                var xVal = float.Parse(coord[0])*scale*relative_scale[i]/Global.U_SEC;
                if(xVal <= rc){
                    float x = float.Parse(coord[0])/Global.U_SEC*relative_scale[i];
                    graph.ShowPlot((Global.GraphType)i, x, float.Parse(coord[1]));
                    lastData[i] = graphLogReader[i].ReadLine();
                }
            }
        }
    }
    public void ShowPlot(){
        // graph.ShowPlot(gType, referenceCounter, graphReplyPackets);
    }

    void UpdateEnable(){
        enabled = true;
    }

    void UpdateDisable(){
        enabled = false;
    }
}
