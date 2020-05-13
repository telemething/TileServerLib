using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TileServerLibTestUi
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        string _bingAccessKey = "xxx";
        int _successfulfetchCount = 0;
        int _failedfetchCount = 0;

        int MapSize;
        int ZoomLevel;
        float MapTileSize = 0.5f;
        float Latitude;
        float Longitude;
        int X;
        int Y;

        public MainPage()
        {
            this.InitializeComponent();
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        //*********************************************************************
        private void ReadUi()
        {
            ZoomLevel = Convert.ToInt32(ZoomValue.Text);
            MapSize = Convert.ToInt32(SizeValue.Text);
            Latitude = Convert.ToSingle(LatValue.Text);
            Longitude = Convert.ToSingle(LonValue.Text);
            X = Convert.ToInt32(XValue.Text);
            Y = Convert.ToInt32(YValue.Text);
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //*********************************************************************
        private void FetchAddress_Click(object sender, RoutedEventArgs e)
        {
            string address = "1 Microsoft Way, Redmond, WA";

            try
            {
                TileServerLib.BingClient ed = 
                    new TileServerLib.BingClient(_bingAccessKey);
                ed.FetchByAddress(address);
            }
            catch (Exception ex)
            {
                var messageDialog = new MessageDialog(ex.Message);
                messageDialog.ShowAsync();
            }
        }

        //*********************************************************************
        /// <summary>
        /// Fetch elevation and image tiles for an area given zoom, size, and
        /// center coords. All tile data is stored in local cache, making this
        /// useful for prefetch for later field use. The callback is invoked
        /// once for each tile success or failure.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //*********************************************************************
        private void FetchMap_Click(object sender, RoutedEventArgs e)
        {
            _successfulfetchCount = 0;
            _failedfetchCount = 0;

            try
            {
                ReadUi();
                TileServerLib.TileCache.AppDataPath = 
                    Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path;
                TileServerLib.MapBuilder mb = 
                    new TileServerLib.MapBuilder(_bingAccessKey);
                //mb.Test1();

                mb.GetMapData(Latitude, Longitude, ZoomLevel, MapSize, 
                    (fetchStatus) => GotStatusUpdate(fetchStatus) );
            }
            catch(Exception ex)
            {
                var messageDialog = new MessageDialog(ex.Message);
                messageDialog.ShowAsync();
            }
        }

        //*********************************************************************
        /// <summary>
        /// Fetch a single elevation tile
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //*********************************************************************
        private async void FetchElevationTile_Click(object sender, RoutedEventArgs e)
        {
            string message;
            _successfulfetchCount = 0;
            _failedfetchCount = 0;

            try
            {
                ReadUi();
                TileServerLib.TileCache.AppDataPath = 
                    Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path;
                TileServerLib.MapTile mt = new TileServerLib.MapTile(_bingAccessKey);
                var resp = await mt.FetchElevationData(ZoomLevel, X, Y, 
                    (fetchStatus) => GotStatusUpdate(fetchStatus));
                message = string.Format("Success: Length: {0}", resp.Length);
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            var messageDialog = new MessageDialog(message);
            messageDialog.ShowAsync();
        }

        //*********************************************************************
        /// <summary>
        /// Fetch a single image tile
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //*********************************************************************
        private async void FetchImageTile_Click(object sender, RoutedEventArgs e)
        {
            string message;
            _successfulfetchCount = 0;
            _failedfetchCount = 0;

            try
            {
                ReadUi();
                TileServerLib.TileCache.AppDataPath = 
                    Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path;
                TileServerLib.MapTile mt = new TileServerLib.MapTile(_bingAccessKey);
                var resp = await mt.FetchImageData(ZoomLevel, X, Y, 
                    (fetchStatus) => GotStatusUpdate(fetchStatus));
                message = string.Format("Success: Length: {0}", resp.Length);
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            var messageDialog = new MessageDialog(message);
            messageDialog.ShowAsync();
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="statusUpdate"></param>
        //*********************************************************************
        private void GotStatusUpdate(TileServerLib.FetchStatus statusUpdate)
        {
            switch(statusUpdate.Result)
            {
                case TileServerLib.FetchStatus.ResultEnum.Success:
                    _successfulfetchCount++;
                    break;
                case TileServerLib.FetchStatus.ResultEnum.Failure:
                    _failedfetchCount++;
                    break;     
            }

            statusTextBlock.Text = $"Status: Success: {_successfulfetchCount}, Failure: {_failedfetchCount}";
        }

    }
}
