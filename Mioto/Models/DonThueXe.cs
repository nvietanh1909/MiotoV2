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
    
    public partial class DonThueXe
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public DonThueXe()
        {
            this.DanhGia = new HashSet<DanhGia>();
            this.PhiHuyChuyen = new HashSet<PhiHuyChuyen>();
        }
    
        public int IDTX { get; set; }
        public System.DateTime NgayThue { get; set; }
        public System.DateTime NgayTra { get; set; }
        public int TrangThaiThanhToan { get; set; }
        public decimal TongTien { get; set; }
        public decimal PhanTramHoaHong { get; set; }
        public int IDKH { get; set; }
        public Nullable<int> IDMGG { get; set; }
        public string BienSo { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<DanhGia> DanhGia { get; set; }
        public virtual Xe Xe { get; set; }
        public virtual KhachHang KhachHang { get; set; }
        public virtual MaGiamGia MaGiamGia { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PhiHuyChuyen> PhiHuyChuyen { get; set; }
    }
}
