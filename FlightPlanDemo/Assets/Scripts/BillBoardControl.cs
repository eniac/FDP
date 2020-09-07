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
    Dictionary<string, BillBoardNodeInfo> boardInfo = new Dictionary<string, BillBoardNodeInfo>();  // Node name : BillBoardNodeInfo
    SortedDictionary<int, Tuple<BillBoardNodeInfo, bool>> timeInfo = new SortedDictionary<int, Tuple<BillBoardNodeInfo, bool>>(); // Time : <BillBoardNodeInfo : shown>
    Global.AnimStatus animStatusBeforeBoard = Global.AnimStatus.Forward;
    bool boardOn=false;

    public void BillBoardInit(){
        UpdateDisable();
        GetBillBoardInfo();
        foreach(string node in boardInfo.Keys){
            topo.SetHeloEffect(node);
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
        GameObject bubble_prefab = Resources.Load("BillBoard1") as GameObject;

        Dictionary<string, Tuple<string, string>> nodeInfo = MetadataParser();

        foreach(var node in nodeInfo.Keys){
            BillBoardNodeInfo info = new BillBoardNodeInfo();
            info.hyperlink = nodeInfo[node].Item2;
            info.nodeName = node;
            Vector3 boardPos = topo.GetNodePosition(node) + new Vector3(0, 0.5f, 0);
            info.boardObject = Instantiate(bubble_prefab) as GameObject;
            info.boardObject.transform.position = boardPos;
            info.boardObject.transform.Find("Text").transform.GetComponent<TextMeshPro>().text = nodeInfo[node].Item1;
            info.boardObject.transform.Find("Canvas").transform.Find("Button").GetComponent<Button>().onClick.AddListener(delegate{OkButtonHandler(info.boardObject);});
            info.boardObject.transform.Find("HyperLinkCanvas").transform.Find("Button").GetComponent<Button>().onClick.AddListener(delegate{HyperlinkButtonHandler(info.hyperlink);});
            info.boardObject.SetActive(false);
            boardInfo.Add(node, info);
        }

        Dictionary<int, Tuple<string, string, string>> tInfo = TimeParser();
        foreach(var time in tInfo.Keys){
            BillBoardNodeInfo info = new BillBoardNodeInfo();
            info.hyperlink = tInfo[time].Item3;
            info.nodeName = tInfo[time].Item1;
            Vector3 boardPos = topo.GetNodePosition(info.nodeName) + new Vector3(0, 0.5f, 0);
            info.boardObject = Instantiate(bubble_prefab) as GameObject;
            info.boardObject.transform.position = boardPos;
            info.boardObject.transform.Find("Text").transform.GetComponent<TextMeshPro>().text = tInfo[time].Item2;
            info.boardObject.transform.Find("Canvas").transform.Find("Button").GetComponent<Button>().onClick.AddListener(delegate{OkButtonHandler(info.boardObject);});
            info.boardObject.transform.Find("HyperLinkCanvas").transform.Find("Button").GetComponent<Button>().onClick.AddListener(delegate{HyperlinkButtonHandler(info.hyperlink);});
            info.boardObject.SetActive(false);
            timeInfo.Add(time, new Tuple<BillBoardNodeInfo, bool>(info, false));
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        BillBoardFollowCam();
        DetectMouseClick();
        DetectTime();
    }

    void BillBoardFollowCam(){
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

    void DetectTime(){
        int time = anim.GetInstantiatedPacketTime();
        if(timeInfo.ContainsKey(time) && timeInfo[time].Item2==false){
            if(ShowBillBoard(timeInfo[time].Item1.boardObject)){
                timeInfo[time] = new Tuple<BillBoardNodeInfo, bool>( timeInfo[time].Item1, true);
            }
        }
    }

    bool ShowBillBoard(GameObject obj){
        if(boardOn==false){
            boardOn = true;
            animStatusBeforeBoard = anim.GetAnimStatus();
            anim.Pause();
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
        anim.Resume(animStatusBeforeBoard);
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
            info.Add(nodeName, new Tuple<string, string>(nodeName + "\n" + "" + nodeText, hyperlink));

            nodeName = "p0a0";
            nodeText = "Lost packets are recovered with the help of parity parity packets";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(nodeName, new Tuple<string, string>(nodeName + "\n" + "" + nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "1_complete_mcd_e2e"){
            nodeName = "p0a0";
            nodeText = "Cached packets resides at the server linked with this node";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(nodeName, new Tuple<string, string>(nodeName + "\n" + "" + nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "1_complete_hc_e2e"){
            nodeName = "p0e0";
            nodeText = "HC Booster - Header is compressed";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(nodeName, new Tuple<string, string>(nodeName + "\n" + "" + nodeText, hyperlink));

            nodeName = "p0a0";
            nodeText = "Header is decompressed";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(nodeName, new Tuple<string, string>(nodeName + "\n" + "" + nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "2_complete_all_e2e"){
            nodeName = "p0e0";
            nodeText = "All the three boosters are active - Forward Error Correction (FEC), Memcached (MCD), Header Compression (HC)";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(nodeName, new Tuple<string, string>(nodeName + "\n" + "" + nodeText, hyperlink));

            nodeName = "p0a0";
            nodeText = "Lost packets are recovered. If requested packet is cached here will dispatched to the source host. Header is decompressed";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(nodeName, new Tuple<string, string>(nodeName + "\n" + "" + nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "3_complete_e2e_1_hl3new"){
            nodeName = "p0e0";
            nodeText = "TODO";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(nodeName, new Tuple<string, string>(nodeName + "\n" + "" + nodeText, hyperlink));

            nodeName = "p0a0";
            nodeText = "TODO";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(nodeName, new Tuple<string, string>(nodeName + "\n" + "" + nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "7_split1"){
            nodeName = "SA_1";
            nodeText = "Supporting device to offload p0e0";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(nodeName, new Tuple<string, string>(nodeName + "\n" + "" + nodeText, hyperlink));

            nodeName = "SA_2";
            nodeText = "Redundent supporting device to support failover";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(nodeName, new Tuple<string, string>(nodeName + "\n" + "" + nodeText, hyperlink));

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
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeName + "\n" + nodeText, hyperlink));

            time = 13273800;
            nodeName = "p0a0";
            nodeText = "One lost packet is about to be recovered at p0a0 as a result of FEC. So the recovered blue packet can be seen travelling from p0a0 towards c0";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeName + "\n" + nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "1_complete_hc_e2e"){
            time = 3254274;
            nodeName = "p0e0";
            nodeText = "Now the parity packet (silver) travels from p0e0 towards p0a0 ";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeName + "\n" + nodeText, hyperlink));

            time = 13273800;
            nodeName = "p0a0";
            nodeText = "One lost packet is about to be recovered at p0a0 as a result of FEC. So the recovered blue packet can be seen travelling from p0a0 towards c0";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeName + "\n" + nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "6_split1"){
            time = 1971;
            nodeName = "SA_1";
            nodeText = "The packet from p0e0 travels to SA_1 for processing [Supporting device SA_1 offloads p0e0]";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeName + "\n" + nodeText, hyperlink));

            time = 29746104;
            nodeName = "SA_2";
            nodeText = "Since SA_1 has stopped working it is failed over by SA_2.";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeName + "\n" + nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "7_split1"){
            time = 1971;
            nodeName = "SA_1";
            nodeText = "The packet from p0e0 travels to SA_1 for processing [Supporting device SA_1 offloads p0e0]";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeName + "\n" + nodeText, hyperlink));

            time = 29746104;
            nodeName = "SA_2";
            nodeText = "Since SA_1 has stopped working it is failed over by SA_2.";
            hyperlink = "https://flightplan.cis.upenn.edu/";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeName + "\n" + nodeText, hyperlink));
        }
        return info;
    }
}
