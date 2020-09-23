using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


using System.IO;
using System.Text;
using System.Linq;

using UnityEngine.Networking;

public class BillBoardControl : MonoBehaviour
{
    struct BillBoardNodeInfo{
        public string nodeName;
        public GameObject boardObject; // Object of Board
        public string hyperlink;    // Hyperlink for info of node
    };

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

    public void BillBoardInit(){
        timeSliderHandle = timeSlider.gameObject.transform.Find("Handle Slide Area").transform.Find("Handle").gameObject;
        UpdateDisable();
        GetBillBoardInfo();
        foreach(string node in boardInfo.Keys){
            topo.AddTagMarker(node);
        }
        UpdateEnable();
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
        GameObject tag_prefab = Resources.Load("TagRight") as GameObject;

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
            BillBoardNodeInfo info = TagUpdate(tag_prefab,
                                            tInfo[time].Item3, 
                                            tInfo[time].Item1,
                                            topo.GetNodePosition(tInfo[time].Item1) + new Vector3(0, 0.5f, 0),
                                            tInfo[time].Item1,
                                            tInfo[time].Item2
                                            );
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
        info.boardObject.transform.Find("Canvas").transform.Find("TextBackground").transform.Find("HyperlinkButton").GetComponent<Button>().onClick.AddListener(delegate{HyperlinkButtonHandler(info.hyperlink);});
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
        if(eventTagStatus==true){
            DetectTime();
        }
    }

    public void BillBoardFollowCam(){
        foreach(var node in boardInfo.Keys){
            Vector3 targetPos = new Vector3(cam.gameObject.transform.position.x, boardInfo[node].boardObject.transform.position.y, cam.gameObject.transform.position.z);
            boardInfo[node].boardObject.transform.LookAt(targetPos);
        }
        foreach(var time in timeInfo.Keys){
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

    void DetectTime(){
        int time = anim.GetInstantiatedPacketTime();
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
        
        if(Global.chosanExperimentName == "1_complete_fec_e2e"){
            nodeName = "p0e0";
            nodeText = "FEC Booster - Parity packets generate at this node";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p0a0";
            nodeText = "Lost packets are recovered with the help of parity parity packets here";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "1_complete_mcd_e2e"){
            nodeName = "p0a0";
            nodeText = "MCD Booster - Cached packets resides at the server linked with this node";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "1_complete_hc_e2e"){
            nodeName = "p0e0";
            nodeText = "HC Booster - Header is compressed here";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p0a0";
            nodeText = "Header is decompressed here";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "2_complete_all_e2e"){
            nodeName = "p0e0";
            nodeText = "All the three boosters are active - Forward Error Correction (FEC), Memcached (MCD), Header Compression (HC)";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p0a0";
            nodeText = "Lost packets are recovered. If requested packet is cached here, they will be dispatched towards the source host. Header is decompressed";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "3_complete_e2e_1_hl3new"){
            nodeName = "p0e0";
            nodeText = "All the three boosters are active - Forward Error Correction (FEC), Memcached (MCD), Header Compression (HC). ";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "S1";
            nodeText = "This is a supporting device. Lost packets are recovered. If requested packet is cached here, they will be dispatched towards the source host. Header is decompressed.";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "3_complete_e2e_2_hl3new"){
            nodeName = "p0e0";
            nodeText = "All the three boosters are active - Forward Error Correction (FEC), Memcached (MCD), Header Compression (HC). ";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "S2_1";
            nodeText = "Lost packets are recovered.  Header is decompressed.";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "S2_2";
            nodeText = "Decompress the header which was compressed on p0e0.";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "S2_3";
            nodeText = "This act as a Cache. If requested packet is cached here, they will be dispatched towards the source host.";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "7_split1"){
            nodeName = "SA_1";
            nodeText = "Supporting device to offload p0e0";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "SA_2";
            nodeText = "Redundent supporting device to support failover";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

        }
        return info;
    }

    Dictionary<int, Tuple<string, string, string>> TimeParser(){
        Dictionary<int, Tuple<string, string, string>> info = new Dictionary<int, Tuple<string, string, string>>();
        int time;
        string nodeName, nodeText, hyperlink;

        if(Global.chosanExperimentName == "1_complete_fec_e2e"){
            time = 3254274;
            nodeName = "p0e0";
            nodeText = "Now the parity packet (silver) travels from p0e0 towards p0a0 ";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));

            time = 13273800;
            nodeName = "p0a0";
            nodeText = "One lost packet is about to be recovered at p0a0 as a result of FEC. So the recovered blue packet can be seen travelling from p0a0 towards c0";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "1_complete_hc_e2e"){
            time = 3254274;
            nodeName = "p0e0";
            nodeText = "Now the parity packet (silver) travels from p0e0 towards p0a0 ";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));

            time = 13273800;
            nodeName = "p0a0";
            nodeText = "One lost packet is about to be recovered at p0a0 as a result of FEC. So the recovered blue packet can be seen travelling from p0a0 towards c0";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "6_split1"){
            time = 1971;
            nodeName = "SA_1";
            nodeText = "The packet from p0e0 travels to SA_1 for processing [Supporting device SA_1 offloads p0e0]";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));

            time = 29746104;
            nodeName = "SA_2";
            nodeText = "Since SA_1 has stopped working it is failed over by SA_2.";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "7_split1"){
            time = 1971;
            nodeName = "SA_1";
            nodeText = "The packet from p0e0 travels to SA_1 for processing [Supporting device SA_1 offloads p0e0]";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));

            time = 29746104;
            nodeName = "SA_2";
            nodeText = "Since SA_1 has stopped working it is failed over by SA_2.";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));
        }
        return info;
    }
}
