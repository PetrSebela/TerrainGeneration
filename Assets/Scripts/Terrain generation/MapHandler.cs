using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapHandler : MonoBehaviour
{
    public RawImage MapImage;
    public Image MapPointer;
    public Transform Viewer;
    public SimulationSettings SimulationSettings;
    public ChunkSettings ChunkSettings;

    void Update()
    {

        Vector2 translatedPosition = new Vector2(Viewer.position.x, Viewer.position.z) / (ChunkSettings.ChunkSize * SimulationSettings.WorldSize) * 0.5f;
        MapPointer.rectTransform.anchoredPosition = translatedPosition * MapImage.rectTransform.sizeDelta;
        MapPointer.transform.rotation = Quaternion.Euler(new Vector3(
            0,
            0,
            Viewer.transform.rotation.eulerAngles.y * -1));
    }
}
