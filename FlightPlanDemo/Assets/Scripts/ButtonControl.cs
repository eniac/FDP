using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonControl : MonoBehaviour
{
    [SerializeField] private Topology topo = default;
    bool showLable;
    List<GameObject> hostTextObject;
    List<GameObject> textObj;
    bool showOpaque;
    List<GameObject> linkObject;
    List<GameObject> switchObject;
    List<GameObject> satObject;
    [SerializeField] private InputField searchField = default;
    [SerializeField] private PopUpControl popup = default;
    [SerializeField] private AnimationControl anim = default;

    // Start is called before the first frame update
    void Start()
    {
        showLable = true;
        showOpaque = true;
        hostTextObject = topo.GetHostTextObjects();
        textObj = topo.GetTextObjects();
        linkObject = topo.GetLinkObjects();
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

    public void ToggleTransparency(){
        if(showOpaque == true){
            // Here a = alpha = opacity (0.0 transparent, 1.0 opaque)
            topo.MakeLinksTransparent();
            topo.MakeNodesTransparent();
            showOpaque = false;
        }
        else{
            topo.MakeLinksOpaque();
            topo.MakeNodesOpaque();
            showOpaque = true;
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

    public void StartAnimation(){
        anim.StartAnimation();
    }

    public void PauseAnimation(){
        anim.Pause();
    }

    public void ForwardAnimation(){
        anim.Forward();
    }

    public void RewindAnimation(){
        anim.Rewind();
    }
}
