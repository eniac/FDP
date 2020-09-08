using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

using YamlDotNet.Serialization;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

using Random=System.Random;

static class Constants
{
    public const string HOST_STRING = "Host";
    public const string SWITCH_STRING = "Switch";
    public const string SAT_STRING = "Satellite";
    public const string DROPPER_STRING = "dropper";
    public const float STRUCTURE_X = 0.0f;
    public const float STRUCTURE_Y = 0.0f;
    public const float STRUCTURE_Z = 0.0f;
    public const float H_SPACE = 8.0f;
    public const float V_SPACE = 8.0f;
    public const float SAT_H_space = 3.0f;
    public const float SAT_V_SPACE = 1.5f;
    public const float SAT_MAX = 8;
    public const int L_START = 0;
    public const int L_END = 1;
}

public class Topology : MonoBehaviour
{
    [SerializeField] private CamMovement camControl = default; 
    [SerializeField] private Camera mainCamera = default;
    [SerializeField] private string LayerToUse = default;
    List<string> h_names;
    List<string> s_names;
    List<string> sat_names;
    List<string> dropper_names;
    Dictionary<string, List<string>> s_h_links;
    Dictionary<string, List<string>> sat_links;
    List<GameObject> goList;
    List<GameObject> hostCanvasObjectList;
    List<GameObject> linkObjectList;
    List<MeshFilter> innerMeshList;
    List<GameObject> innerObjectList;
    Dictionary <string, Vector3> positions;
    Dictionary <float, List<Vector3>> layerCoordinates;  // height of layer : layer dimention
    Dictionary<string, GameObject> switchObjectDict;
    Dictionary<string, GameObject> satObjectDict;
    Dictionary<string, GameObject> hostObjectDict;
    bool highlightedNodesStatus;
    List<string> highlightedNodes;
    Dictionary<string, Color> colorDict;
    List<GameObject> DropperLinkObjects = new List<GameObject>();
    List<GameObject> bubbleObject = new List<GameObject>();
    List<GameObject> tagMarkObject = new List<GameObject>();
    bool linkOpacity = true, nodeOpacity = true;

    public enum BlendMode
    {
        Opaque,
        Cutout,
        Fade,
        Transparent
    }

    // Constructor of class
    public Topology(){
        goList = new List<GameObject>();
        hostCanvasObjectList = new List<GameObject>();
        linkObjectList = new List<GameObject>();
        innerMeshList = new List<MeshFilter>();
        innerObjectList = new List<GameObject>();
        positions = new Dictionary <string, Vector3>();
        layerCoordinates = new Dictionary <float, List<Vector3>>();
        switchObjectDict = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        satObjectDict = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        hostObjectDict = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        highlightedNodesStatus = false;
        highlightedNodes = new List<string>();
        colorDict = new Dictionary<string, Color>();
    }
    public void SetParameters(List<string> h_names, List<string> s_names, 
                                List<string> sat_names, List<string> dropper_names, Dictionary<string, 
                                List<string>> s_h_links, Dictionary<string, List<string>> sat_links){
        this.h_names = h_names;
        this.s_names = s_names;
        this.sat_names = sat_names;
        this.dropper_names = dropper_names;
        this.s_h_links = s_h_links;
        this.sat_links = sat_links;
    }
    public void Display(){
        Debug.Log("I am in Topology :)");
    }


