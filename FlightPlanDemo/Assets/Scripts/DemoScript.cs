using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoScript : MonoBehaviour
{
    public TopoScript topo;
    // Start is called before the first frame update
    void Start()
    {
        topo.Display();
        topo.YamlLoader();
        topo.GetLinks();
        topo.GetPosition();
        topo.DisplayTopology();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
