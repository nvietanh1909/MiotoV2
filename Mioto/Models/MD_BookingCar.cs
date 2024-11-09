using System;
using System.ComponentModel.DataAnnotations;

namespace Mioto.Models
{
    public class MD_BookingCar
    {
        [Required(ErrorMessage = "Ngày thuê là bắt buộc")]
        [DataType(DataType.Date)]
        [CustomValidation(typeof(MD_BookingCar), "ValidateNgayThue")]
        public DateTime NgayThue { get; set; }

        [Required(ErrorMessage = "Ngày trả là bắt buộc")]
        [DataType(DataType.Date)]
        [CustomValidation(typeof(MD_BookingCar), "ValidateNgayTra")]
        public DateTime NgayTra { get; set; }

        public static ValidationResult ValidateNgayThue(DateTime ngayThue, ValidationContext context)
        {
            return ngayThue < DateTime.Today
                ? new ValidationResult("Ngày thuê phải lớn hơn hoặc bằng ngày hiện tại")
                : ValidationResult.Success;
        }

        public static ValidationResult ValidateNgayTra(DateTime ngayTra, ValidationContext context)
        {
            var instance = context.ObjectInstance as MD_BookingCar;
            return instance != null && ngayTra <= instance.NgayThue
                ? new ValidationResult("Ngày trả phải sau ngày thuê")
                : ValidationResult.Success;
        }

        public Xe Xe { get; set; }
        public KhachHang KhachHang { get; set; }
        public MaGiamGia MaGiamGia { get; set; }
    }
}
