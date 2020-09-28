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
    [SerializeField] GameObject panelMenu = default;
    [SerializeField] GameObject footer = default; 
    [SerializeField] GameObject graph = default;
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
        Info
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
                detail = "The slider shows the progress of the experiment across time.";
                TagUpdate(introTag2Dru, TagType.T2Dru, pos, head, detail);
                break;

            case 3:
                pos = footer.transform.Find("ElapsedTime").transform.position;
                head = "Elapsed Time";
                detail = "This shows the elapsed animation time since the animation start.";
                TagUpdate(introTag2Dru, TagType.T2Dru, pos, head, detail);
                break;

            case 4:
                introTag2Dru.SetActive(false);
                pos = footer.transform.Find("RemainingTime").transform.position;
                head = "Remaining Time";
                detail = "This shows the remaining time of animation.";
                TagUpdate(introTag2Dlu, TagType.T2Dlu, pos, head, detail);
                break;

            case 5:
                introTag2Dru.SetActive(false);
                pos = footer.transform.Find("SpeedSlider").transform.position;
                head = "Speed Slider";
                detail = "Speed of animation can be changed here.";
                TagUpdate(introTag2Dlu, TagType.T2Dlu, pos, head, detail);
                break;

            case 6:
                pos = graph.transform.position;
                head = "Graph";
                detail = "This shows relevant quantitative information from the experiment on which the animation is based.";
                TagUpdate(introTag2Dlu, TagType.T2Dlu, pos, head, detail);
                break;

            case 7:
                introTag2Dlu.SetActive(false);
                pos = graph.transform.Find("PacketLegendText").transform.position + new Vector3(70f, 80f, 0);
                head = "Packet Legend";
                detail = "Packet color legend to identify the packets in animation";
                TagUpdate(introTag2Dld, TagType.T2Dld, pos, head, detail);
                break;

            case 8:
                introTag2Dld.SetActive(false);
                pos = topo.GetNodePosition("c3") + new Vector3(0, 1.5f, 0);
                head = "Switch";
                detail = "A switch forwards and transforms packets under the guidance of P4 program.";
                TagUpdate(introTag3Dru, TagType.T3Dru, pos, head, detail);
                break;

            case 9:
                introTag3Dru.SetActive(false);
                pos = topo.GetNodePosition("D_V2_1") + new Vector3(0, 0.5f, -1f);
                head = "<size=60>Supporting Device</size>";
                detail = "Switch may offload part of their program to supporting devices.";
                TagUpdate(introTag3Dru, TagType.T3Dru, pos, head, detail);
                break;

            case 10:
                introTag3Dru.SetActive(false);
                pos = ((topo.GetNodePosition("c3") - topo.GetNodePosition("p0a1"))/2.0f) + topo.GetNodePosition("p0a1"); 
                head = "Link";
                detail = "Links connect switches and host together.";
                TagUpdate(introTag3Dru, TagType.T3Dru, pos, head, detail);
                break;

            case 11:
                introTag3Dru.SetActive(false);
                pos = ((topo.GetNodePosition("p0a0") - topo.GetNodePosition("p0e0"))/4.0f) + topo.GetNodePosition("p0e0"); 
                head = "Lossy Link";
                detail = "Lossy Link may drop packets at random because of hardware fault.";
                TagUpdate(introTag3Dlu, TagType.T3Dlu, pos, head, detail);
                break;

            case 12:
                introTag3Dlu.SetActive(false);
                pos = topo.GetNodePosition("D_FW_1") + new Vector3(0, 0.5f, -1f); 
                head = "Info Tags";
                detail = "Additional information can be seen by clicking on those nodes with a red color square.";
                TagUpdate(introTag3Dru, TagType.T3Dru, pos, head, detail);
                cameraRotate.DoRotate(new Quaternion(0,-0.3f,0,1.0f));
                break;

            case 13:
                introTag3Dru.SetActive(false);
                pos = infoBox.transform.position; 
                head = null;
                detail = "3D model can be zoomed in and out using mouse scroll.";
                TagUpdate(infoBox, TagType.Info, pos, head, detail);
                break;

            case 14:
                pos = infoBox.transform.position + new Vector3(10f, 10f, 0f); 
                head = null;
                detail = "Model can be rotated on its axis using mouse click and drag.";
                TagUpdate(infoBox, TagType.Info, pos, head, detail);
                break;

            case 15:
                pos = infoBox.transform.position  + new Vector3(-10f, -10f, -0f); 
                head = null;
                detail = "The 'Introduction' is over. Please click on the 'play' button in the bottom-left corner of the screen to start the animation.";
                TagUpdate(infoBox, TagType.Info, pos, head, detail);
                break;

            default:
                infoBox.SetActive(false);
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

    void EnableUpdate(){
        enabled = true;
    }

    void DisableUpdate(){
        enabled = false;
    }

}
