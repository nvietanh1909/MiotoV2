//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Mioto.Controllers
{
    using System;
    using System.Collections.Generic;
    
    public partial class DanhGia
    {
        public int IDDG { get; set; }
        public int IDKH { get; set; }
        public string BienSoXe { get; set; }
        public string NoiDung { get; set; }
        public int SoSao { get; set; }
        public System.DateTime NgayDanhGia { get; set; }
    
        public virtual Xe Xe { get; set; }
        public virtual KhachHang KhachHang { get; set; }
    }
}