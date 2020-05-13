using BingMapsRESTToolkit;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;

// https://docs.microsoft.com/en-us/bingmaps/rest-services/using-the-rest-services-with-net 
// https://github.com/Microsoft/BingMapsRESTToolkit/

namespace TileServerLib
{
    /// <summary>
    /// 
    /// </summary>
    public class GeoDataClient
    {
        public const string UserAgentText =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/81.0.4044.138 Safari/537.36";
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*********************************************************************
    public class OpenStreetMapClient : GeoDataClient
    {
        private static readonly string[] TilePathPrefixes = { "a", "b", "c" };

        private const string _imageReqUrlTemplate =
            "http://{0}.tile.openstreetmap.org/{1}/{2}/{3}.png";

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="zoom"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        //*********************************************************************
        public async Task<byte[]> FetchImageTile(int zoom, int x, int y)
        {
            string url= string.Format(_imageReqUrlTemplate,
                   TilePathPrefixes[Mathf.Abs(x) % 3], zoom, x, y);

            using (WebClient client = new WebClient())
            {
                client.Headers.Add("user-agent", UserAgentText);
                return await client.DownloadDataTaskAsync(url);
            }
        }
    }

    //*********************************************************************
    /// <summary>
    /// 
    /// </summary>
    //*********************************************************************
    public class BingClient : GeoDataClient
    {
        private const string _elevationBoundsReqUrlTemplate =
            "http://dev.virtualearth.net/REST/v1/Elevation/Bounds?bounds={0},{1},{2},{3}&rows=11&cols=11&key={4}";

        private static DateTime _lastThrottlePass = DateTime.Now;
        private static TimeSpan _BlockTimeSpan = new TimeSpan(0, 0, 0, 0, 200);
        string _accessKey;

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accessKey"></param>
        //*********************************************************************
        public BingClient(string accessKey)
        {
            _accessKey = accessKey;
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        //*********************************************************************
        private Response DeserializeResponse(OpenReadCompletedEventArgs a, out string body)
        {
            body = null;

            if (null != a.Error)
            {
                Response resp = new Response
                {
                    StatusDescription = "ERROR",
                    ErrorDetails = new string[1]
                };
                resp.ErrorDetails[0] = a.Error.Message;

                return resp;
             }
                        
            try
            {
                body = ExtractBody(a);
                return JsonConvert.DeserializeObject<Response>(body);
            }
            catch (Exception ex)
            {
                Response resp = new Response
                {
                    StatusDescription = "EXCEPTION",
                    ErrorDetails = new string[1]
                };
                resp.ErrorDetails[0] = ex.Message;

                return resp;
            }
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        //*********************************************************************
        private string ExtractBody(OpenReadCompletedEventArgs a)
        {
            Stream reply = null;
            StreamReader s = null;
            string body = null;

            try
            {
                reply = (Stream)a.Result;
                s = new StreamReader(reply);
                body = s.ReadToEnd();
                return body;
            }
            finally
            {
                if (s != null)
                {
                    s.Close();
                }

                if (reply != null)
                {
                    reply.Close();
                }
            }
        }

        //*********************************************************************
        /// <summary>
        /// the Bing URL can only be called 5 times per second
        /// https://social.msdn.microsoft.com/Forums/en-US/3e8b767d-36ee-44bf-92f1-ccb94e20779c/too-many-requests-error-started-on-21617
        /// </summary>
        /// <returns></returns>
        //*********************************************************************
        private static bool ThrottleBlock()
        {
            while(DateTime.Now - _lastThrottlePass < _BlockTimeSpan )
            {
                Thread.Sleep(_BlockTimeSpan - (DateTime.Now - _lastThrottlePass));
            }
            return true;
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="callback"></param>
        //*********************************************************************
        private void GetResponse(Uri uri, Action<Response,string> callback)
        {
            ThrottleBlock();

            WebClient wc = new WebClient();
            wc.OpenReadCompleted += (o, a) =>
            {
                var dserResp = DeserializeResponse(a, out string bodyContents);
                callback?.Invoke(dserResp, bodyContents);
            };
            wc.OpenReadAsync(uri);
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="data"></param>
        /// <param name="callback"></param>
        //*********************************************************************
        private void GetPOSTResponse(Uri uri, string data, Action<Response> callback)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);

            request.Method = "POST";
            request.ContentType = "text/plain;charset=utf-8";

            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            byte[] bytes = encoding.GetBytes(data);

            request.ContentLength = bytes.Length;

            using (Stream requestStream = request.GetRequestStream())
            {
                // Send the data.  
                requestStream.Write(bytes, 0, bytes.Length);
            }

            request.BeginGetResponse((x) =>
            {
                using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(x))
                {
                    if (callback != null)
                    {
                        DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Response));
                        callback(ser.ReadObject(response.GetResponseStream()) as Response);
                    }
                }
            }, null);
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        //*********************************************************************
        public void FetchByAddress(string address)
        {
            string query = "1 Microsoft Way, Redmond, WA";

            Uri geocodeRequest = new Uri(
                string.Format("http://dev.virtualearth.net/REST/v1/Locations?q={0}&key={1}",
                address, _accessKey));

            GetResponse(geocodeRequest, (x, body) =>
            {
                Console.WriteLine(x.ResourceSets[0].Resources.Length + " result(s) found.");
                Console.ReadLine();
            });
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="northEast"></param>
        /// <param name="southWest"></param>
        //*********************************************************************
        public void FetchByCoords(WorldCoordinate northEast, 
            WorldCoordinate southWest, Action<Response,string> callback)
        {
            //FetchByCoords(southWest.Lat, southWest.Lon, northEast.Lat, northEast.Lon, callback );

            FetchByCoords(northEast.Lat, northEast.Lon,
                southWest.Lat, southWest.Lon, callback);
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="northEastLat"></param>
        /// <param name="northEastLon"></param>
        /// <param name="southWestLat"></param>
        /// <param name="southWestLon"></param>
        //*********************************************************************
        public void FetchByCoords(float northEastLat, float northEastLon, 
            float southWestLat, float southWestLon, Action<Response,string> callback)
        {
            Uri req = new Uri(
                string.Format(_elevationBoundsReqUrlTemplate,
                southWestLat, southWestLon, northEastLat, northEastLon, _accessKey));

            GetResponse(req, callback);

            //GetResponse(req, (x,body) =>
            //{
            //    callback?.Invoke(x,body);
            //});
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="northEast"></param>
        /// <param name="southWest"></param>
        /// <returns></returns>
        //*********************************************************************
        public async Task<string> FetchElevationTile(
            WorldCoordinate northEast, WorldCoordinate southWest)
        {
            return await FetchElevationTile(northEast.Lat, northEast.Lon,
                southWest.Lat, southWest.Lon);
        }

        //*********************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="northEastLat"></param>
        /// <param name="northEastLon"></param>
        /// <param name="southWestLat"></param>
        /// <param name="southWestLon"></param>
        /// <returns></returns>
        //*********************************************************************
        public async Task<string> FetchElevationTile(float northEastLat, 
            float northEastLon, float southWestLat, float southWestLon)
        {

            Uri req = new Uri(
                string.Format(_elevationBoundsReqUrlTemplate,
                southWestLat, southWestLon, northEastLat, northEastLon, _accessKey));

            using (WebClient client = new WebClient())
            {
                client.Headers.Add("user-agent", UserAgentText);
                return await client.DownloadStringTaskAsync(req);
            }
        }
    }
}
