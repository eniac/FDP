using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

using UnityEngine;
using UnityEngine.Networking;

struct PacketInfo{
    public GameObject Object;
    public float packetTime;
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
};
public class AnimControl : MonoBehaviour
{
    [SerializeField] Topology topo = default;
    [SerializeField] ColorControl colorControl = default;
    [SerializeField] GraphInput graphInput = default;
    [SerializeField] SliderControl sliderControl = default;
    [SerializeField] GameObject loadingPanel = default;

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
    const float prePlayTimeScale = 50f;
    string elapsedTimeString;
    float referenceCounter=0;
    float timeScaleBeforePause = 1;
    bool prePlay = true;
    Global.AnimStatus animStatus = Global.AnimStatus.Forward;
    List<GameObject> LossyLinkObjects = new List<GameObject>();
    Dictionary<string, Global.PacketType> PacketTypeInfo = new Dictionary<string, Global.PacketType>();
    Dictionary<string, Tuple<string, string>> OriginDestinationMap = new Dictionary<string, Tuple<string, string>>();
    Dictionary<string, Tuple<string, bool>> mcdCache = new Dictionary<string, Tuple<string, bool>>();  // <packetID, <last origin, is cached packet>>
    List<string> mcdCacheCheckNode = new List<string>(){null, null, null, null}; //cache source, cache target, cache incoming source1, cache incoming source2
    Dictionary<string, SortedDictionary<float, PacketInfo>> packetBySource = new Dictionary<string, SortedDictionary<float, PacketInfo>>();
    Dictionary<string, int> packetBySourcePtr = new Dictionary<string, int>();
    Dictionary<string, SortedDictionary<float, PacketInfo>> packetByTarget = new Dictionary<string, SortedDictionary<float, PacketInfo>>();
    Dictionary<string, int> packetByTargetPtr = new Dictionary<string, int>();
    Dictionary<string, Tuple<string, string, string, string>> packetByID = new Dictionary<string, Tuple<string, string, string, string>>();   // ID : (source:target)
    Dictionary<float, PacketInfo> runningQueue = new Dictionary<float, PacketInfo>();

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

        // GetMCDcacheCheckNode();
        
        line = packetInfoString.ReadLine();
        while(line!=null){
            info = line.Split(' ');
            PacketInfo pInfo = new PacketInfo();
            pInfo.packetTime = float.Parse(info[(int)PacketInfoIdx.Time]);
            pInfo.sourcePos = topo.GetNodePosition(info[(int)PacketInfoIdx.Source]);
            pInfo.targetPos = topo.GetNodePosition(info[(int)PacketInfoIdx.Target]);
            pInfo.source = info[(int)PacketInfoIdx.Source];
            pInfo.target = info[(int)PacketInfoIdx.Target];
            pInfo.origin = info[(int)PacketInfoIdx.Origin];
            pInfo.destination = info[(int)PacketInfoIdx.Destination];
            pInfo.packetID = info[(int)PacketInfoIdx.Pid];
            pInfo.packetType = PacketTypeInfo[info[(int)PacketInfoIdx.Type]];


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

            // // TODO hardcoded source and target
            // if(pInfo.packetType == Global.PacketType.MCD){
            //     if(mcdCache.ContainsKey(pInfo.packetID)){
            //         if((pInfo.source == mcdCacheCheckNode[0] && pInfo.target == mcdCacheCheckNode[1] 
            //             && (mcdCache[pInfo.packetID].Item1 == mcdCacheCheckNode[2] 
            //             || mcdCache[pInfo.packetID].Item1 == mcdCacheCheckNode[3]))
            //             || mcdCache[pInfo.packetID].Item2==true){
            //             pInfo.packetType = Global.PacketType.MCDcache;
            //             mcdCache[pInfo.packetID] = new Tuple<string, bool>(pInfo.source, true);
            //         }
            //         else{
            //             mcdCache[pInfo.packetID] = new Tuple<string, bool>(pInfo.source, false);
            //         }
            //     }
            //     else{
            //         mcdCache.Add(pInfo.packetID, new Tuple<string, bool>(pInfo.source, false));
            //     }
            // }

            if(packetBySource.ContainsKey(pInfo.source)){
                if(packetBySource[pInfo.source].ContainsKey(pInfo.packetTime)){
                    pInfo.packetTime = pInfo.packetTime - 1;
                }
                packetBySource[pInfo.source].Add(pInfo.packetTime, pInfo);
            }
            else{
                SortedDictionary<float, PacketInfo> dict = new SortedDictionary<float, PacketInfo>();
                dict.Add(pInfo.packetTime, pInfo);
                packetBySource.Add(pInfo.source, dict);
                packetBySourcePtr.Add(pInfo.source, -1);
            }

            if(packetByTarget.ContainsKey(pInfo.target)){
                packetByTarget[pInfo.target].Add(pInfo.packetTime, pInfo);
            }
            else{
                SortedDictionary<float, PacketInfo> dict = new SortedDictionary<float, PacketInfo>();
                dict.Add(pInfo.packetTime, pInfo);
                packetByTarget.Add(pInfo.target, dict);
                packetByTargetPtr.Add(pInfo.target, -1);
            }
            
            line = packetInfoString.ReadLine();
            // Debug.Log(pInfo.packetTime);

        }

