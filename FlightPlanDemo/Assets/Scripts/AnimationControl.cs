using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEngine;
using UnityEngine.Networking;

struct ObjectInfo{
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

public class AnimationControl : MonoBehaviour
{
    [SerializeField] Topology topo = default;
    [SerializeField] SliderControl sliderControl = default;
    [SerializeField] ColorControl colorControl = default;
    [SerializeField] GameObject loadingPanel = default;
    [SerializeField] float SPEED_FACTOR = 1;
    [SerializeField] GraphInput graphInput = default;
    float fixedDeltaTime = 0f;
    float JUMP_SPEED_FACTOR = 10;
    float FIRST_PASS_SPEED_FACTOR = 10;
    const float MERGE_WINDOW = 0.5f;   
    const float BASE_SPEED = 10.0f;
    Vector3 packetSize = new Vector3(0.7f, 0.7f, 0.7f);
    float speed;
    int counter = 0;
    int counterFix = 0;
    int window_counter;
    string elapsedTimeString;
    StringReader packetTimeString;
    float animStartTime;
    float nextPacketTime;
    GameObject packet_prefab;
    GameObject parity_prefab, mcd_prefab, hc_prefab;
    Dictionary<GameObject, ObjectInfo> runningObject;
    List<GameObject> expiredObjects;
    List<string> runningPacketID;
    Dictionary<string, Queue<ObjectInfo>> packetHoldBackQueue;
    const float packetLossIdentificationTime = 1f;
    List<ObjectInfo> rewindList;
    int rewindListPointer;
    List<ObjectInfo> forwardList;
    int ForwardListPointer;
    string[] nextPacketInfo;
    bool firstUpdate = true;
    bool parseRemain = true;
    bool holdbackRemain = true;
    Global.AnimStatus animationStatus;
    float referenceCounter;
    float referenceCounterThreshold=0;
    float rc = 0f;
    int rcCounter = 0;
    Global.AnimStatus lastAnimStatus;
    bool startCounter;
    bool forwardFlag;
    bool rewindFlag;
    List<GameObject> DropperLinkObjects;
    bool firstPass = true;
    bool firstPassInflight = true;
    float inflightTimeStart = 0;
    // Dictionary<float, int> graphRequestData = new Dictionary<float, int>(); 
    // Dictionary<float, int> graphReplyData = new Dictionary<float, int>(); 
    ObjectInfo lastPacket;
    HashSet<string> usedID = new HashSet<string>();
    Dictionary<string, string> idMap = new Dictionary<string, string>();
    Dictionary<string, Tuple<string, string>> OriginDestinationMap = new Dictionary<string, Tuple<string, string>>();
    Dictionary<string, Tuple<string, bool>> mcdCache = new Dictionary<string, Tuple<string, bool>>();  // <packetID, <last origin, is cached packet>>

    public enum PacketInfoIdx{
        Time=0,
        Source,
        Target,
        Origin,
        Destination,
        Pid,
        Parity
    }
    
    public void Start(){
        enabled = false;        // Stop calling update, it will only be called after StartAnimation
        this.fixedDeltaTime = Time.fixedDeltaTime;
    }

    // Get file from file system or server
    public IEnumerator GetElapsedTimeFile(){
        // var filePath = Path.Combine(Application.streamingAssetsPath, "ALV_split1_autotest1/metadata.txt");
        var filePath = Path.Combine(Application.streamingAssetsPath, Global.experimentMetadata);
        Debug.Log("metadata File = " + filePath);
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
        DropperLinkObjects = topo.GetDropperLinkObjects();
    }

    public void AdjustSpeed(float speed){
        Time.timeScale = speed;
        // Time.fixedDeltaTime = this.fixedDeltaTime * Time.timeScale;
    }

