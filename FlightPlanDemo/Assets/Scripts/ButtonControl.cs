using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonControl : MonoBehaviour
{
    public Topology topo;
    bool showLable;
    List<GameObject> textObj;
    bool showOpaque;
    List<GameObject> linkObject;
    List<GameObject> switchObject;
    List<GameObject> satObject;

    // Start is called before the first frame update
    void Start()
    {
        showLable = true;
        showOpaque = true;
        textObj = topo.GetTextObjects();
        linkObject = topo.GetLinkObjects();
        switchObject = topo.GetSwitchObjects();
        satObject = topo.GetSatObjects();
    }

    public void ToggleLables(){
        if(showLable == true){
            foreach(var obj in textObj){
                obj.SetActive(false);
            }
            showLable = false;
        }
        else{
            foreach(var obj in textObj){
                obj.SetActive(true);
            }
            showLable = true;
        } 
    }  

    public void ToggleTransparency(){
        if(showOpaque == true){
            // Here a = alpha = opacity (0.0 transparent, 1.0 opaque)
            foreach(var obj in linkObject){
                Color color = obj.GetComponent<MeshRenderer>().material.color;
                color.a = 0.1f;
                obj.GetComponent<MeshRenderer>().material.color = color;
            }
            // foreach(var obj in switchObject){
            //     Color color = obj.GetComponent<MeshRenderer>().material.color;
            //     color.a = 0.2f;
            //     obj.GetComponent<MeshRenderer>().material.color = color;
            // }
            // foreach(var obj in satObject){
            //     Color color = obj.GetComponent<MeshRenderer>().material.color;
            //     color.a = 0.2f;
            //     obj.GetComponent<MeshRenderer>().material.color = color;
            // }
            showOpaque = false;
        }
        else{
            foreach(var obj in linkObject){
                Color color = obj.GetComponent<MeshRenderer>().material.color;
                color.a = 1.0f;
                obj.GetComponent<MeshRenderer>().material.color = color;
            }
            // foreach(var obj in switchObject){
            //     Color color = obj.GetComponent<MeshRenderer>().material.color;
            //     color.a = 1.0f;
            //     obj.GetComponent<MeshRenderer>().material.color = color;
            // }
            // foreach(var obj in satObject){
            //     Color color = obj.GetComponent<MeshRenderer>().material.color;
            //     color.a = 1.0f;
            //     obj.GetComponent<MeshRenderer>().material.color = color;
            // }
            showOpaque = true;
        }
    }
}
