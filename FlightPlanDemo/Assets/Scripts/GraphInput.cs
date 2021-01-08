using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEngine;
using UnityEngine.Networking;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

public class GraphInput : MonoBehaviour
{
    struct GraphInfo{
        public bool show;
        public int nCurves;
        public List<string> graphLogText;
        public List<StringReader> graphLogReader;
        public string packetLegend;
        public List<Color> color;
        public string xLabel;
        public string yLabel;
        public string graphLegend;
        public string title;
        public List<float> segmentWidth;
        public float xMax;
        public float yMax;
        public List<float> relative_scale;
        public List<float> relative_offset;
        public List<string> packetTarget;
        public float animTime;
        public float xDiv;
    }
    [SerializeField] GraphControl graph = default;
    [SerializeField] SliderControl sliderControl = default;
    [SerializeField] private ColorControl colorControl = default;
    List<string> lastData = new List<string>();
    int expPktTime = -1;
    string expPktTargetNode = null;
    // TODO Assuming maximum 3 graphs
    List<string> graphLogNames = new List<string>(){"graph_log1.txt", "graph_log2.txt", "graph_log3.txt"};
    List<Color> pointColor = new List<Color>(){new Color(1f, 1f, 0f, 1f), new Color(0f, 1f, 0.25f, 1f), new Color(0f, 1f, 1f, 1f)};
    List<Color> segmentColor = new List<Color>(){new Color(1f, 1f, 0f, 0.5f), new Color(0f, 1f, 0.25f, 0.5f), new Color(0f, 1f, 1f, 0.5f)};
    float rc=0;
    GraphInfo gInfo = new GraphInfo();
    // ConfigRoot configObject;
    JObject dynamicConfigObject;

    public IEnumerator GraphInitStart(){
        UpdateDisable();
        Parser();
        graph.ShowLegendColor(gInfo.packetLegend, gInfo.color);
        if(gInfo.nCurves==0){
            yield break;
        }
        for(int i=0; i<gInfo.nCurves; i++){
            yield return StartCoroutine(GetGraphLogText(Global.chosanExperimentName + "/" + graphLogNames[i]));
        }
        GetMaxCoordinates();
        for(int i=0; i<gInfo.nCurves; i++){
            Debug.Log("gInfo.relative_scale = " + gInfo.relative_scale[i]);
        }
    }

    IEnumerator GetGraphLogText(string fileName){
        if(gInfo.show ==false){
            yield break;
        }
        string graphText="";
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
        gInfo.graphLogText.Add(graphText);
    }

    public void SetConfigObject(JObject dynamicConfigObject){
        // this.configObject = configObject;
        this.dynamicConfigObject = dynamicConfigObject;
    }

    public void GraphInputInit(){
        sliderControl.SetSliderMaxValue(gInfo.animTime);
        if(gInfo.show == false){
            graph.HideGraph(); 
            return;
        }
        gInfo.graphLogReader.Clear();
        lastData.Clear();
        for(int i=0; i<gInfo.graphLogText.Count; i++){
            var reader = new StringReader(gInfo.graphLogText[i]);
            gInfo.graphLogReader.Add(reader);
            lastData.Add(reader.ReadLine());
        }
        graph.GraphParamInit(gInfo.xLabel, gInfo.yLabel, gInfo.graphLegend, gInfo.title);
        if(gInfo.nCurves > 0){
            graph.GraphAxisInit(gInfo.xMax, gInfo.yMax);
        }
        for(int i=0; i<gInfo.nCurves; i++){
            graph.GraphInit((Global.GraphType)i, pointColor[i], segmentColor[i], gInfo.xMax, gInfo.yMax, gInfo.segmentWidth[i] );
        }
        UpdateEnable();
    }

    void FixedUpdate(){
        string[] coord;
        float x=0, y=0;
        for(int i=0; i<gInfo.nCurves; i++){
                // Debug.Log("o--[" + i + "] " + lastData[i] + " : " + expPktTime + " : " + expPktTargetNode + " : " + gInfo.packetTarget[i]);
                if(lastData.Count>0 && lastData[i] != null && expPktTime > -1 && expPktTargetNode == gInfo.packetTarget[i]){
                    coord = lastData[i].Split(' ');
                    var xVal = (float.Parse(coord[0]) * gInfo.relative_scale[i]) + gInfo.relative_offset[i];
                    // Debug.Log("i--[" + i + "] " + xVal + " : " + expPktTime + " : " + expPktTargetNode + " : " + gInfo.packetTarget[i]);
                    if(xVal <= expPktTime){
                        x = float.Parse(coord[0])/gInfo.xDiv*1f;
                        y = float.Parse(coord[1]);
                        // Debug.Log("IN = " + x + " : " + y);
                        graph.ShowPlot((Global.GraphType)i, x, y);
                        lastData[i] = gInfo.graphLogReader[i].ReadLine();
                    }
                }
            }
    }