    public void ResetFixedDeltaTime(){
        Time.fixedDeltaTime = this.fixedDeltaTime;
    }
    public void ResetAnimation(){
        PacketCleanup();
    }
    public void StartAnimation(){
        // Debug.Log("Restarting Animation");
        speed = BASE_SPEED * SPEED_FACTOR;
        AdjustSpeed(1);

        // Objects initialization
        packetTimeString = new StringReader(elapsedTimeString);

        if(expiredObjects==null && runningObject==null && packetHoldBackQueue==null){
            expiredObjects = new List<GameObject>();
            runningObject = new Dictionary<GameObject, ObjectInfo>();
            runningPacketID = new List<string>();
            packetHoldBackQueue = new Dictionary<string, Queue<ObjectInfo>>();
            rewindList = new List<ObjectInfo>();
            forwardList = new List<ObjectInfo>();
        }
        PacketCleanup();
        packet_prefab = Resources.Load("Packet") as GameObject;
        parity_prefab = Resources.Load("Parity") as GameObject;
        mcd_prefab = Resources.Load("Mcd") as GameObject;
        hc_prefab = Resources.Load("Hc") as GameObject;

        animStartTime = Time.time;

        // TODO : Empty file check
        nextPacketInfo = packetTimeString.ReadLine().Split(' ');
        nextPacketTime = (float)Convert.ToInt32(nextPacketInfo[(int)PacketInfoIdx.Time]);
        // Debug.Log("nextPacketInfo = " + nextPacketInfo[(int)PacketInfoIdx.Time] + ":" + nextPacketInfo[(int)PacketInfoIdx.Source] + ":" + nextPacketInfo[(int)PacketInfoIdx.Target]);

        topo.MakeLinksTransparent();
        topo.MakeNodesTransparent();
        SetAnimationStatus(Global.AnimStatus.Disk);
        sliderControl.SetSliderMode(Global.SliderMode.Normal);
        colorControl.ResetColorControl();

        rcCounter = 0;
        rc = 0;

        forwardFlag = false;
        rewindFlag = false;
        rewindListPointer = rewindList.Count - 1;
        ForwardListPointer = 0;
        referenceCounter = 0;
        startCounter = false;
        parseRemain = true;
        holdbackRemain = true;
        firstUpdate = true;
        if(firstPass == true){
            AdjustSpeed(FIRST_PASS_SPEED_FACTOR);
        }
        Debug.Log("Start Time = " + Time.realtimeSinceStartup);

        // Graph cleanup and Initialization
        GraphCleanup();

        enabled = true;
        if(DropperLinkObjects.Count > 0){
            InvokeRepeating("DropperLinkBlink", 0, 0.05f);
        }
    }

    // void FixedUpdate(){
    //     counterFix++;
    // }

    // IEnumerator DropperLinkBlink(){
    //     foreach(var go in DropperLinkObjects){
    //         go.GetComponent<MeshRenderer>().enabled = false;
    //         yield return new WaitForSeconds(0.1f);
    //         go.GetComponent<MeshRenderer>().enabled = true;  
    //     }     
    // }

    void DropperLinkBlink(){
        foreach(var go in DropperLinkObjects){
            if(go.GetComponent<MeshRenderer>().enabled == false){
                go.GetComponent<MeshRenderer>().enabled = true;
            }
            else{
                 go.GetComponent<MeshRenderer>().enabled = false;
            }
            
        }
    }

    void StopDropperLinkBlink(){
        CancelInvoke("DropperLinkBlink");
        foreach(var go in DropperLinkObjects){
            go.GetComponent<MeshRenderer>().enabled = true;
        }
    }

    void PacketCleanup(){
        counter = 0;
        counterFix = 0;
        Debug.Log("End Time = " + Time.realtimeSinceStartup);

        if(firstPassInflight==true){
            // GraphCleanup();
            graphInput.ClearPlot();
        }

        sliderControl.SetSpeedSliderDefault();

        // Removal of objects if any remained while restarting/resetting the animation
        expiredObjects.Clear();
        foreach(GameObject go in runningObject.Keys){
            Destroy(go);
        }
        runningPacketID.Clear();
        runningObject.Clear();
        packetHoldBackQueue.Clear();
        rewindList.Clear();
        forwardList.Clear();
        mcdCache.Clear();

        topo.MakeLinksOpaque();
        topo.MakeNodesOpaque();

        sliderControl.SetTimeSlider(0);
        StopDropperLinkBlink();

        if(firstPass==false){
            firstPassInflight = false;
        }

        enabled = false;
    }

