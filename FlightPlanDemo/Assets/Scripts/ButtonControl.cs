using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ButtonControl : MonoBehaviour
{
    [SerializeField] private GameObject panelFooter = default;
    [SerializeField] private GameObject panelMenu = default;
    [SerializeField] private Animator SettingAnimator = default;
    [SerializeField] private Topology topo = default;
    [SerializeField] private Text messageText = default;
    [SerializeField] private Dropdown colorPatternDropdown = default;
    [SerializeField] private InputField searchField = default;
    [SerializeField] private PopUpControl popup = default;
    [SerializeField] private BillBoardControl billBoard = default;
    // [SerializeField] private AnimationControl anim = default;
    [SerializeField] private AnimControl anim = default;
    [SerializeField] private ColorControl colorControl = default;
    bool slideIn = false;
    bool showLable;
    List<GameObject> hostTextObject;
    List<GameObject> textObj;
    bool showLinkOpaque;
    bool showNodeOpaque;
    bool showBlink = true;
    bool showTagMarker = true;
    bool showEventTag = true;
    List<GameObject> linkObject;
    List<GameObject> switchObject;
    List<GameObject> satObject;
    List<string> colorPatterns = new List<string>(){"Origin based Color", "Request/Reply Color", "Path based Color"};

    // Start is called before the first frame update
    void Start()
    {
        showLable = true;
        showLinkOpaque = true;
        showNodeOpaque = true;
        hostTextObject = topo.GetHostTextObjects();
        textObj = topo.GetTextObjects();
        linkObject = topo.GetLinkObjects();
        PopulateColorPatternDropdown();
        popup.PopUpInit(messageText);
    }

    void Update(){
        ProcessOpacityChange();
        if(anim.GetUpdateStatus()==false){
            ChangePauseResumeButtonText(Global.AnimStatus.Pause);
        }
    }

    public void Settings(){
        if(slideIn == false){
            SettingAnimator.gameObject.transform.GetComponentInChildren<Text>().text = "v";
            SettingAnimator.SetBool("Slidein", true);
            slideIn = true;
        }
        else{
            SettingAnimator.gameObject.transform.GetComponentInChildren<Text>().text = ">";
            SettingAnimator.SetBool("Slidein", false);
            slideIn = false;
        }
    }
    public void ToggleLables(){
        if(showLable == true){
            SetMenuButtonText("ToggleLabels", "Show Labels");
            foreach(var obj in textObj){
                obj.SetActive(false);
            }
            foreach(var obj in hostTextObject){
                obj.SetActive(false);
            }
            showLable = false;
        }
        else{
            SetMenuButtonText("ToggleLabels", "Hide Labels");
            foreach(var obj in textObj){
                obj.SetActive(true);
            }
            foreach(var obj in hostTextObject){
                obj.SetActive(true);
            }
            showLable = true;
        } 
    }  

    public void ProcessOpacityChange(){
        if(showLinkOpaque != topo.GetLinkOpacity()){
            showLinkOpaque = topo.GetLinkOpacity();
            if(showLinkOpaque==true){
                SetMenuButtonText("ToggleLinkOpacity", "Translucent Links");
            }
            else{
                SetMenuButtonText("ToggleLinkOpacity", "Opaque Links");
            }
        }
        if(showNodeOpaque != topo.GetNodeOpacity()){
            showNodeOpaque = topo.GetNodeOpacity();
            if(showNodeOpaque==true){
                SetMenuButtonText("ToggleNodeOpacity", "Translucent Nodes");
            }
            else{
                SetMenuButtonText("ToggleNodeOpacity", "Opaque Nodes");
            }
        }
    }

    public void ToggleLinkTransparency(){
        if(showLinkOpaque == true){
            // Here a = alpha = opacity (0.0 transparent, 1.0 opaque)
            SetMenuButtonText("ToggleLinkOpacity", "Opaque Links");
            topo.MakeLinksTransparent();
            showLinkOpaque = false;
        }
        else{
            SetMenuButtonText("ToggleLinkOpacity", "Translucent Links");
            topo.MakeLinksOpaque();
            showLinkOpaque = true;
        }
    }

    public void ToggleNodeTransparency(){
        if(showNodeOpaque == true){
            // Here a = alpha = opacity (0.0 transparent, 1.0 opaque)
            SetMenuButtonText("ToggleNodeOpacity", "Opaque Nodes");
            topo.MakeNodesTransparent();
            showNodeOpaque = false;
        }
        else{
            SetMenuButtonText("ToggleNodeOpacity", "Translucent Nodes");
            topo.MakeNodesOpaque();
            showNodeOpaque = true;
        }
    }

    public void GetNodeString(){
        // Get the string from UI input field
        string nodeString = searchField.text;
        // Debug.Log("Node string = " + nodeString);
        // Clear the UI input field
        searchField.text = "";
        // Process request
        string invalidString = topo.ProcessSearchRequest(nodeString);
        // Debug.Log("invalid Nodes = "+invalidString);
        if(invalidString!=null){
            popup.ShowErrorMessage("{ " + invalidString + "} are invalid nodes !!!", 8, Color.red);
        }
    }

    public void ClearHighlightedNodes(){
        if(topo.ProcessClearRequest()==true){
            popup.ShowErrorMessage("Cleared Previous Highlights", 8, Color.green);
        }
    }

    void PopulateColorPatternDropdown(){
        colorPatternDropdown.AddOptions(colorPatterns);
    }
    public void ColorPatternDropdownIndexChanged(int index){
        colorControl.SetColorPattern((Global.ColorPattern)index);
    }

    public void StartAnimation(){
        // billBoard.ResetTimeInfo();
        // anim.StartAnimation();
        // ChangePauseResumeButtonText(Global.AnimStatus.Forward);
    }

    public void ResetAnimation(){
        // anim.ResetAnimation();
        anim.Stop();
    }

    public void PauseResumeAnimation(){
        // anim.Pause();
        Global.AnimStatus status = anim.Pause();
        ChangePauseResumeButtonText(status);
    }

    public void ForwardAnimation(){
        anim.Forward();
        ChangePauseResumeButtonText(Global.AnimStatus.Forward);
    }

    public void RewindAnimation(){
        anim.Rewind();
        ChangePauseResumeButtonText(Global.AnimStatus.Rewind);
    }

    void ChangePauseResumeButtonText(Global.AnimStatus status){
        if(status == Global.AnimStatus.Pause){
            // Show Resume Symbol on Button
            panelFooter.transform.Find("P").GetComponentInChildren<Text>().text = ">";
        }
        else{
            // Show Pause sysmbol on button
            panelFooter.transform.Find("P").GetComponentInChildren<Text>().text = "||";
        }
    }

    public void BackToStart(){
        // Reset time
        // anim.ResetFixedDeltaTime();
        // Jump to the previous scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    } 

    public void ToggleLossyLinkBlink(){
        if(showBlink == true){
            SetMenuButtonText("ToggleLossyLinkBlink", "Show Lossy Link Blink");
            anim.StopLossyBlink();
            showBlink = false;
        }
        else{
            SetMenuButtonText("ToggleLossyLinkBlink", "Hide Lossy Link Blink");
            anim.ShowLossyBlink();
            showBlink = true;
        }
    }

    public void Hyperlink(){
        string link = ParseCodeLink();
        if( Application.platform==RuntimePlatform.WebGLPlayer )
        {
            // Application.ExternalEval("window.open(\"https://flightplan.cis.upenn.edu/\")");
            Application.ExternalEval("window.open(\"" + link + "\")");
        }
        else{
            Application.OpenURL(link);
        }
    }

    string ParseCodeLink(){
        string codeLink = "https://flightplan.cis.upenn.edu/";
        if(Global.chosanExperimentName == "FEC_booster"){
           codeLink = "https://www.github.com/eniac/Flightplan/tree/master/Wharf/splits/ALV_Complete/";
        }
        else if(Global.chosanExperimentName == "MCD_booster"){
            codeLink = "https://www.github.com/eniac/Flightplan/tree/master/Wharf/splits/ALV_Complete/";
        }
        else if(Global.chosanExperimentName == "HC_booster"){
            codeLink = "https://www.github.com/eniac/Flightplan/tree/master/Wharf/splits/ALV_Complete/";
        }
        else if(Global.chosanExperimentName == "Crosspod:_FEC,_HC,_and_MCD_boosters"){
           codeLink = "https://www.github.com/eniac/Flightplan/tree/master/Wharf/splits/ALV_Complete/";
        }
        else if(Global.chosanExperimentName == "Split_Crosspod_into_3"){
           codeLink = "https://www.github.com/eniac/Flightplan/tree/master/Wharf/splits2/ALV_Complete_1_hl3new/";
        }
        else if(Global.chosanExperimentName == "Split_Crosspod_into_6"){
            codeLink = "https://www.github.com/eniac/Flightplan/tree/master/Wharf/splits2/ALV_Complete_2_hl3new/";
        }
        else if(Global.chosanExperimentName == "5_complete_2_FW" || Global.chosanExperimentName == "Introduction"){
            codeLink = "https://www.github.com/eniac/Flightplan/tree/master/Wharf/splits/ALV_Complete_2_FW/";
        }
        else if(Global.chosanExperimentName == "Figure_7"){
            codeLink = "https://www.github.com/eniac/Flightplan/tree/master/Wharf/splits2/ALV_Complete_All/";
        }
        else if(Global.chosanExperimentName == "Failover_mechanism"){
            codeLink = "https://www.github.com/eniac/Flightplan/tree/master/Wharf/splits/ALV_split1/";
        }
        else if(Global.chosanExperimentName == "Untunneled_traffic"){
            codeLink = "https://www.github.com/eniac/Flightplan/tree/master/Wharf/splits3/ALV_bt/";
        }
        else if(Global.chosanExperimentName == "Tunneled_traffic"){
            codeLink = "https://www.github.com/eniac/Flightplan/tree/master/Wharf/splits3/ALV_bt/";
        }
        else if(Global.chosanExperimentName == "QoS"){
            codeLink = "https://www.github.com/eniac/Flightplan/tree/master/Wharf/splits3/ALV_qos/";
        }
        return codeLink;
    }

    public void PointerEnterRewind(){
        ToolTipControl.ShowToolTip_Static("Rewind");
    }

    public void PointerEnterForward(){
        ToolTipControl.ShowToolTip_Static("Forward");
    }

    public void PointerEnterPause(){
        ToolTipControl.ShowToolTip_Static("Play/Pause");
    }
    public void PointerEnterStop(){
        ToolTipControl.ShowToolTip_Static("Stop");
    }

    public void PointerEnterSpeedSlider(){
        ToolTipControl.ShowToolTip_Static("Speed Slider");
    }

    public void PointerExit(){
        ToolTipControl.HideToolTip_Static();
    }

    public void ShowHideTagMarker(){
        if(showTagMarker==true){
            SetMenuButtonText("ShowHideTagMarker", "Enable Info Tag");
            billBoard.SetSwitchTagStatus(false);
            topo.HideTagMarker();
            showTagMarker = false;
        }
        else{
            SetMenuButtonText("ShowHideTagMarker", "Disable Info Tag");
            billBoard.SetSwitchTagStatus(true);
            topo.ShowTagMarker();
            showTagMarker = true;
        }
    }

    public void ShowHideEventTags(){
        if(showEventTag == true){
            SetMenuButtonText("ShowHideEventTag", "Enable Event Tag");
            billBoard.SetEventTagStatus(false);
            showEventTag = false;
        }
        else{
            SetMenuButtonText("ShowHideEventTag", "Disable Event Tag");
            billBoard.SetEventTagStatus(true);
            showEventTag = true;
        }
    }

    void SetMenuButtonText(string buttonName, string buttonText){
        panelMenu.transform.Find(buttonName).GetComponentInChildren<Text>().text = buttonText;
    }
}
