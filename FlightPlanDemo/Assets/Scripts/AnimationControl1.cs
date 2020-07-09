// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.IO;
// using System.Text;

// using UnityEngine;
// using UnityEngine.Networking;

// struct ObjectInfo{
//     public GameObject Object;
//     public float packetTime;
//     public float expirationTime;
//     public float instantiationTime;
//     public Vector3 direction;
//     public Vector3 sourcePos;
//     public Vector3 targetPos;
//     public string packetID;
// };
// public class AnimationControl1 : MonoBehaviour
// {
//     [SerializeField] Topology topo = default;
//     const float MERGE_WINDOW = 0.5f;   
//     const float U_SEC = 1000000f;
//     const float speed = 10.0f;
//     int counter;
//     int window_counter;
//     string elapsedTimeString;
//     StringReader packetTimeString;
//     float animStartTime;
//     float nextPacketTime;
//     GameObject packet_prefab;
//     Dictionary<GameObject, ObjectInfo> runningObject;
//     List<GameObject> expiredObjects;
//     List<string> runningPacketID;
//     Dictionary<string, Queue<ObjectInfo>> packetHoldBackQueue;
//     List<ObjectInfo> rewindList;
//     int rewindListPointer;
//     List<ObjectInfo> forwardList;
//     int ForwardListPointer;
//     string[] nextPacketInfo;
//     bool firstUpdate = true;
//     bool parseRemain = true;
//     bool holdbackRemain = true;
//     AnimStatus animationStatus;
//     float referenceCounter;
//     bool startCounter;
//     bool forwardFlag;
//     bool rewindFlag;

//     public enum PacketInfoIdx{
//         Time=0,
//         Source=1,
//         Target=2,
//         Pid=3
//     }

//     public enum AnimStatus{
//         Pause=0,
//         Forward,
//         Rewind,
//         Disk
//     }
    
//     public void Start(){
//         enabled = false;        // Stop calling update, it will only be called after StartAnimation
//     }

//     // Get file from file system or server
//     public IEnumerator GetElapsedTimeFile(){
//         var filePath = Path.Combine(Application.streamingAssetsPath, "interval.txt");
        
//         if (filePath.Contains ("://") || filePath.Contains (":///")) {
//             // Using UnityWebRequest class
//             var loaded = new UnityWebRequest(filePath);
//             loaded.downloadHandler = new DownloadHandlerBuffer();
//             yield return loaded.SendWebRequest();
//             elapsedTimeString = loaded.downloadHandler.text;
//         }
//         else{
//             elapsedTimeString = File.ReadAllText(filePath);
//         }
//     }

//     public void StartAnimation(){
//         Debug.Log("Restarting Animation");
//         counter = 1;
//         window_counter = 1;

//         // Objects initialization
//         packetTimeString = new StringReader(elapsedTimeString);

//         if(expiredObjects==null && runningObject==null && packetHoldBackQueue==null){
//             expiredObjects = new List<GameObject>();
//             runningObject = new Dictionary<GameObject, ObjectInfo>();
//             runningPacketID = new List<string>();
//             packetHoldBackQueue = new Dictionary<string, Queue<ObjectInfo>>();
//             rewindList = new List<ObjectInfo>();
//             forwardList = new List<ObjectInfo>();
//         }

//         // Removal of objects if any remained while restarting the animation
//         expiredObjects.Clear();
//         foreach(GameObject go in runningObject.Keys){
//             Destroy(go);
//         }
//         runningPacketID.Clear();
//         runningObject.Clear();
//         packetHoldBackQueue.Clear();
//         rewindList.Clear();

//         packet_prefab = Resources.Load("Packet") as GameObject;

//         animStartTime = Time.time;

