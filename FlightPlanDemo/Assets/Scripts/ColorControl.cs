using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorControl : MonoBehaviour
{
    Dictionary<Tuple<string, string>, Color> ColorsByPath = new Dictionary<Tuple<string, string>, Color>();
    List<Color> pathColor = new List<Color>(){ new Color(0f, 0f, 1f), new Color(0f, 1f, 0f), new Color(1f, 0f, 0f), new Color(1f, 1f, 0f), new Color(0.5f, 0f, 0.5f), new Color(0f, 1f, 1f), new Color(0.055f, 0.95f, 1f), new Color(0.454f, 0.953f, 0.059f), new Color(1f, 0, 1f),  new Color(1f, 1f, 1f)  };
    int pathColorIndex=0;
    HashSet<Tuple<string, string, string>> paths = new HashSet<Tuple<string, string, string>>();
    Color colorRequest = Color.red;
    Color colorReply = Color.blue;
    Dictionary<string, Color> ColorsByOrigin = new Dictionary<string, Color>();
    List<Color> mcdColor = new List<Color>(){ new Color(0.055f, 0.95f, 1f), new Color(0.454f, 0.953f, 0.059f) };
    Color mcdCacheColor = new Color(1f, 0.54f, 0f);
    List<Color> originColor = new List<Color>(){new Color(0f, 0f, 1f), new Color(1f, 1f, 0f), new Color(1f, 0f, 0f), new Color(0.5f, 0f, 0.5f), new Color(0f, 1f, 1f) };
    int mcdColorIndex=0;
    int originColorIndex=0;
    Global.ColorPattern colorPatternIndex = Global.ColorPattern.OriginBased; 

    public void ResetColorControl(){
        ColorsByPath.Clear();
        ColorsByOrigin.Clear();
        paths.Clear();
        mcdColorIndex=0;
        originColorIndex=0;
    }
    public void SetColorPattern(Global.ColorPattern index){
        colorPatternIndex = index;
    }
    public Color GetPacketColor(string origin, string destination, string pid, Global.PacketType pType, Color pColor){
        Color color = Color.yellow;

        // Packet type is mcd
        if(pType == Global.PacketType.MCD){
            string org = "MCD"+origin;
            if(ColorsByOrigin.ContainsKey(org) == true){
                color = ColorsByOrigin[org];
            }
            else{
                // Debug.Log("MCD = " + org + " : " + mcdColorIndex);
                ColorsByOrigin.Add(org, mcdColor[mcdColorIndex]);
                color = ColorsByOrigin[org];
                mcdColorIndex++;
            }
            // Debug.Log("COLOR = " + color + " : " + pid);
            return color;
        }

        // If packet type is MCD cache
        if(pType == Global.PacketType.MCDcache){
            return mcdCacheColor;
        }
        else if(pType == Global.PacketType.Parity){
            return Color.white;
        }
        // Packet type is other then mcd and normal
        else if(pType == Global.PacketType.HC){
            return new Color(1f, 0, 1f);
        }
        // FOr all normal packets
        switch(colorPatternIndex){
            case Global.ColorPattern.OriginBased:
            // Color Pattern 1
                string org = pType.ToString()+origin;
                if(ColorsByOrigin.ContainsKey(org) == true){
                    color = ColorsByOrigin[org];
                }
                else{
                    // ColorsByOrigin.Add(origin, UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f));
                    ColorsByOrigin.Add(org, originColor[originColorIndex]);
                    color = ColorsByOrigin[org];
                    originColorIndex = (originColorIndex + 1) % originColor.Count;
                }
                break;

            case Global.ColorPattern.RequestReply:
            // Color Pattern 2
                Tuple<string, string, string> fwdPair = new Tuple<string, string, string>(origin, destination, pid);
                Tuple<string, string, string> revPair = new Tuple<string, string, string>(destination, origin, pid);

                if(paths.Contains(fwdPair) == true){
                    color = colorRequest;
                }
                else if(paths.Contains(revPair) == true){
                    color = colorReply;
                }
                else{
                    paths.Add(fwdPair);
                    color = colorRequest;
                }
                break; 

            case Global.ColorPattern.PathBased:
                // Color Pattern 3
                Tuple<string, string> pair = new Tuple<string, string>(origin, destination);
                // If the pair of origin and destination is found in dictionary, return the color
                if(ColorsByPath.ContainsKey(pair) == true){
                    color = ColorsByPath[pair];
                }
                else{
                    // ColorsByPath.Add(pair, UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f));
                    ColorsByPath.Add(pair, pathColor[pathColorIndex]);
                    color = ColorsByPath[pair];
                    pathColorIndex = (pathColorIndex + 1) % pathColor.Count;
                }
                break;
                
            case Global.ColorPattern.None:
                color = Color.yellow;
                break;
        }
        return color;
    }
}
