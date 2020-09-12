using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IntroTagControl : MonoBehaviour
{
    [SerializeField] GameObject introScreen = default;
    [SerializeField] GameObject introTag2D = default;
    [SerializeField] GameObject panelMenu = default;
    [SerializeField] GameObject footer = default;  
    int state = 0;  
    bool isInState = false;
    public void IntroTagInit(Vector3 nodePos)
    {
        introTag2D.SetActive(false);
        introScreen.SetActive(false);
        DisableUpdate();
       
        if(Global.chosanExperimentName != "Introduction"){
            return;
        }
        introTag2D.transform.Find("Background").transform.Find("Button").GetComponent<Button>().onClick.AddListener(delegate{OkButtonHandler();});
        introScreen.transform.Find("Background").transform.Find("Button").GetComponent<Button>().onClick.AddListener(delegate{OkButtonHandler();});
        introScreen.SetActive(true);
        EnableUpdate();
    }

    void Update(){
        if(isInState){
            return;
        }
        switch(state){
            case 0:
                isInState = true;
                break;
            case 1:
                introScreen.SetActive(false);
                introTag2D.SetActive(true);
                introTag2D.transform.position = panelMenu.transform.Find("ToggleNodeOpacity").transform.position;
                isInState = true;
                break;

            case 2:
                introTag2D.transform.position = footer.transform.Find("TimeSlider").transform.position;
                isInState = true;
            break;

            default:
                introTag2D.gameObject.SetActive(false);
                DisableUpdate();
            break;
        }
    }

    void OkButtonHandler(){
        state++;
        isInState = false;
    }

    void EnableUpdate(){
        enabled = true;
    }

    void DisableUpdate(){
        enabled = true;
    }

}
