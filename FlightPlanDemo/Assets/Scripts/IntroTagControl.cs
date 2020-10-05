using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IntroTagControl : MonoBehaviour
{
    [SerializeField] GameObject introScreen = default;
    [SerializeField] GameObject introTag2Drd = default;
    [SerializeField] GameObject introTag2Dru = default;
    [SerializeField] GameObject introTag2Dld = default;
    [SerializeField] GameObject introTag2Dlu = default;
    [SerializeField] GameObject infoBox = default;
    [SerializeField] GameObject infoBoxDetail = default;
    [SerializeField] GameObject panelMenu = default;
    [SerializeField] GameObject footer = default; 
    [SerializeField] GameObject graph = default;
    [SerializeField] GameObject clickForCode = default;
    [SerializeField] private Camera cam = default;
    [SerializeField] private Topology topo = default;
    [SerializeField] private CameraRotate cameraRotate = default;

    enum TagType{
        T2Drd=0,
        T2Dru,
        T2Dld,
        T2Dlu,
        T3Drd,
        T3Dru,
        T3Dld,
        T3Dlu,
        Info,
        InfoDetail
    }

    struct TagInfo{
        public Text headText;
        public Text detailText;
    }
    Dictionary<TagType, TagInfo> tInfo = new Dictionary<TagType, TagInfo>();
    GameObject introTag3D; 
    GameObject introTag3Drd, introTag3Dru, introTag3Dld, introTag3Dlu; 
    TextMeshPro introTag3DText;
    int state = 0;  
    bool isInState = false;
    GameObject tagHideOnOK = null;
    public void IntroTagInit(Vector3 nodePos)
    {   
        DisableUpdate();
        if(Global.chosanExperimentName != "Introduction"){
            return;
        }

        // Intro Screen 
        introScreen.SetActive(false);
        introScreen.transform.transform.Find("Button").GetComponent<Button>().onClick.AddListener(delegate{OkButtonHandler();});
        introScreen.SetActive(true);

        // 2D Tags
        Tag2DInit(introTag2Drd, TagType.T2Drd);
        Tag2DInit(introTag2Dru, TagType.T2Dru);
        Tag2DInit(introTag2Dld, TagType.T2Dld);
        Tag2DInit(introTag2Dlu, TagType.T2Dlu);

        // 3D Tags
        introTag3Drd = Tag3DInit("IntroTag3Drd", TagType.T3Drd);
        introTag3Dru = Tag3DInit("IntroTag3Dru", TagType.T3Dru);
        introTag3Dld = Tag3DInit("IntroTag3Dld", TagType.T3Dld);
        introTag3Dlu = Tag3DInit("IntroTag3Dlu", TagType.T3Dlu);

        // Info box
        InfoBoxInit(infoBox, TagType.Info);
        InfoBoxInit(infoBoxDetail, TagType.InfoDetail);

        EnableUpdate();
    }

    void Tag2DInit(GameObject tag2D, TagType type){
        TagInfo info = new TagInfo();
        tag2D.SetActive(false);
        tag2D.transform.Find("Background").transform.Find("Button").GetComponent<Button>().onClick.AddListener(delegate{OkButtonHandler();});
        info.headText = tag2D.transform.Find("Background").transform.Find("HeadingText").GetComponent<Text>();
        info.detailText = tag2D.transform.Find("Background").transform.Find("TextBackground").transform.Find("Text").GetComponent<Text>();
        tInfo.Add(type, info);
    }

    GameObject Tag3DInit(string tagPrefab, TagType type){
        TagInfo info = new TagInfo();
        GameObject prefabTag3D = Resources.Load(tagPrefab) as GameObject;
        GameObject tag3D = Instantiate(prefabTag3D) as GameObject;
        tag3D.SetActive(false);
        tag3D.transform.Find("Canvas").transform.Find("Button").GetComponent<Button>().onClick.AddListener(delegate{OkButtonHandler();});
        info.headText = tag3D.transform.Find("Canvas").transform.Find("HeadingText").GetComponent<Text>();
        info.detailText = tag3D.transform.Find("Canvas").transform.Find("TextBackground").transform.Find("Text").GetComponent<Text>();
        tInfo.Add(type, info);
        return tag3D;
    }

    void InfoBoxInit(GameObject infoBox, TagType type){
        TagInfo info = new TagInfo();
        infoBox.SetActive(false);
        infoBox.transform.Find("Background").transform.Find("Button").GetComponent<Button>().onClick.AddListener(delegate{OkButtonHandler();});
        info.headText = null;
        info.detailText = infoBox.transform.Find("Background").transform.Find("Text").GetComponent<Text>();
        tInfo.Add(type, info);
    }

    void Update(){
        if(Global.chosanExperimentName != "Introduction"){
            return;
        }
        Vector3 pos;
        string head = null, detail=null;

        IntroTag3DFollowCam();

        if(isInState){
            return;
        }
        switch(state){
            case 0:
                isInState = true;
                break;

            case 1:
                introScreen.SetActive(false);
                pos = panelMenu.transform.position;
                head = "Menu";
                detail = "The menu can be expanded and collaped by clicking this button.";
                TagUpdate(introTag2Drd, TagType.T2Drd, pos, head, detail);
                break;

            case 2:
                introTag2Drd.SetActive(false);
                pos = footer.transform.Find("TimeSlider").transform.position;
                head = "Time Slider";
                detail = "The slider shows the progress of the experiment over time.";
                TagUpdate(introTag2Dru, TagType.T2Dru, pos, head, detail);
                break;

            case 3:
                pos = footer.transform.Find("ElapsedTime").transform.position;
                head = "Elapsed Time";
                detail = "This shows the elapsed time since the experiment's start.";
                TagUpdate(introTag2Dru, TagType.T2Dru, pos, head, detail);
                break;

            case 4:
                introTag2Dru.SetActive(false);
                pos = footer.transform.Find("RemainingTime").transform.position;
                head = "Remaining Time";
                detail = "This shows the remaining time for the experiment.";
                TagUpdate(introTag2Dlu, TagType.T2Dlu, pos, head, detail);
                break;

            case 5:
                introTag2Dru.SetActive(false);
                pos = footer.transform.Find("SpeedSlider").transform.position;
                head = "Speed Slider";
                detail = "Use this to change the display speed of the experiment's visualisation.";
                TagUpdate(introTag2Dlu, TagType.T2Dlu, pos, head, detail);
                break;

            case 6:
                pos = graph.transform.position;
                head = "Graph";
                detail = "This shows relevant quantitative information from the experiment.";
                TagUpdate(introTag2Dlu, TagType.T2Dlu, pos, head, detail);
                break;

            case 7:
                introTag2Dlu.SetActive(false);
                pos = graph.transform.Find("PacketLegendText").transform.position + new Vector3(70f, 80f, 0);
                head = "Packet Legend";
                detail = "Packet color legend to identify packets in the visualisation.";
                TagUpdate(introTag2Dld, TagType.T2Dld, pos, head, detail);
                break;

            case 8:
                introTag2Dld.SetActive(false);
                pos = clickForCode.transform.position;
                head = "Code";
                detail = "Get more details about each experiment by clicking here.";
                TagUpdate(introTag2Dld, TagType.T2Dld, pos, head, detail);
                break;

            case 9:
                introTag2Dld.SetActive(false);
                pos = topo.GetNodePosition("p0e0") + new Vector3(0, 1.0f, -1.5f);
                head = "Edge Switch";
                detail = "(p_e_) The Switch that is directly connected to the host is called edge switch.";
                TagUpdate(introTag3Dlu, TagType.T3Dlu, pos, head, detail);
                break;

            case 10:
                introTag3Dlu.SetActive(false);
                pos = topo.GetNodePosition("p0a1") + new Vector3(0, 1.0f, -1.5f);
                head = "Aggregation Switch";
                detail = "(p_a_) These switches interconnect edge and core switches.";
                TagUpdate(introTag3Dlu, TagType.T3Dlu, pos, head, detail);
                break;

            case 11:
                introTag3Dlu.SetActive(false);
                pos = topo.GetNodePosition("c2") + new Vector3(0, 1.0f, -1.5f);
                head = "Core Switch";
                detail = "(c_) Core switches interconnect aggregation switches.";
                TagUpdate(introTag3Dlu, TagType.T3Dlu, pos, head, detail);
                break;

            case 12:
                introTag3Dlu.SetActive(false);
                pos = topo.GetNodePosition("D_V2_1") + new Vector3(0, 0.5f, -1f);
                head = "<size=60>Supporting Device</size>";
                detail = "A switch may offload part of its program to a supporting device such as this.";
                TagUpdate(introTag3Dru, TagType.T3Dru, pos, head, detail);
                break;

            case 13:
                introTag3Dru.SetActive(false);
                pos = ((topo.GetNodePosition("c3") - topo.GetNodePosition("p0a1"))/2.0f) + topo.GetNodePosition("p0a1"); 
                head = "Link";
                detail = "Links connect switches and host together.";
                TagUpdate(introTag3Dru, TagType.T3Dru, pos, head, detail);
                break;

            case 14:
                introTag3Dru.SetActive(false);
                pos = ((topo.GetNodePosition("p0a0") - topo.GetNodePosition("p0e0"))/4.0f) + topo.GetNodePosition("p0e0"); 
                head = "Lossy Link";
                detail = "Lossy Link may drop packets at random because of a hardware fault.";
                TagUpdate(introTag3Dlu, TagType.T3Dlu, pos, head, detail);
                break;

            case 15:
                introTag3Dlu.SetActive(false);
                pos = topo.GetNodePosition("D_FW_1") + new Vector3(0, 0.5f, -1f); 
                head = "Info Tags";
                detail = "Additional information can be obtained by clicking on red beacons such as this.";
                TagUpdate(introTag3Dru, TagType.T3Dru, pos, head, detail);
                cameraRotate.DoRotate(new Quaternion(0,-0.3f,0,1.0f));
                break;

            case 16:
                introTag3Dru.SetActive(false);
                pos = infoBox.transform.position; 
                head = null;
                detail = "3D model can be zoomed in and out using mouse scroll.";
                TagUpdate(infoBox, TagType.Info, pos, head, detail);
                break;

            case 17:
                pos = infoBox.transform.position + new Vector3(10f, 10f, 0f); 
                head = null;
                detail = "Model can be rotated on its axis using mouse click and drag.";
                TagUpdate(infoBox, TagType.Info, pos, head, detail);
                break;

            case 18:
                infoBox.SetActive(false);
                pos = infoBoxDetail.transform.position; 
                head = null;
                detail = "This experiment tests the firewall's effectiveness. The firewall part of the P4 program has been offloaded to Device D_FW_1. The experiment consists of both positive and negative tests. It starts with a series of positive tests -- i.e., involving packets that we expect the firewall to let through. This is followed by negative tests, where we expect the firewall to block packets.";
                TagUpdate(infoBoxDetail, TagType.InfoDetail, pos, head, detail);
                break;

            case 19:
                infoBoxDetail.SetActive(false);
                pos = infoBox.transform.position  + new Vector3(-10f, -10f, -0f); 
                head = null;
                detail = "The 'Introduction' is over. Please click on the 'play' button in the bottom-left corner of the screen to start the visualisation.";
                TagUpdate(infoBox, TagType.Info, pos, head, detail);
                break;

            default:
                infoBox.SetActive(false);
                // isInState = true;
                DisableUpdate();
                break;
        }
    }

    void TagUpdate(GameObject tag2D, TagType type, Vector3 pos, string head, string detail){
        tag2D.SetActive(true);
        tag2D.transform.position = pos;
        if(tInfo[type].headText != null){
            tInfo[type].headText.text = head;
        }
        tInfo[type].detailText.text = detail;
        isInState = true;
    }

    void OkButtonHandler(){
        state++;
        isInState = false;
        if(tagHideOnOK != null){
            tagHideOnOK.SetActive(false);
            tagHideOnOK = null;
        }
    }

    void IntroTag3DFollowCam(){
        Vector3 targetPos = new Vector3(cam.gameObject.transform.position.x, introTag3Drd.transform.position.y, cam.gameObject.transform.position.z);
        introTag3Drd.transform.LookAt(targetPos);

        targetPos = new Vector3(cam.gameObject.transform.position.x, introTag3Dru.transform.position.y, cam.gameObject.transform.position.z);
        introTag3Dru.transform.LookAt(targetPos);

        targetPos = new Vector3(cam.gameObject.transform.position.x, introTag3Dld.transform.position.y, cam.gameObject.transform.position.z);
        introTag3Dld.transform.LookAt(targetPos);

        targetPos = new Vector3(cam.gameObject.transform.position.x, introTag3Dlu.transform.position.y, cam.gameObject.transform.position.z);
        introTag3Dlu.transform.LookAt(targetPos);
    }


    public void DetectEventTag(int time){
        // Vector3 pos;
        // string head = null, detail=null;
        // if(time == 413491){
        //     pos = graph.transform.position; 
        //     head = "Yellow Curve";
        //     detail = "curve is showing something";
        //     TagUpdate(introTag2Dlu, TagType.T2Dlu, pos, head, detail);
        //     tagHideOnOK = introTag2Dlu;
        // }
        // else if(time == 4997492){
        //     pos = graph.transform.position; 
        //     head = "Green Curve";
        //     detail = "curve is showing something";
        //     TagUpdate(introTag2Dlu, TagType.T2Dlu, pos, head, detail);
        //     tagHideOnOK = introTag2Dlu;
        // }
    }

    void EnableUpdate(){
        enabled = true;
    }

    void DisableUpdate(){
        enabled = false;
    }

}
