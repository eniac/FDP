using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using YamlDotNet.Serialization;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
public class TopoScript : MonoBehaviour
{
    RootObject obj;
    List<String> h_names;
    List<String> s_names;
    Dictionary<string, List<String>> s_h_links;
    Dictionary <string, Vector3> positions;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Display(){
        Debug.Log("I am in Topo :)");
    }

        // Loading Yaml file
    public void YamlLoader(){
        // Load Yaml file
        var filePath = Path.Combine(Application.streamingAssetsPath, "alv_k=4.yml");
        var r = new StreamReader(filePath);

        var deserializer = new Deserializer();
        var yamlObject = deserializer.Deserialize(r);
        
        // Convert Yaml file to Json string
        var serializer = new Newtonsoft.Json.JsonSerializer();
        var w = new StringWriter();
        serializer.Serialize(w, yamlObject);
        Console.WriteLine(w.GetType());
        string jsonText = w.ToString();
        Console.WriteLine(jsonText.ToString());

        // Json string root object
        obj = JsonConvert.DeserializeObject<RootObject>(jsonText);
    }

    public void GetLinks(){
        // Supporting data structure
        positions = new Dictionary <string, Vector3>();
        h_names = new List<String>();
        s_names = new List<String>();
        s_h_links = new Dictionary<string, List<String>>();
        List<String> ll;

        // Extracting links from hosts
        foreach (KeyValuePair<string, HostAttribute> h_kvp in obj.Hosts){
            // Extracting Hosts Names
            h_names.Add(h_kvp.Key);
            // Extracting Interface
            if(h_kvp.Value.Interface != null){
                foreach(var intr in h_kvp.Value.Interface){
                    if(intr.Link!=null){ 
                        // Linking in both the direction
                        if (s_h_links.ContainsKey(intr.Link)){
                            if(s_h_links[intr.Link].Contains(h_kvp.Key)==false){
                                s_h_links[intr.Link].Add(h_kvp.Key);
                            }
                        }
                        else{
                            ll = new List<String>();
                            ll.Add(h_kvp.Key);
                            s_h_links.Add(intr.Link, ll);
                        }
                        if (s_h_links.ContainsKey(h_kvp.Key)){
                            if(s_h_links[h_kvp.Key].Contains(intr.Link)==false){
                                s_h_links[h_kvp.Key].Add(intr.Link);
                            }
                        }
                        else{
                            ll = new List<String>();
                            ll.Add(intr.Link);
                            s_h_links.Add(h_kvp.Key, ll);
                        }
                    }
                }
            }
        }

        // Extracting links from switches
        foreach (KeyValuePair<string, SwitchAttribute> s_kvp in obj.Switches){
            // Extracting Switch Names
            s_names.Add(s_kvp.Key);
            // Extracting Interface
            if(s_kvp.Value.Interface != null){
                foreach(var intr in s_kvp.Value.Interface){
                   if(intr.Link!=null){ 
                        // Linking in both direction
                        if (s_h_links.ContainsKey(s_kvp.Key)){
                            if(s_h_links[s_kvp.Key].Contains(intr.Link)==false){
                                s_h_links[s_kvp.Key].Add(intr.Link);
                            } 
                        }
                        else{
                            ll = new List<String>();
                            ll.Add(intr.Link);
                            s_h_links.Add(s_kvp.Key, ll);
                        }
                        if (s_h_links.ContainsKey(intr.Link)){
                            if(s_h_links[intr.Link].Contains(s_kvp.Key)==false){
                                s_h_links[intr.Link].Add(s_kvp.Key);
                            }
                        }
                        else{
                            ll = new List<String>();
                            ll.Add(s_kvp.Key);
                            s_h_links.Add(intr.Link, ll);
                        }
                   }
                } 
            }
        }

        // // Printing all the links
        // foreach (KeyValuePair<string, List<String>> link in s_h_links){
        //     Debug.Log(link.Key + " ************************************** ");
        //     foreach(var v in s_h_links[link.Key]){
        //         Debug.Log(v);
        //     }
        // }
    }

