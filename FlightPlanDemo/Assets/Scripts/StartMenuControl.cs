using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using System.IO.Compression;

public class StartMenuControl : MonoBehaviour
{
    [SerializeField] private Dropdown experimentDropdown = default;
    [SerializeField] private Text messageText = default;
    [SerializeField] private PopUpControl popup = default;
    List<string> experimentNames = new List<string>(){"Choose an Experiment..."};
    List<FileInfo> experimentZips = new List<FileInfo>();
    FileInfo choosanExperimentZip = null;

    public void Start(){
        DirectoryInfo dir = new DirectoryInfo(Application.streamingAssetsPath);
        FileInfo[] files = dir.GetFiles();
        foreach(FileInfo zip in files){
            if(zip.Extension == ".zip"){
                Debug.Log(zip.Name);
                experimentNames.Add(zip.Name);
                experimentZips.Add(zip);
            }
        }
        PopulateExperimentDropdown();
        popup.PopUpInit(messageText);
    }

    void PopulateExperimentDropdown(){
        experimentDropdown.AddOptions(experimentNames);
    }
    public void ExperimentDropdownIndexChanged(int index){
        if(index == 0){
            choosanExperimentZip = null;
        }
        else{
            // Avoid index=0, since this idex is only a string showing "choose an experiment"
            choosanExperimentZip = experimentZips[index-1];
        }
    }
    
    public void StartDemo(){
        if(choosanExperimentZip == null){
            popup.ShowErrorMessage("Please choose an Experiment!!!", 8, Color.red);
            return;
        }
        // Unzip the experiment file
        unZipFile();
        // Jump to the next scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitDemo(){
        Debug.Log("Quit Demo");
        // Can not see effect of "Application.Quit()" in editor
        Application.Quit();
    }

    void unZipFile(){
        string zipPath = Path.Combine(Application.streamingAssetsPath, choosanExperimentZip.Name);
        string unZipPath = Path.Combine(Application.streamingAssetsPath, Path.GetFileNameWithoutExtension(zipPath));
        string extractPath = Path.Combine(Application.streamingAssetsPath, "");

        // Directory already exists then remove it and create new one
        if (Directory.Exists(unZipPath))  
        {  
            Directory.Delete(unZipPath, true);  
        }
        // Extract the zip, if already exist then overwrite it.
        ZipFile.ExtractToDirectory(zipPath, extractPath);
        // Read files from choosan experiment directory and set the global strings for future use 
        DirectoryInfo unZipDir = new DirectoryInfo(unZipPath);
        FileInfo[] experimentFiles = unZipDir.GetFiles();
        foreach(FileInfo file in experimentFiles){
            if(file.Extension == ".yml" || file.Extension == ".yaml"){
                Global.experimentYaml = file.FullName;
            }
            else if(file.Name == "metadata" || file.Name == "metadata.txt"){
                Global.experimentMetadata = file.FullName;
            }
        }
    }
}
