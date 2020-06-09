using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonControl : MonoBehaviour
{
    public TopoScript topo;
    bool showLable;
    List<GameObject> textObj;

    // Start is called before the first frame update
    void Start()
    {
        showLable = true;
        textObj = topo.GetTextObjects();
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
}
