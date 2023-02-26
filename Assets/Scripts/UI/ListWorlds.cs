using UnityEngine;
using System.IO;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;
public class ListWorlds : MonoBehaviour
{
    public GameObject TerrainField;
    public SceneDirector SceneDirector;
    public DirectoryInfo[] dirs;
    public Transform parent;
    public GameObject AcceptPrompt;
    public GameObject CanvasParent;
    public GameObject ActivePrompt;
    public GameObject PromptBackground;

    void Start()
    {
        LoadWorldList();
    }

    public void LoadWorldList(){
        dirs = SerializationHandler.GetSavedTerrains();
        foreach(Transform child in parent.transform){
            Destroy(child.gameObject);
        }

        for (int i = 0; i < dirs.Length; i++)
        {
            GameObject instance = GameObject.Instantiate(TerrainField,parent:parent);

            instance.GetComponentInChildren<TMP_Text>().text = dirs[i].Name;
            instance.GetComponent<RectTransform>().anchoredPosition = new Vector2(0,i * -50);
            instance.GetComponent<RectTransform>().localScale = Vector3.one;
            
            string path = dirs[i].FullName;
            UnityAction action = delegate{
                SceneDirector.LoadSimulation(path);
            };
            instance.GetComponentsInChildren<Button>()[0].onClick.AddListener(action);


            UnityAction removeAction  = delegate{
                PromptDelete(path);
            };
        
            instance.GetComponentsInChildren<Button>()[1].onClick.AddListener(removeAction);
        }
    }

    public void PromptDelete(string path){
        GameObject g = Instantiate(PromptBackground);
        g.transform.SetParent(CanvasParent.transform);
        g.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
        g.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        g.GetComponent<RectTransform>().anchorMax = Vector2.one;
        g.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        
        GameObject go = Instantiate(AcceptPrompt);
        g.GetComponent<Prompt>().PromptObject = go;
        go.GetComponent<Prompt>().SceneDirector = SceneDirector;
        go.GetComponent<Prompt>().Path = path;
        go.GetComponent<Prompt>().Background = g;
        go.transform.SetParent(CanvasParent.transform);

        go.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f,0.5f);
        go.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f,0.5f);
        go.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
        ActivePrompt = go;
    }
}


