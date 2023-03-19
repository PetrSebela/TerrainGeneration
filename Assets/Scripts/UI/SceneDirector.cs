using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.IO;

public class SceneDirector : MonoBehaviour
{
    public SimulationSettings SimulationSettings;
    public TerrainSettings TerrainSettings;
    public ListWorlds WorldLister;
    [SerializeField] private TMP_Dropdown maxHeightField;
    [SerializeField] private TMP_Dropdown worldSize;
    [SerializeField] private TMP_InputField seedField;
    [SerializeField] private Slider winklesSlider;
    [SerializeField] private TMP_Text winklesSliderValue;
    public UserConfig UserConfig;

    private Dictionary<string,int> SizeMapper = new Dictionary<string, int>(){
       {"16 x 16", 8},
       {"24 x 24", 12},
       {"32 x 32", 16},
       {"48 x 48", 24},
       {"64 x 64", 32},
       {"96 x 96", 48},
    };

    public void Start(){
        UserConfig = UserConfig.LoadConfig();
        Screen.SetResolution(UserConfig.WinWidth, UserConfig.WinHeight, FullScreenMode.FullScreenWindow);
        QualitySettings.SetQualityLevel(UserConfig.LevelDetail);
    }

    public void BeginSimulation(){
        TerrainSettings.MaxHeight = float.Parse(maxHeightField.options[maxHeightField.value].text);
        TerrainSettings.MinHeight = -float.Parse(maxHeightField.options[maxHeightField.value].text) / 10;
        TerrainSettings.WrinkleMagniture = winklesSlider.value;

        SimulationSettings.Seed = seedField.text;
        SimulationSettings.WorldSize = SizeMapper[worldSize.options[worldSize.value].text];
        SimulationSettings.Name = seedField.text;

        SceneManager.LoadScene("Simulation",LoadSceneMode.Single);
    }

    public void QuitApplication(){
        Application.Quit();
    }

    public void LoadSimulation(string path){
        SerializationHandler.DeserializeTerrain(path,this);
        SceneManager.LoadScene("Simulation",LoadSceneMode.Single);
    }

    public void RemoveSimulation(string path){

        System.IO.Directory.Delete(path,true);
        WorldLister.LoadWorldList();
    }

    public void UpdateWrinkleSlider(){
        winklesSliderValue.text = ((int)(winklesSlider.value*100)/100f).ToString();
    }
}
