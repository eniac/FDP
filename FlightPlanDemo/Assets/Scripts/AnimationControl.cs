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
    public string origin;
    public string destination;
    public string packetID;
};
public class AnimationControl : MonoBehaviour
{
    [SerializeField] Topology topo = default;
    [SerializeField] SliderControl sliderControl = default;
    [SerializeField] ColorControl colorControl = default;
    [SerializeField] float SPEED_FACTOR = 1;
    float JUMP_SPEED_FACTOR = 10;
    const float MERGE_WINDOW = 0.5f;   
    const float BASE_SPEED = 10.0f;
    Vector3 packetSize = new Vector3(0.7f, 0.7f, 0.7f);
    float speed;
    int counter;
    int window_counter;
    string elapsedTimeString;
    StringReader packetTimeString;
    float animStartTime;
    float nextPacketTime;
    GameObject packet_prefab;
    Dictionary<GameObject, ObjectInfo> runningObject;
    List<GameObject> expiredObjects;
    List<string> runningPacketID;
    Dictionary<string, Queue<ObjectInfo>> packetHoldBackQueue;
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
    Global.AnimStatus lastAnimStatus;
    bool startCounter;
    bool forwardFlag;
    bool rewindFlag;

    public enum PacketInfoIdx{
        Time=0,
        Source,
        Target,
        Origin,
        Destination,
        Pid
    }
    
    public void Start(){
        enabled = false;        // Stop calling update, it will only be called after StartAnimation
    }