    void GraphCleanup(){
        graphInput.ClearPlot();
        graphInput.GraphInputInit();
    }

    void SetAnimationStatus(Global.AnimStatus status){
        animationStatus = status;
    }
    public Global.AnimStatus GetAnimationStatus(){
        return animationStatus;
    }

    public Global.AnimStatus PauseResume(){
        if(GetAnimationStatus() == Global.AnimStatus.Pause){
            StartAnimationAction(GetLastAnimStatus());
            return GetAnimationStatus();
        }
        else{
            StartAnimationAction(Global.AnimStatus.Pause);
            return Global.AnimStatus.Pause;
        }
    }
    public void Pause(){
        if(GetAnimationStatus() == Global.AnimStatus.Pause){
            return;
        }
        SetLastAnimStatus();
        CleanupExpiredObjects();
        // Debug.Log("PAUSE");
        SetAnimationStatus(Global.AnimStatus.Pause);
    }
    public void Forward(){
        if(GetAnimationStatus() == Global.AnimStatus.Disk || GetAnimationStatus() == Global.AnimStatus.Forward){
            return;
        }
        forwardFlag = true;
        SetAnimationStatus(Global.AnimStatus.Forward);
    }
    public void Rewind(){
        if(GetAnimationStatus() == Global.AnimStatus.Rewind){
            return;
        }
        rewindFlag = true;
        SetAnimationStatus(Global.AnimStatus.Rewind);
    }
    void ReferenceCounterUpdate(Global.AnimStatus status){
        if(status==Global.AnimStatus.Disk || status == Global.AnimStatus.Forward){
            // referenceCounter += Time.deltaTime;
            referenceCounter += Time.fixedDeltaTime;
            if(sliderControl.GetSliderMode() == Global.SliderMode.Normal){
                sliderControl.SetTimeSlider(referenceCounter);
            }
        }
        else if(status == Global.AnimStatus.Rewind){
            // if(referenceCounter - Time.deltaTime >= 0){
            //     referenceCounter -= Time.deltaTime;
            // }
            if(referenceCounter - Time.fixedDeltaTime >= 0){
                referenceCounter -= Time.fixedDeltaTime;
            }
            else{
                referenceCounter = 0f;
            }
            if(sliderControl.GetSliderMode() == Global.SliderMode.Normal){
                sliderControl.SetTimeSlider(referenceCounter);
            }
        }
        graphInput.ReferenceCounterValue(referenceCounter);
        // Debug.Log(referenceCounter);
    }
    public void SetLastAnimStatus(){
        if(GetAnimationStatus() == Global.AnimStatus.Disk){
            lastAnimStatus = Global.AnimStatus.Forward;
            return;
        }
        lastAnimStatus = GetAnimationStatus();
    }
    public Global.AnimStatus GetLastAnimStatus(){
        return lastAnimStatus;
    }

    public void DoJump(float timeDiff){
        if(timeDiff == 0){
            // No change in slider Restore the last animation status
            // Since it is changed to Pause when mouse button down event is detected on slider
            StartAnimationAction(GetLastAnimStatus());
            return;
        }
        else if(timeDiff < 0){
            // If time deifference is negative means need to rewind the game fast
            StartAnimationAction(Global.AnimStatus.Rewind);
        }
        else{
            // If time deifference is negative means need to forward the game fast
            StartAnimationAction(Global.AnimStatus.Forward);
        }
        referenceCounterThreshold = referenceCounter + timeDiff;
        // Set slider mode to jump
        sliderControl.SetSliderMode(Global.SliderMode.Jump);
        // Do fast forward
        AdjustSpeed(JUMP_SPEED_FACTOR);
    }

    void StartAnimationAction(Global.AnimStatus status){
        if(status == Global.AnimStatus.Pause){
            Pause();
        }
        if(status == Global.AnimStatus.Forward || status == Global.AnimStatus.Disk){
            Forward();
        }
        if(status == Global.AnimStatus.Rewind){
            Rewind();
        }
    }

