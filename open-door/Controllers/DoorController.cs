using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Linq;
using System.Web.Http;
using System.Threading.Tasks;
using open_door.Models;
using System.Net;

namespace open_door.Controllers
{
    [RoutePrefix("door")]
    public class DoorController : ApiController
    {
        private Models.doorAccessEntities cnx;
        private Regex rgx;
        private double doorLatitude = 0.0;
        private double doorLongitude = 0.0;
        private int deviceMaxDistance = 0;
        private int srvMaxTimespan = 0;

        public DoorController()
        {
            rgx = new Regex(@"^\w[\w.-]+@nearshoretechnology.com$");
            try
            {
                doorLatitude = Convert.ToDouble(System.Configuration.ConfigurationManager.AppSettings["doorLongitude"]);
                doorLongitude = Convert.ToDouble(System.Configuration.ConfigurationManager.AppSettings["doorLatitude"]);
            }
            catch (FormatException fe)
            {
                doorLatitude = 19.058195;
                doorLongitude = -98.229781;
            }
            try
            {
                deviceMaxDistance = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["deviceMaxDistance"]);
            }
            catch (FormatException fe)
            {
                deviceMaxDistance = 15;
            }
            try
            {
                srvMaxTimespan = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["srvMaxTimespan"]);
            }
            catch (FormatException fe)
            {
                deviceMaxDistance = 2;
            }
            
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("login/{email}/{token}")]
        public IHttpActionResult Login(String email, String token)
        {
            cnx = new doorAccessEntities();

            try
            {

                // Validate email format
                if (!rgx.IsMatch(email))
                {
                    return Ok(new { response = "Bad email", number = 101, detail = "The email you provided does not meet our validation criteria", profile = "" });
                }
                // Makes sure the token is not empty
                if (token == "")
                {
                    return Ok(new { response = "Empty token", number = 102, detail = "The token provided can not be empty", profile = "" });
                }

                // Makes sure the toke is not undefined by the token gen. service
                if (token == "undefined")
                {
                    return Ok(new { response = "Undefined token", number = 103, detail = "There was an error registering the token, try again", profile = "" });
                }

                var x = (from u in cnx.Users
                         where u.email.Equals(email)
                         select u
                         ).FirstOrDefault();

                // Makes sure the user is found in the database
                if (x == null)
                {
                    return Ok(new { response = "User not found", number = 104, detail = "The user was not found in our records", profile = "" });
                }

                // Makes sure the user found is active
                if (!x.is_active)
                {
                    return Ok(new { response = "Inactive User", number = 105, detail = "The user was disabled", profile = "" });
                }

                // Makes sure the token is empty (another device is not linked to the user's records already)
                if (!string.IsNullOrEmpty(x.token))
                {
                    return Ok(new { response = "User has Token", number = 106, detail = "User is already attached to another device, contact your manager to reset your device preferences", profile = "" });
                }

                // if everything is ok, the token and date of initial sign up are set.
                x.token = token;
                x.signup_date = DateTime.Now;
                cnx.SaveChanges();

                return Ok(new { response = "Success", number = 0, detail = "The user was successfully signed up", profile = x.ProfileType.name });
            }catch( Exception ex )
            {
                return Content( HttpStatusCode.BadRequest, ex.Message );
            }
            finally
            {
                cnx.Dispose();
            }
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("open/{email}/{token}/{latitude}/{longitude}")]
        public IHttpActionResult Open(string email, string token, double latitude, double longitude)
        {
            cnx = new doorAccessEntities();

            try
            {
                // check that email matches the format
                if (!rgx.IsMatch(email))
                {
                    return Ok(new { response = "Bad email", number = 101, detail = "The email you provided does not meet our validation criteria", profile = "" });
                }

                var x = (from u in cnx.Users
                         where u.email.Equals(email)
                         select u
                         ).FirstOrDefault();

                // Makes sure the user is found in the database
                if (x == null)
                {
                    return Ok(new { response = "User not found", number = 104, detail = "The user was not found in our records", profile = "" });
                }

                // Makes sure the token is not empty (another device is not linked to the user's records already)
                if (string.IsNullOrEmpty(x.token))
                {
                    return Ok(new { response = "User has no Token", number = 106, detail = "User is not registered", profile = "" });
                }

                // Initialize the access to the 
                Models.Access a = new Access();
                a.access_date = DateTime.Now.Date;
                a.access_time = DateTime.Now.TimeOfDay;
                a.user_id = x.Id;
                a.served = false;

                // Makes sure the user found is active
                if (!x.is_active)
                {
                    a.status = 104;
                    a.descripcion = "Inactive User";
                    cnx.Accesses.Add(a);
                    cnx.SaveChanges();
                    return Ok(new { response = "Inactive User", number = 105, detail = "The user is unable to perform this action", profile = "" });
                }

                // Makes sure the user's token match
                if (!x.token.Equals(token))
                {
                    a.status = 107;
                    a.descripcion = "Bad token";
                    cnx.Accesses.Add(a);
                    cnx.SaveChanges();
                    return Ok(new { response = "Bad token", number = 107, detail = "This user's device is not registered", profile = "" });
                }
                // Initialize the GeoCoordinates objects (device and door) appConfig[doorLatitude], appConfig[doorLongitude]
                System.Device.Location.GeoCoordinate doorCoordinates = new System.Device.Location.GeoCoordinate(doorLatitude, doorLongitude);
                System.Device.Location.GeoCoordinate deviceCoordinates = new System.Device.Location.GeoCoordinate(latitude, longitude);

                // If distance between door and device is greater than appConfig[deviceMaxDistance] meters the device is out of range and is not able to open the door
                if (doorCoordinates.GetDistanceTo(deviceCoordinates) > deviceMaxDistance)
                {
                    a.status = 108;
                    a.descripcion = "Device not in range (" + latitude + ", " + longitude + ")";
                    cnx.Accesses.Add(a);
                    cnx.SaveChanges();
                    return Ok(new { response = "Device not in range", number = 108, detail = "The device is out of the permit range to perform this action", profile = x.ProfileType.name });
                }

                a.status = 0;
                a.descripcion = "Success (" + latitude + ", " + longitude + ")";
                
                //Tries to open de door using the Uri for the door set in web.config
                string doorUri = System.Configuration.ConfigurationManager.AppSettings["doorUri"];
                Task<string> request = GetAsync(doorUri);
                string response = request.Result;

                if(response == "{}")
                {
                    return Ok(new { response = "Success", number = 0, detail = "The door is open", profile = x.ProfileType.name });
                    a.served = true;
                }
                else
                {
                    return Ok(new { response = "Door Connection Error", number = 0, detail = "The door is open", profile = x.ProfileType.name });
                    a.served = false;
                }
                cnx.Accesses.Add(a);
                cnx.SaveChanges();
            }
            catch ( Exception ex )
            {
                return Content(HttpStatusCode.BadRequest, ex.Message);
            }
            finally
            {
                cnx.Dispose();
            }
        }
        /*
        [AllowAnonymous]
        [HttpGet]
        [Route("serve")]
        public IHttpActionResult Serve()
        {
            cnx = new doorAccessEntities();

            try
            {
                DateTime maxRng = DateTime.Now;
                //AppConfig[srvMaxTimespan]
                DateTime minRng = maxRng.AddMinutes( -1*srvMaxTimespan );
                string resp = "";
                string cipher = "";

                var x = (from a in cnx.Accesses
                         where !a.served && a.status.Equals(0) && (a.access_date <= maxRng.Date && a.access_time <= maxRng.TimeOfDay)
                         && (a.access_date >= minRng.Date && a.access_time >= minRng.TimeOfDay)
                         select a).FirstOrDefault();

                if (x != null)
                {
                    x.served = true;
                    cnx.SaveChanges();
                    resp = "1-" + maxRng.Year.ToString() + (maxRng.Month < 10 ? "0" : "") + maxRng.Month.ToString() + (maxRng.Day < 10 ? "0" : "") + maxRng.Day.ToString() + (maxRng.Hour < 10 ? "0" : "") + maxRng.Hour.ToString() + (maxRng.Minute < 10 ? "0" : "") + maxRng.Minute.ToString() + (maxRng.Second < 10 ? "0" : "") + maxRng.Second.ToString();
                    cipher = Encrypt(resp);
                    //  positive response
                    return Ok(new { response = cipher });
                }
                resp = "0-" + maxRng.Year.ToString() + (maxRng.Month < 10 ? "0" : "") + maxRng.Month.ToString() + (maxRng.Day < 10 ? "0" : "") + maxRng.Day.ToString() + (maxRng.Hour < 10 ? "0" : "") + maxRng.Hour.ToString() + (maxRng.Minute < 10 ? "0" : "") + maxRng.Minute.ToString() + (maxRng.Second < 10 ? "0" : "") + maxRng.Second.ToString();
                cipher = Encrypt(resp);

                //  negative response
                return Ok(new { response = cipher });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.BadRequest, ex.Message);
            }
            finally
            {
                cnx.Dispose();
            }

        }

        private static string Encrypt(string message)
        {
            byte[] result;

            using (AesCryptoServiceProvider provider = new AesCryptoServiceProvider() )
            {
                if (provider.IV == null || provider.IV.Length <= 0)
                {
                    provider.GenerateIV();
                }

                provider.Key = SHA256.Create().ComputeHash( Encoding.UTF8.GetBytes( "N57D00R5417" ) );
                ICryptoTransform crypto = provider.CreateEncryptor( provider.Key, provider.IV );

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using ( CryptoStream csEncrypt = new CryptoStream( msEncrypt, crypto, CryptoStreamMode.Write ) )
                    {
                        using ( StreamWriter swEncrypt = new StreamWriter(csEncrypt) )
                        {
                            swEncrypt.Write( message );
                        }
                        result = new byte[ provider.IV.Length + msEncrypt.ToArray().Length ];
                        System.Buffer.BlockCopy(provider.IV, 0, result, 0, provider.IV.Length);
                        System.Buffer.BlockCopy(msEncrypt.ToArray(), 0, result, provider.IV.Length, msEncrypt.ToArray().Length);
                    }
                }
                return Encoding.Default.GetString( result );
            }
        }
        */
        public static async Task<string> GetAsync( string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }
}