    // Find positions of each node in the topology
    public void GetPosition(){
        Dictionary<int, List<string>> level = new Dictionary<int, List<string>>();
        Dictionary<string, List<string>> successor = new Dictionary<string, List<string>>();
        int level_number = 0;
        List<string> ll;
        // Adding all the hosts on level 0
        level.Add(level_number, h_names);

        // Finding the level of every node and their successors (in fat Tree network)
        while(level.ContainsKey(level_number)){
	        // Traversing all the nodes of 
            foreach(var node in level[level_number]){
                if(IsDropper(node)){
                    continue;
                }
		        // Traversing all the nodes linked with 'node'
                foreach(var linked_node in s_h_links[node]){
                    if(IsDropper(linked_node)){
                        continue;
                    }
                    // Find out the elligible parent node which should not be at same level or lower level	
                    if(level[level_number].Contains(linked_node)==false && (level_number<=0 || level[level_number-1].Contains(linked_node)==false)){
                        // Add Parent node to one above level list and update 'level' dictionary
                        if (level.ContainsKey(level_number+1)){
                            if(level[level_number+1].Contains(linked_node)==false){
                                level[level_number+1].Add(linked_node);
                            }
                        }
                        else{
                            ll = new List<string>();
                            ll.Add(linked_node);
                            level.Add(level_number+1, ll);
                        }
                        // Add child node of linked_node to the successor dictionary
                        if (successor.ContainsKey(linked_node)){
                            if(successor[linked_node].Contains(node)==false){
                                successor[linked_node].Add(node);
                            }
                        }
                        else{
                            ll = new List<string>();
                            ll.Add(node);
                            successor.Add(linked_node, ll);
                        }
                    }
                }
            }
            level_number++;	
        }
        // // Printing levels
        // foreach (KeyValuePair<int, List<string>> l in level){
        //     Debug.Log(l.Key.ToString() + " *************************************************** ");
        //     foreach(var v in level[l.Key]){
        //         Debug.Log(v);
        //     }
        // }

        // // Printing successors
        // foreach (KeyValuePair<string, List<string>> s in successor){
        //     Debug.Log(s.Key + " *************************************************** ");
        //     foreach(var v in successor[s.Key]){
        //         Debug.Log(v);
        //     }
        // }

        // Finding position of each node
        int n_levels = level_number;
        float x=0, y=Constants.STRUCTURE_Y, z=0;
        float vertical_spacing = Constants.V_SPACE;
        float horizontal_spacing = Constants.H_SPACE;
        int nx=0, nz=0;
        int n = 0;
        int e = 0;
        int layer_number = 0;
        List <Vector3> vl;

        foreach (KeyValuePair<int, List<string>> l in level){
            // Get the starting coordinates of the layer
            n = level[l.Key].Count;
            if(n==1){
                positions.Add(level[l.Key][0], new Vector3(0, y, 0));
                continue;
            }
            nx = (int)Math.Sqrt(n);
            nz = (int)(n/nx);
            e = 0;
            x = Constants.STRUCTURE_X-((nx-1)*horizontal_spacing)/2.0f;
            z = Constants.STRUCTURE_Z-((nz-1)*horizontal_spacing)/2.0f;
            // Debug.Log(l.Key+":"+n+":"+nx+":"+nz+":"+x+":"+z);

            // Get the coordinates of each layer in space
            vl = new List<Vector3>();
            vl.Add(new Vector3(x, y, z));
            vl.Add(new Vector3(x+horizontal_spacing*(nx-1), y, z+horizontal_spacing*(nz-1)));
            layerCoordinates.Add(y, vl);
            
            // Get the coordinates of every node in space
            foreach(var v in level[l.Key]){
                positions.Add(v, new Vector3(x, y, z));
                e++;
                if(e%nx==0){
                    z = z + horizontal_spacing;
                    x = Constants.STRUCTURE_X-((nx-1)*horizontal_spacing)/2.0f;
                }
                else{
                    x = x + horizontal_spacing;
                }
            }
            y = y + vertical_spacing;
            layer_number++;
        }


        // Get position of dropper
        foreach(var d in dropper_names){
            string node1 = s_h_links[d][0];
            string node2 = s_h_links[d][1];
            positions[d] = (positions[node1]-positions[node2])/2.0f + positions[node2];
        }

        // Removing duplicate pipes from s_h_lnks
        foreach (KeyValuePair<string, List<string>> link in s_h_links){
            foreach(var node in link.Value){
                if(IsDropper(node) || IsDropper(link.Key)){
                    continue;
                }
                if(s_h_links.ContainsKey(node) && s_h_links[node].Contains(link.Key)){
                    s_h_links[node].Remove(link.Key);
                }
            }
        }

        // Get the satellite coordinates in space
        GetSatellitePosition();

        // // Printing layer coordinates
        // foreach (KeyValuePair<float, List<Vector3>> l in layerCoordinates){
        //     Debug.Log("Layer Height : " + l.Key + " ****************************");
        //     foreach(var v in layerCoordinates[l.Key]){
        //         Debug.Log(v.ToString());
        //     }
        // }

        // // Printing positions
        // foreach (KeyValuePair<int, List<string>> l in level){
        //     Debug.Log("Level : " + l.Key + " ****************************");
        //     foreach(var v in level[l.Key]){
        //         Debug.Log(v + " : " + positions[v].ToString());
        //     }
        // }

    }

