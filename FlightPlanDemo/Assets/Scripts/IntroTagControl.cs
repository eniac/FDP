using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IntroTagControl : MonoBehaviour
{
    [SerializeField] GameObject introScreen = default;
    [SerializeField] GameObject introTag2D = default;
    [SerializeField] GameObject introTag2Drd = default;
    [SerializeField] GameObject introTag2Dru = default;
    [SerializeField] GameObject introTag2Dld = default;
    [SerializeField] GameObject introTag2Dlu = default;
    [SerializeField] GameObject panelMenu = default;
    [SerializeField] GameObject footer = default; 
    [SerializeField] GameObject graph = default;
    [SerializeField] private Camera cam = default;
    [SerializeField] private Topology topo = default;

    enum TagType{
        T2Drd=0,
        T2Dru,
        T2Dld,
        T2Dlu,
        T3Drd,
        T3Dru,
        T3Dld,
        T3Dlu
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
        introScreen.transform.Find("Background").transform.Find("Button").GetComponent<Button>().onClick.AddListener(delegate{OkButtonHandler();});
        introScreen.SetActive(true);

        // 2D Tags
        // introTag2D.SetActive(false);
        // introTag2D.transform.Find("Button").GetComponent<Button>().onClick.AddListener(delegate{OkButtonHandler();});
        Tag2DInit(introTag2Drd, TagType.T2Drd);
        Tag2DInit(introTag2Dru, TagType.T2Dru);
        Tag2DInit(introTag2Dld, TagType.T2Dld);
        Tag2DInit(introTag2Dlu, TagType.T2Dlu);

        // 3D Tags
        // GameObject prefabTag3D = Resources.Load("IntroTag3D") as GameObject;
        // introTag3D = Instantiate(prefabTag3D) as GameObject;
        // introTag3D.SetActive(false);
        // introTag3DText = introTag3D.transform.Find("Text").transform.GetComponent<TextMeshPro>();
        // introTag3D.transform.Find("Canvas").transform.Find("Button").GetComponent<Button>().onClick.AddListener(delegate{OkButtonHandler();});
        
        // GameObject prefabTag3Drd = Resources.Load("IntroTag3Drd") as GameObject;
        // introTag3Drd = Instantiate(prefabTag3Drd) as GameObject;
        // introTag3Drd.SetActive(false);
        // introTag3DText = introTag3D.transform.Find("Text").transform.GetComponent<TextMeshPro>();
        // introTag3Drd.transform.Find("Background").transform.Find("Canvas").transform.Find("Button").GetComponent<Button>().onClick.AddListener(delegate{OkButtonHandler();});
        
        introTag3Drd = Tag3DInit("IntroTag3Drd", TagType.T3Drd);
        introTag3Dru = Tag3DInit("IntroTag3Dru", TagType.T3Dru);
        introTag3Dld = Tag3DInit("IntroTag3Dld", TagType.T3Dld);
        introTag3Dlu = Tag3DInit("IntroTag3Dlu", TagType.T3Dlu);


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
                // introTag2D.SetActive(true);
                // introTag2D.transform.position = panelMenu.transform.position;
                pos = panelMenu.transform.position;
                head = "Menu";
                detail = "All the menu button can be expand and collaps by clicking on this button";
                TagUpdate(introTag2Drd, TagType.T2Drd, pos, head, detail);
                break;

            case 2:
                // introTag2D.transform.position = footer.transform.Find("TimeSlider").transform.position;
                introTag2Drd.SetActive(false);
                pos = footer.transform.Find("TimeSlider").transform.position;
                head = "Time Slider";
                detail = "Slider to see the different part of animation any time";
                TagUpdate(introTag2Dru, TagType.T2Dru, pos, head, detail);
                break;

            case 3:
                pos = footer.transform.Find("ElapsedTime").transform.position;
                head = "Elapsed Time";
                detail = "This shows the elapsed animation time since start";
                TagUpdate(introTag2Dru, TagType.T2Dru, pos, head, detail);
                break;

            case 4:
                introTag2Dru.SetActive(false);
                pos = footer.transform.Find("RemainingTime").transform.position;
                head = "Remaining Time";
                detail = "This shows the remaining time of animation";
                TagUpdate(introTag2Dlu, TagType.T2Dlu, pos, head, detail);
                break;

            case 5:
                introTag2Dru.SetActive(false);
                pos = footer.transform.Find("SpeedSlider").transform.position;
                head = "Speed Slider";
                detail = "Speed of animation can be governed by this";
                TagUpdate(introTag2Dlu, TagType.T2Dlu, pos, head, detail);
                break;

            case 6:
                pos = graph.transform.position;
                head = "Graph";
                detail = "This is run time graph. Graph progresses as the animation progresses";
                TagUpdate(introTag2Dlu, TagType.T2Dlu, pos, head, detail);
                break;

            case 7:
                introTag2Dlu.SetActive(false);
                pos = topo.GetNodePosition("c3") + new Vector3(0, 1.5f, 0);
                head = "Switch";
                detail = "Switch breif description is here.";
                TagUpdate(introTag3Dru, TagType.T3Dru, pos, head, detail);
                break;

            case 8:
                introTag3Dru.SetActive(false);
                pos = topo.GetNodePosition("D_V2_1") + new Vector3(0, 1f, 0);
                head = "Satellite";
                detail = "Switch breif description is here.";
                TagUpdate(introTag3Dru, TagType.T3Dru, pos, head, detail);
                break;

            case 9:
                introTag3Dru.SetActive(false);
                pos = ((topo.GetNodePosition("c3") - topo.GetNodePosition("p0a1"))/2.0f) + topo.GetNodePosition("p0a1"); 
                head = "Link";
                detail = "Link breif description is here.";
                TagUpdate(introTag3Dru, TagType.T3Dru, pos, head, detail);
                break;

            case 10:
                introTag3Dru.SetActive(false);
                pos = ((topo.GetNodePosition("p0a0") - topo.GetNodePosition("p0e0"))/4.0f) + topo.GetNodePosition("p0e0"); 
                head = "Lossy Link";
                detail = "Lossy Link breif description is here.";
                TagUpdate(introTag3Dlu, TagType.T3Dlu, pos, head, detail);
                break;


            default:
                introTag3Dlu.SetActive(false);
                DisableUpdate();
                break;
        }
    }

    void TagUpdate(GameObject tag2D, TagType type, Vector3 pos, string head, string detail){
        tag2D.SetActive(true);
        tag2D.transform.position = pos;
        tInfo[type].headText.text = head;
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