    public void GetPosition(){
        Dictionary<int, List<String>> level = new Dictionary<int, List<String>>();
        Dictionary<String, List<String>> successor = new Dictionary<String, List<String>>();
        int level_number = 0;
        List<String> ll;

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
                            ll = new List<String>();
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
                            ll = new List<String>();
                            ll.Add(node);
                            successor.Add(linked_node, ll);
                        }
                    }
                }
            }
            level_number++;	
        }

        // // Printing levels
        // foreach (KeyValuePair<int, List<String>> l in level){
        //     Debug.Log(l.Key.ToString() + " *************************************************** ");
        //     foreach(var v in level[l.Key]){
        //         Debug.Log(v);
        //     }
        // }

        // // Printing successors
        // foreach (KeyValuePair<String, List<String>> s in successor){
        //     Debug.Log(s.Key + " *************************************************** ");
        //     foreach(var v in successor[s.Key]){
        //         Debug.Log(v);
        //     }
        // }

        // Finding position of each node
        int n_levels = level_number;
        float x=0, y=1, z=0;
        float spacing = 6.0f;
        int nx=0, nz=0;
        int n = 0;
        List<String> placed_elements = new List<String>();

        int e = 0;
        int layer_number = 0;
        foreach (KeyValuePair<int, List<String>> l in level){
          n = level[l.Key].Count;
          if(n==1){
            positions.Add(level[l.Key][0], new Vector3(0, y, 0));
            continue;
          }
          nx = (int)Math.Sqrt(n);
          nz = (int)(n/nx);
          e = 0;
          x = 0-((nx-1)*spacing)/2.0f;
          z = 0-((nz-1)*spacing)/2.0f;
          foreach(var v in level[l.Key]){
            positions.Add(v, new Vector3(x, y, z));
            e++;
            if(e%nx==0){
              z = z + spacing;
              x = 0-((nx-1)*spacing)/2.0f;
            }
            else{
              x = x + spacing;
            }
          }
          y = y + spacing;
          layer_number++;
        }

        // Removing duplicate pipes from s_h_lnks
        foreach (KeyValuePair<string, List<String>> link in s_h_links){
            foreach(var node in link.Value){
                if(s_h_links.ContainsKey(node) && s_h_links[node].Contains(link.Key)){
                    s_h_links[node].Remove(link.Key);
                }
            }
        }


        // // Printing positions
        // foreach (KeyValuePair<int, List<String>> l in level){
        //     Debug.Log("Level : " + l.Key + " ****************************");
        //     foreach(var v in level[l.Key]){
        //         Debug.Log(v + " : " + positions[v].ToString());
        //     }
        // }

    }

    public void DisplayTopology(){
        // Showing hosts
        GameObject h_prefab = Resources.Load("Host") as GameObject;
        foreach (String h in h_names){
            // Instantiate Object and set position
            GameObject go = Instantiate(h_prefab) as GameObject;
            go.transform.position = positions[h];
        }

        // Showing Switches
        GameObject s_prefab = Resources.Load("Switch") as GameObject;

        foreach (String s in s_names){
            GameObject go = Instantiate(s_prefab) as GameObject;
            go.transform.position = positions[s];
        }

        // Showing pipes (links)
        GameObject link_prefab = Resources.Load("Link") as GameObject;
        foreach (KeyValuePair<string, List<String>> link in s_h_links){
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
}

class RootObject
{
    [JsonProperty("hosts")]
    public Dictionary<string, HostAttribute> Hosts { get; set; }
    [JsonProperty("switches")]
    public Dictionary<string, SwitchAttribute> Switches { get; set; }
    
}
class HostAttribute
{
    [JsonProperty("interfaces")]
    public List<Interface> Interface { get; set; }
    // [JsonProperty("programs")]
    // public List<string> pm { get; set; }
    [JsonProperty("programs")]
    public List<ProgramAttribute> pm { get; set; }
}

class ProgramAttribute
{
    [JsonProperty("cmd")]
    public string Cmd { get; set; }
    [JsonProperty("fg")]
    public string Fg { get; set; }
}
class SwitchAttribute
{
    [JsonProperty("cfg")]
    public string Cfg  { get; set; }
    [JsonProperty("interfaces")]
    public List<Interface> Interface { get; set; }
    [JsonProperty("replay")]
    public Dictionary<string, string> Replay { get; set; }
    [JsonProperty("cmds")]
    public List<String> Cmds { get; set; }
}
class Interface
{
    [JsonProperty("ip")]
    public string IP { get; set; }
    [JsonProperty("port")]
    public string Port { get; set; }
    [JsonProperty("mac")]
    public string Mac { get; set; }
    [JsonProperty("name")]
    public string Name { get; set; }
    [JsonProperty("link")]
    public string Link { get; set; }
}