    // Get position of Satellites
    void GetSatellitePosition(){
        // Showing satellites
        float R = Constants.SAT_H_space;    // Distance of sat from center of switch
        float r = Constants.SAT_V_SPACE;    // Radius of switch 
        float y_diff = 0;
        List<Vector3> visible_pos = new List<Vector3>();
        List<Vector3> aligned_pos = new List<Vector3>();
        List<Vector3> invisible_pos = new List<Vector3>(); 
        List<float> sat_y_pos = new List<float>();
        float radius=0;
        
        // Iterate through all those switches which has satellite attached
        foreach (KeyValuePair<string, List<string>> link in sat_links){
            visible_pos.Clear();
            aligned_pos.Clear();
            invisible_pos.Clear();
            string switch_name = link.Key;
            float height = positions[switch_name].y;
            y_diff = (float)Math.Sqrt(R*R - r*r);
            sat_y_pos.Clear();
            sat_y_pos.Add(positions[switch_name].y);
            sat_y_pos.Add(positions[switch_name].y + r);
            sat_y_pos.Add(positions[switch_name].y - r);

            // Debug.Log(switch_name + " ------------------ " + height + " = " + positions[switch_name].ToString() );
            // foreach(var v in layerCoordinates[height]){
            //     Debug.Log(v.ToString());
            // }

            // Showing satellites
            // GameObject green_prefab = Resources.Load("s_green") as GameObject;
            // GameObject yellow_prefab = Resources.Load("s_yellow") as GameObject;
            // GameObject red_prefab = Resources.Load("s_red") as GameObject;

            // Iterate through all the possible positions of satellite
            for(int i=0; i<Constants.SAT_MAX; i++){
                float angle = i * Mathf.PI*2f / Constants.SAT_MAX;
                // Iterate throught all the three grid positions (upper, middle, lower)
                foreach(var y_pos in sat_y_pos){
                    if(y_pos==positions[switch_name].y){
                        radius = R;
                    }
                    else{
                        radius = (float)Math.Sqrt(R*R - r*r);
                    }
                    Vector3 pos = new Vector3(positions[switch_name].x + Mathf.Cos(angle)*radius, 
                                            y_pos, 
                                            positions[switch_name].z + Mathf.Sin(angle)*radius);

                    // Avoiding position which are close to the connecting pipes
                    bool overlap_flag = false;
                    void RemoveClosePos(){
                        foreach (KeyValuePair<string, List<string>> l in s_h_links){
                            foreach(var node in l.Value){
                                var lineStart = positions[l.Key];
                                var lineEnd = positions[node];
                                var point = pos;
                                Vector3 rhs = point - lineStart;
                                Vector3 vector2 = lineEnd - lineStart;
                                float magnitude = vector2.magnitude;
                                Vector3 lhs = vector2;
                                if (magnitude > 1E-06f)
                                {
                                    lhs = (Vector3)(lhs / magnitude);
                                }
                                float num2 = Mathf.Clamp(Vector3.Dot(lhs, rhs), 0f, magnitude);
                                var d = Vector3.Magnitude(lineStart + ((Vector3)(lhs * num2)) - point);

                                if(Math.Abs(d) < 1.0f)
                                {
                                    overlap_flag = true;
                                    return;
                                }    
                            }
                        }
                    };
                    
                    RemoveClosePos();
                    if(overlap_flag==true){
                        continue;
                    }

                    // Saparate out visible positions
                    if(pos.x < layerCoordinates[height][Constants.L_START].x || 
                        pos.x > layerCoordinates[height][Constants.L_END].x ||
                        pos.z < layerCoordinates[height][Constants.L_START].z || 
                        pos.z > layerCoordinates[height][Constants.L_END].z){
                        visible_pos.Add(pos);
                        // GameObject go = Instantiate(green_prefab) as GameObject;
                        // go.transform.position = pos;
                    }
                    // Saparate out positions which are aligned to outer edge of layer
                    else if(pos.x == layerCoordinates[height][Constants.L_START].x || 
                        pos.x == layerCoordinates[height][Constants.L_END].x ||
                        pos.z == layerCoordinates[height][Constants.L_START].z || 
                        pos.z == layerCoordinates[height][Constants.L_END].z){
                        aligned_pos.Add(pos);
                        // GameObject go = Instantiate(yellow_prefab) as GameObject;
                        // go.transform.position = pos;
                    }
                    // Saparate out hard to invisible positions 
                    else{
                        invisible_pos.Add(pos);
                        // GameObject go = Instantiate(red_prefab) as GameObject;
                        // go.transform.position = pos;
                    }
                }
            }
            // Setting up the positions for every satellite based upon the above analysis
            int index = 0;
            Random random = new Random();
            foreach(var sat in link.Value){
                if(visible_pos.Count > 0){
                    index = random. Next(visible_pos.Count);
                    positions.Add(sat, visible_pos[index]);
                    visible_pos.RemoveAt(index);
                }
                else if(aligned_pos.Count > 0){
                    index = random. Next(aligned_pos.Count);
                    positions.Add(sat, aligned_pos[index]);
                    aligned_pos.RemoveAt(index);
                }
                else if(invisible_pos.Count > 0){
                    index = random. Next(invisible_pos.Count);
                    positions.Add(sat, invisible_pos[index]);
                    invisible_pos.RemoveAt(index);
                }
                else{
                    // If no position left to place a satellite then display debug log
                    Debug.Log("Number of satellites on one switch exceeded it's maximum limit");
                }
            }
        }
    }

