using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using System.IO;
using System.Text;
using System.Linq;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

using UnityEngine.Networking;

public class BillBoardControl : MonoBehaviour
{
    struct BillBoardNodeInfo{
        public string nodeName;
        public GameObject boardObject; // Object of Board
        public string hyperlink;    // Hyperlink for info of node
    };

    [SerializeField] GameObject tag2DYellow = default;
    [SerializeField] GameObject tag2DGreen = default;
    [SerializeField] GameObject graph = default;
    [SerializeField] private Camera cam = default;
    [SerializeField] private Topology topo = default;
    [SerializeField] AnimControl anim = default;
    [SerializeField] Slider timeSlider = default;
    bool prePlay = Global.PRE_PLAY;
    Dictionary<string, BillBoardNodeInfo> boardInfo = new Dictionary<string, BillBoardNodeInfo>();  // Node name : BillBoardNodeInfo
    SortedDictionary<int, Tuple<BillBoardNodeInfo, bool>> timeInfo = new SortedDictionary<int, Tuple<BillBoardNodeInfo, bool>>(); // Time : <BillBoardNodeInfo : shown>
    Global.AnimStatus animStatusBeforeBoard = Global.AnimStatus.Forward;
    bool boardOn=false;
    bool eventTagStatus = true, switchTagStatus = true;
    GameObject timeSliderHandle;
    // ConfigRoot configObject;
    JObject dynamicConfigObject;

    public void BillBoardInit(){
        timeSliderHandle = timeSlider.gameObject.transform.Find("Handle Slide Area").transform.Find("Handle").gameObject;
        UpdateDisable();
        GetBillBoardInfo();
        foreach(string node in boardInfo.Keys){
            topo.AddTagMarker(node);
        }
        UpdateEnable();
    }

    public void SetConfigObject(JObject dynamicConfigObject){
        // this.configObject = configObject;
        this.dynamicConfigObject = dynamicConfigObject;
    }

    public void ResetTimeInfo(){
        List<int> timeKeys = new List<int>();
        foreach(int k in timeInfo.Keys){
            timeKeys.Add(k);  
        }
        foreach(int k in timeKeys){
            timeInfo[k] = new Tuple<BillBoardNodeInfo, bool>(timeInfo[k].Item1, false);
        }
    }

    void GetBillBoardInfo(){
        GameObject tag_prefab = Resources.Load("TagLeft") as GameObject;

        Dictionary<string, Tuple<string, string>> nodeInfo = MetadataParser();

        foreach(var node in nodeInfo.Keys){
            BillBoardNodeInfo info = TagUpdate(tag_prefab,
                                            nodeInfo[node].Item2, 
                                            node,
                                            topo.GetNodePosition(node) + new Vector3(0, 0.5f, 0),
                                            node,
                                            nodeInfo[node].Item1
                                            );
            boardInfo.Add(node, info);
        }

        Dictionary<int, Tuple<string, string, string>> tInfo = TimeParser();
        foreach(var time in tInfo.Keys){
            BillBoardNodeInfo info;
            if(tInfo[time].Item1 == "Yellow Curve" || tInfo[time].Item1 == "Green Curve"){
                info = Tag2DUpdate("graph", tInfo[time].Item1, tInfo[time].Item2);
            }
            else{
                info = TagUpdate(tag_prefab,
                                                tInfo[time].Item3, 
                                                tInfo[time].Item1,
                                                topo.GetNodePosition(tInfo[time].Item1) + new Vector3(0, 0.5f, 0),
                                                tInfo[time].Item1,
                                                tInfo[time].Item2
                                                );
            }
            timeInfo.Add(time, new Tuple<BillBoardNodeInfo, bool>(info, false));
        }
    }

