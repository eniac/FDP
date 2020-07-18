using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Global
{
     public enum AnimStatus{
        Pause=0,
        Forward,
        Rewind,
        Disk
    }
    public enum SliderMode{
        Normal=0,
        Jump
    }
    public static string experimentYaml;
    public static string experimentMetadata;
    public static float gVar = 10;
    public const float U_SEC = 1000000f;
}
