using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoScript : MonoBehaviour
{
    public YamlParser yamlParser;
    public Topology topo;
    // public AnimationControl anim; 
    [SerializeField] AnimControl anim;
    [SerializeField] GraphInput graphInput = default;

    // Start is called before the first frame update
    public IEnumerator Start(){
        yamlParser.Display();
        yield return StartCoroutine(yamlParser.GetYaml());
        yamlParser.YamlLoader();
        yamlParser.SetLinks();

        topo.SetParameters(yamlParser.GetHostNames(), yamlParser.GetSwitchNames(), 
                            yamlParser.GetSatelliteNames(), yamlParser.GetDropperNames(), yamlParser.GetSwitchHostLinks(), 
                            yamlParser.GetSatelliteLinks());
        topo.Display();
        topo.GetPosition();
        topo.DisplayTopology();

        // yield return StartCoroutine(anim.GetElapsedTimeFile());
        
        // anim.StartAnimation();

        yield return StartCoroutine(anim.GetMetadataFile());
        
        anim.AnimationInit();
    }

    // Update is called once per frame
    void Update(){
        topo.LablesFollowCam();
    }
}
