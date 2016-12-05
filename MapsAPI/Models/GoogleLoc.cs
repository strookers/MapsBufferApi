using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MapsAPI.Models
{
    public class GoogleReturnObj
    {
        public Area Source { get; set; }
        public Area Destination { get; set; }
        public List<LatLngNew> PolyLinePoints { get; set; }
        //public List<Camp.Camp> CampPoints { get; set; }
        public double[,] Points { get; set; }
    }
    public class LatLngNew
    {
        public LatLngNew(double lat, double lng)
        {
            this.lat = lat;
            this.lng = lng;
        }
        public double lat { get; set; }
        public double lng { get; set; }
    }
    public class Area
    {
        public LatLngNew Pos { get; set; }
        public string Name { get; set; }
    }
}