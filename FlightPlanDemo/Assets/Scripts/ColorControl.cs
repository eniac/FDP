using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorControl : MonoBehaviour
{
    Dictionary<Tuple<string, string>, Color> ColorsByPath = new Dictionary<Tuple<string, string>, Color>();
    Dictionary<string, Color> ColorsByOrigin = new Dictionary<string, Color>();
    HashSet<Tuple<string, string, string>> paths = new HashSet<Tuple<string, string, string>>();
    Color colorRequest = Color.yellow;
    Color colorReply = Color.blue;
    int colorPatternIndex = 0; 


    public void SetColorPattern(int index){
        colorPatternIndex = index;
    }
    public Color GetPacketColor(string origin, string destination, string pid){
        switch(colorPatternIndex){
            case 0:
            // Color Pattern 1
                Tuple<string, string> pair = new Tuple<string, string>(origin, destination);
                // If the pair of origin and destination is found in dictionary, return the color
                if(ColorsByPath.ContainsKey(pair) == true){
                    return ColorsByPath[pair];
                }
                else{
                    ColorsByPath.Add(pair, UnityEngine.Random.ColorHSV(0f, 0.5f, 1f, 1f, 0.5f, 1f));
                    return ColorsByPath[pair];
                }
                break;

            case 1:
            // Color Pattern 2
                if(ColorsByOrigin.ContainsKey(origin) == true){
                    return ColorsByOrigin[origin];
                }
                else{
                    ColorsByOrigin.Add(origin, UnityEngine.Random.ColorHSV(0f, 0.5f, 1f, 1f, 0.5f, 1f));
                    return ColorsByOrigin[origin];
                }
                break;

            case 2:
            // Color Pattern 3
                Tuple<string, string, string> fwdPair = new Tuple<string, string, string>(origin, destination, pid);
                Tuple<string, string, string> revPair = new Tuple<string, string, string>(destination, origin, pid);

                if(paths.Contains(fwdPair) == true){
                    return colorRequest;
                }
                if(paths.Contains(revPair) == true){
                    return colorReply;
                }
                else{
                    paths.Add(fwdPair);
                    return colorRequest;
                }
                break;

            default:
                return Color.yellow;
        }
    }
}