    public void ReferenceCounterValue(float rc){
        this.rc = rc;
    }

    public void ExpiredPacketTargetNode(int pktTime, string expPktTargetNode){
        this.expPktTime = pktTime;
        this.expPktTargetNode = expPktTargetNode;
    }

    public void SetAnimTime(float t){
        // animTime = t - 1f;
    }

    void Parser(){
        gInfo.relative_scale = new List<float>();
        gInfo.relative_offset = new List<float>();

        // Parsing packet legend info
        gInfo.packetLegend = "";
        gInfo.color = new List<Color>();

        for(int i=0; i<((JArray)dynamicConfigObject["packet_legend"]).Count; i++){
            gInfo.packetLegend += (string)dynamicConfigObject["packet_legend"][i]["type"] + "\n";
            gInfo.color.Add(ColorHexToRGB((string)dynamicConfigObject["packet_legend"][i]["color"]));
        }

        // foreach(var pkt in configObject.PacketLegend){
        //     gInfo.packetLegend += pkt.Type + "\n";
        //     gInfo.color.Add(ColorHexToRGB(pkt.Color));
        // }

        // Parsing Graph parameters
        gInfo.show = true;
        if((string)dynamicConfigObject["graph"]["show"] != "y"){
            gInfo.nCurves=0;
            gInfo.show = false;
        }
        else{
            gInfo.xDiv = float.Parse((string)dynamicConfigObject["graph"]["x_div"]);
            gInfo.xLabel = (string)dynamicConfigObject["graph"]["x_label"];
            gInfo.yLabel = (string)dynamicConfigObject["graph"]["y_label"];
            gInfo.title = (string)dynamicConfigObject["graph"]["title"];
            gInfo.nCurves=0;
            graphLogNames.Clear();
            pointColor.Clear();
            segmentColor.Clear();
            gInfo.graphLegend = "";
            gInfo.segmentWidth = new List<float>();
            gInfo.packetTarget = new List<string>();

            JArray curveArray = (JArray)dynamicConfigObject["graph"]["curve_info"];
            for(int i=0; i<curveArray.Count; i++){
                (gInfo.nCurves)++;
                graphLogNames.Add((string)curveArray[i]["file_name"]);
                pointColor.Add(ColorHexToRGB((string)curveArray[i]["curve_color"] + "ff"));
                segmentColor.Add(ColorHexToRGB((string)curveArray[i]["curve_color"] + "7f"));
                gInfo.graphLegend += "<color=" + (string)curveArray[i]["curve_color"] + ">---- " + (string)curveArray[i]["legend_text"] + "</color>\n";
                gInfo.segmentWidth.Add(int.Parse((string)curveArray[i]["curve_width"]));
                gInfo.packetTarget.Add((string)curveArray[i]["packet_target"]);
            }


        // // Parsing Graph parameters
        // gInfo.show = true;
        // if(configObject.Graph.Show != "y"){
        //     gInfo.nCurves=0;
        //     gInfo.show = false;
        // }
        // else{
        //     gInfo.xDiv = configObject.Graph.XDiv;
        //     gInfo.xLabel = configObject.Graph.XLabel;
        //     gInfo.yLabel = configObject.Graph.YLabel;
        //     gInfo.title = configObject.Graph.Title;
        //     gInfo.nCurves=0;
        //     graphLogNames.Clear();
        //     pointColor.Clear();
        //     segmentColor.Clear();
        //     gInfo.graphLegend = "";
        //     gInfo.segmentWidth = new List<float>();
        //     gInfo.packetTarget = new List<string>();
        //     foreach(var curve in configObject.Graph.CurveInfo){
        //         (gInfo.nCurves)++;
        //         graphLogNames.Add(curve.FileName);
        //         pointColor.Add(ColorHexToRGB(curve.CurveColor + "ff"));
        //         segmentColor.Add(ColorHexToRGB(curve.CurveColor + "7f"));
        //         gInfo.graphLegend += "<color=" + curve.CurveColor + ">---- " + curve.LegendText + "</color>\n";
        //         gInfo.segmentWidth.Add(curve.CurveWidth);
        //         gInfo.packetTarget.Add(curve.PacketTarget);
        //     }

            for(int i=0; i<gInfo.nCurves; i++){
                gInfo.relative_scale.Add(1f);
            }
            for(int i=0; i<gInfo.nCurves; i++){
                gInfo.relative_offset.Add(0f);
            }
        }

        // TODO Animation Time for each experiment
        if(Global.chosanExperimentName == "FEC_booster"){
            // Animation Time
            gInfo.animTime = 716f;
        }
        else if(Global.chosanExperimentName == "MCD_booster"){
            gInfo.animTime = 2704f;
        }
        else if(Global.chosanExperimentName == "HC_booster"){
            gInfo.animTime = 726f;
        }
        else if(Global.chosanExperimentName == "Crosspod:_FEC,_HC,_and_MCD_boosters"){
            gInfo.animTime = 3447f;
        }
        else if(Global.chosanExperimentName == "Split_Crosspod_into_3"){
            gInfo.animTime = 3912f;
        }
        else if(Global.chosanExperimentName == "Split_Crosspod_into_6"){
            gInfo.animTime = 4882f;
        }
        else if(Global.chosanExperimentName == "5_complete_2_FW" || Global.chosanExperimentName == "Introduction"){
            gInfo.animTime = 83f;
            colorControl.SetColorPattern(Global.ColorPattern.None);

            gInfo.relative_scale.Clear();
            gInfo.relative_offset.Clear();
            float scale = 642823f/100f;
            gInfo.relative_scale.Add(scale);
            scale = (6074785f-642823f)/100f;
            gInfo.relative_scale.Add(scale);
            gInfo.relative_offset.Add(0f);
            gInfo.relative_offset.Add(642823f);
        }
        else if(Global.chosanExperimentName == "Failover_mechanism"){
            gInfo.animTime = 121f;
        }
        else if(Global.chosanExperimentName == "Figure_7"){
            gInfo.animTime = 5555f;
        }
        else if(Global.chosanExperimentName == "Untunneled_traffic"){
            gInfo.animTime = 316f;
        }
        else if(Global.chosanExperimentName == "Tunneled_traffic"){
            gInfo.animTime = 168f;
        }
        else if(Global.chosanExperimentName == "QoS"){
            gInfo.animTime = 1576f;
        }

        if(gInfo.nCurves>0){
            gInfo.graphLogText = new List<string>();
            gInfo.graphLogReader = new List<StringReader>();
        }
    }

