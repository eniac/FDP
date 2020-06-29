using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEngine;
using UnityEngine.Networking;

public class AnimationControl : MonoBehaviour
{
    float MAP_TIME;
    const float MERGE_WINDOW = 0.5f;   
    const float U_SEC = 1000000f;
    const float speed = 10.0f;
    int counter;
    int window_counter;
    [SerializeField] Topology topo = default;
    string elapsedTimeString;
    StringReader packetTimeString;
    float animStartTime;
    float nextPacketTime;
    GameObject packet_prefab;
    List<GameObject> runningObject;
    List<GameObject> expiredObjects;
    Dictionary<GameObject, Vector3> direction;
    Vector3 sourcePos;
    Vector3 targetPos;
    Vector3 dirNormalized;
    // bool initComplete = false;
    bool firstUpdate = true;
    
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

        if(expiredObjects==null && runningObject==null && direction==null){
            expiredObjects = new List<GameObject>();
            runningObject = new List<GameObject>();
            direction = new Dictionary<GameObject, Vector3>();
        }

        // Removal of objects if any remained while restarting the animation
        expiredObjects.Clear();
        foreach(GameObject go in runningObject){
            Debug.Log("Removal of Objects");
            direction.Remove(go);
            Destroy(go);
        }
        runningObject.Clear();

        sourcePos = topo.GetNodePosition("p0h0");
        targetPos = topo.GetNodePosition("p0e0");
        packet_prefab = Resources.Load("Packet") as GameObject;

        animStartTime = Time.time;

        // TODO : Empty file check
        nextPacketTime = (float)Convert.ToInt32(packetTimeString.ReadLine());
        counter++;

        topo.MakeLinksTransparent();
        topo.MakeNodesTransparent();

        firstUpdate = true;
        enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        // Removal of expired objects
        expiredObjects.Clear();
        foreach(GameObject go in runningObject){
            if(Vector3.Distance(targetPos, go.transform.position) <= 1f){
                // Debug.Log("Object Expired");
                go.transform.position = targetPos;
                expiredObjects.Add(go);
            }
        }
        runningObject.RemoveAll(x => expiredObjects.Contains(x));
        foreach(GameObject go in expiredObjects){
            direction.Remove(go);
            Destroy(go);
        }

        if(firstUpdate == true){
            animStartTime = Time.time;
            firstUpdate = false;
        }
        float currentTime = Time.time;  
        if(nextPacketTime/U_SEC <= currentTime - animStartTime){
            string timeStr;
            InstantiatePacket();
            do{
                timeStr = packetTimeString.ReadLine();
                if(timeStr != null || (timeStr!= null && timeStr.Length > 0)){
                    nextPacketTime = (float)Convert.ToInt32(timeStr);
                    float et = currentTime - animStartTime;
                    // Debug.Log("[" + window_counter + "] [" + counter + "] " + nextPacketTime/U_SEC + " :: " + et);
                    counter++;
                }
                else{
                    topo.MakeLinksOpaque();
                    topo.MakeNodesOpaque();
                    enabled = false;
                    Debug.Log("Update Ends"); 
                    break;
                }
            }while(nextPacketTime/U_SEC <= currentTime - animStartTime + MERGE_WINDOW);
            window_counter++;
        }

        // Move running Object further 
        foreach(GameObject go in runningObject){
            go.transform.position = go.transform.position + direction[go] * speed * Time.deltaTime;
        }
    }

    // Instantiate a packet
    void InstantiatePacket(){
        GameObject go = Instantiate(packet_prefab) as GameObject;
        go.transform.position = sourcePos;
        Vector3 dirNormalized = (targetPos - go.transform.position).normalized;
        runningObject.Add(go);
        direction.Add(go, dirNormalized);
    }

}
