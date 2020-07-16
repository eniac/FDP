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

public class YamlParser : MonoBehaviour
{
    string yamlString;
    RootObject obj;
    List<string> h_names;
    List<string> s_names;
    List<string> sat_names;
    Dictionary<string, List<string>> s_h_links;
    Dictionary<string, List<string>> sat_links;


    // Constructor of class
    public YamlParser(){
        yamlString = "";
        h_names = new List<string>();
        s_names = new List<string>();
        s_h_links = new Dictionary<string, List<string>>();
        sat_names = new List<string>();
        sat_links = new Dictionary<string, List<string>>();
    }

    public void Display(){
        Debug.Log("I am in YamlParser :)");
    }

    // Get file from file system or server
    public IEnumerator GetYaml(){
        var filePath = Path.Combine(Application.streamingAssetsPath, "alv_k=4_autotest1.yml");
        

        if (filePath.Contains ("://") || filePath.Contains (":///")) {
            // WWW class is depricated
            // WWW www = new WWW(filePath);
            // yield return www;
            // yamlString = www.text;

            // Uning UnityWebRequest class
            var loaded = new UnityWebRequest(filePath);
            loaded.downloadHandler = new DownloadHandlerBuffer();
            yield return loaded.SendWebRequest();
            yamlString = loaded.downloadHandler.text;
        }
        else{
            yamlString = File.ReadAllText(filePath);
        }
    }

    // Loading Yaml file content to json object
    public void YamlLoader(){
        // Load Yaml file
        // var filePath = Path.Combine(Application.streamingAssetsPath, "alv_k=4.yml");
        // var r = new StreamReader(filePath);
        var r = new StringReader(yamlString);

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

    public void SetLinks(){
        // Local variable
        List<string> ll;

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
                            ll = new List<string>();
                            ll.Add(h_kvp.Key);
                            s_h_links.Add(intr.Link, ll);
                        }
                        if (s_h_links.ContainsKey(h_kvp.Key)){
                            if(s_h_links[h_kvp.Key].Contains(intr.Link)==false){
                                s_h_links[h_kvp.Key].Add(intr.Link);
                            }
                        }
                        else{
                            ll = new List<string>();
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
                            ll = new List<string>();
                            ll.Add(intr.Link);
                            s_h_links.Add(s_kvp.Key, ll);
                        }
                        if (s_h_links.ContainsKey(intr.Link)){
                            if(s_h_links[intr.Link].Contains(s_kvp.Key)==false){
                                s_h_links[intr.Link].Add(s_kvp.Key);
                            }
                        }
                        else{
                            ll = new List<string>();
                            ll.Add(s_kvp.Key);
                            s_h_links.Add(intr.Link, ll);
                        }
                   }
                } 
            }
        }

        // Extracting Supporting Devices
        foreach (string s in s_names){
            if(s_h_links[s].Count == 1){
                sat_names.Add(s);
            }
        }

        // Extracting links between switches and satellites
        foreach (string sat in sat_names){
            // find out satellite name and connected switch
            string sat_name = sat;
            string switch_name = s_h_links[sat][0];
            // remove satellite and it's connected switch entry from relevant dictionary
            s_names.Remove(sat_name);
            s_h_links.Remove(sat_name);
            s_h_links[switch_name].Remove(sat_name);
            // Add satellite links in sat_links dictionary
            if (sat_links.ContainsKey(switch_name)){
                if(sat_links[switch_name].Contains(sat_name)==false){
                    sat_links[switch_name].Add(sat_name);
                }
            }
            else{
                ll = new List<string>();
                ll.Add(sat_name);
                sat_links.Add(switch_name, ll);
            }
        }

        // // Printing all the links (switch-hosts)
        // foreach (KeyValuePair<string, List<string>> link in s_h_links){
        //     Debug.Log(link.Key + " ************************************** ");
        //     foreach(var v in s_h_links[link.Key]){
        //         Debug.Log(v);
        //     }
        // }
        // // Printing all th links (switch-satellite)
        // foreach (KeyValuePair<string, List<string>> link in sat_links){
        //     Debug.Log(link.Key + " ######################################### ");
        //     foreach(var v in sat_links[link.Key]){
        //         Debug.Log(v);
        //     }
        // }
    }
    public List<string> GetHostNames(){
        return h_names;
    }
    public List<string> GetSwitchNames(){
        return s_names;
    }
    public List<string> GetSatelliteNames(){
        return sat_names;
    }
    public Dictionary<string, List<string>> GetSwitchHostLinks(){
        return s_h_links;
    }
    public Dictionary<string, List<string>> GetSatelliteLinks(){
        return sat_links;
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
    public List<string> Cmds { get; set; }
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

