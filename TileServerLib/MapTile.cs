using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace TileServerLib
{
    public class DynamicTextureDownloader
    {
        public string ImageUrl;
        public bool ResizePlane;
        public bool _useCache = true;
        protected TileInfo _tileData;
        protected string _pngExtenstion = "png";

    }

    public class FetchStatus
    {
        public enum DataTypeEnum { Image, Elevation }
        public enum ResultEnum { Success, Failure }

        public DataTypeEnum DataType;

        public ResultEnum Result;

        public string Message = null;

        public object Data = null;

        public FetchStatus(
            DataTypeEnum dataType, ResultEnum result, 
            object data = null, string message = null)
        {
            DataType = dataType;
            Result = result;
            Data = data;
            Message = message;
        }
    }

    public class MapTile : DynamicTextureDownloader
    {
        string _bingAccessKey;

        public IMapUrlBuilder MapBuilder { get; set; }
        private string _txtExtenstion = "txt";
        private static object _webReqLock = new object();
        public int MaxElevation { get; private set; }
        public int MinElevation { get; private set; }
        public bool IsDownloading { get; private set; }

        public TileInfo TileData
        {
            get { return _tileData; }
            private set
            {
                _tileData = value;
                ImageUrl = MapBuilder.GetTileUrl(_tileData);
            }
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        //*********************************************************************
        public MapTile(string bingAccessKey)
        {
            _bingAccessKey = bingAccessKey;
            MapBuilder = MapBuilder != null ? MapBuilder : new OpenStreetMapTileBuilder();
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tileData"></param>
        /// <param name="callabck"></param>
        //*********************************************************************
        public void FetchTileData(TileInfo tileData, Action<FetchStatus> callback)
        {
            TileData = tileData;
            FetchImageData(tileData, callback);
            FetchElevationData(tileData, callback);
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tileData"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        //*********************************************************************
        private async Task<byte[]> FetchImageData(TileInfo tileData, Action<FetchStatus> callback)
        {
            byte[] data = null;

            if (_useCache & TileCache.DoesExist(tileData.ZoomLevel, tileData.X, tileData.Y,
                TileCache.DataTypeEnum.StreetMap, TileCache.DataProviderEnum.OSM, _pngExtenstion))
            {
                data = TileCache.FetchBytes(tileData.ZoomLevel, tileData.X, tileData.Y,
                TileCache.DataTypeEnum.StreetMap, TileCache.DataProviderEnum.OSM, _pngExtenstion);
            }
            else
            {
                try
                {
                    OpenStreetMapClient osmc = new OpenStreetMapClient();
                    data = await osmc.FetchImageTile(tileData.ZoomLevel, tileData.X, tileData.Y);

                    if (_useCache)
                        TileCache.Store(data, tileData.ZoomLevel,
                            tileData.X, tileData.Y, TileCache.DataTypeEnum.StreetMap,
                            TileCache.DataProviderEnum.OSM, _pngExtenstion);
                }
                catch(Exception ex)
                {
                    callback?.Invoke(new FetchStatus(
                        FetchStatus.DataTypeEnum.Image, FetchStatus.ResultEnum.Failure, 
                        null, ex.Message));
                }
            }

            callback?.Invoke(new FetchStatus(
                FetchStatus.DataTypeEnum.Image, FetchStatus.ResultEnum.Success, data));
            return data;
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tiledata"></param>
        /// <param name="forceReload"></param>
        //*********************************************************************
        public async Task<string> FetchElevationData(TileInfo tileData, Action<FetchStatus> callback)
        {
            string data = null;

            if (_useCache & TileCache.DoesExist(tileData.ZoomLevel, tileData.X, tileData.Y,
                TileCache.DataTypeEnum.Elevation, TileCache.DataProviderEnum.VirtualEarth, _txtExtenstion))
            {
                data = TileCache.FetchText(tileData.ZoomLevel, tileData.X, tileData.Y,
                TileCache.DataTypeEnum.Elevation, TileCache.DataProviderEnum.VirtualEarth, _txtExtenstion);
            }
            else
            {
                try
                {
                    BingClient bc = new BingClient(_bingAccessKey);
                    data = await bc.FetchElevationTile(tileData.GetNorthEast(), tileData.GetSouthWest());

                    if (_useCache)
                        TileCache.Store(data, tileData.ZoomLevel,
                            tileData.X, tileData.Y, TileCache.DataTypeEnum.Elevation,
                            TileCache.DataProviderEnum.VirtualEarth, _txtExtenstion);
                }
                catch (Exception ex)
                {
                    callback?.Invoke(new FetchStatus(
                        FetchStatus.DataTypeEnum.Elevation, FetchStatus.ResultEnum.Failure,
                        null, ex.Message));
                }
            }

            callback?.Invoke(new FetchStatus(
                FetchStatus.DataTypeEnum.Elevation, FetchStatus.ResultEnum.Success, data));
            return data;
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="zoom"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="callback"></param>
        /// <returns>Elevation data container</returns>
        //*********************************************************************
        public async Task<string> FetchElevationData(int zoom, int x, int y, Action<FetchStatus> callback)
        {
            return await FetchElevationData(new TileInfo(x, y, zoom, 0, null), callback);
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="zoom"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="callback"></param>
        /// <returns>Image data bytes</returns>
        //*********************************************************************
        public async Task<byte[]> FetchImageData(int zoom, int x, int y, Action<FetchStatus> callback)
        {
            return await FetchImageData(new TileInfo(x, y, zoom, 0, null), callback);
        }
    }
}
