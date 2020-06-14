using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoScript : MonoBehaviour
{
    // public TopoScript topo;
    public YamlParser yamlParser;
    public Topology topo;
    // Start is called before the first frame update
    IEnumerator Start()
    {
        // yamlParser = new YamlParser();
        yamlParser.Display();
        yield return StartCoroutine(yamlParser.GetYaml());
        yamlParser.YamlLoader();
        yamlParser.SetLinks();

        // topo = new Topology();
        topo.SetParameters(yamlParser.GetHostNames(), yamlParser.GetSwitchNames(), yamlParser.GetSatelliteNames(), yamlParser.GetSwitchHostLinks(), yamlParser.GetSatelliteLinks());
        topo.Display();
        topo.GetPosition();
        topo.DisplayTopology();
    }

    // Update is called once per frame
    void Update()
    {
        topo.LablesFollowCam();
    }
}