    // Display topology on the screen
    public void DisplayTopology(){
        // Showing hosts
        GameObject h_prefab = Resources.Load("Host") as GameObject;

        foreach (string h in h_names){
            // Instantiate Object and set position
            GameObject go = Instantiate(h_prefab) as GameObject;
            go.transform.position = positions[h];
            go.name = h;
            hostObjectDict.Add(h, go);
            
            go.transform.Find("CanvasFront/HostText").gameObject.GetComponent<Text>().text = h.ToString();
            go.transform.Find("CanvasLeft/HostText").gameObject.GetComponent<Text>().text = h.ToString();
            go.transform.Find("CanvasRight/HostText").gameObject.GetComponent<Text>().text = h.ToString();
            go.transform.Find("CanvasBack/HostText").gameObject.GetComponent<Text>().text = h.ToString();
            go.transform.Find("CanvasTop/HostText").gameObject.GetComponent<Text>().text = h.ToString();
            go.transform.Find("CanvasBottom/HostText").gameObject.GetComponent<Text>().text = h.ToString();

            hostCanvasObjectList.Add(go.transform.Find("CanvasFront").gameObject);
            hostCanvasObjectList.Add(go.transform.Find("CanvasLeft").gameObject);
            hostCanvasObjectList.Add(go.transform.Find("CanvasRight").gameObject);
            hostCanvasObjectList.Add(go.transform.Find("CanvasBack").gameObject);
            hostCanvasObjectList.Add(go.transform.Find("CanvasTop").gameObject);
            hostCanvasObjectList.Add(go.transform.Find("CanvasBottom").gameObject);

            if(colorDict.ContainsKey(Constants.HOST_STRING)==false){
                colorDict.Add(Constants.HOST_STRING, go.GetComponent<MeshRenderer>().material.color);
            }
        }

        // Showing Switches
        GameObject s_prefab = Resources.Load("Switch") as GameObject;
        // GameObject bubble_prefab = Resources.Load("ChatBubble") as GameObject;
        foreach (string s in s_names){
            GameObject go = Instantiate(s_prefab) as GameObject;
            go.transform.position = positions[s];
            go.name = s;
            goList.Add(go);
            switchObjectDict.Add(s, go);
            if(s.ToLower().Contains(Constants.DROPPER_STRING)){
                go.GetComponent<MeshRenderer>().enabled = false;
            }
            else{
                DisplayLabels(ref go, s, "Switch");
            }
            if(colorDict.ContainsKey(Constants.SWITCH_STRING)==false){
                colorDict.Add(Constants.SWITCH_STRING, go.GetComponent<MeshRenderer>().material.color);
            }
            ChangeRenderMode(go.GetComponent<MeshRenderer>().material, BlendMode.Opaque);
        }

        // Showing satellites
        GameObject sat_prefab = Resources.Load("Satellite") as GameObject;
        foreach(string sat in sat_names){
            GameObject go = Instantiate(sat_prefab) as GameObject;
            go.transform.position = positions[sat];
            go.name = sat;
            goList.Add(go);
            satObjectDict.Add(sat, go);
            DisplayLabels(ref go, sat, "Sat");
            if(colorDict.ContainsKey(Constants.SAT_STRING)==false){
                colorDict.Add(Constants.SAT_STRING, go.GetComponent<MeshRenderer>().material.color);
            }
        }

        // Showing pipes (links between switch an satellites)
        GameObject sat_link_prefab = Resources.Load("SatLink") as GameObject;
        foreach (KeyValuePair<string, List<string>> link in sat_links){
            foreach(var sat in link.Value){
                GameObject go = Instantiate(sat_link_prefab) as GameObject;
                // Setting the position
                go.transform.position = (positions[link.Key]-positions[sat])/2.0f + positions[sat];
                // Scaling
                var scale = go.transform.localScale;
                scale.y = (positions[link.Key]-positions[sat]).magnitude - satObjectDict[sat].transform.localScale.y;
                go.transform.localScale = scale/2.0f;
                // Rotation
                go.transform.rotation = Quaternion.FromToRotation(Vector3.up, positions[link.Key]-positions[sat]);
                // Adding object in a list to change its properties in future
                linkObjectList.Add(go);
            }
        }

        // Showing pipes (links between switches and hosts)
        GameObject link_prefab = Resources.Load("Link") as GameObject;
        foreach (KeyValuePair<string, List<string>> link in s_h_links){
            foreach(var node in link.Value){
                GameObject go = Instantiate(link_prefab) as GameObject;
                // Setting the position
                go.transform.position = PipePosition(link.Key, node);
                // go.transform.position = (positions[link.Key]-positions[node])/2.0f + positions[node];
                // Scaling
                var scale = go.transform.localScale;
                scale.y = PipeScale(link.Key, node);
                // scale.y = (positions[link.Key]-positions[node]).magnitude - switchObjectDict[link.Key].transform.localScale.y;
                
                go.transform.localScale = scale/2.0f;
                // Rotation
                go.transform.rotation = Quaternion.FromToRotation(Vector3.up, positions[link.Key]-positions[node]);
                // Highlight lossy link which involves dropper
                HighlightLossyLink(ref go, link.Key, node);
                // Adding object in a list to change its properties in future
                linkObjectList.Add(go);
            }
        }
    }