    float GetReferenceCounterThreshold(){
        return referenceCounterThreshold;
    }

    void FixedUpdate(){
        counter++;
        rcCounter++;
        rc = rc + Time.fixedDeltaTime;
        // Debug.Log(rcCounter + " : " + rc + " : " + counter + " : " + referenceCounter);
        
        CleanupExpiredObjects();
        speed = BASE_SPEED * SPEED_FACTOR;
        if(sliderControl.GetSliderMode() == Global.SliderMode.Jump){
            if((GetAnimationStatus() == Global.AnimStatus.Rewind 
                && referenceCounter <= GetReferenceCounterThreshold())
                || ((GetAnimationStatus() == Global.AnimStatus.Forward 
                || GetAnimationStatus() == Global.AnimStatus.Disk )
                && referenceCounter >= GetReferenceCounterThreshold())){

                sliderControl.SetSliderMode(Global.SliderMode.Normal);
                StartAnimationAction(GetLastAnimStatus());
                // Normal speed
                AdjustSpeed(1);
            } 
        }

        Global.AnimStatus status = GetAnimationStatus();
        if(startCounter == true){
            ReferenceCounterUpdate(status);
        }

        if(status == Global.AnimStatus.Disk){
                ReadDisk();
        }
        else if(status == Global.AnimStatus.Forward){
            ReadForward();
        }
        else if(status == Global.AnimStatus.Rewind){
            ReadRewind();
        }
    }

    void RewindListPointerInc(){
        rewindListPointer++;
    }
    void RewindListPointerDec(){
        rewindListPointer--;
    }
    void SetRewindListPointer(int ptr){
        rewindListPointer = ptr;
    }
    int GetRewindListPointer(){
        return rewindListPointer;
    }

    void ForwardListPointerInc(){
        ForwardListPointer++;
    }
    void ForwardListPointerDec(){
        ForwardListPointer--;
    }
    void SetForwardListPointer(int ptr){
        ForwardListPointer = ptr;
    }
    int GetForwardListPointer(){
        return ForwardListPointer;
    }

    void CleanupExpiredObjects(){
        Global.AnimStatus status = GetAnimationStatus();
        if(status == Global.AnimStatus.Pause){
            return;
        }
        Vector3 startPos = new Vector3(0,0,0), endPos = new Vector3(0,0,0);

        // Find expired objects
        expiredObjects.Clear();
        foreach(GameObject go in runningObject.Keys){
            if(status == Global.AnimStatus.Forward || status == Global.AnimStatus.Disk){
                startPos = runningObject[go].sourcePos;
                endPos = runningObject[go].targetPos;
            }
            else if(status == Global.AnimStatus.Rewind){
                startPos = runningObject[go].targetPos;
                endPos = runningObject[go].sourcePos;
            }
            if( Vector3.Normalize(endPos - startPos) != Vector3.Normalize(endPos - go.transform.position) || 
                Vector3.Distance(endPos, go.transform.position) <= 1f*Time.timeScale){
                go.transform.position = endPos;
                expiredObjects.Add(go);
                if(status == Global.AnimStatus.Disk){
                    ObjectInfo oInfo = runningObject[go];
                    oInfo.expirationTime = referenceCounter;
                    rewindList.Add(oInfo);
                    if(oInfo.origin!="00000000"){
                        // Avoiding broadcast packet
                        graphInput.ExpiredPacketTargetNode((int)oInfo.packetTime, oInfo.target);
                    }
                }
            }
        }
        // Remove expired objects
        foreach(GameObject go in expiredObjects){
            runningPacketID.Remove(runningObject[go].packetID);
            runningObject.Remove(go);
            Destroy(go);
        }
    }

