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

public class ConfigParser : MonoBehaviour
{
    string yamlString = null;
    ConfigRoot obj;


    // Get file from file system or server
    public IEnumerator GetYaml(){
        var filePath = Path.Combine(Application.streamingAssetsPath, Global.configYaml);
        Debug.Log("config Yaml Path in parser = " + filePath);

        if (filePath.Contains ("://") || filePath.Contains (":///")) {
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
        obj = JsonConvert.DeserializeObject<ConfigRoot>(jsonText);
    }

    // Get config details from yaml and store them into appropriate data structure
    public ConfigRoot GetConfigObject(){
        return obj;
    }
}

public class ConfigRoot{
    [JsonProperty("experiment_info")]
    public ExperimentAttribute ExperimentInfo { get; set; }
    [JsonProperty("static_tags")]
    public List<StaticTagAttribute> StaticTags { get; set; }
    [JsonProperty("event_tags")]
    public List<EventTagAttribute> EventTags { get; set; }
    [JsonProperty("graph")]
    public GraphAttribute Graph { get; set; }
     [JsonProperty("packet_legend")]
    public List<PacketLegendAttribute> PacketLegend { get; set; }
}

public class ExperimentAttribute{
    [JsonProperty("hyperlink")]
    public string Hyperlink { get; set; }
}

public class StaticTagAttribute{
    [JsonProperty("node")]
    public string Node { get; set; }
    [JsonProperty("text")]
    public string Text { get; set; }
    [JsonProperty("hyperlink")]
    public string Hyperlink { get; set; }
}

// TODO validity of time as integer
public class EventTagAttribute{
    [JsonProperty("time")]
    public Int32 Time { get; set; }
    [JsonProperty("node")]
    public string Node { get; set; }
    [JsonProperty("text")]
    public string Text { get; set; }
    [JsonProperty("hyperlink")]
    public string Hyperlink { get; set; }
}

public class GraphAttribute{
    [JsonProperty("show")]
    public string Show { get; set; }
    [JsonProperty("x_div")]
    public float XDiv { get; set; }
    [JsonProperty("x_label")]
    public string XLabel { get; set; }
    [JsonProperty("y_label")]
    public string YLabel { get; set; }
    [JsonProperty("title")]
    public string Title { get; set; }
    [JsonProperty("curve_info")]
    public List<CurveInfoAttribute> CurveInfo { get; set; }

}

public class CurveInfoAttribute{
    [JsonProperty("file_name")]
    public string FileName { get; set; }
    [JsonProperty("curve_color")]
    public string CurveColor { get; set; }
    [JsonProperty("legend_text")]
    public string LegendText { get; set; }
    [JsonProperty("curve_width")]
    public float CurveWidth { get; set; }
    [JsonProperty("packet_target")]
    public string PacketTarget { get; set; }
}

public class PacketLegendAttribute{
    [JsonProperty("type")]
    public string Type { get; set; }
    [JsonProperty("color")]
    public string Color { get; set; }
}