        // Display packet by source
        // Debug.Log("Packet By Source");
        // foreach(var s in packetBySource.Keys){
        //     foreach(var k in packetBySource[s].Keys){
        //         Debug.Log(s + " : " + k + " : " + packetBySource[s][k].packetID);
        //     }
        // }
        // Debug.Log("Packet By Target");
        // // foreach(var s in packetByTarget.Keys){
        // //     foreach(var k in packetByTarget[s].Keys){
        // //         Debug.Log(s + " : " + k + " : " + packetByTarget[s][k].packetID);
        // //     }
        // // }
        // foreach(var t in packetByTarget.Keys){
        //     for(int i=0; i<packetByTarget[t].Count; i++){
        //         var k = packetByTarget[t].ElementAt(i).Key;
        //         Debug.Log(i + " = " + t + " : " + k + " : " + packetByTarget[t][k].packetID);
        //     }
        // }

        mcdCache.Clear();

        graphInput.ClearPlot();
        graphInput.GraphInputInit();

        LossyLinkObjects = topo.GetDropperLinkObjects();

        if(LossyLinkObjects.Count > 0){
            InvokeRepeating("LossyLinkBlink", 0, 0.05f);
        }
        InvokeRepeating("DispatchPacket", 0f, 0.1f);  

        prePlay = true;

        AdjustSpeed(prePlayTimeScale);

        StartAnimation();

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

    // TODO Hardcoded Paths
    void GetMCDcacheCheckNode(){
        if(Global.chosanExperimentName == "complete_mcd_e2e"){
            mcdCacheCheckNode[0] = "p0a0";
            mcdCacheCheckNode[1] = "dropper";
            mcdCacheCheckNode[2] = "dropper";
            mcdCacheCheckNode[3] = "p0e0";
        }
        else if(Global.chosanExperimentName == "complete_e2e_1_hl3new"){
            mcdCacheCheckNode[0] = "c0";
            mcdCacheCheckNode[1] = "p0a0";
            mcdCacheCheckNode[2] = "p0a0";
            mcdCacheCheckNode[3] = "p0a0";
        }
        else if(Global.chosanExperimentName == "complete_e2e_2_hl3new"){
            // mcdCacheCheckNode[0] = "HL_V3_3";
            // mcdCacheCheckNode[1] = "p0a0";
            // mcdCacheCheckNode[2] = "p0a0";
            // mcdCacheCheckNode[3] = "p0a0";
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

    public void StartAnimation(){
        timeScaleBeforePause = Time.timeScale;
        referenceCounter = 0;
        foreach(var k in packetBySource.Keys){
            Debug.Log("Source = " + k);
            packetBySourcePtr[k] = -1;
        }
        foreach(var k in packetByTarget.Keys){
            Debug.Log("Target = " + k);
            packetByTargetPtr[k] = -1;
        }
        RemoveRunningPackets();
        packetByID.Clear();
        Forward();
        graphInput.ClearPlot();
        graphInput.GraphInputInit();
        sliderControl.SetTimeSlider(0);
        topo.MakeLinksTransparent();
        topo.MakeNodesTransparent();
        EnableUpdate();
    }

    public void Pause(){
        if(animStatus != Global.AnimStatus.Pause){
            timeScaleBeforePause = Time.timeScale;
            AdjustSpeed(0f);
            SetAnimationStatus(Global.AnimStatus.Pause);
        }
    }

    public void Forward(){
        if(animStatus == Global.AnimStatus.Pause && Time.timeScale == 0){
            // Debug.Log("Scale before Pause = " + timeScaleBeforePause);
            AdjustSpeed(timeScaleBeforePause);
        }
        if(animStatus != Global.AnimStatus.Forward){
            packetByID.Clear();
            SetAnimationStatus(Global.AnimStatus.Forward);
        }
    }
    public void Rewind(){
        if(animStatus == Global.AnimStatus.Pause && Time.timeScale == 0){
            AdjustSpeed(timeScaleBeforePause);
        }
        if(animStatus != Global.AnimStatus.Rewind){
            packetByID.Clear();
            SetAnimationStatus(Global.AnimStatus.Rewind);
        }
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
        graphInput.ReferenceCounterValue(referenceCounter);
        sliderControl.SetTimeSlider(referenceCounter);
    }

    float RCtime(){
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
        }
        else if(animStatus == Global.AnimStatus.Rewind){
            RemoveRewindExpiredPackets();
            MoveRewindPackets();
        }
        
        
    }

    void DispatchPacket(){
        if(animStatus == Global.AnimStatus.Forward){
            DispatchForwardPacket();
        }
        else if(animStatus == Global.AnimStatus.Rewind){
            DispatchRewindPacket();
        }
    }

    bool DispatchHostNodePacket(string hostNode, float time){
        float spTime = 0, tpTime = 0;
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
            info.instantiationTime = RCtime();
            info.Object = go;
            packetBySource[hostNode][time] = info;
            packetBySourcePtr[hostNode] = packetBySourcePtr[hostNode] + 1;

            // put to the running queue
            runningQueue.Add(info.packetTime, info);
            return true;
        }
        return false;
    }

