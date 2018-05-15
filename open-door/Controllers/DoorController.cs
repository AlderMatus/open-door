using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Device;
using open_door.Models;

namespace open_door.Controllers
{
    [RoutePrefix("door")]
    public class DoorController : ApiController
    {
        private Models.doorAccessEntities cnx = new doorAccessEntities();
        private Regex rgx = new Regex(@"^\w[\w.-]+@nearshoretechnology.com$");

        [AllowAnonymous]
        [HttpGet]
        [Route("login/{email}/{token}")]
        public IHttpActionResult Login(String email, String token)
        {
            // Validate email format

            if (!rgx.IsMatch(email))
            {
                return Ok(new { response = "Bad email", number = 1, detail = "The email you provided does not meet our validation criteria", profile = "" });
            }

            // Makes sure the toke is not empty
            if (token == "")
            {
                return Ok(new { response = "Empty token", number = 1, detail = "The token provided can not be empty", profile = "" });
            }

            var x = (from u in cnx.Users
                     where u.email.Equals(email)
                     select u
                     ).FirstOrDefault();

            // Makes sure the user is found in the database
            if (x == null)
            {
                return Ok(new { response = "User not found", number = 1, detail = "The user was not found in our records", profile = "" });
            }

            // Makes sure the user found is active
            if (!x.is_active)
            {
                return Ok(new { response = "Inactive User", number = 1, detail = "The user was disabled", profile = "" });
            }

            // Makes sure the token is empty (another device is not linked to the user's records already)
            if (!string.IsNullOrEmpty(x.token))
            {
                return Ok(new { response = "User has Token", number = 1, detail = "User is already attached to another device, contact your manager to reset your device preferences", profile = "" });
            }

            // if everything is ok, the token and date of initial sign up are set.
            x.token = token;
            x.signup_date = DateTime.Now;
            cnx.SaveChanges();

            return Ok(new { response = "Success", number = 1, detail = "The user was successfully signed up", profile = x.ProfileType.name });
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("open/{email}/{token}/{latitude}/{longitude}")]
        public IHttpActionResult Open(string email, string token, double latitude, double longitude)
        {
            // check that email matches the format
            if (!rgx.IsMatch(email))
            {
                return Ok(new { response = "Bad email", number = 2, detail = "The email you provided does not meet our validation criteria", profile = "" });
            }

            var x = (from u in cnx.Users
                     where u.email.Equals(email)
                     select u
                     ).FirstOrDefault();

            // Makes sure the user is found in the database
            if (x == null)
            {
                return Ok(new { response = "User not found", number = 1, detail = "The user was not found in our records", profile = "" });
            }

            // Makes sure the token is not empty (another device is not linked to the user's records already)
            if ( string.IsNullOrEmpty( x.token ) )
            {
                return Ok(new { response = "User has no Token", number = 1, detail = "User is not registered", profile = "" });
            }

            // Initialize the access to the 
            Models.Access a = new Access();
            a.access_date = DateTime.Now.Date;
            a.access_time = DateTime.Now.TimeOfDay;
            a.user_id = x.Id;
            a.served = false;

            // Makes sure the user found is active
            if ( !x.is_active )
            {
                a.status = 0;
                a.descripcion = "Inactive User";
                cnx.Accesses.Add(a);
                cnx.SaveChanges();
                return Ok(new { response = "Inactive User", number = 1, detail = "The user is unable to perform this action", profile = "" });
            }

            // Makes sure the user's token match
            if (!x.token.Equals(token))
            {
                a.status = 0;
                a.descripcion = "Bad token";
                cnx.Accesses.Add(a);
                cnx.SaveChanges();
                return Ok(new { response = "Bad token", number = 1, detail = "This user's device is not registered", profile = "" });
            }
            // Initialize the GeoCoordinates objects (device and door)
            System.Device.Location.GeoCoordinate doorCoordinates = new System.Device.Location.GeoCoordinate(19.058195, -98.229781);
            System.Device.Location.GeoCoordinate deviceCoordinates = new System.Device.Location.GeoCoordinate(latitude, longitude);

            // If distance between door and device is greater than maxDistance meters the device is out of range and is not able to open the door
            if ( doorCoordinates.GetDistanceTo(deviceCoordinates) > 15 )
            {
                a.status = 0;
                a.descripcion = "Device not in range";
                cnx.Accesses.Add(a);
                cnx.SaveChanges();
                return Ok(new { response = "Device not in range", number = 2, detail = "The device is out of the permit range to perform this action", profile = x.ProfileType.name });
            }

            a.status = 1;
            a.descripcion = "Success";
            cnx.Accesses.Add(a);
            cnx.SaveChanges();
            
            return Ok(new { response = "Success", number = 2, detail = "The door is open", profile = x.ProfileType.name });
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("serve")]
        public IHttpActionResult Serve()
        {
            DateTime maxRng = DateTime.Now;
            DateTime minRng = maxRng.AddMinutes(-2);
            string resp = "";
            string cipher = "";

            var x = ( from a in cnx.Accesses
                     where !a.served && a.status.Equals(1) && (a.access_date <= maxRng.Date && a.access_time <= maxRng.TimeOfDay)
                     && (a.access_date >= minRng.Date && a.access_time >= minRng.TimeOfDay)
                     select a ).FirstOrDefault();

            if ( x != null )
            {
                x.served = true;
                cnx.SaveChanges();
                resp = "1-" + maxRng.Year.ToString() + (maxRng.Month < 10? "0" : "") + maxRng.Month.ToString() + (maxRng.Day < 10 ? "0" : "") + maxRng.Day.ToString() + (maxRng.Hour < 10 ? "0" : "") + maxRng.Hour.ToString() + (maxRng.Minute < 10 ? "0" : "") + maxRng.Minute.ToString() + (maxRng.Second < 10 ? "0" : "") + maxRng.Second.ToString();
                cipher = Encrypt(resp);
                //  positive response
                return Ok(new { response = cipher });
            }
            resp = "0-" + maxRng.Year.ToString() + (maxRng.Month < 10 ? "0" : "") + maxRng.Month.ToString() + (maxRng.Day < 10 ? "0" : "") + maxRng.Day.ToString() + (maxRng.Hour < 10 ? "0" : "") + maxRng.Hour.ToString() + (maxRng.Minute < 10 ? "0" : "") + maxRng.Minute.ToString() + (maxRng.Second < 10 ? "0" : "") + maxRng.Second.ToString();
            cipher = Encrypt(resp);

            //  negative response
            return Ok(new { response = cipher });
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
    }
}
