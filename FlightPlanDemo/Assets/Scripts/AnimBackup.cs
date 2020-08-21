using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

using UnityEngine;
using UnityEngine.Networking;

// struct PacketInfo{
//     public GameObject Object;
//     public float packetTime;
//     public float expirationTime;
//     public float instantiationTime;
//     public Vector3 sourcePos;
//     public Vector3 targetPos;
//     public string source;
//     public string target;
//     public string origin;
//     public string destination;
//     public string packetID;
//     public Global.PacketType packetType;
// };
public class AnimBackup : MonoBehaviour
{
    [SerializeField] Topology topo = default;
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
    string elapsedTimeString;
    float referenceCounter=0;
    Global.AnimStatus animStatus = Global.AnimStatus.PrePlay;
    Dictionary<string, Global.PacketType> PacketTypeInfo = new Dictionary<string, Global.PacketType>();
    Dictionary<string, SortedDictionary<float, PacketInfo>> packetBySource = new Dictionary<string, SortedDictionary<float, PacketInfo>>();
    Dictionary<string, int> packetBySourcePtr = new Dictionary<string, int>();
    Dictionary<string, SortedDictionary<float, PacketInfo>> packetByTarget = new Dictionary<string, SortedDictionary<float, PacketInfo>>();
    Dictionary<string, int> packetByTargetPtr = new Dictionary<string, int>();
    Dictionary<float, PacketInfo> runningQueue = new Dictionary<float, PacketInfo>();
    Dictionary<string, bool> prePlayFinish = new Dictionary<string, bool>();
    List<PacketInfo> ForwardPackets = new List<PacketInfo>();
    List<PacketInfo> RewindPackets = new List<PacketInfo>();
    int forwardPtr = 0;
    int rewindPtr = 0;

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

            if(packetBySource.ContainsKey(pInfo.source)){
                packetBySource[pInfo.source].Add(pInfo.packetTime, pInfo);
            }
            else{
                SortedDictionary<float, PacketInfo> dict = new SortedDictionary<float, PacketInfo>();
                dict.Add(pInfo.packetTime, pInfo);
                packetBySource.Add(pInfo.source, dict);
                packetBySourcePtr.Add(pInfo.source, -1);
                if(prePlayFinish.ContainsKey(pInfo.source)==false){
                    prePlayFinish.Add(pInfo.source, false);
                }
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

        // // Display packet by source
        // Debug.Log("Packet By Source");
        // foreach(var s in packetBySource.Keys){
        //     foreach(var k in packetBySource[s].Keys){
        //         Debug.Log(s + " : " + k + " : " + packetBySource[s][k].packetID);
        //     }
        // }
        // Debug.Log("Packet By Target");
        // foreach(var s in packetByTarget.Keys){
        //     foreach(var k in packetByTarget[s].Keys){
        //         Debug.Log(s + " : " + k + " : " + packetByTarget[s][k].packetID);
        //     }
        // }


        topo.MakeLinksTransparent();
        topo.MakeNodesTransparent();

        InvokeRepeating("DispatchPacket", 0f, 0.1f);  

        AdjustSpeed(10f);

        EnableUpdate();
    }

    Global.AnimStatus GetAnimationStatus(){
        return animStatus;
    }
    void SetAnimationStatus(Global.AnimStatus status){
        animStatus = status;
    }

    public void ResetAnimation(){
        topo.MakeLinksOpaque();
        topo.MakeNodesOpaque();
        AdjustSpeed(1f);
        referenceCounter = 0;
        runningQueue.Clear();
        CancelInvoke("DispatchPacket");
        Forward();
    }
    public void PrePlay(){
        SetAnimationStatus(Global.AnimStatus.PrePlay);
    }
    public void Forward(){
        topo.MakeLinksTransparent();
        topo.MakeNodesTransparent();
        if(GetAnimationStatus() != Global.AnimStatus.Forward){
            UpdateForwardPtr();
            SetAnimationStatus(Global.AnimStatus.Forward);
        }
        
    }
    public void Rewind(){
        if(GetAnimationStatus() != Global.AnimStatus.Rewind){
            UpdateRewindPtr();
            SetAnimationStatus(Global.AnimStatus.Rewind);
        }
    }

