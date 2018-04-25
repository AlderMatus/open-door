using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using open_door.Models;

namespace open_door.Controllers
{
    [RoutePrefix("door")]
    public class DoorController : ApiController
    {
        Models.doorAccessEntities cnx = new doorAccessEntities();
        
        [AllowAnonymous]
        [HttpGet]
        [Route("login/{email}/{token}")]
        public IHttpActionResult login(String email, String token)
        {
            // Validate email format
            Regex rgx = new Regex( @"^\w[\w.-]+@nearshoretechnology.com$" );

            if ( !rgx.IsMatch( email ) )
            {
                return Ok( new { response = "Bad email", number = 1, detail = "The email you provided does not meet our validation criteria", profile = "" });
            }

            // Makes sure the toke is not empty
            if(token == "")
            {
                return Ok( new { response = "Empty token", number = 1, detail = "The token provided can not be empty", profile = "" });
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
    }
}
