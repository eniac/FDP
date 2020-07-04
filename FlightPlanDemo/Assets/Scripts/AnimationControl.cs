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
    public Vector3 direction;
    public Vector3 sourcePos;
    public Vector3 targetPos;
    public string packetID;
};
public class AnimationControl : MonoBehaviour
{
    [SerializeField] Topology topo = default;
    const float MERGE_WINDOW = 0.5f;   
    const float U_SEC = 1000000f;
    const float speed = 10.0f;
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
    string[] nextPacketInfo;
    bool firstUpdate = true;
    bool parseRemain = true;
    bool holdbackRemain = true;

    public enum PacketInfoIdx{
        Time=0,
        Source=1,
        Target=2,
        Pid=3
    }
    
    public void Start(){
        enabled = false;        // Stop calling update, it will only be called after StartAnimation
    }

    // Get file from file system or server
    public IEnumerator GetElapsedTimeFile(){
        var filePath = Path.Combine(Application.streamingAssetsPath, "interval.txt");
        
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

    public void StartAnimation(){
        Debug.Log("Restarting Animation");
        counter = 1;
        window_counter = 1;

        // Objects initialization
        packetTimeString = new StringReader(elapsedTimeString);

        if(expiredObjects==null && runningObject==null && packetHoldBackQueue==null){
            expiredObjects = new List<GameObject>();
            runningObject = new Dictionary<GameObject, ObjectInfo>();
            runningPacketID = new List<string>();
            packetHoldBackQueue = new Dictionary<string, Queue<ObjectInfo>>();
        }

        // Removal of objects if any remained while restarting the animation
        expiredObjects.Clear();
        foreach(GameObject go in runningObject.Keys){
            Destroy(go);
        }
        runningPacketID.Clear();
        runningObject.Clear();
        packetHoldBackQueue.Clear();

        packet_prefab = Resources.Load("Packet") as GameObject;

        animStartTime = Time.time;

        // TODO : Empty file check
        nextPacketInfo = packetTimeString.ReadLine().Split(' ');
        nextPacketTime = (float)Convert.ToInt32(nextPacketInfo[(int)PacketInfoIdx.Time]);
        // Debug.Log("nextPacketInfo = " + nextPacketInfo[(int)PacketInfoIdx.Time] + ":" + nextPacketInfo[(int)PacketInfoIdx.Source] + ":" + nextPacketInfo[(int)PacketInfoIdx.Target]);
        counter++;

        topo.MakeLinksTransparent();
        topo.MakeNodesTransparent();

        parseRemain = true;
        holdbackRemain = true;
        firstUpdate = true;
        enabled = true;
    }

    void Update(){
        // Find expired objects
        expiredObjects.Clear();
        foreach(GameObject go in runningObject.Keys){
            // if(Time.time - runningObject[go].packetTime >= 
            //     Vector3.Distance(runningObject[go].sourcePos, runningObject[go].targetPos)/ speed ||
            //     Vector3.Distance(runningObject[go].targetPos, go.transform.position) <= 1f){
            if(Vector3.Distance(runningObject[go].targetPos, go.transform.position) <= 1f){
                // Debug.Log("Object Expired");
                go.transform.position = runningObject[go].targetPos;
                expiredObjects.Add(go);
            }
        }
        // Remove expired objects
        foreach(GameObject go in expiredObjects){
            runningPacketID.Remove(runningObject[go].packetID);
            runningObject.Remove(go);
            Destroy(go);
        }

        if(parseRemain==false && holdbackRemain==false && runningObject.Count==0){
            topo.MakeLinksOpaque();
            topo.MakeNodesOpaque();
            Debug.Log("Update Ends"); 
            enabled = false;
        }

        if(firstUpdate == true){
            animStartTime = Time.time;
            firstUpdate = false;
        }
        // if any packet in hold back queue is elligible to run, then run it
        InstantiateHoldBackPackets();

        // If the last parsed packet time meets the current time of animation the instantiate it
        float currentTime = Time.time;  
        if(parseRemain && nextPacketTime/U_SEC <= currentTime - animStartTime){
            string timeStr;
            do{
                InstantiatePacket();
                // Parse next packet from file
                timeStr = packetTimeString.ReadLine();
                if(timeStr != null ){
                    nextPacketInfo = timeStr.Split(' ');
                    nextPacketTime = (float)Convert.ToInt32(nextPacketInfo[(int)PacketInfoIdx.Time]);
                    float et = currentTime - animStartTime;
                    // Debug.Log("[" + window_counter + "] [" + counter + "] " + nextPacketTime/U_SEC + " :: " + et + " :: " + nextPacketInfo[(int)PacketInfoIdx.Time] + " : " + nextPacketInfo[(int)PacketInfoIdx.Source] + " : " + nextPacketInfo[(int)PacketInfoIdx.Target]);
                    // Debug.Log("[" + window_counter + "] [" + counter + "] " + nextPacketTime/U_SEC + " :: " + et);
                    counter++;
                }
                else{
                    // enabled = false;
                    parseRemain = false;
                    break;
                }
                // parseRemain = false;
                // break;
            }while(nextPacketTime/U_SEC <= currentTime - animStartTime);
            window_counter++;
        }

        // Move running Object further 
        foreach(GameObject go in runningObject.Keys){
            go.transform.position = go.transform.position + runningObject[go].direction * speed * Time.deltaTime;
        }
    }

    // Instantiate a packet and store it's info
    void InstantiatePacket(){
        // Debug.Log("nextPacketInfo (InstantiatePacket) = " + nextPacketInfo[(int)PacketInfoIdx.Time] + " : " + nextPacketInfo[(int)PacketInfoIdx.Source] + " : " + nextPacketInfo[(int)PacketInfoIdx.Target]);
        ObjectInfo oInfo = new ObjectInfo();
        oInfo.sourcePos = topo.GetNodePosition(nextPacketInfo[(int)PacketInfoIdx.Source]);
        oInfo.targetPos = topo.GetNodePosition(nextPacketInfo[(int)PacketInfoIdx.Target]);
        oInfo.packetTime = nextPacketTime;
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
        // Debug.Log("Instantiate = " + oInfo.packetTime + " " + oInfo.packetID);
        go.transform.position = oInfo.sourcePos;
        oInfo.Object = go;
        oInfo.direction = (oInfo.targetPos - go.transform.position).normalized;

        // Store the running object info to track it later
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
                    // Debug.Log("Instantiate = " + oInfo.packetTime + " " + oInfo.packetID);
                    go.transform.position = oInfo.sourcePos;
                    oInfo.Object = go;
                    oInfo.direction = (oInfo.targetPos - go.transform.position).normalized;

                    // Store the running object info to track it later
                    runningObject.Add(go, oInfo);
                    runningPacketID.Add(oInfo.packetID);
                }
                isRemain = true;
            }
        }
        holdbackRemain = isRemain;
    }
}
