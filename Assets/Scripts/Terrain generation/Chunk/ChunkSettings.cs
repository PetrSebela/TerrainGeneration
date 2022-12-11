[System.Serializable]
public class ChunkSettings
{
    public float size;
    public int maxResolution;
    public int treesPerChunk;

    public ChunkSettings(float size, int maxResolution, int treesPerChunk)
    {
        this.size = size;
        this.maxResolution = maxResolution;
        this.treesPerChunk = treesPerChunk;
    }
}