//         // TODO : Empty file check
//         nextPacketInfo = packetTimeString.ReadLine().Split(' ');
//         nextPacketTime = (float)Convert.ToInt32(nextPacketInfo[(int)PacketInfoIdx.Time]);
//         // Debug.Log("nextPacketInfo = " + nextPacketInfo[(int)PacketInfoIdx.Time] + ":" + nextPacketInfo[(int)PacketInfoIdx.Source] + ":" + nextPacketInfo[(int)PacketInfoIdx.Target]);
//         counter++;

//         topo.MakeLinksTransparent();
//         topo.MakeNodesTransparent();
//         SetAnimationStatus(AnimStatus.Disk);

//         forwardFlag = false;
//         rewindFlag = false;
//         rewindListPointer = rewindList.Count - 1;
//         ForwardListPointer = 0;
//         referenceCounter = 0;
//         startCounter = false;
//         parseRemain = true;
//         holdbackRemain = true;
//         firstUpdate = true;
//         enabled = true;
//     }

//     void SetAnimationStatus(AnimStatus status){
//         animationStatus = status;
//     }
//     AnimStatus GetAnimationStatus(){
//         return animationStatus;
//     }
//     public void Pause(){
//         if(GetAnimationStatus() == AnimStatus.Pause){
//             return;
//         }
//         Debug.Log("PAUSE");
//         SetAnimationStatus(AnimStatus.Pause);
//     }
//     public void Forward(){
//         if(GetAnimationStatus() == AnimStatus.Disk || GetAnimationStatus() == AnimStatus.Forward){
//             return;
//         }
//         forwardFlag = true;
//         SetAnimationStatus(AnimStatus.Forward);
//     }
//     public void Rewind(){
//         if(GetAnimationStatus() == AnimStatus.Rewind){
//             return;
//         }
//         rewindFlag = true;
//         SetAnimationStatus(AnimStatus.Rewind);
//     }
//     void ReferenceCounterUpdate(AnimStatus status){
//         if(status==AnimStatus.Disk || status == AnimStatus.Forward){
//             referenceCounter += Time.deltaTime;
//         }
//         else if(status == AnimStatus.Rewind){
//             if(referenceCounter - Time.deltaTime >= 0){
//                 referenceCounter -= Time.deltaTime;
//             }
//         }
//     }

//     void Update(){
//         AnimStatus status = GetAnimationStatus();
//         if(startCounter == true){
//             ReferenceCounterUpdate(status);
//         }

//         if(status == AnimStatus.Disk){
//                 ReadDisk();
//         }
//         else if(status == AnimStatus.Forward){
//             ReadForward();
//         }
//         else if(status == AnimStatus.Rewind){
//             ReadRewind();
//         }
//     }

//     void RewindListPointerInc(){
//         rewindListPointer++;
//     }
//     void RewindListPointerDec(){
//         rewindListPointer--;
//     }
//     void SetRewindListPointer(int ptr){
//         rewindListPointer = ptr;
//     }
//     int GetRewindListPointer(){
//         return rewindListPointer;
//     }

//     void ForwardListPointerInc(){
//         ForwardListPointer++;
//     }
//     void ForwardListPointerDec(){
//         ForwardListPointer--;
//     }
//     void SetForwardListPointer(int ptr){
//         ForwardListPointer = ptr;
//     }
//     int GetForwardListPointer(){
//         return ForwardListPointer;
//     }

//     void ReadForward(){
//         // Find expired objects
//         expiredObjects.Clear();
//         foreach(GameObject go in runningObject.Keys){
//             if(Vector3.Distance(runningObject[go].targetPos, go.transform.position) <= 1f){
//                 go.transform.position = runningObject[go].targetPos;
//                 expiredObjects.Add(go);
//             }
//         }
//         // Remove expired objects
//         foreach(GameObject go in expiredObjects){
//             runningPacketID.Remove(runningObject[go].packetID);
//             runningObject.Remove(go);
//             Destroy(go);
//         }

