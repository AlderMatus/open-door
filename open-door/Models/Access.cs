//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace open_door.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Access
    {
        public int Id { get; set; }
        public int user_id { get; set; }
        public byte status { get; set; }
        public string descripcion { get; set; }
        public System.DateTime access_date { get; set; }
        public System.TimeSpan access_time { get; set; }
        public bool served { get; set; }
    
        public virtual User User { get; set; }
    }
}