    public void AdjustSpeed(float speed){
        Time.timeScale = speed;
    }

    void RCupdate(){
        if(animStatus == Global.AnimStatus.PrePlay){
            referenceCounter += Time.fixedDeltaTime;
        }
        else if(animStatus == Global.AnimStatus.Forward){
            referenceCounter += Time.fixedDeltaTime;
        }
        else if(animStatus == Global.AnimStatus.Rewind){
            referenceCounter -= Time.fixedDeltaTime;
        }
    }

    float RCtime(){
        return referenceCounter;
    }
    void FixedUpdate()
    {
        RCupdate();

        // if expired then distroy them and remove from srunning, source and target queues
        // move running queue packets
        
        if(animStatus == Global.AnimStatus.PrePlay){
            RemovePrePlayExpiredPackets();
            MovePrePlayPackets();
        }
        else if(animStatus == Global.AnimStatus.Forward){
            RemoveForwardExpiredPackets();
            MoveForwardPackets();
            DispatchForwardPacket();
        }
        else if(animStatus == Global.AnimStatus.Rewind){
            RemoveRewindExpiredPackets();
            MoveRewindPackets();
            DispatchRewindPacket();
        }
        
    }

    bool IsPrePlayFinish(){
        foreach(var k in prePlayFinish.Keys){
            if(prePlayFinish[k]==false){
                return false;
            }
        }
        return true;
    }

    void DispatchPacket(){
        if(IsPrePlayFinish() && runningQueue.Count==0){
            topo.MakeLinksOpaque();
            topo.MakeNodesOpaque();
            SetAnimationStatus(Global.AnimStatus.Forward);
            AdjustSpeed(1f);
            referenceCounter = 0;
            runningQueue.Clear();
            return;
        }
        DispatchPrePlayPacket();
    }

    void DispatchPrePlayPacket(){
        foreach(string s in packetBySource.Keys){

            if(packetBySourcePtr[s] == packetBySource[s].Count && packetByTargetPtr[s] == packetByTarget[s].Count){
                continue;
            }

            // Dispatch it and update instantiate time, object
            float spTime = -1f, tpTime = -1f;
            try{
                spTime = packetBySource[s].ElementAt(packetBySourcePtr[s]+1).Key;
            }
            catch{
                spTime = -1;
                // packetBySourcePtr[s] = packetBySource[s].Count;
            }
            try{
                // Debug.Log("F target = " + s + " : " + packetByTarget[s].Count + " : " + packetByTargetPtr[s] + " :: " + packetBySource[s].Count + " : " + packetBySourcePtr[s]);
                tpTime = packetByTarget[s].ElementAt(packetByTargetPtr[s]+1).Key;
            }
            catch{
                tpTime = -1;
                // packetByTargetPtr[s] = packetByTarget[s].Count;
            }
            
            
            Debug.Log(s + " : " + spTime + " : " + tpTime + " : " + RCtime());
            if((packetByTarget.ContainsKey(s)==false && spTime!=-1f && spTime <= RCtime()) 
                || (packetByTarget.ContainsKey(s)==true && tpTime!=-1 && spTime!=-1f && spTime <= tpTime)
                || (packetByTarget.ContainsKey(s)==true && tpTime==-1 && spTime!=-1f)){
                
                Debug.Log("F ACCEPTED = " + s + " : " + spTime + " : " + tpTime + " : " + RCtime());
                GameObject go = InstantiatePacket();
                PacketInfo info = packetBySource[s][spTime];

                go.transform.position = info.sourcePos;
                info.instantiationTime = RCtime();
                info.Object = go;
                // packetBySource[s].Remove(spTime);
                packetBySource[s][spTime] = info;
                packetBySourcePtr[s] = packetBySourcePtr[s] + 1;

                // Put to Forward Packet List
                ForwardPackets.Add(info);

                // put to the running queue
                runningQueue.Add(info.packetTime, info);
            }
        }
    }

