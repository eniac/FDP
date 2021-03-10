using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

// Slide show on the place of graphs
public class SlideShow : MonoBehaviour
{
    [SerializeField] Image image = default;
    [SerializeField] Image helperImage = default;
    [SerializeField] AnimControl anim = default;

    JObject dynamicConfigObject = null;
    Dictionary<int, string> imageInfo = new Dictionary<int, string>();
    Dictionary<int, string> helperImageInfo = new Dictionary<int, string>();
    private IEnumerator coroutine;
    Global.AnimStatus animStatusBeforeSlideShow = Global.AnimStatus.Forward;

    void Awake(){
        image.gameObject.SetActive(false);
        helperImage.gameObject.SetActive(false);
    }

    void Start()
    {
        image.gameObject.transform.Find("Button").GetComponent<Button>().onClick.AddListener(delegate{OkButtonHandler();});
        helperImage.gameObject.transform.Find("Button").GetComponent<Button>().onClick.AddListener(delegate{OkButtonHandler();});
        DisableUpdate();
    }

    public void SetConfigObject(JObject dynamicObj){
        dynamicConfigObject = dynamicObj;
        EnableUpdate();
    }

    void Update()
    {
        if((string)dynamicConfigObject["slide_show"]["show"] == "yes"){
            SlideShowParser();
        }
        DisableUpdate();
    }

    public void DetectSlideShowTime(int time){
        bool showImage=false;
        if((string)dynamicConfigObject["slide_show"]["show"] == "yes"){
            if(time!=-1 && imageInfo.ContainsKey(time)){
                showImage = true;
                helperImage.gameObject.SetActive(false);
                image.gameObject.SetActive(true);
                coroutine = LoadImage(image, imageInfo, time);
                StartCoroutine(coroutine);
            }
            if(time!=-1 && helperImageInfo.ContainsKey(time)){
                showImage = true;
                helperImage.gameObject.SetActive(true);
                coroutine = LoadImage(helperImage, helperImageInfo, time);
                StartCoroutine(coroutine);
            }
        }
        if(showImage==true){
            animStatusBeforeSlideShow = anim.GetAnimStatus();
            if(animStatusBeforeSlideShow != Global.AnimStatus.Pause){
                anim.Pause();
            }
        }
    }

    // Get file from file system or server
    public IEnumerator LoadImage(Image img, Dictionary<int, string> imgInfo, int time){
        var filePath = Path.Combine(Application.streamingAssetsPath, Global.images + imgInfo[time]);
        Debug.Log("Image Path in parser = " + filePath);
        byte[] textureBytes;

        if (filePath.Contains ("://") || filePath.Contains (":///")) {
            // Uning UnityWebRequest class
            var unityWebRequest =  UnityWebRequest.Get(filePath);
            yield return unityWebRequest.SendWebRequest();
            textureBytes = unityWebRequest.downloadHandler.data;
            
        }
        else{
            textureBytes = File.ReadAllBytes(filePath);
        }

        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(textureBytes);
 
        //Creates a new Sprite based on the Texture2D
        Sprite fromTex = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
 
        //Assigns the UI sprite
        img.sprite = fromTex;

    }

    public void SlideShowParser(){
        JArray info = (JArray)dynamicConfigObject["slide_show"]["schedule_info"];
        string name = "";
        int time=0;
        for(int i=0; i<info.Count; i++){
            time = int.Parse((string)info[i]["time"]);
            name = (string)info[i]["image_name"];
            if(name!=null){
                imageInfo.Add(time, name);
            }
            name = (string)info[i]["helper_image_name"];
            if(name!=null){
                helperImageInfo.Add(time, name);
            }
        }
    }

    public void HideHelperImage(){
        helperImage.gameObject.SetActive(false);
    }

    void OkButtonHandler(){
        helperImage.gameObject.SetActive(false);
        image.gameObject.SetActive(false);
        if(animStatusBeforeSlideShow != Global.AnimStatus.Pause){
            anim.Resume(animStatusBeforeSlideShow);
        }
    }

    void EnableUpdate(){
        enabled = true;
    }
    void DisableUpdate(){
        enabled = false;
    }
}
