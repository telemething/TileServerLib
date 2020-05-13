namespace TileServerLib
{
    public interface IMapUrlBuilder
    {
        string GetTileUrl(TileInfo tileInfo);
    }
}
