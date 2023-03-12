using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleHUD : MonoBehaviour
{
    [SerializeField] private ChunkManager ChunkManager;
    [SerializeField] private KeyCode ToggleKey;
    [SerializeField] private GameObject[] ToToggle;
    private bool[] StateArray;
    bool OriginalState = true;

    void Start(){
        StateArray = new bool[ToToggle.Length];
        for (int i = 0; i < ToToggle.Length; i++)
        {
            StateArray[i] = ToToggle[i].activeSelf;
        }
    }

    void Update(){
        if(!ChunkManager.GenerationComplete)
            return;

        if(Input.GetKeyDown(ToggleKey))
        {
            if ( OriginalState ) TurnOffAllElements();
            else SetElementsToOriginalState();
        }
    }

    void TurnOffAllElements(){
        for (int i = 0; i < ToToggle.Length; i++)
        {
            StateArray[i] = ToToggle[i].activeSelf;
            ToToggle[i].SetActive(false);
        }
        OriginalState = false;
    }

    void SetElementsToOriginalState(){
        for (int i = 0; i < ToToggle.Length; i++)
        {
            ToToggle[i].SetActive(StateArray[i]);
        }
        OriginalState = true;                
    }
}
