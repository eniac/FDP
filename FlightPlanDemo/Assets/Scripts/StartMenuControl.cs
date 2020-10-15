using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

using System.IO.Compression;

public class StartMenuControl : MonoBehaviour
{
    [SerializeField] private Dropdown experimentDropdown = default;
    [SerializeField] private Text messageText = default;
    [SerializeField] private PopUpControl popup = default;
    const string EXP_TEXT = "Choose an Experiment...";
    List<string> experimentNames = new List<string>(){EXP_TEXT};
    string chosenExperiment = EXP_TEXT;

    public IEnumerator Start(){
        yield return StartCoroutine(GetExperiments());
        PopulateExperimentDropdown();
        popup.PopUpInit(messageText);
    }

    // Load all the experiment name from the Streaming Assets/experiments.txt file
    public IEnumerator GetExperiments(){
        var filePath = Path.Combine(Application.streamingAssetsPath, "experiments.txt");
        Debug.Log("Experiment file path = " + filePath);
        string experimentsString;

        if (filePath.Contains ("://") || filePath.Contains (":///")) {
            // TODO: if experiment file doesn't exist
            var loaded = new UnityWebRequest(filePath);
            loaded.downloadHandler = new DownloadHandlerBuffer();
            yield return loaded.SendWebRequest();
            experimentsString = loaded.downloadHandler.text;
        }
        else{
            experimentsString = File.ReadAllText(filePath);
        }
        var r = new StringReader(experimentsString);
        string name = r.ReadLine();;
        while(name != null){
            experimentNames.Add(name);
            name = r.ReadLine();
        }
    } 
    // Populate experiment name in drop down menu
    void PopulateExperimentDropdown(){
        List<string> listNames = new List<string>();
        foreach(string name in experimentNames){
            listNames.Add(name.Replace('_',' '));
        }
        experimentDropdown.AddOptions(listNames);
    }

    public void ExperimentDropdownIndexChanged(int index){
        chosenExperiment = experimentNames[index];
        Global.chosanExperimentName = chosenExperiment;
    }
    
    public void StartDemo(){
        if(chosenExperiment == EXP_TEXT){
            popup.ShowErrorMessage("Please choose an Experiment!!!", 8, Color.red);
            return;
        }
        // Set the global file name variable
        SetFileNames();
        // Jump to the next scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    void SetFileNames(){
        Global.experimentYaml = chosenExperiment + "/topology.yml";
        Global.configYaml = chosenExperiment + "/config.yml";
        Global.experimentMetadata = chosenExperiment + "/metadata.txt";
    }

    public void QuitDemo(){
        Debug.Log("Quit Demo");
        // Can not see effect of "Application.Quit()" in editor
        Application.Quit();
    }
}


// public class StartMenuControlForDesktop : MonoBehaviour
// {
//     [SerializeField] private Dropdown experimentDropdown = default;
//     [SerializeField] private Text messageText = default;
//     [SerializeField] private PopUpControl popup = default;
//     List<string> experimentNames = new List<string>(){"Choose an Experiment..."};
//     List<FileInfo> experimentZips = new List<FileInfo>();
//     FileInfo chosenExperiment = null;

//     public void Start(){
//         DirectoryInfo dir = new DirectoryInfo(Application.streamingAssetsPath);
//         FileInfo[] files = dir.GetFiles();
//         foreach(FileInfo zip in files){
//             if(zip.Extension == ".zip"){
//                 Debug.Log(zip.Name);
//                 experimentNames.Add(zip.Name);
//                 experimentZips.Add(zip);
//             }
//         }
//         PopulateExperimentDropdown();
//         popup.PopUpInit(messageText);
//     }

//     void PopulateExperimentDropdown(){
//         experimentDropdown.AddOptions(experimentNames);
//     }
//     public void ExperimentDropdownIndexChanged(int index){
//         if(index == 0){
//             chosenExperiment = null;
//         }
//         else{
//             // Avoid index=0, since this idex is only a string showing "choose an experiment"
//             chosenExperiment = experimentZips[index-1];
//         }
//     }
    
//     public void StartDemo(){
//         if(chosenExperiment == null){
//             popup.ShowErrorMessage("Please choose an Experiment!!!", 8, Color.red);
//             return;
//         }
//         // Unzip the experiment file
//         unZipFile();
//         // Jump to the next scene
//         SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
//     }

//     public void QuitDemo(){
//         Debug.Log("Quit Demo");
//         // Can not see effect of "Application.Quit()" in editor
//         Application.Quit();
//     }

//     void unZipFile(){
//         string zipPath = Path.Combine(Application.streamingAssetsPath, chosenExperiment.Name);
//         string unZipPath = Path.Combine(Application.streamingAssetsPath, Path.GetFileNameWithoutExtension(zipPath));
//         string extractPath = Path.Combine(Application.streamingAssetsPath, "");

//         // Directory already exists then remove it and create new one
//         if (Directory.Exists(unZipPath))  
//         {  
//             Directory.Delete(unZipPath, true);  
//         }
//         // Extract the zip, if already exist then overwrite it.
//         ZipFile.ExtractToDirectory(zipPath, extractPath);
//         // Read files from choosan experiment directory and set the global strings for future use 
//         DirectoryInfo unZipDir = new DirectoryInfo(unZipPath);
//         FileInfo[] experimentFiles = unZipDir.GetFiles();
//         foreach(FileInfo file in experimentFiles){
//             if(file.Extension == ".yml" || file.Extension == ".yaml"){
//                 Global.experimentYaml = file.FullName;
//             }
//             else if(file.Name == "metadata" || file.Name == "metadata.txt"){
//                 Global.experimentMetadata = file.FullName;
//             }
//         }
//     }
// }
