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
        if(info.hyperlink==null){
            info.boardObject.transform.Find("Canvas").transform.Find("TextBackground").transform.Find("Hyperlink").gameObject.SetActive(false);
            info.boardObject.transform.Find("Canvas").transform.Find("TextBackground").transform.Find("HyperlinkButton").gameObject.SetActive(false);
        }
        else{
            info.boardObject.transform.Find("Canvas").transform.Find("TextBackground").transform.Find("HyperlinkButton").GetComponent<Button>().onClick.AddListener(delegate{HyperlinkButtonHandler(info.hyperlink);});
        }
        
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
        
        if(Global.chosanExperimentName == "1_complete_fec_e2e"){
            nodeName = "p0e0";
            nodeText = "FEC Booster - Parity packets are generated at this node";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete/ALV_Complete.p4";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p0a0";
            nodeText = "Lost packets are recovered with the help of parity packets here";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete/ALV_Complete.p4";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p0h0";
            nodeText = "This is the source host";
            hyperlink = null;
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p1h0";
            nodeText = "This is the destination host";
            hyperlink = null;
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "1_complete_mcd_e2e"){
            nodeName = "p0a0";
            nodeText = "MCD Booster - provides in-network caching of memcached entries.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete/ALV_Complete.p4";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p0h0";
            nodeText = "This is the client host";
            hyperlink = null;
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p1h0";
            nodeText = "This is the server host";
            hyperlink = null;
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "1_complete_hc_e2e"){
            nodeName = "p0e0";
            nodeText = "HC Booster - Header is compressed here";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete/ALV_Complete.p4";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p0a0";
            nodeText = "Header is decompressed here";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete/ALV_Complete.p4";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p0h0";
            nodeText = "This is the source host";
            hyperlink = null;
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p1h0";
            nodeText = "This is the destination host";
            hyperlink = null;
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "2_complete_all_e2e"){
            nodeName = "p0e0";
            nodeText = "All the three boosters are active - Forward Error Correction (FEC), Memcached (MCD), Header Compression (HC)";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete/ALV_Complete.p4";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p0a0";
            nodeText = "Lost packets are recovered. If requested packet is cached here, they will be dispatched towards the source host. Header is decompressed";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete/ALV_Complete.p4";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p0h0";
            nodeText = "This is the source host";
            hyperlink = null;
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p1h0";
            nodeText = "This is the destination host";
            hyperlink = null;
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "3_complete_e2e_1_hl3new"){
            nodeName = "p0e0";
            nodeText = "All the three boosters are active - Forward Error Correction (FEC), Memcached (MCD), Header Compression (HC). ";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete/ALV_Complete.p4";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "S1";
            nodeText = "This is a supporting device. Lost packets are recovered. If requested packet is cached here, they will be dispatched towards the source host. Header is decompressed.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits2/ALV_Complete_1_hl3new/ALV_Complete_split2_hl3new.p4";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p0h0";
            nodeText = "This is the source host";
            hyperlink = null;
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p1h0";
            nodeText = "This is the destination host";
            hyperlink = null;
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "3_complete_e2e_2_hl3new"){
            nodeName = "p0e0";
            nodeText = "All the three boosters are active - Forward Error Correction (FEC), Memcached (MCD), Header Compression (HC). ";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete/ALV_Complete.p4";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "S2_1";
            nodeText = "Lost packets are recovered.  Header is decompressed.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits2/ALV_Complete_2_hl3new/ALV_Complete_split2_hl3new_2.p4";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "S2_2";
            nodeText = "Decompress the header which was compressed on p0e0.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits2/ALV_Complete_2_hl3new/ALV_Complete_split2_hl3new_2.p4";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "S2_3";
            nodeText = "This acts as a Cache. If the requested packet is the cache, it is sent to the client.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits2/ALV_Complete_2_hl3new/ALV_Complete_split2_hl3new_2.p4";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p0h0";
            nodeText = "This is the source host";
            hyperlink = null;
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p1h0";
            nodeText = "This is the destination host";
            hyperlink = null;
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "5_complete_2_FW" || Global.chosanExperimentName == "Introduction"){
            nodeName = "D_FW_1";
            nodeText = "This is the firewall, resides on the supporting device connected to the switch p1e1.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete_2_FW/ALV_FW_split2.p4";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "6_split2_all"){
            nodeName = "p0h0";
            nodeText = "This is the source host";
            hyperlink = null;
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p1h0";
            nodeText = "This is the destination host";
            hyperlink = null;
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "7_split1"){
            nodeName = "SA_1";
            nodeText = "Supporting device to offload p0e0";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_split1/ALV_part2.p4";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "SA_2";
            nodeText = "Redundent supporting device to support failover";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_split1/ALV_part2.p4";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p0h0";
            nodeText = "This is the source host";
            hyperlink = null;
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p0h2";
            nodeText = "This is the destination host";
            hyperlink = null;
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

        }
        else if(Global.chosanExperimentName == "8_tunnel_base"){
            nodeName = "p0h3";
            nodeText = "This is the source host";
            hyperlink = null;
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p3h2";
            nodeText = "This is the destination host";
            hyperlink = null;
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "9_tunnel_encapsulated"){
            nodeName = "c0";
            nodeText = "Tunneling happens here. Packet is supposed to go towards p3a0 directly, but due to tunneling it goes towards p1a0.";
            hyperlink = "https://www.github.com/eniac/FlightplanWharf/splits3/ALV_bt/ALV_bt.p4";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p3a0";
            nodeText = "Tunneling happens here. Packet is supposed to go towards p3e1 directly, but due to tunneling it goes towards p3e0.";
            hyperlink = "https://www.github.com/eniac/FlightplanWharf/splits3/ALV_bt/ALV_bt.p4";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p0h3";
            nodeText = "This is the source host";
            hyperlink = null;
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p3h2";
            nodeText = "This is the destination host";
            hyperlink = null;
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "10_qos"){
            nodeName = "p0e1";
            nodeText = "Diffserv field is set to 44 here, to enable higher priority service in the network.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits3/ALV_qos/ALV_qos.p4";
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p0h3";
            nodeText = "This is the source host";
            hyperlink = null;
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));

            nodeName = "p3h2";
            nodeText = "This is the destination host";
            hyperlink = null;
            info.Add(nodeName, new Tuple<string, string>(nodeText, hyperlink));
        }

        
        return info;
    }

    Dictionary<int, Tuple<string, string, string>> TimeParser(){
        Dictionary<int, Tuple<string, string, string>> info = new Dictionary<int, Tuple<string, string, string>>();
        int time;
        string nodeName, nodeText, hyperlink;

        if(Global.chosanExperimentName == "1_complete_fec_e2e"){
            time = 3008916;
            nodeName = "p0e0";
            nodeText = "Now silver colored FEC packet will travel from p0e0 to p0a0.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete/ALV_Complete.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));

            time = 13032597;
            nodeName = "p0a0";
            nodeText = "p0a0 receives the FEC packet. FEC retrieves the lost packet. This can be seen as aditional blue packet moving out of p0a0";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete/ALV_Complete.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "1_complete_hc_e2e"){
            time = 3962;
            nodeName = "p0e0";
            nodeText = "header compression booster compresses header bytes. The packet turning from blue to pink shows that it is now a compressed packet.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete/ALV_Complete.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));

            time = 7094;
            nodeName = "p0a0";
            nodeText = "At p0e0 pink packet turns blue. This shows that the compressed packet has been decompressed now.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete/ALV_Complete.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "1_complete_mcd_e2e"){
            time = 105300;
            nodeName = "p0a0";
            nodeText = "The cached packets reside at p0a0. The orange packet reply from p0a0 offloads the target destination host and sends the cached copy of requested.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete/ALV_Complete.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "2_complete_all_e2e"){
            time = 4318;
            nodeName = "p0e0";
            nodeText = "header compression booster compresses header bytes. The packet turning from blue to pink shows that it is now a compressed packet.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete/ALV_Complete.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));

            time = 7025;
            nodeName = "p0a0";
            nodeText = "At p0e0 pink packet turns blue. This shows that the compressed packet has been decompressed now.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete/ALV_Complete.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));

            time = 8019205;
            nodeName = "p0e0";
            nodeText = "Now silver colored FEC packet will travel from p0e0 to p0a0.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete/ALV_Complete.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));

            time = 13033964;
            nodeName = "p0a0";
            nodeText = "p0a0 receives the FEC packet. FEC retrieves the lost packet. This can be seen as aditional blue packet moving out of p0a0";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete/ALV_Complete.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));

            time = 98824182;
            nodeName = "p0a0";
            nodeText = "The cached packets reside at p0a0. The orange packet reply from p0a0 offloads the target destination host and sends the cached copy of requested.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete/ALV_Complete.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "3_complete_e2e_1_hl3new"){
            time = 101832;
            nodeName = "p0e0";
            nodeText = "header compression booster compresses header bytes. The packet turning from blue to pink shows that it is now a compressed packet.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete/ALV_Complete.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));

            time = 104354;
            nodeName = "S1";
            nodeText = "At S1 pink packet turns blue. This shows that the compressed packet has been decompressed now.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits2/ALV_Complete_1_hl3new/ALV_Complete_split2_hl3new.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));

            time = 903726;
            nodeName = "p0e0";
            nodeText = "Now silver colored FEC packet will travel from p0e0 to p0a0.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete/ALV_Complete.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));

            time = 1408013;
            nodeName = "S1";
            nodeText = "S1 receives the FEC packet. FEC retrieves the lost packet. This can be seen as aditional blue packet moving out of p0a0";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits2/ALV_Complete_1_hl3new/ALV_Complete_split2_hl3new.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));

            time = 10291712;
            nodeName = "S1";
            nodeText = "The cached packets reside at S1. The orange packet reply from p0a0 offloads the target destination host and sends the cached copy of requested.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits2/ALV_Complete_1_hl3new/ALV_Complete_split2_hl3new.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "3_complete_e2e_2_hl3new"){
            time = 6348;
            nodeName = "p0e0";
            nodeText = "header compression booster compresses header bytes. The packet turning from blue to pink shows that it is now a compressed packet.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete/ALV_Complete.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));

            time = 16740;
            nodeName = "S2_2";
            nodeText = "At S2_2 pink packet turns blue. This shows that the compressed packet has been decompressed now.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits2/ALV_Complete_2_hl3new/ALV_Complete_split2_hl3new_2.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));

            time = 8030086;
            nodeName = "p0e0";
            nodeText = "Now silver colored FEC packet will travel from p0e0 to p0a0.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete/ALV_Complete.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));

            time = 13046449;
            nodeName = "S2_1";
            nodeText = "S2_1 receives the FEC packet. FEC retrieves the lost packet. This can be seen as aditional HC packet moving out of p0a0";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits2/ALV_Complete_2_hl3new/ALV_Complete_split2_hl3new_2.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));

            time = 98639076;
            nodeName = "S2_3";
            nodeText = "The cached packets reside at S2_3. The orange packet reply from p0a0 offloads the target destination host and sends the cached copy of requested.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits2/ALV_Complete_2_hl3new/ALV_Complete_split2_hl3new_2.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "5_complete_2_FW" || Global.chosanExperimentName == "Introduction"){
            time = 8256;
            nodeName = "D_FW_1";
            nodeText = "The packets are allowed by Firewall to continue their journey.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete_2_FW/ALV_FW_split2.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));

            time = 716224;
            nodeName = "D_FW_1";
            nodeText = "This packet is not allowed to pass through the Firewall. It is going to be dropped at p1e1 switch";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_Complete_2_FW/ALV_FW_split2.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));
        }
        // TODO
        else if(Global.chosanExperimentName == "6_split2_all"){
            // time = 1971;
            // nodeName = "D_V2_1";
            // nodeText = "The packet from p0e0 travels to SA_1 for processing [Supporting device SA_1 offloads p0e0]";
            // hyperlink = "https://flightplan.cis.upenn.edu/";
            // info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));

            // time = 29746104;
            // nodeName = "D_V2_1";
            // nodeText = "Since SA_1 has stopped working it is failed over by SA_2.";
            // hyperlink = "https://flightplan.cis.upenn.edu/";
            // info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "7_split1"){
            time = 1971;
            nodeName = "p0e0";
            nodeText = "This packet from p0e0 travels to SA_1 for processing [Supporting device SA_1 offloads p0e0]";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_split1/ALV_part2.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));

            time = 29746104;
            nodeName = "p0e0";
            nodeText = "Since SA_1 has stopped working it is failed over by SA_2.So this packet is going towards SA_2";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits/ALV_split1/ALV_part2.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "8_tunnel_base"){
            
        }
        else if(Global.chosanExperimentName == "9_tunnel_encapsulated"){
            time = 3418;
            nodeName = "c0";
            nodeText = "Tunneling happens here. Packet is supposed to go towards p3a0 directly, but due to tunneling it goes towards p1a0.";
            hyperlink = "https://www.github.com/eniac/FlightplanWharf/splits3/ALV_bt/ALV_bt.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));

            time = 6607;
            nodeName = "p3a0";
            nodeText = "Tunneling happens here. Packet is supposed to go towards p3e1 directly, but due to tunneling it goes towards p3e0.";
            hyperlink = "https://www.github.com/eniac/FlightplanWharf/splits3/ALV_bt/ALV_bt.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));
        }
        else if(Global.chosanExperimentName == "10_qos"){
            time = 1528;
            nodeName = "p0e1";
            nodeText = "Diffserv field is set to 44 here, to enable higher priority service in the network. Packet turns from blue to pink color.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits3/ALV_qos/ALV_qos.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));

            time = 12885;
            nodeName = "p0e1";
            nodeText = "enable higher priority service again in return journey of the packet. Packet turns from yellow to light blue color.";
            hyperlink = "https://www.github.com/eniac/Flightplan/Wharf/splits3/ALV_qos/ALV_qos.p4";
            info.Add(time, new Tuple<string, string, string>(nodeName, nodeText, hyperlink));
        }
        return info;
    }
}
