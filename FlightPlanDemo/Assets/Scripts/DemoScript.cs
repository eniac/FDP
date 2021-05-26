/*
Copyright 2021 Heena Nagda

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System.Collections;
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
    [SerializeField] GameObject loadingPanel = default;
    [SerializeField] SlideShow slideShow = default;

    public void Awake(){
        // Debug.Log("##################### before = " + Application.targetFrameRate + " : " + QualitySettings.vSyncCount);
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;

        if(Global.showAnimation==1){
            loadingPanel.SetActive(true);
        }
        // Debug.Log("##################### after = " + Application.targetFrameRate + " : " + QualitySettings.vSyncCount);
    }

    // Start is called before the first frame update
    public IEnumerator Start(){
        // Debug.Log("##################### after Demo = " + Application.targetFrameRate + " : " + QualitySettings.vSyncCount);
        yamlParser.Display();
        yield return StartCoroutine(yamlParser.GetYaml());
        yamlParser.YamlLoader();
        yamlParser.SetLinks();

        if(Global.showAnimation==1){
            yield return StartCoroutine(configParser.GetYaml());
            configParser.YamlLoader();

            buttonControl.SetConfigObject(configParser.GetDynamicConfigObject());
            billBoard.SetConfigObject(configParser.GetDynamicConfigObject());
            graphInput.SetConfigObject(configParser.GetDynamicConfigObject());
            slideShow.SetConfigObject(configParser.GetDynamicConfigObject());
            anim.SetConfigObject(configParser.GetDynamicConfigObject());

            yield return StartCoroutine(graphInput.GraphInitStart());
        }

        topo.SetParameters(yamlParser.GetHostNames(), yamlParser.GetSwitchNames(), 
                            yamlParser.GetSatelliteNames(), yamlParser.GetDropperNames(), yamlParser.GetSwitchHostLinks(), 
                            yamlParser.GetSatelliteLinks());
        topo.Display();
        topo.GetPosition();
        topo.DisplayTopology();

        if(Global.showAnimation==1){
            billBoard.BillBoardInit();

            introTag.IntroTagInit(configParser.GetDynamicConfigObject());

            // yield return StartCoroutine(anim.GetElapsedTimeFile());
            
            // anim.StartAnimation();

            yield return StartCoroutine(anim.GetMetadataFile());
            yield return StartCoroutine(anim.WriteFile());
            
            anim.AnimationInit();
        }
    }

    // Update is called once per frame
    void Update(){
        topo.LablesFollowCam();
        if(Global.showAnimation==1){
            billBoard.BillBoardFollowCam();
        }
    }
}