    BillBoardNodeInfo TagUpdate(GameObject tag_prefab, string hyperlink, string nodeName, Vector3 boardPos, string head, string detail){
        BillBoardNodeInfo info = new BillBoardNodeInfo();
        info.hyperlink = hyperlink;
        info.nodeName = nodeName;
        info.boardObject = Instantiate(tag_prefab) as GameObject;
        info.boardObject.SetActive(false);
        info.boardObject.transform.position = boardPos;
        info.boardObject.transform.Find("Canvas").transform.Find("Button").GetComponent<Button>().onClick.AddListener(delegate{OkButtonHandler(info.boardObject);});
        info.boardObject.transform.Find("Canvas").transform.Find("HeadingText").GetComponent<Text>().text = head;
        info.boardObject.transform.Find("Canvas").transform.Find("TextBackground").transform.Find("Text").GetComponent<Text>().text = detail;
        if(info.hyperlink==null){
            info.boardObject.transform.Find("Canvas").transform.Find("TextBackground").transform.Find("Hyperlink").gameObject.SetActive(false);
            info.boardObject.transform.Find("Canvas").transform.Find("TextBackground").transform.Find("HyperlinkButton").gameObject.SetActive(false);
        }
        else{
            info.boardObject.transform.Find("Canvas").transform.Find("TextBackground").transform.Find("HyperlinkButton").GetComponent<Button>().onClick.AddListener(delegate{HyperlinkButtonHandler(info.hyperlink);});
        }
        
        return info;
    }

    BillBoardNodeInfo Tag2DUpdate(string nodeName, string head, string detail){
        // Vector3 pos = graph.transform.position; 
        // string head = "Yellow Curve";
        // string detail = "curve is showing something";

        BillBoardNodeInfo info = new BillBoardNodeInfo();
        info.hyperlink = null;
        info.nodeName = nodeName;
        if(head == "Yellow Curve"){
            info.boardObject = tag2DYellow;
        }
        else{
            info.boardObject = tag2DGreen;
        }
        info.boardObject.SetActive(false);
        // info.boardObject.transform.position = graph.transform.position + new Vector3(70f, 200f, 0); // 70 270 0
        // info.boardObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(420f, 240f);
        info.boardObject.transform.SetParent(graph.transform.Find("GraphContainer").transform.Find("Background").transform.Find("TagAnchorObject").GetComponent<RectTransform>());
        info.boardObject.transform.position = graph.transform.Find("GraphContainer").transform.Find("Background").transform.Find("TagAnchorObject").transform.position;
        info.boardObject.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
        info.boardObject.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
        info.boardObject.transform.Find("Background").transform.Find("Button").GetComponent<Button>().onClick.AddListener(delegate{OkButtonHandler(info.boardObject);});
        info.boardObject.transform.Find("Background").transform.Find("HeadingText").GetComponent<Text>().text = head;
        info.boardObject.transform.Find("Background").transform.Find("TextBackground").transform.Find("Text").GetComponent<Text>().text = detail;
        
        return info;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(prePlay==true){
            prePlay = anim.GetPrePlayStatus();
        }
        if(switchTagStatus==true){
            DetectMouseClick();
        }
        // if(eventTagStatus==true){
        //     DetectTime();
        // }
    }

    public void DetectEventTag(int time){
        if(eventTagStatus==true){
            DetectTime(time);
        }
    }

    public void BillBoardFollowCam(){
        foreach(var node in boardInfo.Keys){
            Vector3 targetPos = new Vector3(cam.gameObject.transform.position.x, boardInfo[node].boardObject.transform.position.y, cam.gameObject.transform.position.z);
            boardInfo[node].boardObject.transform.LookAt(targetPos);
        }
        foreach(var time in timeInfo.Keys){
            if(timeInfo[time].Item1.nodeName == "graph"){
                continue;
            }
            Vector3 targetPos = new Vector3(cam.gameObject.transform.position.x, timeInfo[time].Item1.boardObject.transform.position.y, cam.gameObject.transform.position.z);
            timeInfo[time].Item1.boardObject.transform.LookAt(targetPos);
        }
    }

    void DetectMouseClick(){
        if(Input.GetMouseButtonDown(0)){
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if(Physics.Raycast(ray, out hit, 100.0f)){
                if(hit.transform != null && boardInfo.ContainsKey(hit.collider.gameObject.name)){
                    ShowBillBoard(boardInfo[hit.collider.gameObject.name].boardObject);
                }
            }
        }
    }

    public void CreateButton(Vector3 position, Vector2 size){
        GameObject button_prefab = timeSlider.gameObject.transform.Find("Marker").gameObject;
        GameObject go = Instantiate(button_prefab) as GameObject;
        go.transform.parent = timeSlider.gameObject.transform;
        Vector2 bPos = new Vector2(position.x, position.y + 20f);
        go.transform.position = bPos;
        go.SetActive(true);
    }