    void DispatchForwardPacket(){
        // Debug.Log("Forward outside = " + forwardPtr + " : " + ForwardPackets[forwardPtr].instantiationTime + " : " + ForwardPackets[forwardPtr].source + " : " + RCtime());
        while(forwardPtr < ForwardPackets.Count && ForwardPackets[forwardPtr].instantiationTime <= RCtime()){
            Debug.Log("Forward inside = " + forwardPtr + " : " + ForwardPackets[forwardPtr].instantiationTime + " : " + RCtime());
            GameObject go = InstantiatePacket();
            PacketInfo info = ForwardPackets[forwardPtr];
            go.transform.position = info.sourcePos;
            info.Object = go;
            ForwardPackets[forwardPtr] = info;
            runningQueue.Add(info.packetTime, info);
            forwardPtr++;
        }
    }

    void DispatchRewindPacket(){
        while(rewindPtr >= 0 && RewindPackets[rewindPtr].expirationTime >= RCtime()){
            GameObject go = InstantiatePacket();
            PacketInfo info = RewindPackets[rewindPtr];
            go.transform.position = info.targetPos;
            info.Object = go;
            RewindPackets[rewindPtr] = info;
            runningQueue.Add(info.packetTime, info);
            rewindPtr--;
        }
    }

    GameObject InstantiatePacket(){
        GameObject packet_prefab = Resources.Load("Packet") as GameObject;
        GameObject go = Instantiate(packet_prefab) as GameObject;
        return go;
    }

    void MovePrePlayPackets(){
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
            runningQueue[k] = info;
        }
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

    void RemovePrePlayExpiredPackets(){
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
            if( Vector3.Normalize(endPos - startPos) != Vector3.Normalize(endPos - go.transform.position) || 
                Vector3.Distance(endPos, go.transform.position) <= 1f*Time.timeScale){
                go.transform.position = endPos;
                expPkt.Add(pInfo);
            }
        }

        // Remove expired objects
        float pTime;
        string end;
        foreach(PacketInfo info in expPkt){
            PacketInfo newInfo = info;
            newInfo.expirationTime = RCtime();
            RewindPackets.Add(newInfo);
            pTime = info.packetTime;
            go = info.Object;
            end = info.target;
            packetByTargetPtr[end] = packetByTargetPtr[end] + 1;
            runningQueue.Remove(pTime);
            Destroy(go);
        }
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
            if( Vector3.Normalize(endPos - startPos) != Vector3.Normalize(endPos - go.transform.position) || 
                Vector3.Distance(endPos, go.transform.position) <= 1f*Time.timeScale){
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
            end = info.target;
            runningQueue.Remove(pTime);
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
                Vector3.Distance(endPos, go.transform.position) <= 1f*Time.timeScale){
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
            runningQueue.Remove(pTime);
            Destroy(go);
        }
        expPkt.Clear();
    }

     void UpdateForwardPtr(){
        Debug.Log("Forward Update ptr count = " + ForwardPackets.Count);
        for(int i=0; i<ForwardPackets.Count; i++){
            Debug.Log("Forward Update ptr = " + i + " : " + ForwardPackets[i].instantiationTime + " : " + RCtime());
            if(ForwardPackets[i].instantiationTime >= RCtime()){
                forwardPtr = i;
                return;
            }
        }
        forwardPtr = ForwardPackets.Count;
    }
    void UpdateRewindPtr(){
        for(int i=RewindPackets.Count-1; i>=0; i--){
            if(RewindPackets[i].expirationTime <= RCtime()){
                rewindPtr = i;
                return;
            }
        }
        rewindPtr = -1;
    }

    void EnableUpdate(){
        enabled = true;
    }
    void DisableUpdate(){
        enabled = false;
    }
}