    void ReadForward(){
        if(forwardList[forwardList.Count-1].instantiationTime <= referenceCounter){
            // Disk mode is to be run
            SetAnimationStatus(Global.AnimStatus.Disk);
            return;
        }

        if(forwardFlag==true){
            SetForwardListPointer(0);
            while(GetForwardListPointer() < forwardList.Count && forwardList[GetForwardListPointer()].instantiationTime < referenceCounter){
                // Debug.Log("POLL FWD = " + forwardList[GetForwardListPointer()].instantiationTime + " : " + referenceCounter);
                ForwardListPointerInc();
            }
            forwardFlag = false;
        }

        int ptr = GetForwardListPointer();
        if(ptr < forwardList.Count ){
            // Debug.Log("FWD = " + ptr + " : " + forwardList[ptr].instantiationTime + " : " + referenceCounter);
        }
        

        while(ptr < forwardList.Count && forwardList[ptr].instantiationTime <= referenceCounter){
            // Debug.Log("FWD IN = " + ptr + " : " + forwardList[ptr].instantiationTime + " : " + referenceCounter);
            ObjectInfo oInfo = forwardList[ptr];
            GameObject go = InstantiatePacket(oInfo);
            // Instantiate on source position in forward
            go.transform.position = oInfo.sourcePos;
            oInfo.Object = go;
            forwardList[ptr] = oInfo;
            // Store the running object info to track it later
            runningObject.Add(go, oInfo);
            // runningPacketID.Add(oInfo.packetID);
            AddToRunningPacketID(oInfo);
            // If we reached at the end of the list, get out of this loop, 
            // later we will start reading from disk file
            if(ptr >= forwardList.Count - 1){
                SetAnimationStatus(Global.AnimStatus.Disk);
                break;
            }
            // Increment forward list pointer
            ForwardListPointerInc();
            ptr = GetForwardListPointer(); 
            // Debug.Log("FWD = " + ptr);
        }
        if(ptr >= forwardList.Count - 1){
            SetAnimationStatus(Global.AnimStatus.Disk);
        }

        // Move running Object further 
        foreach(GameObject go in runningObject.Keys){
            go.transform.position = go.transform.position + (runningObject[go].targetPos - go.transform.position).normalized * speed * Time.deltaTime;
        }
    }

    void ReadRewind(){
        if(rewindFlag==true){
            SetRewindListPointer(rewindList.Count - 1);
            while(GetRewindListPointer() >= 0 && rewindList[GetRewindListPointer()].expirationTime > referenceCounter){
                // Debug.Log("POLL RWD = " + rewindList[GetRewindListPointer()].expirationTime + " : " + referenceCounter);
                RewindListPointerDec();
            }
            rewindFlag = false;
        }
        int ptr = GetRewindListPointer();
        // int ptr = GetRewindListPointer();
        if(ptr >= 0){
            // Debug.Log("REV = " + ptr + " : " + rewindList[ptr].expirationTime + " : " + referenceCounter);
        }
        
        // Debug.Log("START = " + rewindList.Count + " : " + ptr + " : " + runningObject.Count);
        if(rewindList.Count != 0 && ptr < 0 && runningObject.Count==0){
            PacketCleanup();
            // Debug.Log("Rewind Ends"); 
            return;
        }

        while(ptr >= 0 && rewindList[ptr].expirationTime >= referenceCounter){
            ObjectInfo oInfo = rewindList[ptr];
            GameObject go = InstantiatePacket(oInfo);
            // Instantiate on target position in rewind
            go.transform.position = oInfo.targetPos;
            if(sliderControl.GetSliderMode() == Global.SliderMode.Jump){
                // Make packets invisible
                // go.transform.localScale = new Vector3(0,0,0);
            }
            oInfo.Object = go;
            rewindList[ptr] = oInfo;
            // Store the running object info to track it later
            runningObject.Add(go, oInfo);
            // runningPacketID.Add(oInfo.packetID);
            AddToRunningPacketID(oInfo);
            // Decrement rewind list pointer
            RewindListPointerDec();
            ptr = GetRewindListPointer();
            // Debug.Log("REV = " + ptr);
        }
        // Debug.Log("END   = " + rewindList.Count + " : " + ptr + " : " + runningObject.Count);
        // Move running Object further 
        foreach(GameObject go in runningObject.Keys){
            go.transform.position = go.transform.position + (runningObject[go].sourcePos - go.transform.position).normalized * speed * Time.deltaTime;
        }
    }