    void DispatchForwardPacket(){
        bool doTerminate=true;
        foreach(string s in packetBySource.Keys){

            if(packetBySourcePtr[s] == packetBySource[s].Count && (packetByTarget.ContainsKey(s)==false || packetByTargetPtr[s] == packetByTarget[s].Count)){
                continue;
            }
            // Dispatch it and update instantiate time, object
            float spTime = -1f, tpTime = -1f;
            try{
                spTime = packetBySource[s].ElementAt(packetBySourcePtr[s]+1).Key;
            }
            catch{
                spTime = -1;
                packetBySourcePtr[s] = packetBySource[s].Count;
                
            }
            try{
                // Debug.Log("F target = " + s + " : " + packetByTarget[s].Count + " : " + packetByTargetPtr[s] + " :: " + packetBySource[s].Count + " : " + packetBySourcePtr[s]);
                tpTime = packetByTarget[s].ElementAt(packetByTargetPtr[s]+1).Key;
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
            
            
            // Debug.Log(s + " : " + spTime + " : " + tpTime + " : " + RCtime());
            if((packetByTarget.ContainsKey(s)==false && spTime!=-1f && spTime <= RCtime()) 
                || (packetByTarget.ContainsKey(s)==true && tpTime!=-1 && spTime!=-1f && spTime < tpTime)
                || (packetByTarget.ContainsKey(s)==true && tpTime==-1 && spTime!=-1f)
                || (spTime != -1
                && packetBySource[s][spTime].packetType != Global.PacketType.Parity
                && packetByID.ContainsKey(packetBySource[s][spTime].packetID)
                && topo.IsSatellite(packetByTarget[s][tpTime].source) == false
                && topo.IsSatellite(packetByTarget[s][tpTime].target) == false
                && topo.IsSatellite(packetBySource[s][spTime].source) == false
                && topo.IsSatellite(packetBySource[s][spTime].target) == false
                && topo.IsSatellite(packetByID[packetBySource[s][spTime].packetID].Item1) == false
                && topo.IsSatellite(packetByID[packetBySource[s][spTime].packetID].Item2) == false
                && packetByID[packetBySource[s][spTime].packetID].Item3 == packetBySource[s][spTime].origin
                && packetByID[packetBySource[s][spTime].packetID].Item4 == packetBySource[s][spTime].destination 
                && packetByID[packetBySource[s][spTime].packetID].Item2 == packetBySource[s][spTime].source) ){
                
                // // Debug.Log("F ACCEPTED = " + s + " : " + spTime + " : " + tpTime + " : " + RCtime());
                // if(packetByID.ContainsKey(packetBySource[s][spTime].packetID) ){
                //     // Debug.Log("F = " + packetBySource[s][spTime].packetID + " (" + packetByID[packetBySource[s][spTime].packetID].Item1 + " : " + packetByID[packetBySource[s][spTime].packetID].Item2 + " ) " + packetBySource[s][spTime].source + " : " + packetBySource[s][spTime].target);
                //     Debug.Log("ID = " + packetBySourcePtr[s] + " : " + packetByTargetPtr[s] + " : " + spTime + " : " + tpTime + " : " + " (" + packetByID[packetBySource[s][spTime].packetID].Item1 + " : " + packetByID[packetBySource[s][spTime].packetID].Item2 + " ) " + packetBySource[s][spTime].source + " : " + packetBySource[s][spTime].target + " : " + packetBySource[s][spTime].packetID + "-------------------------------");
                // }
                // else{
                //     Debug.Log("ND = " + packetBySourcePtr[s] + " : " + packetByTargetPtr[s] + " : " + spTime + " : " + tpTime + " : " + packetBySource[s][spTime].source + " : " + packetBySource[s][spTime].target + " : " + packetBySource[s][spTime].packetID );
                // }
                
                
                PacketInfo info = packetBySource[s][spTime];
                GameObject go = InstantiatePacket(info);

                go.transform.position = info.sourcePos;
                info.instantiationTime = RCtime();
                info.Object = go;
                // packetBySource[s].Remove(spTime);
                packetBySource[s][spTime] = info;
                packetBySourcePtr[s] = packetBySourcePtr[s] + 1;

                // put to the running queue
                if(runningQueue.ContainsKey(info.packetTime)){
                    // eXCEPTION HANDELING IN FAST MODE
                    Destroy(runningQueue[info.packetTime].Object);
                    runningQueue.Remove(info.packetTime);

                }
                runningQueue.Add(info.packetTime, info);
                
                doTerminate = false;
            }
        }
        if(doTerminate==true && runningQueue.Count==0){
            topo.MakeLinksOpaque();
            topo.MakeNodesOpaque();
            sliderControl.SetTimeSlider(0);
            if(prePlay == true){
                loadingPanel.SetActive(false);
                AdjustSpeed(1f);
                sliderControl.SetSliderMaxValue(RCtime()/Global.U_SEC);
                graphInput.ClearPlot();
                graphInput.SetAnimTime(RCtime()/Global.U_SEC);
                prePlay = false;
            }
            DisableUpdate();
        }
    }


    void DispatchRewindPacket(){
        foreach(string t in packetByTarget.Keys){
            if(packetBySourcePtr[t]==-1 && (packetBySource.ContainsKey(t)==false || packetByTargetPtr[t] == -1)){
                continue;
            }

            // Debug.Log("R = " + t + " : " + packetByTarget[t].Count + " : " + packetByTargetPtr[t] + " :: " + packetBySource[t].Count + " : " + packetBySourcePtr[t]);
            
            // Dispatch it and update instantiate time, object
            float spTime = -1f, tpTime = -1f;
            try{
                spTime = packetBySource[t].ElementAt(packetBySourcePtr[t]).Key;
            }
            catch{
                spTime = -1;
                if(packetBySource.ContainsKey(t)){
                    packetBySourcePtr[t] = -1;
                }
            }
            try{
                tpTime = packetByTarget[t].ElementAt(packetByTargetPtr[t]).Key;
            }
            catch{
                tpTime = -1;
                packetByTargetPtr[t] = -1;
            }

            // Debug.Log("R = " + t + " : " + spTime + " : " + tpTime + " : " + RCtime());
            if((packetBySource.ContainsKey(t)==false && tpTime!=-1f && tpTime <= RCtime()) 
                || (packetBySource.ContainsKey(t)==true && spTime!=-1 && tpTime!=-1f && spTime < tpTime)
                || (packetBySource.ContainsKey(t)==true && spTime==-1 && tpTime!=-1f)
                || ( tpTime != -1
                && packetByTarget[t][tpTime].packetType != Global.PacketType.Parity
                && packetByID.ContainsKey(packetByTarget[t][tpTime].packetID)
                && topo.IsSatellite(packetBySource[t][spTime].source) == false
                && topo.IsSatellite(packetBySource[t][spTime].target) == false
                && topo.IsSatellite(packetByTarget[t][tpTime].source) == false
                && topo.IsSatellite(packetByTarget[t][tpTime].target) == false
                && topo.IsSatellite(packetByID[packetByTarget[t][tpTime].packetID].Item1) == false
                && topo.IsSatellite(packetByID[packetByTarget[t][tpTime].packetID].Item2) == false
                && packetByID[packetByTarget[t][tpTime].packetID].Item3 == packetByTarget[t][tpTime].origin
                && packetByID[packetByTarget[t][tpTime].packetID].Item4 == packetByTarget[t][tpTime].destination 
                && packetByID[packetByTarget[t][tpTime].packetID].Item1 == packetByTarget[t][tpTime].target) ){
                // Debug.Log("R ACCEPTED = " + t + " : " + spTime + " : " + tpTime + " : " + RCtime());
                // Debug.Log("R = " + packetByTarget[t][tpTime].packetID + " : " + packetByID[packetByTarget[t][tpTime].packetID].Item1 + " - " + packetByID[packetByTarget[t][tpTime].packetID].Item2 + " : " + packetByTarget[t][tpTime].source);
                PacketInfo info = packetByTarget[t][tpTime];
                GameObject go = InstantiatePacket(info);

                go.transform.position = info.targetPos;
                info.instantiationTime = RCtime();
                info.Object = go;
                // packetBySource[s].Remove(spTime);
                packetByTarget[t][tpTime] = info;
                packetByTargetPtr[t] = packetByTargetPtr[t] - 1;
                // put to the running queue
                runningQueue.Add(info.packetTime, info);
            }
        }
    }

    GameObject InstantiatePacket(PacketInfo pInfo){
        Debug.Log(RCtime() + " : " + Time.time + " : " + Time.fixedTime + " : " + Time.fixedUnscaledTime + " : " + Time.realtimeSinceStartup + " : " + Time.timeSinceLevelLoad + " : " + Time.unscaledTime);
        GameObject packet_prefab = Resources.Load("Packet") as GameObject;
        GameObject go = Instantiate(packet_prefab) as GameObject;
        go.GetComponent<MeshRenderer>().material.color = colorControl.GetPacketColor(pInfo.origin, pInfo.destination, pInfo.packetID, pInfo.packetType, go.GetComponent<MeshRenderer>().material.color);
        return go;
    }

    void MoveForwardPackets(){
        List<float> allKeys = new List<float>();
        foreach(float k in runningQueue.Keys){
            allKeys.Add(k);
        }

        
        foreach(float k in allKeys){
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
        List<float> allKeys = new List<float>();
        foreach(float k in runningQueue.Keys){
            allKeys.Add(k);
        }

        
        foreach(float k in allKeys){
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
        float pTime;
        PacketInfo pInfo;

        foreach(var k in runningQueue.Keys){
            pInfo = runningQueue[k];
            expPkt.Add(pInfo);
        }

        foreach(PacketInfo info in expPkt){
            pTime = info.packetTime;
            go = info.Object;
            runningQueue.Remove(pTime);
            Destroy(go);
        }
        runningQueue.Clear();
        expPkt.Clear();
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
                graphInput.ExpiredPacketTargetNode(pInfo.target);
                expPkt.Add(pInfo);
            }
        }

        // Remove expired objects
        float pTime;
        string end;
        foreach(PacketInfo info in expPkt){
            pTime = info.packetTime;
            go = info.Object;
            end = info.target;
            // packetByTarget[end].Remove(pTime);
            packetByTargetPtr[end] = packetByTargetPtr[end] + 1;
    
            // Debug.Log("distroy = " + info.source + " : " + info.target + " : " + pTime + " : " +  info.packetID + " : " + packetByTargetPtr[end]);
            
            runningQueue.Remove(pTime);
            if(packetByID.ContainsKey(info.packetID)){
                packetByID[info.packetID] = new Tuple<string, string, string, string>(info.source, info.target, info.origin, info.destination);
            }
            else{
                packetByID.Add(info.packetID, new Tuple<string, string, string, string>(info.source, info.target, info.origin, info.destination));
            }
            Destroy(go);
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
        float pTime;
        string end;
        foreach(PacketInfo info in expPkt){
            pTime = info.packetTime;
            go = info.Object;
            end = info.source;
            packetBySourcePtr[end] = packetBySourcePtr[end] - 1;
            runningQueue.Remove(pTime);
            if(packetByID.ContainsKey(info.packetID)){
                packetByID[info.packetID] = new Tuple<string, string, string, string>(info.source, info.target, info.origin, info.destination);
            }
            else{
                packetByID.Add(info.packetID, new Tuple<string, string, string, string>(info.source, info.target, info.origin, info.destination));
            }
            Destroy(go);
        }
        expPkt.Clear();
    }

    void EnableUpdate(){
        enabled = true;
    }
    void DisableUpdate(){
        enabled = false;
    }
}