    void GetMaxCoordinates(){
        if(gInfo.nCurves==0){
            return;
        }
        float xmax=0f, ymax=0f;

        List<float> minX = new List<float>();
        List<float> maxX = new List<float>();
        for(int i=0; i<gInfo.graphLogText.Count; i++){
            string[] lines = gInfo.graphLogText[i].Split(new[] { Environment.NewLine }, StringSplitOptions.None);   
            string[] minData = lines[0].Split(' ');
            string[] maxData = lines[lines.Length - 1].Split(' ');

            minX.Add(float.Parse(minData[0]));
            maxX.Add(float.Parse(maxData[0]));
            if(xmax<float.Parse(maxData[0])){
                xmax = float.Parse(maxData[0]);
            }
            if(ymax<float.Parse(maxData[1])){
                ymax = float.Parse(maxData[1]);
            }
        }
        
        // TODO no check for more then 2 curves
        // if(gInfo.nCurves==1 || (gInfo.nCurves==2 && minX[1] >= maxX[0]) || (gInfo.nCurves==2 && minX[0] >= maxX[1])){
        //     for(int i=0; i<gInfo.nCurves; i++){
        //         gInfo.relative_scale.Add(1f);
        //     }
        // }
        // else{
        //     for(int i=0; i<gInfo.nCurves; i++){
        //         gInfo.relative_scale.Add(xmax/maxX[i]);
        //     }
        // }

        gInfo.xMax = xmax/gInfo.xDiv;
        gInfo.yMax = ymax;
    }

    Color ColorHexToRGB(string hexColor){
        Color outColor;
        if ( ColorUtility.TryParseHtmlString(hexColor, out outColor)){
                return outColor;
        }
        else{
            return Color.black;
        }
    }
    List<Color> ColorHexToRGB(List<string> hexColor){
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

    public void ClearPlot(){
        for(int i=0; i<gInfo.nCurves; i++){
            graph.ClearPlot((Global.GraphType)i);
        }
    }

    void UpdateEnable(){
        enabled = true;
    }

    void UpdateDisable(){
        enabled = false;
    }
}
