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
    [SerializeField] Topology topo = default;
    string elapsedTimeString;
    StringReader packetTimeString;
    float animStartTime;
    float nextPacketTime;
    [SerializeField] float speed = 10.0f;
    GameObject packet_prefab;
    List<GameObject> runningObject;
    List<GameObject> expiredObjects;
    Dictionary<GameObject, Vector3> direction;
    Vector3 sourcePos;
    Vector3 targetPos;
    Vector3 dirNormalized;
    bool initComplete = false;
    bool firstUpdate = true;
    
    // Get file from file system or server
    public IEnumerator GetElapsedTimeFile(){
        initComplete = false;

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

    public void AnimationInit(){

        packetTimeString = new StringReader(elapsedTimeString);

        expiredObjects = new List<GameObject>();
        runningObject = new List<GameObject>();
        direction = new Dictionary<GameObject, Vector3>();

        sourcePos = topo.GetNodePosition("p0h2");
        targetPos = topo.GetNodePosition("p0e1");
        packet_prefab = Resources.Load("Packet") as GameObject;

        animStartTime = Time.time;
        nextPacketTime = Convert.ToInt32(packetTimeString.ReadLine());

        float median = 54425421f;
        MAP_TIME = (Vector3.Distance(targetPos, sourcePos)/speed)/median;
        Debug.Log("time to reach packets to it's destination = " + MAP_TIME);

        firstUpdate = true;
    }
    public void StartAnimation(){
        topo.MakeLinksTransparent();
        initComplete = true;
    }

    // Update is called once per frame
    void Update()
    {
        if(initComplete==false){
            return;
        }

        // Removal of expired objects
        expiredObjects.Clear();
        foreach(GameObject go in runningObject){
            if(Vector3.Distance(targetPos, go.transform.position) <= 2f){
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

        // Instantiate new ready object
        if(firstUpdate==true || (float)nextPacketTime*MAP_TIME <= Time.time - animStartTime){
            if(firstUpdate==true){
                firstUpdate = false;
                animStartTime = Time.time;
            }
            float et = Time.time - animStartTime;
            Debug.Log("nextPacketTime = " + nextPacketTime*MAP_TIME + "  , Time = " + et);
            GameObject go = Instantiate(packet_prefab) as GameObject;
            go.transform.position = sourcePos;
            Vector3 dirNormalized = (targetPos - go.transform.position).normalized;
            runningObject.Add(go);
            direction.Add(go, dirNormalized);
            string timeStr = packetTimeString.ReadLine();
            if(timeStr != null){
                if(timeStr.Length>0){
                    nextPacketTime = Convert.ToInt32(timeStr);
                }
                else{
                    topo.MakeLinksOpaque();
                    enabled = false;
                    Debug.Log("Update Ends"); 
                }
            }
            else{
                topo.MakeLinksOpaque();
                enabled = false;
                Debug.Log("Update Ends"); 
            }
        }

        // Move running Object further 
        foreach(GameObject go in runningObject){
            go.transform.position = go.transform.position + direction[go] * speed * Time.deltaTime;
        }
    }

}
