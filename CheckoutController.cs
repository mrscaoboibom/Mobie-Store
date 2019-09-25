using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MvcMobileStore.Models;//

namespace MvcMobileStore.Controllers
{
    public class CheckoutController : Controller
    {
        DataClassesDataContext db = new DataClassesDataContext();

        #region Thanh toán (AddressAndPayment)
        public ActionResult AddressAndPayment()
        {
            //Chưa đăng nhập thì không cho vào trang Thanh toán
            if (Session["Username"] == null)
                return RedirectToAction("Index", "Home");

            //Nếu giỏ hàng đang trống thì không cho thanh toán
            var _GioHang=ShoppingCart.LayGioHang(this.HttpContext);
            if (_GioHang.LaySoLuong()<=0)
            {
                return Content("<script>alert('Giỏ hàng của bạn đang trống. Không thể thanh toán!');window.location='/ShoppingCart/Index';</script>");
            }

            //Lấy ra thông tin khách hàng để add vào mục thông tin THÔNG TIN TÀI KHOẢN trên Form
            int _MaKH = int.Parse(Session["MaKH"].ToString());
            var ttkh = db.KhachHangs.SingleOrDefault(k => k.MaKH == _MaKH);
            return View(ttkh);
        }

        [HttpPost]
        public ActionResult AddressAndPayment(FormCollection collection)
        {
            var _DonHang = new DonHang();
            TryUpdateModel(_DonHang);

            try
            {
                //Gán các thông tin cho bảng Đơn Hàng để thêm mới
                _DonHang.MaKH = int.Parse(Session["MaKH"].ToString());
                _DonHang.NgayMua = DateTime.Now;
                _DonHang.NgayGiao = Convert.ToDateTime(collection["txt_NgayGiaoHang"]);
                _DonHang.TenNguoiNhan = collection["txt_HoTenNhan"];
                _DonHang.DiaChiNhan = collection["txt_DiaChiNhan"];
                _DonHang.DienThoaiNhan = collection["txt_DienThoaiNhan"];

                int HTTH = int.Parse(collection["sl_ThanhToan"]);
                if (HTTH == 0)
                    _DonHang.HTThanhToan = false;
                else
                    _DonHang.HTThanhToan = true;

                //Trị giá của đơn hàng bên hàm TaoDonHang có rồi nhưng chưa Add được nên ở đây lấy giỏ hàng về và lấy ra tổng tiền để add vào CSDL
                var _LayGioHang = ShoppingCart.LayGioHang(this.HttpContext);
                _DonHang.Trigia = _LayGioHang.LayTongTien();

                _DonHang.Dagiao = false;//Mặc định chưa giao

                //Lưu Đơn Hàng
                db.DonHangs.InsertOnSubmit(_DonHang);
                db.SubmitChanges();

                //Xử lý Chi tiết đặt hàng
                var _GioHang = ShoppingCart.LayGioHang(this.HttpContext);
                _GioHang.TaoDonHang(_DonHang);//Gọi hàm tạo đơn hàng về

                return RedirectToAction("Complete", new { id = _DonHang.MaDH });
            }
            catch
            {
                return View(_DonHang);
            }
        }
        #endregion

        #region Hoàn thành (Complete)
        public ActionResult Complete(int id)
        {
            //Xác nhận khách hàng đặt hàng
            bool _HopLe = db.DonHangs.Any(d => d.MaDH == id && d.MaKH == int.Parse(Session["MaKH"].ToString()));

            if (_HopLe)
            {
                return View(id);
            }
            else
            {
                return View("Error");//View  lỗi  đã  được  tự  động  tạo  ra  cho  chúng  ta  trong  thư  mục /Views/Shared/_Error.cshtml khi chúng ta bắt đầu dự án.
            }
        }
        #endregion
    }
}