//         if(forwardFlag==true){
//             SetForwardListPointer(0);
//             while(GetForwardListPointer() < forwardList.Count && forwardList[GetForwardListPointer()].instantiationTime < referenceCounter){
//                 Debug.Log("POLL FWD = " + forwardList[GetForwardListPointer()].instantiationTime + " : " + referenceCounter);
//                 ForwardListPointerInc();
//             }
//             forwardFlag = false;
//         }

//         int ptr = GetForwardListPointer();
//         if(ptr < forwardList.Count ){
//             Debug.Log("FWD = " + ptr + " : " + forwardList[ptr].instantiationTime + " : " + referenceCounter);
//         }
        

//         while(ptr < forwardList.Count && forwardList[ptr].instantiationTime <= referenceCounter){
//             Debug.Log("FWD IN = " + ptr + " : " + forwardList[ptr].instantiationTime + " : " + referenceCounter);
//             // if(runningObject.ContainsKey(forwardList[ptr].Object)==true){
//             //     // Skip inflight packets
//             //     RewindListPointerInc();
//             //     ptr = GetRewindListPointer() + 1; 
//             //     continue;
//             // }
//             ObjectInfo oInfo = forwardList[ptr];
//             GameObject go = Instantiate(packet_prefab) as GameObject;
//             // Instantiate on source position in forward
//             go.transform.position = oInfo.sourcePos;
//             oInfo.Object = go;
//             forwardList[ptr] = oInfo;
//             // Store the running object info to track it later
//             runningObject.Add(go, oInfo);
//             runningPacketID.Add(oInfo.packetID);
//             // If we reached at the end of the list, get out of this loop, 
//             // later we will start reading from disk file
//             if(ptr >= forwardList.Count - 1){
//                 SetAnimationStatus(AnimStatus.Disk);
//                 break;
//             }
//             // Increment forward list pointer
//             ForwardListPointerInc();
//             ptr = GetForwardListPointer(); 
//             Debug.Log("FWD = " + ptr);
//         }
//         if(ptr >= forwardList.Count - 1){
//             SetAnimationStatus(AnimStatus.Disk);
//         }

//         // Move running Object further 
//         foreach(GameObject go in runningObject.Keys){
//             go.transform.position = go.transform.position + runningObject[go].direction * speed * Time.deltaTime;
//         }
//     }

//     void ReadRewind(){
//         // Find expired objects
//         expiredObjects.Clear();
//         foreach(GameObject go in runningObject.Keys){
//             if(Vector3.Distance(runningObject[go].sourcePos, go.transform.position) <= 1f){
//                 go.transform.position = runningObject[go].sourcePos;
//                 expiredObjects.Add(go);
//             }
//         }
//         // Remove expired objects
//         foreach(GameObject go in expiredObjects){
//             runningPacketID.Remove(runningObject[go].packetID);
//             runningObject.Remove(go);
//             Destroy(go);
//         }

//         if(rewindFlag==true){
//             SetRewindListPointer(rewindList.Count - 1);
//             while(GetRewindListPointer() >= 0 && rewindList[GetRewindListPointer()].expirationTime > referenceCounter){
//                 Debug.Log("POLL RWD = " + rewindList[GetRewindListPointer()].expirationTime + " : " + referenceCounter);
//                 RewindListPointerDec();
//             }
//             rewindFlag = false;
//         }
//         int ptr = GetRewindListPointer();
//         // int ptr = GetRewindListPointer();
//         if(ptr >= 0){
//             Debug.Log("REV = " + ptr + " : " + rewindList[ptr].expirationTime + " : " + referenceCounter);
//         }
        
//         // Debug.Log("START = " + rewindList.Count + " : " + ptr + " : " + runningObject.Count);
//         if(rewindList.Count != 0 && ptr < 0 && runningObject.Count==0){
//             topo.MakeLinksOpaque();
//             topo.MakeNodesOpaque();
//             Debug.Log("Rewind Ends"); 
//             enabled = false;
//             return;
//         }

