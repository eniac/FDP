﻿/*
Copyright 2021 Heena Nagda

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

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
    [SerializeField] Sprite defaultSprite = default;
    [SerializeField] AnimControl anim = default;

    JObject dynamicConfigObject = null;
    Dictionary<int, string> imageInfo = new Dictionary<int, string>();
    Dictionary<int, string> helperImageInfo = new Dictionary<int, string>();
    private IEnumerator coroutine;
    Global.AnimStatus animStatusBeforeSlideShow = Global.AnimStatus.Forward;

    void Awake(){
        image.gameObject.SetActive(false);
        helperImage.gameObject.SetActive(false);
        image.sprite = defaultSprite;
        helperImage.sprite = defaultSprite;
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
                coroutine = LoadImage(image, imageInfo, time);
                StartCoroutine(coroutine);
                image.gameObject.SetActive(true);
            }
            if(time!=-1 && helperImageInfo.ContainsKey(time)){
                showImage = true;
                coroutine = LoadImage(helperImage, helperImageInfo, time);
                StartCoroutine(coroutine);
                helperImage.gameObject.SetActive(true);
            }
        }
        if(showImage==true){
            animStatusBeforeSlideShow = anim.GetAnimStatus();
            Debug.Log("Animation Status BEFORE = " + animStatusBeforeSlideShow);
            if(animStatusBeforeSlideShow != Global.AnimStatus.Pause){
                anim.Pause();
            }
            anim.EventTagAppear();
        }
    }

    // Get file from file system or server
    public IEnumerator LoadImage(Image img, Dictionary<int, string> imgInfo, int time){
        img.sprite = defaultSprite;
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
            Debug.Log("Animation Status AFTER = " + animStatusBeforeSlideShow);
            anim.Resume(animStatusBeforeSlideShow, true);
        }
    }

    void EnableUpdate(){
        enabled = true;
    }
    void DisableUpdate(){
        enabled = false;
    }
}