    // Get file from file system or server
    public IEnumerator GetElapsedTimeFile(){
        // var filePath = Path.Combine(Application.streamingAssetsPath, "interval.txt");
        var filePath = Global.experimentMetadata;
        Debug.Log("metadata File = " + filePath);
        if (filePath.Contains ("://") || filePath.Contains (":///")) {
            Debug.Log("Anim WebRequest");
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

    public void AdjustSpeed(float speed_factor){
        SPEED_FACTOR = speed_factor;
    }

    public void ResetAnimation(){
        PacketCleanup();
    }
    public void StartAnimation(){
        // Debug.Log("Restarting Animation");


        counter = 1;
        window_counter = 1;
        speed = BASE_SPEED * SPEED_FACTOR;

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

        animStartTime = Time.time;

        // TODO : Empty file check
        nextPacketInfo = packetTimeString.ReadLine().Split(' ');
        nextPacketTime = (float)Convert.ToInt32(nextPacketInfo[(int)PacketInfoIdx.Time]);
        // Debug.Log("nextPacketInfo = " + nextPacketInfo[(int)PacketInfoIdx.Time] + ":" + nextPacketInfo[(int)PacketInfoIdx.Source] + ":" + nextPacketInfo[(int)PacketInfoIdx.Target]);
        counter++;

        topo.MakeLinksTransparent();
        topo.MakeNodesTransparent();
        SetAnimationStatus(Global.AnimStatus.Disk);
        sliderControl.SetSliderMode(Global.SliderMode.Normal);
        colorControl.ResetColorControl();

        Time.timeScale = 1;
        forwardFlag = false;
        rewindFlag = false;
        rewindListPointer = rewindList.Count - 1;
        ForwardListPointer = 0;
        referenceCounter = 0;
        startCounter = false;
        parseRemain = true;
        holdbackRemain = true;
        firstUpdate = true;
        enabled = true;
    }

    void PacketCleanup(){
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

        topo.MakeLinksOpaque();
        topo.MakeNodesOpaque();

        sliderControl.SetTimeSlider(0);

        enabled = false;
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
            referenceCounter += (Time.deltaTime * SPEED_FACTOR);
            if(sliderControl.GetSliderMode() == Global.SliderMode.Normal){
                sliderControl.SetTimeSlider(referenceCounter);
            }
        }
        else if(status == Global.AnimStatus.Rewind){
            if(referenceCounter - (Time.deltaTime * SPEED_FACTOR) >= 0){
                referenceCounter -= (Time.deltaTime * SPEED_FACTOR);
            }
            else{
                referenceCounter = 0f;
            }
            if(sliderControl.GetSliderMode() == Global.SliderMode.Normal){
                sliderControl.SetTimeSlider(referenceCounter);
            }
        }
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
        Time.timeScale = JUMP_SPEED_FACTOR;
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

    void Update(){
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
                Time.timeScale = 1;
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
            GameObject go = Instantiate(packet_prefab) as GameObject;
            go.GetComponent<MeshRenderer>().material.color = colorControl.GetPacketColor(oInfo.origin, oInfo.destination, oInfo.packetID);
            // Instantiate on source position in forward
            go.transform.position = oInfo.sourcePos;
            if(sliderControl.GetSliderMode() == Global.SliderMode.Jump){
                // Make packets invisible
                // go.transform.localScale = new Vector3(0,0,0);
            }
    
            oInfo.Object = go;
            forwardList[ptr] = oInfo;
            // Store the running object info to track it later
            runningObject.Add(go, oInfo);
            runningPacketID.Add(oInfo.packetID);
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
            GameObject go = Instantiate(packet_prefab) as GameObject;
            go.GetComponent<MeshRenderer>().material.color = colorControl.GetPacketColor(oInfo.origin, oInfo.destination, oInfo.packetID);
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
            runningPacketID.Add(oInfo.packetID);
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
            return;
        }

        // Kept it here, Since above code takes time to execute and 
        // curent time changes so the animStartTime will be stale, 
        // which will generate multiple packets simultaneously in the begining
        if(firstUpdate == true){
            animStartTime = Time.time;
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
                    counter++;
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
        oInfo.packetTime = nextPacketTime;
        oInfo.origin = nextPacketInfo[(int)PacketInfoIdx.Origin];
        oInfo.destination = nextPacketInfo[(int)PacketInfoIdx.Destination];
        oInfo.packetID = nextPacketInfo[(int)PacketInfoIdx.Pid];

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
        GameObject go = Instantiate(packet_prefab) as GameObject;
        go.GetComponent<MeshRenderer>().material.color = colorControl.GetPacketColor(oInfo.origin, oInfo.destination, oInfo.packetID);
        // Debug.Log("Instantiate = " + oInfo.packetTime + " " + oInfo.packetID);
        go.transform.position = oInfo.sourcePos;
        if(sliderControl.GetSliderMode() == Global.SliderMode.Jump){
            // Make packets invisible
            // go.transform.localScale = new Vector3(0,0,0);
        }
        oInfo.Object = go;

        // Store the running object info to track it later
        SetForwardListPointer(forwardList.Count);
        oInfo.instantiationTime = referenceCounter;
        forwardList.Add(oInfo);
        runningObject.Add(go, oInfo);
        runningPacketID.Add(oInfo.packetID);
    }

    void InstantiateHoldBackPackets(){
        bool isRemain = false;
        foreach(var pid in packetHoldBackQueue.Keys){
            // If the packet is not running on the link then instantiate this packet
            if(packetHoldBackQueue[pid].Count > 0){
                if(runningPacketID.Contains(pid)==false){
                    ObjectInfo oInfo = packetHoldBackQueue[pid].Dequeue();
                    // Debug.Log("Deque = " + oInfo.packetTime + " " + oInfo.packetID);
                    GameObject go = Instantiate(packet_prefab) as GameObject;
                    go.GetComponent<MeshRenderer>().material.color = colorControl.GetPacketColor(oInfo.origin, oInfo.destination, oInfo.packetID);
                    // Debug.Log("Instantiate = " + oInfo.packetTime + " " + oInfo.packetID);
                    go.transform.position = oInfo.sourcePos;
                    if(sliderControl.GetSliderMode() == Global.SliderMode.Jump){
                        // Make packets invisible
                        // go.transform.localScale = new Vector3(0,0,0);
                    }
                    oInfo.Object = go;

                    // Store the running object info to track it later
                    SetForwardListPointer(forwardList.Count);
                    oInfo.instantiationTime = referenceCounter;
                    forwardList.Add(oInfo);
                    runningObject.Add(go, oInfo);
                    runningPacketID.Add(oInfo.packetID);
                }
                isRemain = true;
            }
        }
        holdbackRemain = isRemain;
    }
}