//         while(ptr >= 0 && rewindList[ptr].expirationTime >= referenceCounter){
//             ObjectInfo oInfo = rewindList[ptr];
//             GameObject go = Instantiate(packet_prefab) as GameObject;
//             // Instantiate on target position in rewind
//             go.transform.position = oInfo.targetPos;
//             oInfo.Object = go;
//             rewindList[ptr] = oInfo;
//             // Store the running object info to track it later
//             runningObject.Add(go, oInfo);
//             runningPacketID.Add(oInfo.packetID);
//             // Decrement rewind list pointer
//             RewindListPointerDec();
//             ptr = GetRewindListPointer();
//             Debug.Log("REV = " + ptr);
//         }
//         // Debug.Log("END   = " + rewindList.Count + " : " + ptr + " : " + runningObject.Count);
//         // Move running Object further 
//         foreach(GameObject go in runningObject.Keys){
//             go.transform.position = go.transform.position + (runningObject[go].sourcePos - go.transform.position).normalized * speed * Time.deltaTime;
//         }
//     }

//     void ReadDisk(){
//         // Find expired objects
//         expiredObjects.Clear();
//         foreach(GameObject go in runningObject.Keys){
//             if(Vector3.Distance(runningObject[go].targetPos, go.transform.position) <= 1f){
//                 // Debug.Log("Object Expired");
//                 go.transform.position = runningObject[go].targetPos;
//                 expiredObjects.Add(go);
//                 ObjectInfo oInfo = runningObject[go];
//                 oInfo.expirationTime = Time.time - animStartTime;
//                 rewindList.Add(oInfo);
//                 // Debug.Log("RP Reset = " + GetRewindListPointer());
//             }
//         }
//         // Remove expired objects
//         foreach(GameObject go in expiredObjects){
//             runningPacketID.Remove(runningObject[go].packetID);
//             runningObject.Remove(go);
//             Destroy(go);
//         }

//         if(parseRemain==false && holdbackRemain==false && runningObject.Count==0){
//             topo.MakeLinksOpaque();
//             topo.MakeNodesOpaque();
//             Debug.Log("Update Ends"); 
//             enabled = false;
//         }

//         // Kept it here, Since above code takes time to execute and 
//         // curent time changes so the animStartTime will be stale, 
//         // which will generate multiple packets simultaneously in the begining
//         if(firstUpdate == true){
//             animStartTime = Time.time;
//             referenceCounter = 0;
//             startCounter = true;
//             firstUpdate = false;
//         }
//         // if any packet in hold back queue is elligible to run, then run it
//         InstantiateHoldBackPackets();

//         // If the last parsed packet time meets the current time of animation the instantiate it
//         float currentTime = Time.time;  
//         if(parseRemain && nextPacketTime/U_SEC <= currentTime - animStartTime){
//             string timeStr;
//             do{
//                 InstantiatePacket();
//                 // Parse next packet from file
//                 timeStr = packetTimeString.ReadLine();
//                 if(timeStr != null ){
//                     nextPacketInfo = timeStr.Split(' ');
//                     nextPacketTime = (float)Convert.ToInt32(nextPacketInfo[(int)PacketInfoIdx.Time]);
//                     float et = currentTime - animStartTime;
//                     // Debug.Log("[" + window_counter + "] [" + counter + "] " + nextPacketTime/U_SEC + " :: " + et + " :: " + nextPacketInfo[(int)PacketInfoIdx.Time] + " : " + nextPacketInfo[(int)PacketInfoIdx.Source] + " : " + nextPacketInfo[(int)PacketInfoIdx.Target]);
//                     // Debug.Log("[" + window_counter + "] [" + counter + "] " + nextPacketTime/U_SEC + " :: " + et);
//                     counter++;
//                 }
//                 else{
//                     // enabled = false;
//                     parseRemain = false;
//                     break;
//                 }
//                 // parseRemain = false;
//                 // break;
//             }while(nextPacketTime/U_SEC <= currentTime - animStartTime);
//             window_counter++;
//         }

