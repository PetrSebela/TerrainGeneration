using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class UpdateUI : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private Image image;

    [SerializeField] TextMeshProUGUI percentage;
    [SerializeField] private GameObject worldManager;
    private ChunkManager chunkManager;
    void Start()
    {
        chunkManager = worldManager.GetComponent<ChunkManager>();
    }
    void Update()
    {
        if (!chunkManager.generationComplete)
        {
            slider.value = chunkManager.progress;
            percentage.text = (int)(chunkManager.progress * 100) + "%";
        }

        if (chunkManager.generationComplete)
        {
            image.transform.gameObject.SetActive(false);
            slider.transform.gameObject.SetActive(false);
        }
    }
}
