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
    public enum GraphType{
        Type0=0,
        Type1,
        Type2,
        Type3,
        Type4
    }
    public enum PacketType{
        Normal=0,
        Parity,
        MCD,
        HC
    }
    public static string chosanExperimentName;
    public static string experimentYaml;
    public static string experimentMetadata;
    public static float gVar = 10;
    public const float U_SEC = 1000000f;
}
