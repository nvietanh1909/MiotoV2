using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Mioto.Models
{
    public class MD_InfoCar
    {
        public Xe Xe { get; set; }
        public List<KhachHang> KhachHang { get; set; }
        public KhachHang ChuXe { get; set; }
        public DonThueXe DonThueXe { get; set; }
        public List<DanhGia> DanhGia { get; set; }
        public bool Check { get; set; }
    }
}