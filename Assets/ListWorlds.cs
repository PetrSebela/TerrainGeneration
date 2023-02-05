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
    void Start()
    {
        dirs = SerializationHandler.GetSavedTerrains();

        for (int i = 0; i < dirs.Length; i++)
        {
            GameObject instance = GameObject.Instantiate(TerrainField,parent:this.transform);

            instance.GetComponentInChildren<TMP_Text>().text = dirs[i].Name;
            instance.GetComponent<RectTransform>().anchoredPosition = new Vector2(0,i * -50);
            instance.GetComponent<RectTransform>().localScale = Vector3.one;
            
            string path = dirs[i].FullName;
            UnityAction action = delegate{
                SceneDirector.LoadSimulation(path);
            };
            
            instance.GetComponentInChildren<Button>().onClick.AddListener(action);
        }
    }
}
