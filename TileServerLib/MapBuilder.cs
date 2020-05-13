using System;
using System.Collections.Generic;
using System.Linq;
//using UnityEngine;

namespace TileServerLib
{
    public class MapBuilder //: MonoBehaviour
    {
        public int ZoomLevel = 12;
        public float MapTileSize = 0.5f;
        public float Latitude = 47.642567f;
        public float Longitude = -122.136919f;
        public bool BuildOnStart = false;
        // the number of tiles per edge (-1 because center tile)
        //public float MapSize = 12;
        public float MapSize = 8;

        string _bingAccessKey;

        public float CenterTileTopLeftLatitude { get; private set; }
        public float CenterTileTopLeftLongitude { get; private set; }
        public int MaxElevation { get; private set; }
        public int MinElevation { get; private set; }

        public float MapTotal2DEdgeLength
        {
            get
            {
                return MapTileSize * (MapSize + 1);
            }
        }
        public float MapTotal2DDiagonalLength
        {
            get
            {
                return (float)Math.Sqrt(MapTotal2DEdgeLength * MapTotal2DEdgeLength * 2);
            }
        }
        public float MapTotal3DDiagonalLength
        {
            get
            {
                return (float)Math.Sqrt((MapTotal2DDiagonalLength * MapTotal2DDiagonalLength) + (MaxElevation * MaxElevation));
            }
        }

        private TileInfo _centerTile;
        private List<MapTile> _mapTiles;

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bingAccessKey"></param>
        //*********************************************************************
        public MapBuilder(string bingAccessKey)
        {
            _bingAccessKey = bingAccessKey;
        }

        //*********************************************************************
        /// <summary>
        /// Fetch elevation and image tiles for an area given zoom, size, and
        /// center coords. All tile data is stored in local cache, making this
        /// useful for prefetch for later field use. The callback is invoked
        /// once for each tile success or failure.
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <param name="zoom"></param>
        /// <param name="size"></param>
        /// <param name="callback"></param>
        //*********************************************************************
        public void GetMapData(float lat, float lon, 
            int zoom, int size, Action<FetchStatus> callback)
        {
            _mapTiles = new List<MapTile>();

            Latitude = lat;
            Longitude = lon;
            ZoomLevel = zoom;
            MapSize = size;

            _mapTiles = new List<MapTile>();
            GetMapData(callback);
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        //*********************************************************************
        public void GetMapData(Action<FetchStatus> callback)
        {
            if (0 == MapTileSize)
                MapTileSize = (float)TileInfo.TileSizeMeters(Latitude, ZoomLevel, 256);

            _centerTile = new TileInfo(new WorldCoordinate { Lat = Latitude, Lon = Longitude },
                ZoomLevel, MapTileSize);

            var LL = _centerTile.TopLeftLatLon();

            CenterTileTopLeftLatitude = LL.Lat;
            CenterTileTopLeftLongitude = LL.Lon;

            ////_centerTile.X -= (int)(MapTileSize / 2.0f);
            ////_centerTile.Y += (int)(MapTileSize / 2.0f);

            FetchTiles(callback);
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="forceReload"></param>
        //*********************************************************************
        private void FetchTiles(Action<FetchStatus> callback)
        {
            var size = (int)(MapSize / 2);
            var countFetched = 0;

            var tileIndex = 0;
            for (var x = -size; x <= size; x++)
            {
                for (var y = -size; y <= size; y++)
                {
                   // _infoTextLarge.text = $"Fetching Tile: {countFetched++} of: {(size + 1) * (size + 1)}";

                    var tile = GetOrCreateTile(tileIndex++);

                    tile.FetchTileData(new TileInfo(
                        _centerTile.X - x, _centerTile.Y + y,
                        ZoomLevel, MapTileSize, _centerTile.CenterLocation),
                        callback);

                    MaxElevation = Math.Max(MaxElevation, tile.MaxElevation);
                    MinElevation = Math.Min(MinElevation, tile.MinElevation);
                }
            }
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        //*********************************************************************
        private MapTile GetOrCreateTile(int i)
        {
            if (_mapTiles.Any() && _mapTiles.Count > i)
                return _mapTiles[i];

            var tile = new MapTile(_bingAccessKey);

            _mapTiles.Add(tile);
            return tile;
        }
    }
}
