using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class Prompt : MonoBehaviour
{
    public ChunkManager ChunkManager;
    public string Path;
    public SceneDirector SceneDirector;
    public GameObject PromptObject;
    public GameObject Background;
    public GameObject PauseMenu;



    public void SaveWithName(TMP_InputField inputField){
        Debug.Log(inputField.text);
        SerializationHandler.SaveTerrain(ChunkManager,inputField.text);
        PauseMenu.SetActive(true);
        Destroy(this.transform.gameObject);
    }

    public void CancelPrompt(){
        Debug.Log("canceling save prompt");
        try{
            PauseMenu.SetActive(true);
        }
        catch{}
        Destroy(Background);
        Destroy(this.transform.gameObject);
    }
    public void RemoveSimulation(){
        SceneDirector.RemoveSimulation(Path);
        Destroy(Background);
        Destroy(this.transform.gameObject);
    }

    public void CancelPromptBackground(){
        Debug.Log("bg destroy");
        Destroy(PromptObject);
        // Destroy(Background);
        Destroy(this.transform.gameObject);
    }
}