    void ReadDisk(){
        // Debug.Log("DISK = " + parseRemain + " : " + holdbackRemain + " : " + runningObject.Count);
        if(parseRemain==false && holdbackRemain==false && runningObject.Count==0){
            PacketCleanup();
            // Debug.Log("Update Ends"); 
            if(firstUpdate == false && firstPass == true){
                firstPass = false;
                // sliderControl.SetSliderMaxValue(referenceCounter);
                loadingPanel.SetActive(false);
            }
            return;
        }

        // Kept it here, Since above code takes time to execute and 
        // curent time changes so the animStartTime will be stale, 
        // which will generate multiple packets simultaneously in the begining
        if(firstUpdate == true){
            animStartTime = Time.time;
            // Debug.Log("animStartTime = " + Time.fixedTime + " : " + Time.fixedUnscaledTime + " : " + Time.unscaledTime + " : " + Time.time);
            referenceCounter = 0;
            startCounter = true;
            firstUpdate = false;
        }
        // if any packet in hold back queue is elligible to run, then run it
        InstantiateHoldBackPackets();

        // If the last parsed packet time meets the current time of animation the instantiate it
        if(parseRemain && nextPacketTime/Global.U_SEC <= referenceCounter){
            string timeStr;
            do{
                InstantiatePacket();
                // Parse next packet from file
                timeStr = packetTimeString.ReadLine();
                if(timeStr != null ){
                    nextPacketInfo = timeStr.Split(' ');
                    nextPacketTime = (float)Convert.ToInt32(nextPacketInfo[(int)PacketInfoIdx.Time]);
                    // float et = currentTime - animStartTime;
                    // Debug.Log("[" + window_counter + "] [" + counter + "] " + nextPacketTime/Global.U_SEC + " :: " + et + " :: " + nextPacketInfo[(int)PacketInfoIdx.Time] + " : " + nextPacketInfo[(int)PacketInfoIdx.Source] + " : " + nextPacketInfo[(int)PacketInfoIdx.Target]);
                    // Debug.Log("[" + window_counter + "] [" + counter + "] " + nextPacketTime/Global.U_SEC + " :: " + et);
                }
                else{
                    // enabled = false;
                    parseRemain = false;
                    break;
                }
                // parseRemain = false;
                // break;
            }while(nextPacketTime/Global.U_SEC <= referenceCounter);
            window_counter++;
        }

        // Move running Object further 
        foreach(GameObject go in runningObject.Keys){
            go.transform.position = go.transform.position + (runningObject[go].targetPos - go.transform.position).normalized * speed * Time.deltaTime;
        }
    }

