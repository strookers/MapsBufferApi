using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using MapsAPI.Models;
using Newtonsoft.Json;
using System.Globalization;
using System.Web.Http.Cors;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;

namespace MapsAPI.Controllers
{
    public class MapsController : ApiController
    {
        // GET: api/Maps
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        [HttpGet]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public object Search(string from, string to, int radius)
        {
            
            HttpClient webservice = new HttpClient();

            string url = "https://maps.googleapis.com/maps/api/directions/json?origin=" + from + "&destination=" + to + "&key=AIzaSyDIR_Gt9qJxpvTgCr9z-wfCVFTKPjGs_8w";

            //Debug.WriteLine(postData);

            HttpResponseMessage response = webservice.GetAsync(url).Result;

            /*  */
            GoogleDirectionClass objRoutes = JsonConvert.DeserializeObject<GoogleDirectionClass>(response.Content.ReadAsStringAsync().Result);
            if (objRoutes.routes.Count > 0)
            {
                string encodedPoints = objRoutes.routes[0].overview_polyline.points;

                List<Models.Location> lstDecodedPoints = FnDecodePolylinePoints(encodedPoints);
                //convert list of location point to array of latlng type
                LatLngNew[] latLngPoints = new LatLngNew[lstDecodedPoints.Count];
                Coordinate[] ss = new Coordinate[lstDecodedPoints.Count];
                
                int index = 0;
                foreach (Models.Location loc in lstDecodedPoints)
                {
                    latLngPoints[index] = new LatLngNew(loc.lat, loc.lng);
                    ss[index] = new Coordinate(loc.lat, loc.lng);
                    index++;
                }
                //TODO: do bd....
                double searchRadios = (double)radius / 37;

                var lineString = new LineString(ss);
                var buffer = VariableWidthBuffer.Buffer(lineString, searchRadios, searchRadios);
                var bArray = buffer.Coordinates;
                var bArrayLength = buffer.Coordinates.Length;
                double[,] pArray = new double[bArrayLength, 2];
                for (int i = 0; i < bArrayLength; i++)
                {
                    pArray[i, 0] = bArray[i].X;
                    pArray[i, 1] = bArray[i].Y;
                }
                GoogleReturnObj returnObj = new GoogleReturnObj()
                    {
                        PolyLinePoints = latLngPoints.ToList(),
                        Source = new Area() { Pos = new LatLngNew(objRoutes.routes[0].legs[0].start_location.lat, objRoutes.routes[0].legs[0].start_location.lng), Name = from },
                        Destination = new Area() { Pos = new LatLngNew(objRoutes.routes[0].legs[0].end_location.lat, objRoutes.routes[0].legs[0].end_location.lng), Name = to },
                        Points = pArray
                };

                var s = new { convert = buffer, coordinates = latLngPoints, coords = buffer.Coordinates.Select(x => new LatLngNew(x.X, x.Y)), darr = buffer.Coordinates.Select(d => new double[] { d.X, d.Y } ) };
                return returnObj;
                }
            return null;
        }

        List<Models.Location> FnDecodePolylinePoints(string encodedPoints)
        {
            if (string.IsNullOrEmpty(encodedPoints))
                return null;
            List<Models.Location> poly = new List<Models.Location>();
            char[] polylinechars = encodedPoints.ToCharArray();
            int index = 0;

            int currentLat = 0;
            int currentLng = 0;
            int next5bits;
            int sum;
            int shifter;


            while (index < polylinechars.Length)
            {
                // calculate next latitude
                sum = 0;
                shifter = 0;
                do
                {
                    next5bits = (int)polylinechars[index++] - 63;
                    sum |= (next5bits & 31) << shifter;
                    shifter += 5;
                } while (next5bits >= 32 && index < polylinechars.Length);

                if (index >= polylinechars.Length)
                    break;

                currentLat += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

                //calculate next longitude
                sum = 0;
                shifter = 0;
                do
                {
                    next5bits = (int)polylinechars[index++] - 63;
                    sum |= (next5bits & 31) << shifter;
                    shifter += 5;
                } while (next5bits >= 32 && index < polylinechars.Length);

                if (index >= polylinechars.Length && next5bits >= 32)
                    break;

                currentLng += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);
                Models.Location p = new Models.Location();
                p.lat = Convert.ToDouble(currentLat) / 100000.0;
                p.lng = Convert.ToDouble(currentLng) / 100000.0;
                poly.Add(p);
            }

            return poly;
        }
    }
}
