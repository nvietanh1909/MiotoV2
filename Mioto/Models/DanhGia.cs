//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Mioto.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class DanhGia
    {
        public int IDDG { get; set; }
        public string NoiDung { get; set; }
        public System.DateTime Ngay { get; set; }
        public int IDDT { get; set; }
    
        public virtual DonThueXe DonThueXe { get; set; }
    }
}
