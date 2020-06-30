using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEngine;
using UnityEngine.Networking;

struct ObjectInfo{
    public float packetTime;
    public Vector3 direction;
    public Vector3 sourcePos;
    public Vector3 targetPos;
};
public class AnimationControl : MonoBehaviour
{

    float MAP_TIME;
    const float MERGE_WINDOW = 0.5f;   
    const float U_SEC = 1000000f;
    const float speed = 5.0f;
    int counter;
    int window_counter;
    [SerializeField] Topology topo = default;
    string elapsedTimeString;
    StringReader packetTimeString;
    float animStartTime;
    float nextPacketTime;
    GameObject packet_prefab;
    // List<GameObject> runningObject;
    Dictionary<GameObject, ObjectInfo> runningObject;
    List<GameObject> expiredObjects;
    string[] nextPacketInfo;
    // Dictionary<GameObject, Vector3> direction;
    // Vector3 sourcePos;
    // Vector3 targetPos;
    Vector3 dirNormalized;
    // bool initComplete = false;
    bool firstUpdate = true;

    public enum PacketInfoIdx{
        Time=0,
        Source=1,
        Target=2
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

        if(expiredObjects==null && runningObject==null){
            expiredObjects = new List<GameObject>();
            runningObject = new Dictionary<GameObject, ObjectInfo>();
        }

        // Removal of objects if any remained while restarting the animation
        expiredObjects.Clear();
        foreach(GameObject go in runningObject.Keys){
            Destroy(go);
        }
        runningObject.Clear();

        // sourcePos = topo.GetNodePosition("p0h0");
        // targetPos = topo.GetNodePosition("p0e0");
        packet_prefab = Resources.Load("Packet") as GameObject;

        animStartTime = Time.time;

        // TODO : Empty file check
        nextPacketInfo = packetTimeString.ReadLine().Split(' ');
        nextPacketTime = (float)Convert.ToInt32(nextPacketInfo[(int)PacketInfoIdx.Time]);
        // Debug.Log("nextPacketInfo = " + nextPacketInfo[(int)PacketInfoIdx.Time] + ":" + nextPacketInfo[(int)PacketInfoIdx.Source] + ":" + nextPacketInfo[(int)PacketInfoIdx.Target]);
        counter++;

        topo.MakeLinksTransparent();
        topo.MakeNodesTransparent();

        firstUpdate = true;
        enabled = true;
        // InvokeRepeating("CheckExpiredObjects", 0f, 0.01f);
    }

    void CheckExpiredObjects(){
        // Find expired objects
        expiredObjects.Clear();
        foreach(GameObject go in runningObject.Keys){
            if(Vector3.Distance(runningObject[go].targetPos, go.transform.position) <= 1f){
                // Debug.Log("Object Expired");
                go.transform.position = runningObject[go].targetPos;
                expiredObjects.Add(go);
            }
        }
        // Remove expired objects
        foreach(GameObject go in expiredObjects){
            runningObject.Remove(go);
            Destroy(go);
        }
    }

    void Update(){
        // Find expired objects
        expiredObjects.Clear();
        foreach(GameObject go in runningObject.Keys){
            if(Time.time - runningObject[go].packetTime >= 
                Vector3.Distance(runningObject[go].sourcePos, runningObject[go].targetPos)/ speed ||
                Vector3.Distance(runningObject[go].targetPos, go.transform.position) <= 1f){
                // Debug.Log("Object Expired");
                go.transform.position = runningObject[go].targetPos;
                expiredObjects.Add(go);
            }
        }
        // Remove expired objects
        foreach(GameObject go in expiredObjects){
            runningObject.Remove(go);
            Destroy(go);
        }

        if(firstUpdate == true){
            animStartTime = Time.time;
            firstUpdate = false;
        }
        float currentTime = Time.time;  
        if(nextPacketTime/U_SEC <= currentTime - animStartTime){
            string timeStr;
            do{
                InstantiatePacket();
                timeStr = packetTimeString.ReadLine();
                if(timeStr != null ){
                    nextPacketInfo = timeStr.Split(' ');
                    nextPacketTime = (float)Convert.ToInt32(nextPacketInfo[(int)PacketInfoIdx.Time]);
                    float et = currentTime - animStartTime;
                    Debug.Log("[" + window_counter + "] [" + counter + "] " + nextPacketTime/U_SEC + " :: " + et + " :: " + nextPacketInfo[(int)PacketInfoIdx.Time] + " : " + nextPacketInfo[(int)PacketInfoIdx.Source] + " : " + nextPacketInfo[(int)PacketInfoIdx.Target]);
                    // Debug.Log("[" + window_counter + "] [" + counter + "] " + nextPacketTime/U_SEC + " :: " + et);
                    counter++;
                }
                else{
                    Debug.Log("timeStr = " + timeStr + " nextPacketInfo = " + nextPacketInfo);
                    topo.MakeLinksOpaque();
                    topo.MakeNodesOpaque();
                    enabled = false;
                    Debug.Log("Update Ends"); 
                    break;
                }
                break;
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

        GameObject go = Instantiate(packet_prefab) as GameObject;
        go.transform.position = oInfo.sourcePos;
        oInfo.direction = (oInfo.targetPos - go.transform.position).normalized;

        runningObject.Add(go, oInfo);
    }


}