    // Instantiate a packet and store it's info
    void InstantiatePacket(){
        // Debug.Log("nextPacketInfo (InstantiatePacket) = " + nextPacketInfo[(int)PacketInfoIdx.Time] + " : " + nextPacketInfo[(int)PacketInfoIdx.Source] + " : " + nextPacketInfo[(int)PacketInfoIdx.Target]);
        ObjectInfo oInfo = new ObjectInfo();
        oInfo.sourcePos = topo.GetNodePosition(nextPacketInfo[(int)PacketInfoIdx.Source]);
        oInfo.targetPos = topo.GetNodePosition(nextPacketInfo[(int)PacketInfoIdx.Target]);
        oInfo.source = nextPacketInfo[(int)PacketInfoIdx.Source];
        oInfo.target = nextPacketInfo[(int)PacketInfoIdx.Target];
        oInfo.packetTime = nextPacketTime;
        oInfo.origin = nextPacketInfo[(int)PacketInfoIdx.Origin];
        oInfo.destination = nextPacketInfo[(int)PacketInfoIdx.Destination];
        oInfo.packetID = nextPacketInfo[(int)PacketInfoIdx.Pid];
        oInfo.packetType = Global.PacketType.Normal;

        // Findout packet type
        try{
            if(nextPacketInfo[(int)PacketInfoIdx.Parity] == "PR"){
                oInfo.packetType = Global.PacketType.Parity;
            }
            else if(nextPacketInfo[(int)PacketInfoIdx.Parity] == "MCD"){
                oInfo.packetType = Global.PacketType.MCD;
            }
            else if(nextPacketInfo[(int)PacketInfoIdx.Parity] == "HC"){
                oInfo.packetType = Global.PacketType.HC;
            }
            else if(nextPacketInfo[(int)PacketInfoIdx.Parity] == "TCP"){
                oInfo.packetType = Global.PacketType.TCP;
            }
            else if(nextPacketInfo[(int)PacketInfoIdx.Parity] == "ICMP"){
                oInfo.packetType = Global.PacketType.ICMP;
            }
            else{
                oInfo.packetType = Global.PacketType.Normal;
            }
        }
        catch{
            oInfo.packetType = Global.PacketType.Normal;
        }
        // TODO hardcoded source and target
        if(oInfo.packetType == Global.PacketType.MCD){
            if(mcdCache.ContainsKey(oInfo.packetID)){
                if((oInfo.source == "p0a0" && oInfo.target == "dropper" 
                    && (mcdCache[oInfo.packetID].Item1 == "dropper" 
                    || mcdCache[oInfo.packetID].Item1 == "p0e0")) 
                    || mcdCache[oInfo.packetID].Item2==true){
                    // Debug.Log("MCD COLOR = " + oInfo.packetID);
                    oInfo.packetType = Global.PacketType.MCDcache;
                    mcdCache[oInfo.packetID] = new Tuple<string, bool>(oInfo.source, true);
                }
                else{
                    mcdCache[oInfo.packetID] = new Tuple<string, bool>(oInfo.source, false);
                }
            }
            else{
                mcdCache.Add(oInfo.packetID, new Tuple<string, bool>(oInfo.source, false));
            }
        }

        // Findout origin and destination of the pckets which doesn't have these
        if(oInfo.origin == "00000000" && OriginDestinationMap.ContainsKey(oInfo.packetID)==true){
            oInfo.origin = OriginDestinationMap[oInfo.packetID].Item1;
            oInfo.destination = OriginDestinationMap[oInfo.packetID].Item2;
            // Debug.Log("CHANGE = " + oInfo.packetID + " : " + oInfo.origin + " - " + oInfo.destination);
        }
        else if(oInfo.origin != "00000000" && OriginDestinationMap.ContainsKey(oInfo.packetID)==false){
            Tuple<string, string> orgDest = new Tuple<string, string>(oInfo.origin, oInfo.destination);
            OriginDestinationMap.Add(oInfo.packetID, orgDest);
        }

        // Approximate ack/reply packet finding
        try{
            if(idMap.ContainsKey(oInfo.packetID)){
                oInfo.packetID = idMap[oInfo.packetID];
            }
            else if(topo.IsHost(lastPacket.target) && topo.IsHost(oInfo.source) && lastPacket.target==oInfo.source && usedID.Contains(lastPacket.packetID)==false){
                idMap.Add(oInfo.packetID, lastPacket.packetID);
                oInfo.packetID = lastPacket.target;
                usedID.Add(lastPacket.packetID); 
            }
        }
        catch{
            // lastpacket info was null
        }
        lastPacket = oInfo;
        // // More trick
        // if(topo.IsHost(oInfo.target)){
        //     lastPacket = oInfo;
        // }
        

        // If packet is already running on link store the info in holdback queue for future reference (in time order) 
        if(runningPacketID.Contains(oInfo.packetID)){
            // Debug.Log("Enque = " + oInfo.packetTime + " " + oInfo.packetID);
            if(packetHoldBackQueue.ContainsKey(oInfo.packetID)){
                packetHoldBackQueue[oInfo.packetID].Enqueue(oInfo);
            }
            else{
                Queue<ObjectInfo> queue = new Queue<ObjectInfo>();
                queue.Enqueue(oInfo);
                packetHoldBackQueue.Add(oInfo.packetID, queue); 
            }
            return;
        }
        // If this is new packet, instantiate an object 
        // if(oInfo.source=="p1h0"){
        //     Debug.Log("Disk ACK = " + oInfo.packetID);
        // }
        // if(oInfo.source=="p1h0" && oInfo.packetType==Global.PacketType.Normal){
        //     Debug.Log("Disk ACK Normal = " + oInfo.packetID);
        // }
        // if(oInfo.source=="p0a0"){
        //     Debug.Log("Disk Green = " + oInfo.packetID);
        // }
        if(oInfo.source=="p1e0" && oInfo.target=="p1a0" && oInfo.packetType==Global.PacketType.TCP){
            Debug.Log("Disk = " + oInfo.source + " : " + oInfo.target + " : " + oInfo.packetID);
        }
        // Debug.Log("Disk = " + oInfo.source + " : " + oInfo.target + " : " + oInfo.packetID);
        // if(oInfo.target=="dropper"){
        //     Debug.Log("Disk To Dropper = " + oInfo.packetID);
        // }
        // if(oInfo.source=="dropper"){
        //     Debug.Log("Disk From Dropper = " + oInfo.packetID);
        // }
        GameObject go = InstantiatePacket(oInfo);
        go.transform.position = oInfo.sourcePos;
        oInfo.Object = go;

        // Store the running object info to track it later
        SetForwardListPointer(forwardList.Count);
        oInfo.instantiationTime = referenceCounter;
        forwardList.Add(oInfo);
        runningObject.Add(go, oInfo);
        // runningPacketID.Add(oInfo.packetID);
        AddToRunningPacketID(oInfo);
    }

