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
    [SerializeField] TextMeshProUGUI jobType;

    [SerializeField] private GameObject worldManager;
    
    private ChunkManager chunkManager;
    void Start()
    {
        chunkManager = worldManager.GetComponent<ChunkManager>();
    }
    void Update()
    {
        if (!chunkManager.GenerationComplete)
        {
            slider.value = chunkManager.Progress;
            percentage.text = (int)(chunkManager.Progress * 100) + "%";
            jobType.text = chunkManager.ActiveGenerationJob;
            return;
        }

        if (chunkManager.GenerationComplete)
        {
            image.transform.gameObject.SetActive(false);
            slider.transform.gameObject.SetActive(false);
        }

        
    }
}
