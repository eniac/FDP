/*
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
using UnityEngine.UI;
using System.IO;
using System.Text;
using System.Linq;
using TMPro;

using UnityEngine;
using UnityEngine.Networking;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;



struct PacketInfo{
    public GameObject Object;
    public int packetTime;
    public float expirationTime;
    public float instantiationTime;
    public Vector3 sourcePos;
    public Vector3 targetPos;
    public string source;
    public string target;
    public string origin;
    public string destination;
    public string packetID;
    public Global.PacketType packetType;
    public Color color;
    public Global.PacketTag tag;
};

struct AnimationParameters{
    public bool sliderJump;
    public Global.AnimStatus animStatus;
    public float timeScale;
    public float timeScaleBeforePause;
    public float jumpDuration;
    public float jumpRC;
};
public class AnimControl : MonoBehaviour
{
    [SerializeField] Topology topo = default;
    [SerializeField] ColorControl colorControl = default;
    [SerializeField] GraphInput graphInput = default;
    [SerializeField] SliderControl sliderControl = default;
    [SerializeField] BillBoardControl billBoard = default;
    [SerializeField] SlideShow slideShow = default;
    [SerializeField] IntroTagControl introTag = default;
    [SerializeField] GameObject loadingPanel = default;
    [SerializeField] GameObject aminTimePopUp = default;

    public enum PacketInfoIdx{
        Time=0,
        Source,
        Target,
        Origin,
        Destination,
        Pid,
        Type
    }

    const float speed = 20f;
    const float prePlayTimeScale = 30f;
    bool updateStatus = false;
    string elapsedTimeString;
    float referenceCounter=0;
    float timeScaleBeforePause = 1;
    bool prePlay = Global.PRE_PLAY;
    int instantiatedPacketTime = 0, lastPktTime=-1;
    AnimationParameters animParamBeforeSliderJump = new AnimationParameters();
    Global.AnimStatus animStatus = Global.AnimStatus.Forward;
    Global.AnimStatus animationStatusBeforePause = Global.AnimStatus.Forward;
    List<GameObject> LossyLinkObjects = new List<GameObject>();
    Dictionary<string, Global.PacketType> PacketTypeInfo = new Dictionary<string, Global.PacketType>();
    Dictionary<string, Tuple<string, string>> OriginDestinationMap = new Dictionary<string, Tuple<string, string>>();
    Dictionary<string, Tuple<string, bool>> mcdCache = new Dictionary<string, Tuple<string, bool>>();  // <packetID, <last origin, is cached packet>>
    List<string> mcdCacheCheckNode = new List<string>(){null, null, null, null}; //cache source, cache target, cache incoming source1, cache incoming source2
    Dictionary<string, SortedDictionary<int, PacketInfo>> packetBySource = new Dictionary<string, SortedDictionary<int, PacketInfo>>();
    Dictionary<string, int> packetBySourcePtr = new Dictionary<string, int>();
    Dictionary<string, SortedDictionary<int, PacketInfo>> packetByTarget = new Dictionary<string, SortedDictionary<int, PacketInfo>>();
    Dictionary<string, int> packetByTargetPtr = new Dictionary<string, int>();
    Dictionary<string, List<int>> packetIDSequence = new Dictionary<string, List<int>>(); // PacketID, list of packet time in sequence
    Dictionary<string, int> packetIDSequencePtr = new Dictionary<string, int>();
    List<PacketInfo> rewindSequence = new List<PacketInfo>();
    List<PacketInfo> forwardSequence = new List<PacketInfo>();
    Dictionary<int, PacketInfo> runningQueue = new Dictionary<int, PacketInfo>();
    Dictionary<string, GameObject> HoldbackPackets = new Dictionary<string, GameObject>();  // Packetid : game object
    Dictionary<string, GameObject> HoldBackParity = new Dictionary<string, GameObject>();
    HashSet<int> packetTime = new HashSet<int>();
    bool eventTagAppearFlag = false;
    float animTime=0;
    JObject dynamicConfigObject;

    void Start(){
        DisableUpdate();
    }

    // Get file from file system or server
    public IEnumerator GetMetadataFile(){
        var filePath = Path.Combine(Application.streamingAssetsPath, Global.experimentMetadata);
        if (filePath.Contains ("://") || filePath.Contains (":///")) {
            // Using UnityWebRequest class
            var loaded = new UnityWebRequest(filePath);
            loaded.downloadHandler = new DownloadHandlerBuffer();
            yield return loaded.SendWebRequest();
            elapsedTimeString = loaded.downloadHandler.text;
        }
        else{
            elapsedTimeString = File.ReadAllText(filePath);
        }
    }

    public IEnumerator WriteFile(){
        //"text/html
        var filePath = Path.Combine(Application.streamingAssetsPath, Global.animTimeFile);
        if (filePath.Contains ("://") || filePath.Contains (":///")) {
            Debug.Log("File to write on = " + filePath);
            String data = "350";
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            // var request = new UnityWebRequest(filePath, "POST");
            // request.uploadHandler = (UploadHandler) new UploadHandlerRaw(dataBytes);
            // request.SetRequestHeader("Content-Type", "text/html");
            // yield return request.SendWebRequest();
            // Debug.Log("Status Code: " + request.responseCode);

            // UnityWebRequest webRequest = UnityWebRequest.Put(filePath, data);
            // UploadHandler customUploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(data));
            // customUploadHandler.contentType = "text/html";
            // webRequest.uploadHandler = customUploadHandler;
            // SendRequest(webRequest);

            // UnityWebRequest requestU= new UnityWebRequest(filePath, UnityWebRequest.kHttpVerbPOST);
            // UploadHandlerRaw uH= new UploadHandlerRaw(dataBytes);
            // requestU.uploadHandler= uH;
            // requestU.SetRequestHeader("Content-Type", "application/txt");
            // DownloadHandlerBuffer dH= new DownloadHandlerBuffer();
            // requestU.downloadHandler= dH;
            // yield return requestU.SendWebRequest();
            // Debug.Log("Status Code: " + requestU.responseCode);

            // UnityWebRequest wr= new UnityWebRequest(filePath);
            // UploadHandler uploader = new UploadHandlerRaw(dataBytes);
            // wr.uploadHandler = uploader;
            // yield return wr.SendWebRequest();
            // Debug.Log("Status Code: " + wr.responseCode);

            // WWWForm form = new WWWForm();
            // form.AddField("name", "usernameText");
            // form.AddField("data", "mobileNoText");
            // UnityWebRequest www = UnityWebRequest.Post(filePath, form);
            // www.SetRequestHeader("Content-Type", "application/json");
            // yield return www.SendWebRequest();
            // if (www.isNetworkError){
            //     Debug.Log(www.error);
            // }
            // else{
            //     Debug.Log("Uploaded");
            //     Debug.Log("Status Code: " + www.responseCode);
            // }

            // List<IMultipartFormSection> wwwForm = new List<IMultipartFormSection>();
            // wwwForm.Add(new MultipartFormDataSection("curScoreKey", "340"));

            // UnityWebRequest www = UnityWebRequest.Post(filePath, wwwForm);
            // yield return www.SendWebRequest();
            // if(www.isNetworkError || www.isHttpError){
            //     Debug.Log("Web request Error = " + www.error);
            // }
            // else{
            //     Debug.Log("Message received from server = " + www.downloadHandler.text);
            //     Debug.Log("Status Code: " + www.responseCode);
            // }

            string databaseURL =  filePath+"?txt=YourStringToBeAdded";

            // List<IMultipartFormSection> wwwForm = new List<IMultipartFormSection>();
            // wwwForm.Add(new MultipartFormDataSection("curScoreKey", "340"));

            WWWForm wwwForm = new WWWForm();
            wwwForm.AddField("curScoreKey", "340");

            UnityWebRequest www = UnityWebRequest.Post(databaseURL, wwwForm);
            yield return www.SendWebRequest();
            if(www.isNetworkError || www.isHttpError){
                Debug.Log("Web request Error = " + www.error);
            }
            else{
                Debug.Log("Message received from server = " + www.downloadHandler.text);
                Debug.Log("Status Code: " + www.responseCode);
            }       
        }
        else{
            // elapsedTimeString = File.ReadAllText(filePath);
        }
    }

    public void SetConfigObject(JObject dynamicConfigObject){
        // this.configObject = configObject;
        this.dynamicConfigObject = dynamicConfigObject;
    }

    public void AnimationInit(){
        string line;
        string[] info;
        StringReader packetInfoString = new StringReader(elapsedTimeString);
        PacketTypeInfo.Add("NO", Global.PacketType.Normal);
        PacketTypeInfo.Add("PR", Global.PacketType.Parity);
        PacketTypeInfo.Add("MCD", Global.PacketType.MCD);
        PacketTypeInfo.Add("MCDC", Global.PacketType.MCDcache);
        PacketTypeInfo.Add("HC", Global.PacketType.HC);
        PacketTypeInfo.Add("TCP", Global.PacketType.TCP);
        PacketTypeInfo.Add("ICMP", Global.PacketType.ICMP);
        PacketTypeInfo.Add("NAK", Global.PacketType.NAK);
        PacketTypeInfo.Add("TUNNEL", Global.PacketType.Tunnel);
        PacketTypeInfo.Add("QOS", Global.PacketType.Qos);
        PacketTypeInfo.Add("HTTP2", Global.PacketType.HTTP2);
        
        line = packetInfoString.ReadLine();
        while(line!=null){
            info = line.Split(' ');
            PacketInfo pInfo = new PacketInfo();
            pInfo.packetTime = int.Parse(info[(int)PacketInfoIdx.Time]);
            pInfo.sourcePos = topo.GetNodePosition(info[(int)PacketInfoIdx.Source]);
            pInfo.targetPos = topo.GetNodePosition(info[(int)PacketInfoIdx.Target]);
            pInfo.source = info[(int)PacketInfoIdx.Source];
            pInfo.target = info[(int)PacketInfoIdx.Target];
            pInfo.origin = info[(int)PacketInfoIdx.Origin];
            pInfo.destination = info[(int)PacketInfoIdx.Destination];
            pInfo.packetID = info[(int)PacketInfoIdx.Pid];
            pInfo.packetType = PacketTypeInfo[info[(int)PacketInfoIdx.Type]];

            // Setting up the time if two packet have exactly same time
            if(packetTime.Contains(pInfo.packetTime)){
                pInfo.packetTime = pInfo.packetTime + 1;
            }
            else{
                packetTime.Add(pInfo.packetTime);
            }

            // Findout origin and destination of the pckets which doesn't have these
            if(pInfo.origin == "00000000" && OriginDestinationMap.ContainsKey(pInfo.packetID)==true){
                pInfo.origin = OriginDestinationMap[pInfo.packetID].Item1;
                pInfo.destination = OriginDestinationMap[pInfo.packetID].Item2;
                // Debug.Log("CHANGE = " + pInfo.packetTime + " : " +  pInfo.packetID + " : " + pInfo.origin + " - " + pInfo.destination);
            }
            else if(pInfo.origin != "00000000" && OriginDestinationMap.ContainsKey(pInfo.packetID)==false){
                Tuple<string, string> orgDest = new Tuple<string, string>(pInfo.origin, pInfo.destination);
                OriginDestinationMap.Add(pInfo.packetID, orgDest);
            }

            if(packetBySource.ContainsKey(pInfo.source)){
                packetBySource[pInfo.source].Add(pInfo.packetTime, pInfo);
            }
            else{
                SortedDictionary<int, PacketInfo> dict = new SortedDictionary<int, PacketInfo>();
                dict.Add(pInfo.packetTime, pInfo);
                packetBySource.Add(pInfo.source, dict);
                packetBySourcePtr.Add(pInfo.source, -1);
            }

            if(packetByTarget.ContainsKey(pInfo.target)){
                packetByTarget[pInfo.target].Add(pInfo.packetTime, pInfo);
            }
            else{
                SortedDictionary<int, PacketInfo> dict = new SortedDictionary<int, PacketInfo>();
                dict.Add(pInfo.packetTime, pInfo);
                packetByTarget.Add(pInfo.target, dict);
                packetByTargetPtr.Add(pInfo.target, -1);
            }

            if(packetIDSequence.ContainsKey(pInfo.packetID)){
                packetIDSequence[pInfo.packetID].Add(pInfo.packetTime);
            }
            else{
                List<int> l = new List<int>();
                l.Add(pInfo.packetTime);
                packetIDSequence.Add(pInfo.packetID, l);
                packetIDSequencePtr.Add(pInfo.packetID, 0);
            }
            
            line = packetInfoString.ReadLine();
            // Debug.Log(pInfo.packetTime);

        }

        // Display packet by source
        Debug.Log("Packet By Source");
        // foreach(var s in packetBySource.Keys){
        //     foreach(var k in packetBySource[s].Keys){
        //         Debug.Log(s + " : " + k + " : " + packetBySource[s][k].packetID);
        //     }
        // }
        Debug.Log("Packet By Target");
        // foreach(var s in packetByTarget.Keys){
        //     foreach(var k in packetByTarget[s].Keys){
        //         Debug.Log(s + " : " + k + " : " + packetByTarget[s][k].packetID);
        //     }
        // }
        foreach(var t in packetByTarget.Keys){
            for(int i=0; i<packetByTarget[t].Count; i++){
                var k = packetByTarget[t].ElementAt(i).Key;
                Debug.Log(i + " = " + t + " : " + k + " : " + packetByTarget[t][k].packetID);
            }
        }
        // Debug.Log("Packet ID Sequence");
        // foreach(var k in packetIDSequence.Keys){
        //     foreach(var t in packetIDSequence[k]){
        //         Debug.Log(k + " : " + packetIDSequencePtr[k] + " : " + t);
        //     }
        // }
        
        packetTime.Clear();
        mcdCache.Clear();

        graphInput.ClearPlot();
        graphInput.GraphInputInit();

        animParamBeforeSliderJump.sliderJump = false;
        animParamBeforeSliderJump.jumpDuration = 0f;

        LossyLinkObjects = topo.GetDropperLinkObjects();

        if(LossyLinkObjects.Count > 0){
            InvokeRepeating("LossyLinkBlink", 0, 0.05f);
        } 
        loadingPanel.SetActive(false);
        AdjustSpeed(1f);
        if(prePlay==true){
            AdjustSpeed(prePlayTimeScale);
            StartAnimation();
        }
        
    }

    public void ShowLossyBlink(){
        if(LossyLinkObjects.Count > 0){
            InvokeRepeating("LossyLinkBlink", 0, 0.05f);
        }
    }

    public void StopLossyBlink(){
        if(LossyLinkObjects.Count > 0){
            foreach(var go in LossyLinkObjects){
                go.GetComponent<MeshRenderer>().enabled = true;
            }
            CancelInvoke("LossyLinkBlink");
        }
    }

    void LossyLinkBlink(){
        foreach(var go in LossyLinkObjects){
            if(go.GetComponent<MeshRenderer>().enabled == false){
                go.GetComponent<MeshRenderer>().enabled = true;
            }
            else{
                 go.GetComponent<MeshRenderer>().enabled = false;
            }
            
        }
    }

    void SetAnimationStatus(Global.AnimStatus status){
        animStatus = status;
    }

    public Global.AnimStatus GetAnimStatus(){
        return animStatus;
    }

    void StartAnimation(){
        Stop();
        topo.MakeLinksTransparent();
        topo.MakeNodesTransparent();
        Forward();
        EnableUpdate();
        InvokeRepeating("DispatchPacket", 0f, 0.1f); 
    }

    public void Stop(){
        DisableUpdate();
        timeScaleBeforePause = Time.timeScale;
        referenceCounter = 0;
        foreach(var k in packetBySource.Keys){
            // Debug.Log("Source = " + k);
            packetBySourcePtr[k] = -1;
        }
        foreach(var k in packetByTarget.Keys){
            // Debug.Log("Target = " + k);
            packetByTargetPtr[k] = -1;
        }
        foreach(var k in packetIDSequence.Keys){
            packetIDSequencePtr[k] = 0;
        }
        forwardSequence.Clear();
        rewindSequence.Clear();
        RemoveRunningPackets();
        graphInput.ClearPlot();
        graphInput.GraphInputInit();
        sliderControl.SetTimeSlider(0);
        topo.MakeLinksOpaque();
        topo.MakeNodesOpaque();
        if(Time.timeScale == 0){
            if(timeScaleBeforePause == 0){
                AdjustSpeed(1f);
            }
            else{
                AdjustSpeed(timeScaleBeforePause);
            }
        }
    }
    public Global.AnimStatus Pause(){
        Debug.Log("Pause start = " + Time.timeScale);
        // if(animStatus != Global.AnimStatus.Pause){
        //     timeScaleBeforePause = Time.timeScale;
        //     AdjustSpeed(0f);
        //     SetAnimationStatus(Global.AnimStatus.Pause);
        //     Debug.Log("Pause in = " + Time.timeScale);
        // }

        if(animStatus == Global.AnimStatus.Pause){
            Resume(animationStatusBeforePause);
        }
        else{
            timeScaleBeforePause = Time.timeScale;
            animationStatusBeforePause = animStatus;
            AdjustSpeed(0f);
            SetAnimationStatus(Global.AnimStatus.Pause);
            Debug.Log("Pause in = " + Time.timeScale);
        }
        return animStatus;
    }

    public void Forward(){
        if(eventTagAppearFlag==true){
            return;
        }
        Debug.Log("Forward start = " + Time.timeScale);
        if(animStatus == Global.AnimStatus.Pause && Time.timeScale == 0){
            AdjustSpeed(timeScaleBeforePause);
        }
        if(animStatus != Global.AnimStatus.Forward){
            SetAnimationStatus(Global.AnimStatus.Forward);
            Debug.Log("Forward in = " + Time.timeScale);
        }
    }
    public void Rewind(){
        if(eventTagAppearFlag==true){
            return;
        }
        Debug.Log("Rewind start = " + Time.timeScale);
        if(animStatus == Global.AnimStatus.Pause && Time.timeScale == 0){
            AdjustSpeed(timeScaleBeforePause);
        }
        if(animStatus != Global.AnimStatus.Rewind){
            SetAnimationStatus(Global.AnimStatus.Rewind);
            Debug.Log("Rewind in = " + Time.timeScale);
        }
    }

    public void Resume(Global.AnimStatus status, bool byEvent=false){
        if(byEvent == true){
            eventTagAppearFlag=false;
        }
        if(eventTagAppearFlag==true){
            return;
        }
        if(GetUpdateStatus() == false){
            StartAnimation();
        }
        else if(status == Global.AnimStatus.Pause){
            Pause();
        }
        else if(status == Global.AnimStatus.Forward){
            Forward();
        }
        else if(status == Global.AnimStatus.Rewind){
            Rewind();
        }
    }

    public void SetAnimParamBeforeSliderJump(float jumpDuration=0f){
        if(jumpDuration!=0f){
            sliderControl.SetTimeSlider(referenceCounter);
            animParamBeforeSliderJump.jumpDuration = jumpDuration;
            animParamBeforeSliderJump.jumpRC = (RCtime()/Global.U_SEC)+jumpDuration;
            animParamBeforeSliderJump.sliderJump = true;
            AdjustSpeed(prePlayTimeScale);
            if(jumpDuration>0){
                Forward();
            }
            else{
                Rewind();
            }
            return;
        }
        animParamBeforeSliderJump.timeScaleBeforePause = timeScaleBeforePause;
        animParamBeforeSliderJump.animStatus = animStatus;
        animParamBeforeSliderJump.timeScale = Time.timeScale;
    }
    void ResetAnimParamBeforeSliderJump(){
        if(animParamBeforeSliderJump.animStatus == Global.AnimStatus.Pause){
            Pause();
        }
        else if(animParamBeforeSliderJump.animStatus == Global.AnimStatus.Forward){
            Forward();
        }
        else if(animParamBeforeSliderJump.animStatus == Global.AnimStatus.Rewind){
            Rewind();
        }
        animParamBeforeSliderJump.sliderJump = false;
        animParamBeforeSliderJump.jumpDuration = -1f;
        AdjustSpeed(animParamBeforeSliderJump.timeScale);
        timeScaleBeforePause = animParamBeforeSliderJump.timeScaleBeforePause;
    }

    bool SliderEvent(){
        if(animParamBeforeSliderJump.sliderJump==true){
            return true;
        }
        return false;
    }

    public void AdjustSpeed(float speed){
        Time.timeScale = speed;
    }
    void RCupdate(){
        if(animStatus == Global.AnimStatus.Forward){
            referenceCounter += Time.fixedDeltaTime;
        }
        else if(animStatus == Global.AnimStatus.Rewind){
            referenceCounter -= Time.fixedDeltaTime;
        }
        if(SliderEvent()){
            if((animParamBeforeSliderJump.jumpDuration<0 
                && referenceCounter <= animParamBeforeSliderJump.jumpRC)
                || (animParamBeforeSliderJump.jumpDuration>0 
                && referenceCounter >= animParamBeforeSliderJump.jumpRC)){
                ResetAnimParamBeforeSliderJump();
                Debug.Log("******************************START*********************************************");

            }
            if(eventTagAppearFlag==true){
                animParamBeforeSliderJump.sliderJump = false;
                animParamBeforeSliderJump.jumpDuration = -1f;
                AdjustSpeed(animParamBeforeSliderJump.timeScale);
                timeScaleBeforePause = animParamBeforeSliderJump.timeScaleBeforePause;
            }
        }
        else{
            // sliderControl.SetTimeSlider(referenceCounter);
        }
        sliderControl.SetTimeSlider(referenceCounter);
        // if(eventTagAppearFlag == true){
        //     eventTagAppearFlag = false;
        // }
        graphInput.ReferenceCounterValue(referenceCounter);
    }

    public float RCtime(){
        return referenceCounter * Global.U_SEC;
    }
    void FixedUpdate()
    {
        RCupdate();

        // if expired then distroy them and remove from srunning, source and target queues
        // move running queue packets
        if(animStatus == Global.AnimStatus.Forward){
            RemoveForwardExpiredPackets();
            MoveForwardPackets();
            FollowForwardSequence();
        }
        else if(animStatus == Global.AnimStatus.Rewind){
            RemoveRewindExpiredPackets();
            MoveRewindPackets();
            FollowRewindSequence();
        }
        
        
    }

    void DispatchPacket(){
        if(animStatus == Global.AnimStatus.Forward){
            DispatchForwardPacket();
        }
    }

    bool DispatchHostNodePacket(string hostNode, int time){
        int spTime = 0, tpTime = 0;
        bool doDispatch = true;
        foreach(string s in packetBySource.Keys){
            try{
                spTime = packetBySource[s].ElementAt(packetBySourcePtr[s]+1).Key;
                if(spTime < time){
                    doDispatch = false;
                    break;
                }
            }
            catch{
                spTime = -1;
                packetBySourcePtr[s] = packetBySource[s].Count;
            }
        }
        foreach(string t in packetByTarget.Keys){
            try{
                tpTime = packetByTarget[t].ElementAt(packetByTargetPtr[t]+1).Key;
                if(tpTime < time){
                    doDispatch = false;
                    break;
                }
            }
            catch{
                tpTime = -1;
                packetByTargetPtr[t] = packetByTarget[t].Count;
            }
        }
        if(doDispatch){

            PacketInfo info = packetBySource[hostNode][time];
            GameObject go = InstantiatePacket(info);

            go.transform.position = info.sourcePos;
            info.Object = go;
            info.tag = Global.PacketTag.N;
            packetBySource[hostNode][time] = info;
            packetBySourcePtr[hostNode] = packetBySourcePtr[hostNode] + 1;

            // put to the running queue
            // if(runningQueue.ContainsKey(info.packetTime)){
            //     // eXCEPTION HANDELING IN FAST MODE
            //     Destroy(runningQueue[info.packetTime].Object);
            //     runningQueue.Remove(info.packetTime);
            // }
            runningQueue.Add(info.packetTime, info);
            return true;
        }
        return false;
    }

    void DispatchForwardPacket(){
        if(forwardSequence.Count!=0){
            return;
        }
        bool doTerminate=true;
        bool packetInSeq=false;
        string pid = null;
        foreach(string s in packetBySource.Keys){

            if(packetBySourcePtr[s] == packetBySource[s].Count && (packetByTarget.ContainsKey(s)==false || packetByTargetPtr[s] == packetByTarget[s].Count)){
                continue;
            }
            // Dispatch it and update instantiate time, object
            int spTime = -1, tpTime = -1;
            try{
                spTime = packetBySource[s].ElementAt(packetBySourcePtr[s]+1).Key;
                doTerminate=false;
            }
            catch{
                spTime = -1;
                packetBySourcePtr[s] = packetBySource[s].Count;
                
            }
            try{
                // Debug.Log("F target = " + s + " : " + packetByTarget[s].Count + " : " + packetByTargetPtr[s] + " :: " + packetBySource[s].Count + " : " + packetBySourcePtr[s]);
                tpTime = packetByTarget[s].ElementAt(packetByTargetPtr[s]+1).Key;
                doTerminate=false;
            }
            catch{
                tpTime = -1;
                if(packetByTarget.ContainsKey(s)){
                    packetByTargetPtr[s] = packetByTarget[s].Count;
                }
            }

            if(spTime != -1 && topo.IsHost(s)==true){

                if(DispatchHostNodePacket(s, spTime)){
                    doTerminate=false;
                }
                continue;
            }

            packetInSeq = false;
            if(spTime != -1){
                pid = packetBySource[s][spTime].packetID;
                if(packetIDSequencePtr[pid] < 0){
                    packetIDSequencePtr[pid] = 0;
                }
                packetInSeq = (packetIDSequencePtr[pid] < packetIDSequence[pid].Count && packetIDSequence[pid][packetIDSequencePtr[pid]] == spTime);
                // if(packetInSeq==true && pid=="c7169ba12a5a9f9b1dce3179befc1c57"){
                    // Debug.Log(pid + " : " + packetInSeq + " : " + packetIDSequence[pid][packetIDSequencePtr[pid]] + " : " + spTime + " : " + tpTime + " : " + packetIDSequencePtr[pid]);
                // }                
            }
            
                
            if( (packetByTarget.ContainsKey(s)==false && spTime!=-1f && spTime <= (int)RCtime()) 
                || (packetByTarget.ContainsKey(s)==true && tpTime!=-1 && spTime!=-1f && spTime < tpTime && packetInSeq)
                || (packetByTarget.ContainsKey(s)==true && tpTime==-1 && spTime!=-1f && packetInSeq)
                || (packetInSeq) ){
                
                
                
                PacketInfo info = packetBySource[s][spTime];
                GameObject go = InstantiatePacket(info);

                go.transform.position = info.sourcePos;
                info.Object = go;
                info.color = go.GetComponent<MeshRenderer>().material.color;
                info.tag = Global.PacketTag.N;
                // packetBySource[s].Remove(spTime);
                packetBySource[s][spTime] = info;
                packetBySourcePtr[s] = packetBySourcePtr[s] + 1;

                runningQueue.Add(info.packetTime, info);
                
                doTerminate = false;
            }
        }
        if(doTerminate==true && runningQueue.Count==0){
            Debug.Log("##### END #### " + referenceCounter);
            string time = (string)dynamicConfigObject["experiment_info"]["animation_time"];
            if(time==null){
                aminTimePopUp.transform.Find("Time").GetComponent<Text>().text = ((int)Mathf.Floor(referenceCounter)).ToString();
                aminTimePopUp.SetActive(true);
            }
            
            topo.MakeLinksOpaque();
            topo.MakeNodesOpaque();
            sliderControl.SetTimeSlider(0);
            AdjustSpeed(1f);
            if(prePlay == true){
                loadingPanel.SetActive(false);
                AdjustSpeed(1f);
                // sliderControl.SetSliderMaxValue(RCtime()/Global.U_SEC);
                graphInput.ClearPlot();
                // graphInput.SetAnimTime(RCtime()/Global.U_SEC);
                prePlay = false;
            }
            DisableUpdate();
        }
    }

    void FollowForwardSequence(){
        // Debug.Log("Forward Count = " + forwardSequence.Count);
        if(forwardSequence.Count == 0){
            return;
        }
        
        PacketInfo info = forwardSequence[forwardSequence.Count-1];
        // Debug.Log("Forward = " + info.instantiationTime + " : " + RCtime());
        if(info.instantiationTime <= RCtime()){
            GameObject go = InstantiatePacket(info);
            go.transform.position = info.sourcePos;
            info.Object = go;
            info.color = go.GetComponent<MeshRenderer>().material.color;
            info.tag = Global.PacketTag.F;

            runningQueue.Add(info.packetTime, info);
            forwardSequence.RemoveAt(forwardSequence.Count-1);
        }
    }

    void FollowRewindSequence(){
        if(rewindSequence.Count == 0){
            if(runningQueue.Count == 0){
                forwardSequence.Clear();
                rewindSequence.Clear();
                topo.MakeLinksOpaque();
                topo.MakeNodesOpaque();
                sliderControl.SetTimeSlider(0);
                AdjustSpeed(1f);
                DisableUpdate();
            }
            return;
        }
        PacketInfo info = rewindSequence[rewindSequence.Count-1];
        // Debug.Log("Rewind = " + info.expirationTime + " : " + RCtime());
        if(info.expirationTime >= RCtime()){
            GameObject go = InstantiatePacket(info);
            go.transform.position = info.targetPos;
            info.Object = go;
            info.color = go.GetComponent<MeshRenderer>().material.color;
            info.tag = Global.PacketTag.R;

            packetByTargetPtr[info.target] = packetByTargetPtr[info.target] - 1;
            packetIDSequencePtr[info.packetID] = packetIDSequencePtr[info.packetID] - 1;

            runningQueue.Add(info.packetTime, info);
            rewindSequence.RemoveAt(rewindSequence.Count-1);
        }
    }

    GameObject InstantiatePacket(PacketInfo pInfo){
        // Debug.Log(RCtime() + " : " + Time.time + " : " + Time.fixedTime + " : " + Time.fixedUnscaledTime + " : " + Time.realtimeSinceStartup + " : " + Time.timeSinceLevelLoad + " : " + Time.unscaledTime);
        // Debug.Log(RCtime() + " : " + pInfo.packetTime + " : " + pInfo.packetID + " : " + pInfo.source + " : " + pInfo.target);
        GameObject packet_prefab = Resources.Load("Packet") as GameObject;
        GameObject go = Instantiate(packet_prefab) as GameObject;
        go.GetComponent<MeshRenderer>().material.color = colorControl.GetPacketColor(pInfo.origin, pInfo.destination, pInfo.packetID, pInfo.packetType, go.GetComponent<MeshRenderer>().material.color);
        // Debug.Log(pInfo.packetID + " : " + pInfo.packetTime + " : " + pInfo.source + " : " + pInfo.target );
        RemoveHoldBackPackets(pInfo.packetID, pInfo.packetType);
        instantiatedPacketTime = pInfo.packetTime;
        GetInstantiatedPacketTime();
        return go;
    }

    public int GetInstantiatedPacketTime(){
        int pktTime = -1;
        if(lastPktTime != instantiatedPacketTime && GetAnimStatus() == Global.AnimStatus.Forward){
            pktTime = instantiatedPacketTime;
        }
        lastPktTime = instantiatedPacketTime;
        billBoard.DetectEventTag(pktTime);
        introTag.DetectEventTag(pktTime);
        slideShow.DetectSlideShowTime(pktTime);
        return pktTime;
    }

    void MoveForwardPackets(){
        List<int> allKeys = new List<int>();
        foreach(int k in runningQueue.Keys){
            allKeys.Add(k);
        }

        
        foreach(int k in allKeys){
            PacketInfo info = runningQueue[k];
            GameObject go = info.Object;
            Vector3 endPos = info.targetPos;
            info.Object.transform.position = go.transform.position 
                                                        + (endPos - go.transform.position).normalized 
                                                        * speed 
                                                        * Time.fixedDeltaTime;
            // Debug.Log("Position = " + info.Object.transform.position);
            runningQueue[k] = info;
        }
  
    }

    void MoveRewindPackets(){
        List<int> allKeys = new List<int>();
        foreach(int k in runningQueue.Keys){
            allKeys.Add(k);
        }

        
        foreach(int k in allKeys){
            PacketInfo info = runningQueue[k];
            GameObject go = info.Object;
            Vector3 endPos = info.sourcePos;
            info.Object.transform.position = go.transform.position 
                                                        + (endPos - go.transform.position).normalized 
                                                        * speed 
                                                        * Time.fixedDeltaTime;
            runningQueue[k] = info;
        }
  
    }

    void RemoveRunningPackets(){
        // Find expired objects
        List<PacketInfo> expPkt = new List<PacketInfo>();        
        GameObject go;
        int pTime;
        PacketInfo pInfo;

        // Remove Packets from running queue
        foreach(var k in runningQueue.Keys){
            pInfo = runningQueue[k];
            expPkt.Add(pInfo);
        }
        foreach(PacketInfo info in expPkt){
            pTime = info.packetTime;
            go = info.Object;
            Destroy(go);
            runningQueue.Remove(pTime);
        }
        runningQueue.Clear();
        expPkt.Clear();

        List<string> expHoldPkt = new List<string>();

        // Remove packets from HoldBack queue
        foreach(var k in HoldbackPackets.Keys){
            expHoldPkt.Add(k);
        }
        foreach(var k in expHoldPkt){
            Destroy(HoldbackPackets[k]);
            HoldbackPackets.Remove(k);
        }
        HoldbackPackets.Clear();
        expHoldPkt.Clear();

        // Remove parity packets from holdback queue
        foreach(var k in HoldBackParity.Keys){
            expHoldPkt.Add(k);
        }
        foreach(var k in expHoldPkt){
            Destroy(HoldBackParity[k]);
            HoldBackParity.Remove(k);
        }
        HoldBackParity.Clear();
    }
    void RemoveForwardExpiredPackets(){
        // Find expired objects
        List<PacketInfo> expPkt = new List<PacketInfo>();        
        Vector3 startPos, endPos;
        GameObject go;
        PacketInfo pInfo;

        foreach(var k in runningQueue.Keys){
            pInfo = runningQueue[k];
            startPos = pInfo.sourcePos;
            endPos = pInfo.targetPos;
            go = pInfo.Object;
            if( Vector3.Normalize(endPos - startPos) != Vector3.Normalize(endPos - go.transform.position) 
                || Vector3.Distance(endPos, go.transform.position) <= 1f){

                // Debug.Log("Expired = " + Vector3.Distance(endPos, go.transform.position));
                go.transform.position = endPos;
                graphInput.ExpiredPacketTargetNode(pInfo.packetTime, pInfo.target);
                expPkt.Add(pInfo);
            }
        }

        // Remove expired objects
        int pTime;
        string end;
        foreach(PacketInfo info in expPkt){
            pTime = info.packetTime;
            go = info.Object;
            end = info.target;
            // packetByTarget[end].Remove(pTime);
            // if(info.tag == Global.PacketTag.N){
            //     packetByTargetPtr[end] = packetByTargetPtr[end] + 1;
            // }
            packetByTargetPtr[end] = packetByTargetPtr[end] + 1;
            packetIDSequencePtr[info.packetID] = packetIDSequencePtr[info.packetID] + 1;
            PacketInfo rInfo = info;
            rInfo.expirationTime = RCtime();
            rewindSequence.Add(rInfo);
    
            // Debug.Log("distroy = " + info.source + " : " + info.target + " : " + pTime + " : " +  info.packetID + " : " + packetByTargetPtr[end]);
            
            runningQueue.Remove(pTime);
            
            
            // if(packetIDSequencePtr[info.packetID] >= packetIDSequence[info.packetID].Count
            //     || topo.IsDropper(info.target)
            //     || info.packetType==Global.PacketType.Parity){
            //     Destroy(go);
            // }
            // else{
            //     if(HoldbackPackets.ContainsKey(info.packetID)){
            //         Destroy(HoldbackPackets[info.packetID]);
            //         // Destroy(go);
            //         HoldbackPackets.Remove(info.packetID);
            //     }
            //     else{
            //         HoldbackPackets.Add(info.packetID, go);
            //     }
            // }

            bool drop=true;
            if(topo.IsDropper(info.target)||info.packetType==Global.PacketType.Parity){
                for(int i=packetIDSequencePtr[info.packetID]; i<packetIDSequence[info.packetID].Count; i++){
                    // Debug.Log("DROP = " + i + " : " + info.packetID + " : " + info.packetType + " : " + info.packetTime + " : " + packetIDSequence[info.packetID][i] + " : " + packetBySource[info.target].ContainsKey(packetIDSequence[info.packetID][i]));
                    if(packetBySource[info.target].ContainsKey(packetIDSequence[info.packetID][i]) ){
                        // Debug.Log("DROP = " + i + " : " + info.packetID + " : " + info.packetType + " : " + info.packetTime + " : " + packetIDSequence[info.packetID][i] + " : " + packetBySource[info.target].ContainsKey(packetIDSequence[info.packetID][i]) + " : " + packetBySource[info.target][packetIDSequence[info.packetID][i]].packetType);
                    }
                    if(packetBySource[info.target].ContainsKey(packetIDSequence[info.packetID][i]) 
                        && packetBySource[info.target][packetIDSequence[info.packetID][i]].packetType == info.packetType){
                        drop = false;
                        break;
                    }
                }
            }

            if(packetIDSequencePtr[info.packetID] >= packetIDSequence[info.packetID].Count
                || ((topo.IsDropper(info.target)||info.packetType==Global.PacketType.Parity)&& drop==true)){
                    Destroy(go);
            }
            else{
                if(info.packetType==Global.PacketType.Parity){
                    HoldBackParity.Add(info.packetID, go);
                }
                else{
                    HoldbackPackets.Add(info.packetID, go);
                }
            }
            
        }
        expPkt.Clear();
    }

    void RemoveRewindExpiredPackets(){
        // Find expired objects
        List<PacketInfo> expPkt = new List<PacketInfo>();        
        Vector3 startPos, endPos;
        GameObject go;
        PacketInfo pInfo;

        foreach(var k in runningQueue.Keys){
            pInfo = runningQueue[k];
            startPos = pInfo.targetPos;
            endPos = pInfo.sourcePos;
            go = pInfo.Object;
            if( Vector3.Normalize(endPos - startPos) != Vector3.Normalize(endPos - go.transform.position) || 
                Vector3.Distance(endPos, go.transform.position) <= 1f){
                go.transform.position = endPos;
                expPkt.Add(pInfo);
            }
        }

        // Remove expired objects
        int pTime;
        foreach(PacketInfo info in expPkt){
            pTime = info.packetTime;
            go = info.Object;
            PacketInfo fInfo = info;
            fInfo.instantiationTime = RCtime();
            forwardSequence.Add(fInfo);
            runningQueue.Remove(pTime);

            // if(packetIDSequencePtr[fInfo.packetID] < 0 
            //     || fInfo.packetType==Global.PacketType.Parity 
            //     || topo.IsDropper(fInfo.target)){
            //     Destroy(go);
            // }

            // if(packetIDSequencePtr[fInfo.packetID] < 0
            //     || fInfo.packetType==Global.PacketType.Parity ){
            //     Destroy(go);
            // }
            // else{
            //     // HoldbackPackets.Add(info.packetID, go);
            //     if(HoldbackPackets.ContainsKey(info.packetID)){
            //         Destroy(HoldbackPackets[info.packetID]);
            //         // Destroy(go);
            //         HoldbackPackets.Remove(info.packetID);
            //     }
            //     else{
            //         HoldbackPackets.Add(info.packetID, go);
            //     }
            // }

            if(packetIDSequencePtr[info.packetID] < 0
                || info.packetType==Global.PacketType.Parity ){
                Destroy(go);
            }
            else{
                if(info.packetType == Global.PacketType.Parity){
                    HoldBackParity.Add(info.packetID, go);
                }
                else{
                    HoldbackPackets.Add(info.packetID, go);
                }
            }
        }
        expPkt.Clear();
    }

    void RemoveHoldBackPackets(string pid, Global.PacketType pType){
        if(pType==Global.PacketType.Parity){
            if(HoldBackParity.ContainsKey(pid)==true){
                Destroy(HoldBackParity[pid]);
                HoldBackParity.Remove(pid);
            }
        }
        else{
            if(HoldbackPackets.ContainsKey(pid)==true){
                Destroy(HoldbackPackets[pid]);
                HoldbackPackets.Remove(pid);
            }
        }
    }

    void EnableUpdate(){
        enabled = true;
        updateStatus = true;
    }
    void DisableUpdate(){
        SetAnimationStatus(Global.AnimStatus.Pause);
        enabled = false;
        updateStatus = false;
    }

    public bool GetUpdateStatus(){
        return updateStatus;
    }

    public bool GetPrePlayStatus(){
        return prePlay;
    }

    public void EventTagAppear(){
        eventTagAppearFlag = true;
    }

    public void SetAnimTime(float time){
        animTime = time;
    }

    public float GetAnimTime(){
        return animTime;
    }
}


//packetBySource
//packetByTarget
//packetIDSequence
// runningQueue