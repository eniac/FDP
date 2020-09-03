using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BillBoardControl : MonoBehaviour
{
    struct BillBoardNodeInfo{
        public string boardText; // text on board
        public Vector3 boardPos;   // Position of board
        public GameObject boardObject; // Object of Board
    };
    [SerializeField] private Camera cam = default;
    [SerializeField] private Topology topo = default;
    [SerializeField] AnimControl anim = default;
    Dictionary<string, BillBoardNodeInfo> boardInfo = new Dictionary<string, BillBoardNodeInfo>();  // Node name : BillBoardNodeInfo
    Global.AnimStatus animStatusBeforeBoard = Global.AnimStatus.Forward;
    public void BillBoardInit(){
        UpdateDisable();
        GetBillBoardInfo();
        UpdateEnable();
    }

    void GetBillBoardInfo(){
        Dictionary<string, string> nodeInfo = new Dictionary<string, string>();
        GameObject bubble_prefab = Resources.Load("BillBoard1") as GameObject;
        if(Global.chosanExperimentName == "7_split1"){
            nodeInfo.Add("p0a0", "p0a0: sample text sample text <link=\"www.google.com\">www.google.com</link>");
            nodeInfo.Add("FPoffload", "FPoffload: sample text sample text sample text sample text sample text");
            nodeInfo.Add("FPoffload2", "FPoffload2: sample text sample text sample text sample text sample text");
        }
        else if(Global.chosanExperimentName == "complete_fec_e2e"){
            nodeInfo.Add("p0a0", "p0a0: sample text sample text sample text sample text sample text");
        }

        foreach(var node in nodeInfo.Keys){
            BillBoardNodeInfo info = new BillBoardNodeInfo();
            info.boardText = nodeInfo[node];
            info.boardPos = topo.GetNodePosition(node) + new Vector3(0, 0.5f, 0);
            info.boardObject = Instantiate(bubble_prefab) as GameObject;
            info.boardObject.transform.position = info.boardPos;
            info.boardObject.transform.Find("Text").transform.GetComponent<TextMeshPro>().text = info.boardText;
            info.boardObject.transform.Find("Canvas").transform.Find("Button").GetComponent<Button>().onClick.AddListener(delegate{ButtonHandler(node);});
            info.boardObject.SetActive(false);
            boardInfo.Add(node, info);
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        BillBoardFollowCam();
        DetectMouseClick();
    }

    void BillBoardFollowCam(){
        foreach(var node in boardInfo.Keys){
            Vector3 targetPos = new Vector3(cam.gameObject.transform.position.x, boardInfo[node].boardObject.transform.position.y, cam.gameObject.transform.position.z);
            boardInfo[node].boardObject.transform.LookAt(targetPos);
        }
    }

    void DetectMouseClick(){
        if(Input.GetMouseButtonDown(0)){
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if(Physics.Raycast(ray, out hit, 100.0f)){
                if(hit.transform != null && boardInfo.ContainsKey(hit.collider.gameObject.name)){
                    animStatusBeforeBoard = anim.GetAnimStatus();
                    anim.Pause();
                    boardInfo[hit.collider.gameObject.name].boardObject.SetActive(true);
                }
            }
        }
    }

    void SetAnimStatusAfterBoard(){
        if(animStatusBeforeBoard == Global.AnimStatus.Pause){
            anim.Pause();
        }
        else if(animStatusBeforeBoard == Global.AnimStatus.Forward){
            anim.Forward();
        }
        else if(animStatusBeforeBoard == Global.AnimStatus.Rewind){
            anim.Rewind();
        }
    }

    void ButtonHandler(string node){
        boardInfo[node].boardObject.SetActive(false);
        SetAnimStatusAfterBoard();
    }
    void UpdateEnable(){
        enabled = true;
    }
    void UpdateDisable(){
        enabled = false;
    }
}