    void DetectTime(int time){
        // int time = anim.GetInstantiatedPacketTime();
        if(time!=-1 && timeInfo.ContainsKey(time)){
            Debug.Log("Event Tag time = " + (anim.RCtime()/Global.U_SEC).ToString() + " : " + time + " : " + timeSliderHandle.transform.position);
            if(prePlay){
                CreateButton(timeSliderHandle.transform.position, new Vector2(10f, 5f));
            }
            else{
                if(ShowBillBoard(timeInfo[time].Item1.boardObject)){
                    timeInfo[time] = new Tuple<BillBoardNodeInfo, bool>( timeInfo[time].Item1, true);
                }
            }
        }
    }

    bool ShowBillBoard(GameObject obj){
        if(boardOn==false){
            boardOn = true;
            animStatusBeforeBoard = anim.GetAnimStatus();
            if(animStatusBeforeBoard != Global.AnimStatus.Pause){
                anim.Pause();
            }
            obj.SetActive(true);
            return true;
        }
        return false;
    }

    // void SetAnimStatusAfterBoard(){
    //     if(animStatusBeforeBoard == Global.AnimStatus.Pause){
    //         anim.Pause();
    //     }
    //     else if(animStatusBeforeBoard == Global.AnimStatus.Forward){
    //         anim.Forward();
    //     }
    //     else if(animStatusBeforeBoard == Global.AnimStatus.Rewind){
    //         anim.Rewind();
    //     }
    // }

    void OkButtonHandler(GameObject obj){
        obj.SetActive(false);
        // SetAnimStatusAfterBoard();
        if(animStatusBeforeBoard != Global.AnimStatus.Pause){
            anim.Resume(animStatusBeforeBoard);
        }
        boardOn = false;
    }

    void HyperlinkButtonHandler(string link){
        Debug.Log("In HyperlinkButtonHandler");
        if( Application.platform==RuntimePlatform.WebGLPlayer )
        {
            Debug.Log("In HyperlinkButtonHandler ExternalEval");
            Application.ExternalEval("window.open(\"" + link + "\")");
        }
        else{
            Debug.Log("In HyperlinkButtonHandler OpenURL");
            Application.OpenURL(link);
        }
    }

    public void SetSwitchTagStatus(bool status){
        switchTagStatus = status;
    }
    public void SetEventTagStatus(bool status){
        eventTagStatus = status;
    }
    void UpdateEnable(){
        enabled = true;
    }
    void UpdateDisable(){
        enabled = false;
    }

    Dictionary<string, Tuple<string, string>> MetadataParser(){
        Dictionary<string, Tuple<string, string>> info = new Dictionary<string, Tuple<string, string>>();
        string nodeName, nodeText, hyperlink;

        JArray sTag = (JArray)dynamicConfigObject["static_tags"];
        for(int i=0; i<sTag.Count; i++){
            nodeName = (string)sTag[i]["node"];
            nodeText = (string)sTag[i]["text"];
            hyperlink = (string)sTag[i]["hyperlink"];
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));
        }

        // if(configObject.StaticTags==null){
        //     return info;
        // }
        // foreach(var attr in configObject.StaticTags){
        //     nodeName = attr.Node;
        //     nodeText = attr.Text;
        //     hyperlink = attr.Hyperlink;
        //     info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));
        // }
        return info;
    }

    Dictionary<int, Tuple<string, string, string>> TimeParser(){
        Dictionary<int, Tuple<string, string, string>> info = new Dictionary<int, Tuple<string, string, string>>();
        int time;
        string nodeName, nodeText, hyperlink;

        JArray eTag = (JArray)dynamicConfigObject["event_tags"];
        for(int i=0; i<eTag.Count; i++){
            time = int.Parse((string)eTag[i]["time"]);
            nodeName = (string)eTag[i]["node"];
            nodeText = (string)eTag[i]["text"];
            hyperlink = (string)eTag[i]["hyperlink"];
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));
        }


        // if(configObject.EventTags==null){
        //     return info;
        // }
        // foreach(var attr in configObject.EventTags){
        //     time = attr.Time;
        //     nodeName = attr.Node;
        //     nodeText = attr.Text;
        //     hyperlink = attr.Hyperlink;
        //     info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));
        // }
        return info;
    }
}