    void InstantiateHoldBackPackets(){
        bool isRemain = false;
        foreach(var pid in packetHoldBackQueue.Keys){
            // If the packet is not running on the link then instantiate this packet
            if(packetHoldBackQueue[pid].Count > 0){
                if(runningPacketID.Contains(pid)==false){
                    ObjectInfo oInfo = packetHoldBackQueue[pid].Dequeue();
                    // Debug.Log("Deque = " + oInfo.packetTime + " " + oInfo.packetID);
                    // if(oInfo.source=="p1h0" && oInfo.packetType==Global.PacketType.Normal){
                    //     Debug.Log("Hold ACK Normal = " + oInfo.packetID);
                    // }
                    if(oInfo.source=="p1e0" && oInfo.target=="p1a0" && oInfo.packetType==Global.PacketType.TCP){
                        Debug.Log("Hold = " + oInfo.source + " : " + oInfo.target + " : " + oInfo.packetID);
                    }
                    // if(oInfo.target=="dropper"){
                    //     Debug.Log("Hold To Dropper = " + oInfo.packetID);
                    // }
                    // if(oInfo.source=="dropper"){
                    //     Debug.Log("Hold From Dropper = " + oInfo.packetID);
                    // }
                    GameObject go = InstantiatePacket(oInfo);
                    go.transform.position = oInfo.sourcePos;
                    oInfo.Object = go;

                    // Store the running object info to track it later
                    SetForwardListPointer(forwardList.Count);
                    oInfo.instantiationTime = referenceCounter;
                    forwardList.Add(oInfo);
                    runningObject.Add(go, oInfo);
                    // runningPacketID.Add(oInfo.packetID);
                    AddToRunningPacketID(oInfo);
                }
                isRemain = true;
            }
        }
        holdbackRemain = isRemain;
    }

    private GameObject InstantiatePacket(ObjectInfo oInfo){
        GameObject go;
        if(oInfo.packetType==Global.PacketType.Parity){
            go = Instantiate(parity_prefab) as GameObject;
        }
        else if(oInfo.packetType==Global.PacketType.MCD){
            go = Instantiate(mcd_prefab) as GameObject;
        }
        else if(oInfo.packetType==Global.PacketType.HC){
            go = Instantiate(hc_prefab) as GameObject;
        }
        else{
            go = Instantiate(packet_prefab) as GameObject;
        }

        go.GetComponent<MeshRenderer>().material.color = colorControl.GetPacketColor(oInfo.origin, oInfo.destination, oInfo.packetID, oInfo.packetType, go.GetComponent<MeshRenderer>().material.color);
        return go;
    }

    void AddToRunningPacketID(ObjectInfo oInfo){
        string pid = oInfo.packetID;
        if(oInfo.packetType==Global.PacketType.Parity){
            pid = pid + "PR";
        }
        runningPacketID.Add(pid);
    }
}


