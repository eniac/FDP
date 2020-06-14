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

static class Constants
{
    public const float H_SPACE = 8.0f;
    public const float V_SPACE = 8.0f;
}

public class Topology : MonoBehaviour
{

    [SerializeField] private Camera mainCamera = default;
    [SerializeField] private string LayerToUse = default;
    List<string> h_names;
    List<string> s_names;
    List<string> sat_names;
    Dictionary<string, List<string>> s_h_links;
    Dictionary<string, List<string>> sat_links;
    List<GameObject> goList;
    List<MeshFilter> innerMeshList;
    List<GameObject> innerObjectList;
    Dictionary <string, Vector3> positions;

    // Constructor of class
    public Topology(){
        positions = new Dictionary <string, Vector3>();
        goList = new List<GameObject>();
        innerMeshList = new List<MeshFilter>();
        innerObjectList = new List<GameObject>();
    }
    public void SetParameters(List<string> h_names, List<string> s_names, List<string> sat_names, Dictionary<string, List<string>> s_h_links, Dictionary<string, List<string>> sat_links){
        this.h_names = h_names;
        this.s_names = s_names;
        this.sat_names = sat_names;
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
		        // Traversing all the nodes linked with 'node'
                foreach(var linked_node in s_h_links[node]){
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
        float x=0, y=1, z=0;
        float vertical_spacing = Constants.V_SPACE;
        float horizontal_spacing = Constants.H_SPACE;
        int nx=0, nz=0;
        int n = 0;
        List<string> placed_elements = new List<string>();

        int e = 0;
        int layer_number = 0;
        foreach (KeyValuePair<int, List<string>> l in level){
          n = level[l.Key].Count;
          if(n==1){
            positions.Add(level[l.Key][0], new Vector3(0, y, 0));
            continue;
          }
          nx = (int)Math.Sqrt(n);
          nz = (int)(n/nx);
          e = 0;
          x = 0-((nx-1)*horizontal_spacing)/2.0f;
          z = 0-((nz-1)*horizontal_spacing)/2.0f;
          foreach(var v in level[l.Key]){
            positions.Add(v, new Vector3(x, y, z));
            e++;
            if(e%nx==0){
              z = z + horizontal_spacing;
              x = 0-((nx-1)*horizontal_spacing)/2.0f;
            }
            else{
              x = x + horizontal_spacing;
            }
          }
          y = y + vertical_spacing;
          layer_number++;
        }

        // Removing duplicate pipes from s_h_lnks
        foreach (KeyValuePair<string, List<string>> link in s_h_links){
            foreach(var node in link.Value){
                if(s_h_links.ContainsKey(node) && s_h_links[node].Contains(link.Key)){
                    s_h_links[node].Remove(link.Key);
                }
            }
        }

        // // Printing positions
        // foreach (KeyValuePair<int, List<string>> l in level){
        //     Debug.Log("Level : " + l.Key + " ****************************");
        //     foreach(var v in level[l.Key]){
        //         Debug.Log(v + " : " + positions[v].ToString());
        //     }
        // }

    }

    // Display topology on the screen
    public void DisplayTopology(){
        // Showing hosts
        GameObject h_prefab = Resources.Load("Host") as GameObject;
        foreach (string h in h_names){
            // Instantiate Object and set position
            GameObject go = Instantiate(h_prefab) as GameObject;
            // goList.Add(go);
            go.transform.position = positions[h];
            DisplayLabels(ref go, h, true);
        }

        // Showing Switches
        GameObject s_prefab = Resources.Load("Switch") as GameObject;
        foreach (string s in s_names){
            GameObject go = Instantiate(s_prefab) as GameObject;
            goList.Add(go);
            go.transform.position = positions[s];
            DisplayLabels(ref go, s, false);
        }

        // // Showing satellites
        // GameObject sat_prefab = Resources.Load("Satellite") as GameObject;
        // float radius = 3f;
        // for (int i = 0; i < 1; i++)
        // {
        //     float angle = i * Mathf.PI*2f / 8;
        //     Vector3 newPos = new Vector3(positions["p0a0"].x+Mathf.Cos(angle)*radius, positions["p0a0"].y, positions["p0a0"].z+Mathf.Sin(angle)*radius);
        //     GameObject go = Instantiate(sat_prefab) as GameObject;
        //     go.transform.position = newPos;
        // }

        // Showing pipes (links)
        GameObject link_prefab = Resources.Load("Link") as GameObject;
        foreach (KeyValuePair<string, List<string>> link in s_h_links){
            foreach(var node in link.Value){
                GameObject go = Instantiate(link_prefab) as GameObject;
                // Setting the position
                go.transform.position = (positions[link.Key]-positions[node])/2.0f + positions[node];
                // Scaling
                var scale = go.transform.localScale;
                scale.y = (positions[link.Key]-positions[node]).magnitude;
                go.transform.localScale = scale/2.0f;
                // Rotation
                go.transform.rotation = Quaternion.FromToRotation(Vector3.up, positions[link.Key]-positions[node]);
            }
        }
    }
    // Display labels on the nodes
    void DisplayLabels(ref GameObject go, string label, bool is_host){
        // 0. make the clone of this and make it a child
        var innerObject = new GameObject(go.name + "_original", typeof(MeshRenderer));
        innerObjectList.Add(innerObject);
        var innerMesh = innerObject.AddComponent<MeshFilter>();
        if(is_host==false){
            innerMeshList.Add(innerMesh);
        }
        innerMesh.transform.SetParent(go.transform);
        innerMesh.transform.position = go.transform.position;
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
        textMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        textMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        textMaterial.SetInt("_ZWrite", 0);
        textMaterial.DisableKeyword("_ALPHATEST_ON");
        textMaterial.EnableKeyword("_ALPHABLEND_ON");
        textMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        textMaterial.renderQueue = 3000;

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
        text.color = Color.black;
        text.fontSize = 60;
        if(is_host){
            text.fontSize = 120;
            text.color = Color.white;
        }
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        Canvas.gameObject.layer = LayerMask.NameToLayer(LayerToUse);
        text.gameObject.layer = LayerMask.NameToLayer(LayerToUse);

        text.text = label + "            ";
        if(is_host){
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

    // Get the inner object (text mesh) list
    public List<GameObject> GetTextObjects(){
        return innerObjectList;
    }

}
