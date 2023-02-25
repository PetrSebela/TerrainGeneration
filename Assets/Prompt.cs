using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class Prompt : MonoBehaviour
{
    public ChunkManager ChunkManager;
    public void SaveWithName(TMP_InputField inputField){
        Debug.Log(inputField.text);
        SerializationHandler.SaveTerrain(ChunkManager,inputField.text);
        Destroy(this.transform.gameObject);
    }

    public void CancelPrompt(){
        Debug.Log("canceling save prompt");
        Destroy(this.transform.gameObject);
    }
}
