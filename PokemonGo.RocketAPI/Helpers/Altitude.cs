using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PokemonGo.RocketAPI.Logic.Utils
{

    public class AltitudeResults
    {
        public Result[] results { get; set; }
        public string status { get; set; }
    }

    public class Result
    {
        public float elevation { get; set; }
        public Location location { get; set; }
        public float resolution { get; set; }
    }

    public class Location
    {
        public float lat { get; set; }
        public float lng { get; set; }
    }

    class Altitude
    {
        public static double GetAltitude(double latitude, double longitude)
        {
            try
            {
                var altitudeRequest =
                    WebRequest.Create(
                        $"https://maps.googleapis.com/maps/api/elevation/json?locations={latitude},{longitude}");
                var altitudeResponse = altitudeRequest.GetResponse().GetResponseStream();
                var jsonResponse = new StreamReader(altitudeResponse).ReadToEnd();
                var altitude = JsonConvert.DeserializeObject<AltitudeResults>(jsonResponse);
                return altitude.results.First().elevation;
            }
            catch (Exception)
            {
                return 10.0;
            }

        }
    }
}
