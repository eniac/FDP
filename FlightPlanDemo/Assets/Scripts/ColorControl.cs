using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorControl : MonoBehaviour
{
    Dictionary<Tuple<string, string>, Color> ColorsByPath = new Dictionary<Tuple<string, string>, Color>();
    Dictionary<string, Color> ColorsByOrigin = new Dictionary<string, Color>();
    HashSet<Tuple<string, string, string>> paths = new HashSet<Tuple<string, string, string>>();
    Color colorRequest = Color.red;
    Color colorReply = Color.blue;
    List<Color> mcdColor = new List<Color>(){ new Color(0.055f, 0.95f, 1f), new Color(0.454f, 0.953f, 0.059f) };
    List<Color> originColor = new List<Color>(){new Color(0f, 0f, 1f), new Color(1f, 0f, 0f), new Color(1f, 1f, 0f), new Color(0.5f, 0f, 0.5f), new Color(0f, 1f, 1f) };
    int mcdColorIndex=0;
    int originColorIndex=0;
    int colorPatternIndex = 0; 

    public void ResetColorControl(){
        ColorsByPath.Clear();
        ColorsByOrigin.Clear();
        paths.Clear();
        mcdColorIndex=0;
        originColorIndex=0;
    }
    public void SetColorPattern(int index){
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
                Debug.Log("MCD = " + org + " : " + mcdColorIndex);
                ColorsByOrigin.Add(org, mcdColor[mcdColorIndex]);
                color = ColorsByOrigin[org];
                mcdColorIndex++;
            }
            return color;
        }
        // Packet type is other then mcd and normal
        else if(pType != Global.PacketType.Normal){
            return pColor;
        }
        // FOr all normal packets
        switch(colorPatternIndex){
            case 0:
            // Color Pattern 1
                if(ColorsByOrigin.ContainsKey(origin) == true){
                    color = ColorsByOrigin[origin];
                }
                else{
                    // ColorsByOrigin.Add(origin, UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f));
                    ColorsByOrigin.Add(origin, originColor[originColorIndex]);
                    color = ColorsByOrigin[origin];
                    originColorIndex = (originColorIndex + 1)%originColor.Count;
                }
                break;

            case 1:
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

            case 2:
                // Color Pattern 3
                Tuple<string, string> pair = new Tuple<string, string>(origin, destination);
                // If the pair of origin and destination is found in dictionary, return the color
                if(ColorsByPath.ContainsKey(pair) == true){
                    color = ColorsByPath[pair];
                }
                else{
                    ColorsByPath.Add(pair, UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f));
                    color = ColorsByPath[pair];
                }
                break;
        }
        return color;
    }
}
