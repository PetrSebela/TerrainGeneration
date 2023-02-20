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
                SceneDirector.RemoveSimulation(path);
            };
        
            instance.GetComponentsInChildren<Button>()[1].onClick.AddListener(removeAction);
        }
    }
}
