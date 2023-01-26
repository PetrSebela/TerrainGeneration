[System.Serializable]
public class ChunkSettings
{
    public float size;
    public int resolution;
    public int treesPerChunk;

    public ChunkSettings(float size, int resolution, int treesPerChunk)
    {
        this.size = size;
        this.resolution = resolution;
        this.treesPerChunk = treesPerChunk;
    }
}