    Vector3 PipePosition(string node1, string node2){
        // string d_node="", node="";
        // if(node1.ToLower().Contains(Constants.DROPPER_STRING)){
        //     d_node = node1;
        //     node = node2;
        // }
        // else if(node2.ToLower().Contains(Constants.DROPPER_STRING)){
        //     d_node = node2;
        //     node = node1;
        // }

        // if(d_node.Length==0 && node.Length==0){
        //     return (positions[node1]-positions[node2])/2.0f + positions[node2];
        // }
        // else{
        //     return (positions[d_node]-positions[node])/2.0f + positions[node];
        // }
        if(IsDropper(node1)){
            string d = node1;
            node1 = s_h_links[d][0];
            node2 = s_h_links[d][1];
        }
        else if(IsDropper(node2)){
            string d = node2;
            node1 = s_h_links[d][0];
            node2 = s_h_links[d][1];
        }
        return (positions[node1]-positions[node2])/2.0f + positions[node2];
    }
    float PipeScale(string node1, string node2){
        // if(node1.ToLower().Contains(Constants.DROPPER_STRING) || node2.ToLower().Contains(Constants.DROPPER_STRING)){
        //     return (positions[node1]-positions[node2]).magnitude;
        // }
        if(IsDropper(node1)){
            string d = node1;
            node1 = s_h_links[d][0];
            node2 = s_h_links[d][1];
        }
        else if(IsDropper(node2)){
            string d = node2;
            node1 = s_h_links[d][0];
            node2 = s_h_links[d][1];
        }
        return (positions[node1]-positions[node2]).magnitude - switchObjectDict[node1].transform.localScale.y;
    }
    void HighlightLossyLink(ref GameObject go, string node1, string node2){
        if(node1.ToLower().Contains(Constants.DROPPER_STRING) || node2.ToLower().Contains(Constants.DROPPER_STRING)){
        // if(node1.ToLower().Contains("p0h0") || node2.ToLower().Contains("p0h0")){
            go.GetComponent<MeshRenderer>().material.color = Color.red;
            DropperLinkObjects.Add(go);
        }
    }
    // Display labels on the nodes
    void DisplayLabels(ref GameObject go, string label, string obj_type){
        // 0. make the clone of this and make it a child
        var innerObject = new GameObject(go.name + "_original", typeof(MeshRenderer));
        // if(is_host){
        //     go.transform.rotation = Quaternion.FromToRotation(Vector3.up, go.transform.forward*(90));
        //     innerObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, innerObject.transform.forward*(90));
        // }
        innerObjectList.Add(innerObject);
        var innerMesh = innerObject.AddComponent<MeshFilter>();
        if(obj_type=="Switch" || obj_type=="Sat"){
            innerMeshList.Add(innerMesh);
        }
        innerMesh.transform.SetParent(go.transform);
        innerMesh.transform.position = go.transform.position;
        innerMesh.transform.rotation = go.transform.rotation;
        innerMesh.transform.localScale = new Vector3(1,1,1);
        // copy over the mesh
        innerMesh.mesh = go.GetComponent<MeshFilter>().mesh;
        name = go.name + "_textDecal";

        // 1. Create and configure the RenderTexture
        var renderTexture = new RenderTexture(2048, 2048, 24) { name = go.name + "_RenderTexture" };

        // 2. Create material
        var textMaterial = new Material(Shader.Find("Standard"));

        // assign the new renderTexture as Albedo
        textMaterial.SetTexture("_MainTex", renderTexture);

        // set RenderMode to Fade
        ChangeRenderMode(textMaterial, BlendMode.Fade);
        // textMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        // textMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        // textMaterial.SetInt("_ZWrite", 0);
        // textMaterial.DisableKeyword("_ALPHATEST_ON");
        // textMaterial.EnableKeyword("_ALPHABLEND_ON");
        // textMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        // textMaterial.renderQueue = 3000;

        // 3. WE CAN'T CREATE A NEW LAYER AT RUNTIME SO CONFIGURE THEM BEFOREHAND AND USE LayerToUse

        // 4. exclude the Layer in the normal camera
        if (!mainCamera) mainCamera = Camera.main;
        mainCamera.cullingMask &= ~(1 << LayerMask.NameToLayer(LayerToUse));

        // 5. Add new Camera as child of this object
        var camera = new GameObject("TextCamera").AddComponent<Camera>();
        camera.transform.SetParent(go.transform, false);
        camera.backgroundColor = new Color(0, 0, 0, 0);
        camera.clearFlags = CameraClearFlags.Color;
        camera.cullingMask = 1 << LayerMask.NameToLayer(LayerToUse);
        // camera.focalLength = 5;
        camera.farClipPlane = 5;
        camera.usePhysicalProperties = true;
        camera.fieldOfView = 15f;

        // make it render to the renderTexture
        camera.targetTexture = renderTexture;
        camera.forceIntoRenderTexture = true;

        // 6. add the UI to your scene as child of the camera
        var Canvas = new GameObject("Canvas", typeof(RectTransform)).AddComponent<Canvas>();
        Canvas.transform.SetParent(camera.transform, false);
        Canvas.gameObject.AddComponent<CanvasScaler>();
        Canvas.renderMode = RenderMode.WorldSpace;
        var canvasRectTransform = Canvas.GetComponent<RectTransform>();
        canvasRectTransform.anchoredPosition3D = new Vector3(0, 0, 1);
        canvasRectTransform.sizeDelta = Vector2.one;

        var text = new GameObject("Text", typeof(RectTransform)).AddComponent<Text>();
        text.transform.SetParent(Canvas.transform, false);
        var textRectTransform = text.GetComponent<RectTransform>();
        textRectTransform.localScale = Vector3.one * 0.001f;
        textRectTransform.sizeDelta = new Vector2(2000, 1000);

        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        if(obj_type=="Switch"){
            text.color = Color.white;
            text.fontSize = 60;
        }
        if(obj_type=="Host"){
            text.fontSize = 120;
            text.color = Color.white;
        }
        else if(obj_type=="Sat"){
            text.color = Color.black;
            text.fontSize = 35;
        }
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        Canvas.gameObject.layer = LayerMask.NameToLayer(LayerToUse);
        text.gameObject.layer = LayerMask.NameToLayer(LayerToUse);

        text.text = label + "            ";
        if(obj_type=="Sat"){
            text.text = label + "                   ";
        }
        if(obj_type=="Host"){
            text.text = label;
        }

        // 7. finally assign the material to the child object and hope everything works ;)
        innerMesh.GetComponent<MeshRenderer>().material = textMaterial; 
    }

