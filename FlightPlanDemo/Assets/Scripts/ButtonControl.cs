using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonControl : MonoBehaviour
{
    [SerializeField] private Topology topo = default;
    [SerializeField] Button pauseResumeButton = default;
    bool showLable;
    List<GameObject> hostTextObject;
    List<GameObject> textObj;
    bool showLinkOpaque;
    bool showNodeOpaque;
    List<GameObject> linkObject;
    List<GameObject> switchObject;
    List<GameObject> satObject;
    List<string> colorPatterns = new List<string>(){"Color Pattern 1", "Color Pattern 2", "Color Pattern 3"};
    [SerializeField] private Dropdown colorPatternDropdown = default;
    [SerializeField] private InputField searchField = default;
    [SerializeField] private PopUpControl popup = default;
    [SerializeField] private AnimationControl anim = default;
    [SerializeField] private ColorControl colorControl = default;

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
    }

    public void ToggleLables(){
        if(showLable == true){
            foreach(var obj in textObj){
                obj.SetActive(false);
            }
            foreach(var obj in hostTextObject){
                obj.SetActive(false);
            }
            showLable = false;
        }
        else{
            foreach(var obj in textObj){
                obj.SetActive(true);
            }
            foreach(var obj in hostTextObject){
                obj.SetActive(true);
            }
            showLable = true;
        } 
    }  

    public void ToggleLinkTransparency(){
        if(showLinkOpaque == true){
            // Here a = alpha = opacity (0.0 transparent, 1.0 opaque)
            topo.MakeLinksTransparent();
            showLinkOpaque = false;
        }
        else{
            topo.MakeLinksOpaque();
            showLinkOpaque = true;
        }
    }

    public void ToggleNodeTransparency(){
        if(showNodeOpaque == true){
            // Here a = alpha = opacity (0.0 transparent, 1.0 opaque)
            topo.MakeNodesTransparent();
            showNodeOpaque = false;
        }
        else{
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
        colorControl.SetColorPattern(index);
    }

    public void StartAnimation(){
        anim.StartAnimation();
        ChangePauseResumeButtonText(Global.AnimStatus.Forward);
    }

    public void ResetAnimation(){
        anim.ResetAnimation();
        ChangePauseResumeButtonText(Global.AnimStatus.Forward);
    }

    public void PauseResumeAnimation(){
        Global.AnimStatus status = anim.PauseResume();
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
            pauseResumeButton.gameObject.GetComponentInChildren<Text>().text = ">";
        }
        else{
            // Show Pause sysmbol on button
            pauseResumeButton.gameObject.GetComponentInChildren<Text>().text = "||";
        }
    }
}
