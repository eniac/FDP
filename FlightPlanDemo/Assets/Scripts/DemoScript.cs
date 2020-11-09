﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoScript : MonoBehaviour
{
    [SerializeField]  YamlParser yamlParser = default;
    [SerializeField] ConfigParser configParser = default;
    [SerializeField] Topology topo = default;
    [SerializeField] AnimControl anim = default;
    [SerializeField] BillBoardControl billBoard = default;
    [SerializeField] IntroTagControl introTag = default;
    [SerializeField] ButtonControl buttonControl = default;
    [SerializeField] GraphInput graphInput = default;

    // Start is called before the first frame update
    public IEnumerator Start(){
        yamlParser.Display();
        yield return StartCoroutine(yamlParser.GetYaml());
        yamlParser.YamlLoader();
        yamlParser.SetLinks();

        yield return StartCoroutine(configParser.GetYaml());
        configParser.YamlLoader();

        buttonControl.SetConfigObject(configParser.GetDynamicConfigObject());
        billBoard.SetConfigObject(configParser.GetDynamicConfigObject());
        graphInput.SetConfigObject(configParser.GetDynamicConfigObject());

        yield return StartCoroutine(graphInput.GraphInitStart());

        topo.SetParameters(yamlParser.GetHostNames(), yamlParser.GetSwitchNames(), 
                            yamlParser.GetSatelliteNames(), yamlParser.GetDropperNames(), yamlParser.GetSwitchHostLinks(), 
                            yamlParser.GetSatelliteLinks());
        topo.Display();
        topo.GetPosition();
        topo.DisplayTopology();
        billBoard.BillBoardInit();

        introTag.IntroTagInit(configParser.GetDynamicConfigObject());

        // yield return StartCoroutine(anim.GetElapsedTimeFile());
        
        // anim.StartAnimation();

        yield return StartCoroutine(anim.GetMetadataFile());
        yield return StartCoroutine(anim.WriteFile());
        
        anim.AnimationInit();
    }

    // Update is called once per frame
    void Update(){
        topo.LablesFollowCam();
        billBoard.BillBoardFollowCam();
    }
}