    // Labels following camera 
    public void LablesFollowCam(){
        foreach(var obj in goList.Zip(innerMeshList, (a, b) => new { parent = a, child = b})){
            obj.parent.transform.rotation = Quaternion.LookRotation(-Camera.main.transform.forward, Camera.main.transform.up);
            obj.parent.transform.LookAt(obj.parent.transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
            obj.child.transform.rotation = obj.parent.transform.rotation;
        }
    }

    public static void ChangeRenderMode(Material standardShaderMaterial, BlendMode blendMode)
    {
        switch (blendMode)
        {
            case BlendMode.Opaque:
                standardShaderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                standardShaderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                standardShaderMaterial.SetInt("_ZWrite", 1);
                standardShaderMaterial.DisableKeyword("_ALPHATEST_ON");
                standardShaderMaterial.DisableKeyword("_ALPHABLEND_ON");
                standardShaderMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                standardShaderMaterial.renderQueue = -1;
                break;
            case BlendMode.Cutout:
                standardShaderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                standardShaderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                standardShaderMaterial.SetInt("_ZWrite", 1);
                standardShaderMaterial.EnableKeyword("_ALPHATEST_ON");
                standardShaderMaterial.DisableKeyword("_ALPHABLEND_ON");
                standardShaderMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                standardShaderMaterial.renderQueue = 2450;
                break;
            case BlendMode.Fade:
                standardShaderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                standardShaderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                standardShaderMaterial.SetInt("_ZWrite", 0);
                standardShaderMaterial.DisableKeyword("_ALPHATEST_ON");
                standardShaderMaterial.EnableKeyword("_ALPHABLEND_ON");
                standardShaderMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                standardShaderMaterial.renderQueue = 3000;
                break;
            case BlendMode.Transparent:
                standardShaderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                standardShaderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                standardShaderMaterial.SetInt("_ZWrite", 0);
                standardShaderMaterial.DisableKeyword("_ALPHATEST_ON");
                standardShaderMaterial.DisableKeyword("_ALPHABLEND_ON");
                standardShaderMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                standardShaderMaterial.renderQueue = 3000;
                break;
        }

    }

    // Test that input string entered by search field is valid or not
    public string ProcessSearchRequest(string nodeString){
        string[] nodes = nodeString.Split(' ');
        string invalidNames = "";
        foreach(var node in nodes){
            if(node.Length>0){
                Debug.Log("node = "+node);
                if(h_names.Contains(node, StringComparer.OrdinalIgnoreCase)==false && 
                    s_names.Contains(node, StringComparer.OrdinalIgnoreCase)==false &&
                    sat_names.Contains(node, StringComparer.OrdinalIgnoreCase)==false){
                    invalidNames += node + " ";
                }
                else{
                    highlightedNodes.Add(node);
                }
            }
        }
        if(invalidNames.Length>0){
            return invalidNames;
        }
        // Do highlighting
        if(highlightedNodes.Count == 0){
            // No name entered or only spaces are entered
            return null;
        }
        highlightedNodesStatus = true;
        List<GameObject> highlitedObjects = new List<GameObject>();
        foreach(var node in highlightedNodes){
            if(switchObjectDict.ContainsKey(node)){
                highlitedObjects.Add(switchObjectDict[node]);
                switchObjectDict[node].GetComponent<MeshRenderer>().material.color = Color.red;
            }
            else if(satObjectDict.ContainsKey(node)){
                highlitedObjects.Add(satObjectDict[node]);
                satObjectDict[node].GetComponent<MeshRenderer>().material.color = Color.red;
            }
            else if(hostObjectDict.ContainsKey(node)){
                highlitedObjects.Add(hostObjectDict[node]);
                hostObjectDict[node].GetComponent<MeshRenderer>().material.color = Color.red;
            }
        }
        camControl.MoveCamToNodes(highlitedObjects);
        return null;
    }
    
    // Remove highlighted nodes
    public bool ProcessClearRequest(){
        if(highlightedNodesStatus == true){
            foreach(var node in highlightedNodes){
                if(switchObjectDict.ContainsKey(node)){
                    switchObjectDict[node].GetComponent<MeshRenderer>().material.color = colorDict[Constants.SWITCH_STRING];
                }
                else if(satObjectDict.ContainsKey(node)){
                    satObjectDict[node].GetComponent<MeshRenderer>().material.color = colorDict[Constants.SAT_STRING];
                }
                else if(hostObjectDict.ContainsKey(node)){
                    hostObjectDict[node].GetComponent<MeshRenderer>().material.color = colorDict[Constants.HOST_STRING];
                }
            }
            highlightedNodes.Clear();
            highlightedNodesStatus = false;
            return true;
        }
        return false;
    }

    // Make links transparent
    public void MakeLinksTransparent(){
        // Here a = alpha = opacity (0.0 transparent, 1.0 opaque)
        foreach(var obj in linkObjectList){
            Color color = obj.GetComponent<MeshRenderer>().material.color;
            color.a = 0.05f;
            obj.GetComponent<MeshRenderer>().material.color = color;
        }
        linkOpacity = false;
    }
    // Make links opaque
    public void MakeLinksOpaque(){
        foreach(var obj in linkObjectList){
            Color color = obj.GetComponent<MeshRenderer>().material.color;
            color.a = 1.0f;
            obj.GetComponent<MeshRenderer>().material.color = color;
        }
        linkOpacity = true;
    }

    // Make Nodes transparent
    public void MakeNodesTransparent(){
        // Here a = alpha = opacity (0.0 transparent, 1.0 opaque)
        foreach(var obj in switchObjectDict.Values){
            ChangeRenderMode(obj.GetComponent<MeshRenderer>().material, BlendMode.Transparent);
            Color color = obj.GetComponent<MeshRenderer>().material.color;
            color.a = 0.1f;
            obj.GetComponent<MeshRenderer>().material.color = color;
        }
        foreach(var obj in satObjectDict.Values){
            ChangeRenderMode(obj.GetComponent<MeshRenderer>().material, BlendMode.Transparent);
            Color color = obj.GetComponent<MeshRenderer>().material.color;
            color.a = 0.1f;
            obj.GetComponent<MeshRenderer>().material.color = color;
        }
        nodeOpacity = false;
    }

    // Make Nodes Opaque
    public void MakeNodesOpaque(){
        foreach(var obj in switchObjectDict.Values){
            // Change the blend mode itself to opaque
            ChangeRenderMode(obj.GetComponent<MeshRenderer>().material, BlendMode.Opaque);
        }
        foreach(var obj in satObjectDict.Values){
            // Change the blend mode itself to opaque
            ChangeRenderMode(obj.GetComponent<MeshRenderer>().material, BlendMode.Opaque);
        }
        nodeOpacity = true;
    }

    // Get canvas objects of hosts
    public List<GameObject> GetHostTextObjects(){
        return hostCanvasObjectList;
    }

    // Get the inner object (text mesh) list
    public List<GameObject> GetTextObjects(){
        return innerObjectList;
    }

    // Get the link object (connecting pipes) list
    public List<GameObject> GetLinkObjects(){
        return linkObjectList;
    }

    // Get the node position by name
    public Vector3 GetNodePosition(string name){
        return positions[name];
    }

    // Get the Dropper link objcts
    public List<GameObject> GetDropperLinkObjects(){
        return DropperLinkObjects;
    }

    public bool IsHost(string name){
        if(h_names.Contains(name)){
            return true;
        }
        return false;
    }

    public bool IsSatellite(string name){
        if(sat_names.Contains(name)){
            return true;
        }
        return false;
    }

    // Find out dropper Node
    public bool IsDropper(string node){
        if(dropper_names.Contains(node)){
            return true;
        }
        return false;
    }

    public void AddTagMarker(string node){
        // if(switchObjectDict.ContainsKey(node)){
        //     GameObject helo_switch_prefab = Resources.Load("HaloSwitch") as GameObject;
        //     GameObject nodeObj = switchObjectDict[node];
        //     GameObject halo = Instantiate(helo_switch_prefab) as GameObject;
        //     halo.transform.SetParent (nodeObj.transform, false);
        // }
        // else if(satObjectDict.ContainsKey(node)){
        //     GameObject helo_sat_prefab = Resources.Load("HaloSat") as GameObject;
        //     GameObject nodeObj = satObjectDict[node];
        //     GameObject halo = Instantiate(helo_sat_prefab) as GameObject;
        //     halo.transform.SetParent (nodeObj.transform, false);
        // }   
        if(switchObjectDict.ContainsKey(node)){
            GameObject mark_switch_prefab = Resources.Load("MarkSwitch") as GameObject;
            GameObject nodeObj = switchObjectDict[node];
            GameObject mark = Instantiate(mark_switch_prefab) as GameObject;
            mark.transform.SetParent (nodeObj.transform, false);
            tagMarkObject.Add(mark);
        }
        else if(satObjectDict.ContainsKey(node)){
            GameObject mark_sat_prefab = Resources.Load("MarkSat") as GameObject;
            GameObject nodeObj = satObjectDict[node];
            GameObject mark = Instantiate(mark_sat_prefab) as GameObject;
            mark.transform.SetParent (nodeObj.transform, false);
            tagMarkObject.Add(mark);
        }         
    }

    public void ShowTagMarker(){
        foreach(var go in tagMarkObject){
            go.SetActive(true);
        }
    }

    public void HideTagMarker(){
        foreach(var go in tagMarkObject){
            go.SetActive(false);
        }
    }

    public bool GetLinkOpacity(){
        return linkOpacity;
    }

    public bool GetNodeOpacity(){
        return nodeOpacity;
    }
}