//         // Move running Object further 
//         foreach(GameObject go in runningObject.Keys){
//             go.transform.position = go.transform.position + runningObject[go].direction * speed * Time.deltaTime;
//         }
//     }

//     // Instantiate a packet and store it's info
//     void InstantiatePacket(){
//         // Debug.Log("nextPacketInfo (InstantiatePacket) = " + nextPacketInfo[(int)PacketInfoIdx.Time] + " : " + nextPacketInfo[(int)PacketInfoIdx.Source] + " : " + nextPacketInfo[(int)PacketInfoIdx.Target]);
//         ObjectInfo oInfo = new ObjectInfo();
//         oInfo.sourcePos = topo.GetNodePosition(nextPacketInfo[(int)PacketInfoIdx.Source]);
//         oInfo.targetPos = topo.GetNodePosition(nextPacketInfo[(int)PacketInfoIdx.Target]);
//         oInfo.packetTime = nextPacketTime;
//         oInfo.packetID = nextPacketInfo[(int)PacketInfoIdx.Pid];

//         // If packet is already running on link store the info in holdback queue for future reference (in time order) 
//         if(runningPacketID.Contains(oInfo.packetID)){
//             // Debug.Log("Enque = " + oInfo.packetTime + " " + oInfo.packetID);
//             if(packetHoldBackQueue.ContainsKey(oInfo.packetID)){
//                 packetHoldBackQueue[oInfo.packetID].Enqueue(oInfo);
//             }
//             else{
//                 Queue<ObjectInfo> queue = new Queue<ObjectInfo>();
//                 queue.Enqueue(oInfo);
//                 packetHoldBackQueue.Add(oInfo.packetID, queue); 
//             }
//             return;
//         }
//         // If this is new packet, instantiate an object 
//         GameObject go = Instantiate(packet_prefab) as GameObject;
//         // Debug.Log("Instantiate = " + oInfo.packetTime + " " + oInfo.packetID);
//         go.transform.position = oInfo.sourcePos;
//         oInfo.Object = go;
//         oInfo.direction = (oInfo.targetPos - go.transform.position).normalized;

//         // Store the running object info to track it later
//         SetForwardListPointer(forwardList.Count);
//         oInfo.instantiationTime = Time.time - animStartTime;
//         forwardList.Add(oInfo);
//         runningObject.Add(go, oInfo);
//         runningPacketID.Add(oInfo.packetID);
//     }

//     void InstantiateHoldBackPackets(){
//         bool isRemain = false;
//         foreach(var pid in packetHoldBackQueue.Keys){
//             // If the packet is not running on the link then instantiate this packet
//             if(packetHoldBackQueue[pid].Count > 0){
//                 if(runningPacketID.Contains(pid)==false){
//                     ObjectInfo oInfo = packetHoldBackQueue[pid].Dequeue();
//                     // Debug.Log("Deque = " + oInfo.packetTime + " " + oInfo.packetID);
//                     GameObject go = Instantiate(packet_prefab) as GameObject;
//                     // Debug.Log("Instantiate = " + oInfo.packetTime + " " + oInfo.packetID);
//                     go.transform.position = oInfo.sourcePos;
//                     oInfo.Object = go;
//                     oInfo.direction = (oInfo.targetPos - go.transform.position).normalized;

//                     // Store the running object info to track it later
//                     SetForwardListPointer(forwardList.Count);
//                     oInfo.instantiationTime = Time.time - animStartTime;
//                     forwardList.Add(oInfo);
//                     runningObject.Add(go, oInfo);
//                     runningPacketID.Add(oInfo.packetID);
//                 }
//                 isRemain = true;
//             }
//         }
//         holdbackRemain = isRemain;
//     }
// }
