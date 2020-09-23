using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEngine;
using UnityEngine.Networking;

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
    public IEnumerator Start(){
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
        gInfo.graphLogText.Add(graphText);
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
        
        if(Global.chosanExperimentName == "1_complete_fec_e2e"){
            gInfo.animTime = 716f;
            gInfo.show = true;
            gInfo.nCurves = 2;
            gInfo.packetLegend = "Parity\nTCP p0h0->p1h0\nTCP p1h0->p0h0";
            gInfo.color = ColorHexToRGB(new List<string>(){"#ffffff", "#0000ff", "#ffff00"});
            gInfo.xLabel = "time (sec)";
            gInfo.yLabel = "# packets received at receiver";
            gInfo.graphLegend = "<color=#ffff00>---- No FEC</color>\n <color=#00ff40>---- With FEC (k=5, h=1)</color>";
            gInfo.title = "FEC Effectiveness";
            gInfo.xDiv = Global.U_SEC;
            gInfo.segmentWidth = new List<float>(){1f, 1f};
            gInfo.packetTarget = new List<string>(){"p1h0", "p1h0"};
            for(int i=0; i<gInfo.nCurves; i++){
                gInfo.relative_scale.Add(1f);
            }
            for(int i=0; i<gInfo.nCurves; i++){
                gInfo.relative_offset.Add(0f);
            }
        }
        else if(Global.chosanExperimentName == "1_complete_mcd_e2e"){
            gInfo.animTime = 2704f;
            gInfo.show = true;
            gInfo.nCurves = 2;
            gInfo.packetLegend = "MCD Request\nMCD Reply\nMCD Cached\nParity\nICMP Request";
            gInfo.color = ColorHexToRGB(new List<string>(){"#0EF3E1", "#61D612", "#FF8A00", "#ffffff", "#0000ff"});
            gInfo.xLabel = "time (sec)";
            gInfo.yLabel = "# packets received at receiver";
            gInfo.graphLegend = "<color=#ffff00>---- No MCD</color>\n <color=#00ff40>---- With MCD</color>";
            gInfo.title = "MCD Effectiveness";
            gInfo.xDiv = Global.U_SEC;
            gInfo.segmentWidth = new List<float>(){1f, 1f};
            gInfo.packetTarget = new List<string>(){"p0e0", "p1h0"};
            for(int i=0; i<gInfo.nCurves; i++){
                gInfo.relative_scale.Add(1f);
            }
            for(int i=0; i<gInfo.nCurves; i++){
                gInfo.relative_offset.Add(0f);
            }
        }
        else if(Global.chosanExperimentName == "1_complete_hc_e2e"){
            gInfo.animTime = 726f;
            gInfo.show = true;
            gInfo.nCurves = 2;
            gInfo.packetLegend = "Compressed\nParity\nTCP p0h0->p1h0\nTCP p1h0->p0h0";
            gInfo.color = ColorHexToRGB(new List<string>(){"#ff00ff", "#ffffff", "#0000ff", "#ffff00"});
            gInfo.xLabel = "time (sec)";
            gInfo.yLabel = "# bytes";
            gInfo.graphLegend = "<color=#ffff00>---- Before Header Compression</color>\n <color=#00ff40>---- After Header Compression</color>";
            gInfo.title = "HC Effectiveness";
            gInfo.xDiv = Global.U_SEC;
            gInfo.segmentWidth = new List<float>(){1f, 1f};
            gInfo.packetTarget = new List<string>(){"p0e0", "dropper"};
            for(int i=0; i<gInfo.nCurves; i++){
                gInfo.relative_scale.Add(1f);
            }
            for(int i=0; i<gInfo.nCurves; i++){
                gInfo.relative_offset.Add(0f);
            }
        }
        else if(Global.chosanExperimentName == "2_complete_all_e2e"){
            gInfo.animTime = 3447f;
            gInfo.show = false;
            gInfo.nCurves = 0;
            gInfo.packetLegend = "TCP p0h0->p1h0\nTCP p1h0->p0h0\nMCD Request\nMCD Reply\nMCD Cached\nCompressed\nParity\nICMP Request";
            gInfo.color = ColorHexToRGB(new List<string>(){"#0EF3E1", "#61D612","#FF8A00", "#ff00ff", "#ffffff", "#0000ff", "#ffff00", "#ff0000"});
        }
        else if(Global.chosanExperimentName == "3_complete_e2e_1_hl3new"){
            gInfo.animTime = 3912f;
            gInfo.show = false;
            gInfo.nCurves = 0;
            gInfo.packetLegend = "TCP p0h0->p1h0\nTCP p1h0->p0h0\nCompressed\nMCD Request\nMCD Reply\nMCD Cached\nParity\nICMP Request";
            gInfo.color = ColorHexToRGB(new List<string>(){"#0000ff", "#ffff00", "#ff00ff","#0EF3E1", "#61D612","#FF8A00", "#ffffff", "#ff0000"});
        }
        else if(Global.chosanExperimentName == "3_complete_e2e_2_hl3new"){
            gInfo.animTime = 4882f;
            gInfo.show = false;
            gInfo.nCurves = 0;
            gInfo.packetLegend = "MCD Request\nMCD Reply\nMCD Cached\nParity\nICMP Request";
            gInfo.color = ColorHexToRGB(new List<string>(){"#0EF3E1", "#61D612","#FF8A00", "#ffffff", "#0000ff"});
        }
        else if(Global.chosanExperimentName == "5_complete_2_FW"){
            gInfo.animTime = 83f;
            gInfo.show = true;
            gInfo.nCurves = 2;
            colorControl.SetColorPattern(Global.ColorPattern.None);
            gInfo.packetLegend = "TCP Packets";
            gInfo.color = ColorHexToRGB(new List<string>(){"#ffff00"});
            gInfo.xLabel = "% test completed";
            gInfo.yLabel = "% success rate";
            gInfo.graphLegend = "<color=#ffff00>---- Positive Test</color>\n <color=#00ff40>---- Negative Test</color>";
            gInfo.title = "Firewall Effectiveness";
            gInfo.xDiv = 1f;
            gInfo.segmentWidth = new List<float>(){1f, 4f};
            gInfo.packetTarget = new List<string>(){"D_FW_1", "D_FW_1"};
            float scale = 642823f/100f;
            gInfo.relative_scale.Add(scale);
            scale = (6074785f-642823f)/100f;
            gInfo.relative_scale.Add(scale);
            gInfo.relative_offset.Add(0f);
            gInfo.relative_offset.Add(642823f);
        }
        else if(Global.chosanExperimentName == "7_split1"){
            gInfo.animTime = 121f;
            gInfo.show = true;
            gInfo.nCurves = 2;
            gInfo.packetLegend = "ICMP Request\nICMP Reply\nFeedback";
            gInfo.color = ColorHexToRGB(new List<string>(){"#0000ff", "#ffff00", "#EC119D"});
            gInfo.xLabel = "time (sec)";
            gInfo.yLabel = "# bytes passing through the devices";
            gInfo.graphLegend = "<color=#ffff00>---- SA_1</color>\n <color=#00ff40>---- SA_2</color>";
            gInfo.title = "Failover Mechanism";
            gInfo.xDiv = Global.U_SEC;
            gInfo.segmentWidth = new List<float>(){1f, 1f};
            gInfo.packetTarget = new List<string>(){"SA_1", "SA_2"};
            for(int i=0; i<gInfo.nCurves; i++){
                gInfo.relative_scale.Add(1f);
            }
            for(int i=0; i<gInfo.nCurves; i++){
                gInfo.relative_offset.Add(0f);
            }
        }
        else if(Global.chosanExperimentName == "Introduction"){
            // gInfo.animTime = 121f;
            // gInfo.show = true;
            // gInfo.nCurves = 2;
            // gInfo.packetLegend = "ICMP Request\nICMP Reply\nFeedback";
            // gInfo.color = ColorHexToRGB(new List<string>(){"#0000ff", "#ffff00", "#EC119D"});
            // gInfo.xLabel = "time (sec)";
            // gInfo.yLabel = "# bytes passing through the devices";
            // gInfo.graphLegend = "<color=#ffff00>---- SA_1</color>\n <color=#00ff40>---- SA_2</color>";
            // gInfo.title = "Failover Mechanism"; 
            // gInfo.xDiv = Global.U_SEC; 
            // gInfo.segmentWidth = new List<float>(){1f, 1f};   
            // gInfo.packetTarget = new List<string>(){"SA_1", "SA_2"}; 
            // for(int i=0; i<gInfo.nCurves; i++){
            //     gInfo.relative_scale.Add(1f);
            // }
            // for(int i=0; i<gInfo.nCurves; i++){
            //     gInfo.relative_offset.Add(0f);
            // }     
            gInfo.animTime = 83f;
            gInfo.show = true;
            gInfo.nCurves = 2;
            colorControl.SetColorPattern(Global.ColorPattern.None);
            gInfo.packetLegend = "TCP Packets";
            gInfo.color = ColorHexToRGB(new List<string>(){"#ffff00"});
            gInfo.xLabel = "% test completed";
            gInfo.yLabel = "% success rate";
            gInfo.graphLegend = "<color=#ffff00>---- Positive Test</color>\n <color=#00ff40>---- Negative Test</color>";
            gInfo.title = "Firewall Effectiveness";
            gInfo.xDiv = 1f;
            gInfo.segmentWidth = new List<float>(){1f, 4f};
            gInfo.packetTarget = new List<string>(){"D_FW_1", "D_FW_1"};
            float scale = 642823f/100f;
            gInfo.relative_scale.Add(scale);
            scale = (6074785f-642823f)/100f;
            gInfo.relative_scale.Add(scale);
            gInfo.relative_offset.Add(0f);
            gInfo.relative_offset.Add(642823f);
        }
        else if(Global.chosanExperimentName == "6_split2_all"){
            gInfo.animTime = 5555f;
            gInfo.show = false;
            gInfo.nCurves = 0;
            gInfo.packetLegend = "TCP p0h0->p1h0\nTCP p1h0->p0h0\nMCD Request\nMCD Reply\nMCD Cached\nParity\nICMP Request\nFeedback";
            gInfo.color = ColorHexToRGB(new List<string>(){"#0000ff", "#ffff00","#0EF3E1", "#61D612","#FF8A00", "#ffffff", "#ff0000", "#EC119D"});
        }
        else if(Global.chosanExperimentName == "8_tunnel_base"){
            gInfo.animTime = 316f;
            gInfo.show = false;
            gInfo.nCurves = 0;
            gInfo.packetLegend = "TCP p0h3->p3h2\nTCP p3h2->p0h3";
            gInfo.color = ColorHexToRGB(new List<string>(){"#0000ff", "#ffff00"});
        }
        else if(Global.chosanExperimentName == "9_tunnel_encapsulated"){
            gInfo.animTime = 168f;
            gInfo.show = false;
            gInfo.nCurves = 0;
            gInfo.packetLegend = "TUNNEL p0h3->p3h2";
            gInfo.color = ColorHexToRGB(new List<string>(){"#0000ff"});
        }
        else if(Global.chosanExperimentName == "10_qos"){
            gInfo.animTime = 1576f;
            gInfo.show = false;
            gInfo.nCurves = 0;
            gInfo.packetLegend = "TCP p0h3->p3h2\nTCP p3h2->p0h3\nQOS p0e1->p0a1\nQOS p0e1->p0h3";
            gInfo.color = ColorHexToRGB(new List<string>(){"#0000ff", "#ffff00", "#EC119D", "#0EF3E1"});
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
        float xmax=0f, ymax=0f, div=1f;

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
