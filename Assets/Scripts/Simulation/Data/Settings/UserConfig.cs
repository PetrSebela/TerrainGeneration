using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


[System.Serializable]
public class UserConfig
{    
    public int WinHeight = 1080;
    public int WinWidth = 1920;
    public int UserFOV = 60;
    public int LevelDetail = 0;
    public float MouseSensitivity = 1.5f;
    public bool InputSmoothing = false;

    public static void SaveConfig(UserConfig userConfig){
        string userConfigJson = JsonUtility.ToJson(userConfig);
        File.WriteAllText(Application.dataPath + "/user.conf",userConfigJson);
        Debug.Log("saving user config");
        Debug.Log(userConfig.ToString());
        // Debug.Log("Config saved in path: " + Application.dataPath + "/user.conf");
    }

    public static UserConfig LoadConfig(){
        Debug.Log("loading user config");

        string configPath = Application.dataPath + "/user.conf";

        if(File.Exists(configPath)){
            using(StreamReader sr = new StreamReader(configPath)){
                UserConfig uc = JsonUtility.FromJson<UserConfig>(sr.ReadToEnd());
                Debug.Log(uc.ToString());
                return uc;
            }
        }
        else{
            return new UserConfig();
        }
    }

    public override string ToString()
    {
        return string.Format("Resolution {0} | {1} - Quality {2} - FOV {3}",WinWidth,WinHeight,LevelDetail,UserFOV);
    }
}