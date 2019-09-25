using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MvcMobileStore.Models;//
using PagedList;//Phân trang
using System.IO;//Upload File

namespace MvcMobileStore.Controllers
{
    public class AdminController : Controller
    {
        //Kết nối CSDL
        DataClassesDataContext db = new DataClassesDataContext();

        #region Trang chủ (Index)
        public ActionResult Index()
        {
            //Chưa đăng nhập => Login
            if (Session["Username_Admin"] == null)
                return RedirectToAction("Login");

            return View();
        }
        #endregion

        #region Tài khoản (Login - Logout - Account - ChangePassword)
        #region Đăng nhập (Login)
        [HttpGet]
        public ActionResult Login()
        {
            //Đã đăng nhập => Index
            if (Session["Username_Admin"] != null)
                return RedirectToAction("Index");

            return View();
        }

        [HttpPost]
        public ActionResult Login(FormCollection collection)
        {
            try
            {
                //Lấy giá trị ở Form Login
                string _Username = collection["txt_Username"];
                string _Password = collection["txt_Password"];

                //Tạo biến _UserLogin để kiểm tra tài khoản đăng nhập có trong CSDL không
                var _UserLogin = db.Admins.SingleOrDefault(a => a.Username == _Username && a.Password == _Password);
                if (ModelState.IsValid && _UserLogin != null)
                {
                    if (_UserLogin.AnHien == true)//không bị lock tài khoản
                    {
                        //Lưu các thông tin vào Session
                        Session.Add("MaAdmin", _UserLogin.MaAdmin);
                        Session.Add("Username_Admin", _Username);
                        Session.Add("HoTen_Admin", _UserLogin.HoTen);
                        Session.Add("Avatar_Admin", _UserLogin.Avatar);

                        //Lấy ra thông tin phân quyền của tài khoản vừa Login và vào Session
                        var _LayPQ = db.PhanQuyen_Admins.SingleOrDefault(p => p.MaAdmin == int.Parse(Session["MaAdmin"].ToString()));
                        Session.Add("PQ_Menu", _LayPQ.PQ_Menu);
                        Session.Add("PQ_Slider", _LayPQ.PQ_Slider);
                        Session.Add("PQ_NhaSanXuat", _LayPQ.PQ_NhaSanXuat);
                        Session.Add("PQ_SanPham", _LayPQ.PQ_SanPham);
                        Session.Add("PQ_KhachHang", _LayPQ.PQ_KhachHang);
                        Session.Add("PQ_DonHang", _LayPQ.PQ_DonHang);
                        Session.Add("PQ_TinTuc", _LayPQ.PQ_TinTuc);
                        Session.Add("PQ_QuangCao", _LayPQ.PQ_QuangCao);
                        Session.Add("PQ_LienHe", _LayPQ.PQ_LienHe);
                        Session.Add("PQ_GioiThieu", _LayPQ.PQ_GioiThieu);
                        Session.Add("PQ_SiteMap", _LayPQ.PQ_SiteMap);
                        Session.Add("PQ_QuanTriAdmin", _LayPQ.PQ_QuanTriAdmin);

                        //Chuyển đến trang Index
                        return RedirectToAction("Index");
                    }
                    else
                        return Content("<script>alert('Tài khoản quản trị của bạn đã bị khóa!');window.location='/Admin/Login';</script>");
                }
                else
                    return Content("<script>alert('Tên đăng nhập hoặc mật khẩu không đúng!');window.location='/Admin/Login';</script>");
            }
            catch
            {
                return Content("<script>alert('Đăng nhập thất bại!');window.location='/Admin/Login';</script>");
            }


        }
        #endregion

        #region Đăng xuất (Logout)
        public ActionResult Logout()
        {
            Session.RemoveAll();
            Session.Abandon();
            return RedirectToAction("Login");
        }
        #endregion

        #region Thông tin cá nhân (Account)
        public ActionResult Account()
        {
            //Chưa đăng nhập => Login
            if (Session["Username_Admin"] == null)
                return RedirectToAction("Login");

            int _MaAdmin = int.Parse(Session["MaAdmin"].ToString());
            var ttad = db.Admins.SingleOrDefault(a => a.MaAdmin == _MaAdmin);
            return View(ttad);
        }

        [HttpPost]
        public ActionResult Account(FormCollection collection)
        {
            try
            {
                //Lấy giá trị ở Form Account                
                string _Email = collection["txt_Email"];
                string _HoTen = collection["txt_HoTen"];
                string _DiaChi = collection["txt_DiaChi"];
                string _DienThoai = collection["txt_DienThoai"];
                string _Cmnd = collection["txt_Cmnd"];
                string _NgaySinh = collection["txt_NgaySinh"];
                int _GioiTinh = int.Parse(collection["sl_GioiTinh"]);

                int _MaAdmin = int.Parse(Session["MaAdmin"].ToString());
                var ttad = db.Admins.SingleOrDefault(a => a.MaAdmin == _MaAdmin);

                //Gán giá trị để chỉnh sửa
                ttad.Email = _Email;
                ttad.HoTen = _HoTen;
                ttad.DiaChi = _DiaChi;
                ttad.DienThoai = _DienThoai;
                ttad.CMND = _Cmnd;
                ttad.NgaySinh = Convert.ToDateTime(_NgaySinh);

                if (_GioiTinh == 0)//Ở đây không thể Convert sang kiểu Bool nên gán điều kiện
                    ttad.GioiTinh = false;
                else
                    ttad.GioiTinh = true;

                //Khai báo _FileUpload ở <input type="file" id="_FileUpload" name="_FileUpload" /> trên Form Account
                HttpPostedFileBase _FileUpload = Request.Files["_FileUpload"];
                if (_FileUpload != null && _FileUpload.ContentLength > 0)//Kiểm tra đã chọn 1 file Upload để thực hiện tiếp
                {
                    //khai báo biến _FileName là tên File
                    string _FileName = Path.GetFileName(_FileUpload.FileName);

                    //Khai báo biến _Path là đường dẫn Upload File
                    string _Path = Path.Combine(Server.MapPath("~/Content/Images/Upload/"), _FileName);

                    //Kiểm tra chỉ cho Upload File có kính thước < 1 MB
                    if (_FileUpload.ContentLength > 1 * 1024 * 1024)
                    {
                        return Content("<script>alert('Kích thước của tập tin không được vượt quá 1 MB!');window.location='/Admin/Account';</script>");
                    }

                    //Ngoài hạn chế dung lượng File Upload lên Server thì quan trọng nhất là chỉ cho phép User Upload được dạng File ảnh lên
                    //Vì nếu cho Upload được tất cả các File thì User có thể Upload File Backdoor, Shell lên Server dẫn đến Site bị hacker tấn công

                    //Khai báo mảng chứa các đuôi file hợp lệ cho Upload
                    var _DuoiFile = new[] { "jpg", "jpeg", "png", "gif" };

                    //Khai báo biến _FileExt: trong đó GetExtension là lấy phần mở rộng (đuôi File), Substring(1): lấy từ vị trí thứ nhất => Tức sẽ lấy ra đuôi File
                    var _FileExt = Path.GetExtension(_FileUpload.FileName).Substring(1);

                    //Kiểm tra trong mảng _DuoiFile KHÔNG chứa phần đuôi file của tập tin User upload lên
                    if (!_DuoiFile.Contains(_FileExt))
                    {
                        return Content("<script>alert('Bảo mật Website! Chỉ được Upload tập tin hình ảnh dạng (.jpg, .jpeg, .png, .gif)!');window.location='/Admin/Account';</script>");
                    }

                    //Thực thi Upload tập tin lên Server
                    _FileUpload.SaveAs(_Path);

                    //Gán giá trị Avatar là đường dẫn của tập tin vừa Upload để Update trong Database
                    ttad.Avatar = "/Content/Images/Upload/" + _FileName;
                }

                //Thực hiện chỉnh sửa
                UpdateModel(ttad);
                db.SubmitChanges();
                return Content("<script>alert('Cập nhật thông tin cá nhân thành công!');window.location='/Admin/Account';</script>");
            }
            catch
            {
                return Content("<script>alert('Lỗi hệ thống.Vui lòng thử lại!');window.location='/Admin/Account';</script>");
            }
        }
        #endregion

        #region Đổi mật khẩu (ChangePassword)
        public ActionResult ChangePassword()
        {
            //Chưa đăng nhập => Login
            if (Session["Username_Admin"] == null)
                return RedirectToAction("Login");

            return View();
        }

        [HttpPost]
        public ActionResult ChangePassword(FormCollection collection)
        {
            try
            {
                //Lấy giá trị ở Form ChangePassword
                string _PassOld = collection["txt_Password"];
                string _PassNew = collection["txt_PasswordNew"];
                string _RePassNew = collection["txt_NhapLaiPass"];

                int _MaAdmin = int.Parse(Session["MaAdmin"].ToString());
                var ad = db.Admins.SingleOrDefault(a => a.MaAdmin == _MaAdmin);

                if (ad.Password == _PassOld)
                {
                    if (_RePassNew == _PassNew)
                    {
                        if (_PassNew.Length >= 6)
                        {
                            //Gán giá trị để chỉnh sửa
                            ad.Password = _PassNew;

                            //Thực thi chỉnh sửa và thông báo
                            UpdateModel(ad);
                            db.SubmitChanges();
                            return Content("<script>alert('Đổi mật khẩu thành công!');window.location='/Admin/ChangePassword';</script>");
                        }
                        else
                            return Content("<script>alert('Mật khẩu mới phải có ít nhất 6 ký tự!');window.location='/Admin/ChangePassword';</script>");
                    }
                    else
                        return Content("<script>alert('Mật khẩu nhập lại không đúng!');window.location='/Admin/ChangePassword';</script>");
                }
                else
                    return Content("<script>alert('Mật Khẩu cũ không đúng!');window.location='/Admin/ChangePassword';</script>");
            }
            catch
            {
                return Content("<script>alert('Thao tác đổi mật khẩu thất bại!');window.location='/Admin/ChangePassword';</script>");
            }
        }
        #endregion
        #endregion

        #region Bảng Menu (Menu - EditMenu)
        #region Danh sách Menu
        public ActionResult Menu(int? page)
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_Menu"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Menu !');window.location='/Admin/';</script>");

            int PageSize = 11;//Chỉ lấy ra 11 dòng (11 Menu)
            int PageNum = (page ?? 1);

            //Lấy ra Danh sách menu
            var _menu = (from mn in db.Menus
                         orderby mn.MaMenu
                         select mn).ToPagedList(PageNum, PageSize);
            return View(_menu);
        }
        #endregion

        #region EditMenu
        public ActionResult EditMenu(int id)
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_Menu"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Menu !');window.location='/Admin/';</script>");

            //Lấy ra thông tin Menu từ MaMenu truyền vào
            var _menu = db.Menus.First(mn => mn.MaMenu == id);
            return View(_menu);
        }

        [HttpPost]
        public ActionResult EditMenu(int id, FormCollection collection)
        {
            try
            {
                //Lấy giá trị ở Form EditMenu
                string _TenMenu = collection["txt_TenMenu"];
                string _Url = collection["txt_Url"];
                int _ThuTu = int.Parse(collection["txt_ThuTu"]);
                var _menu = db.Menus.First(mn => mn.MaMenu == id);

                //Gán giá trị để chỉnh sửa
                _menu.TenMenu = _TenMenu;
                _menu.url = _Url;
                _menu.Thutu = _ThuTu;

                //Thực hiện chỉnh sửa
                UpdateModel(_menu);
                db.SubmitChanges();
                return Content("<script>alert('Chỉnh sửa thành công!');window.location='/Admin/Menu';</script>");
            }
            catch
            {
                return Content("<script>alert('Lỗi hệ thống !');window.location='/Admin/EditMenu/" + id + "';</script>");
            }
        }
        #endregion
        #endregion

        #region Bảng Slider (Slider và Delete - CreateSlider - EditSlider - UpdateSlider)
        #region Danh sách Slider và Delete
        public ActionResult Slider(int? page)
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_Slider"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Slider !');window.location='/Admin/';</script>");

            int PageSize = 10;//Chỉ lấy ra 10 dòng (10 Slider)
            int PageNum = (page ?? 1);

            //Lấy ra Danh sách Slider
            var _Slider = (from s in db.Sliders
                           orderby s.MaSlider
                           select s).ToPagedList(PageNum, PageSize);
            return View(_Slider);
        }

        //Hàm xóa Danh Sách Slider trực tiếp trên Form Slider
        [HttpPost]
        public ActionResult Slider(int[] ckb_ID)
        {
            if (ckb_ID == null)//ckb_ID==null: Mảng ckb_ID trống, chưa chọn CheckBox nào để xóa
            {
                return Content("<script>alert('Vui lòng chọn 1 Slider để xóa!');window.location='/Admin/Slider';</script>");
            }
            else //Thực hiện các bước để xóa
            {
                try
                {
                    for (int i = 0; i < ckb_ID.Length; i++)//Duyệt vòng lặp tất cả các CheckBox đã chọn
                    {
                        //Tạo biến tạm chứa CheckBox đã chọn
                        int temp = ckb_ID[i];

                        //Lấy ra nhà sản xuất có mã trùng với CheckBox đã chọn
                        var sl = this.db.Sliders.Where(s => s.MaSlider == temp).SingleOrDefault();

                        //Thực hiện xóa
                        this.db.Sliders.DeleteOnSubmit(sl);
                    }
                    //Lưu thay đổi và thông báo
                    this.db.SubmitChanges();
                    return Content("<script>alert('Xóa thành công!');window.location='/Admin/Slider';</script>");
                }
                catch
                {
                    return Content("<script>alert('Lỗi hệ thống!');window.location='/Admin/Slider';</script>");
                }
            }
        }
        #endregion

        #region CreateSlider
        public ActionResult CreateSlider()
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_Slider"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Slider !');window.location='/Admin/';</script>");

            return View();
        }

        [HttpPost]
        public ActionResult CreateSlider(FormCollection collection, Slider sl)
        {
            try
            {
                //Lấy giá trị ở Form CreateSlider
                string _UrlHinh = collection["txt_UrlHinh"];
                string _LinkUrl = collection["txt_LinkUrl"];
                int _ThuTu = int.Parse(collection["txt_ThuTu"]);
                int _AnHien = int.Parse(collection["sl_AnHien"]);

                //Gán giá trị để thêm mới
                sl.UrlHinh = _UrlHinh;
                sl.LinkUrl = _LinkUrl;
                sl.Thutu = _ThuTu;
                if (_AnHien == 0)//Ở đây không thể Convert sang kiểu Bool nên gán điều kiện
                    sl.AnHien = false;
                else
                    sl.AnHien = true;

                //Thực hiện thêm mới
                db.Sliders.InsertOnSubmit(sl);
                db.SubmitChanges();
                return Content("<script>alert('Thêm mới thành công!');window.location='/Admin/Slider';</script>");
            }
            catch
            {
                return Content("<script>alert('Lỗi hệ thống!');window.location='/Admin/CreateSlider';</script>");
            }
        }
        #endregion

        #region EditSlider
        public ActionResult EditSlider(int id)
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_Slider"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Slider !');window.location='/Admin/';</script>");

            //Lấy ra thông tin Slider từ MaSlider truyền vào
            var sl = db.Sliders.First(s => s.MaSlider == id);
            return View(sl);
        }

        [HttpPost]
        public ActionResult EditSlider(int id, FormCollection collection)
        {
            try
            {
                //Lấy giá trị ở Form CreateSlider
                string _UrlHinh = collection["txt_UrlHinh"];
                string _LinkUrl = collection["txt_LinkUrl"];
                int _ThuTu = int.Parse(collection["txt_ThuTu"]);
                int _AnHien = int.Parse(collection["sl_AnHien"]);
                var sl = db.Sliders.First(s => s.MaSlider == id);

                //Gán giá trị để chỉnh sửa
                sl.UrlHinh = _UrlHinh;
                sl.LinkUrl = _LinkUrl;
                sl.Thutu = _ThuTu;
                if (_AnHien == 0)//Ở đây không thể Convert sang kiểu Bool nên gán điều kiện
                    sl.AnHien = false;
                else
                    sl.AnHien = true;

                //Thực hiện chỉnh sửa
                UpdateModel(sl);
                db.SubmitChanges();
                return Content("<script>alert('Chỉnh sửa thành công !');window.location='/Admin/Slider';</script>");
            }
            catch
            {
                return Content("<script>alert('Lỗi hệ thống !');window.location='/Admin/EditSlider/" + id + "';</script>");
            }
        }
        #endregion

        #region UpdateSlider
        //Hàm Ẩn hoặc Hiện Slider (ở đây sử dụng hàm void để Response.Write hình update lại)
        [HttpPost]
        public void UpdateSlider(int id)
        {
            //Lấy ra Slider cần Update Ẩn Hiện
            var _Slider = (from s in db.Sliders where s.MaSlider == id select s).SingleOrDefault();

            //Tạo chuỗi _Hinh để chưa đường dẫn hình Ẩn Hiện khi Update lại
            string _Hinh = "";

            //Ẩn thì cập nhật lại thành hiện và ngược lại
            if (_Slider.AnHien == true)
            {
                _Slider.AnHien = false;
                _Hinh = "/Content/Admin/Images/icon_An.png";
            }
            else
            {
                _Slider.AnHien = true;
                _Hinh = "/Content/Admin/Images/icon_Hien.png";
            }

            //Lưu chỉnh sửa
            UpdateModel(_Slider);
            db.SubmitChanges();

            //Xuất ra (Trả về) đường dẫn hình để Update lại trên Form
            Response.Write(_Hinh);
        }
        #endregion
        #endregion

        #region Bảng nhà sản xuất (Producer và Delete - CreateProducer - EditProducer - UpdateProducer)
        #region Producer và Delete
        public ActionResult Producer(int? page)
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_NhaSanXuat"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Nhà Sản Xuất !');window.location='/Admin/';</script>");

            int PageSize = 15;//Chỉ lấy ra 15 dòng (15 nhà sản xuất)
            int PageNum = (page ?? 1);

            //Lấy ra Danh sách nhà sản xuất
            var _nsx = (from nsx in db.NhaSanXuats
                        orderby nsx.MaNSX
                        select nsx).ToPagedList(PageNum, PageSize);
            return View(_nsx);
        }

        //Hàm xóa Danh Sách Nhà Sản Xuất trực tiếp trên Form Producer
        [HttpPost]
        public ActionResult Producer(int[] ckb_ID)
        {
            if (ckb_ID == null)//ckb_ID==null: Mảng ckb_ID trống, chưa chọn CheckBox nào để xóa
            {
                return Content("<script>alert('Vui lòng chọn 1 Nhà Sản Xuất để xóa!');window.location='/Admin/Producer';</script>");
            }
            else //Thực hiện các bước để xóa
            {
                try
                {
                    for (int i = 0; i < ckb_ID.Length; i++)//Duyệt vòng lặp tất cả các CheckBox đã chọn
                    {
                        //Tạo biến tạm chứa CheckBox đã chọn
                        int temp = ckb_ID[i];

                        //Lấy ra nhà sản xuất có mã trùng với CheckBox đã chọn
                        var nsx = this.db.NhaSanXuats.Where(m => m.MaNSX == temp).SingleOrDefault();

                        //Thực hiện xóa
                        this.db.NhaSanXuats.DeleteOnSubmit(nsx);
                    }
                    //Lưu thay đổi và thông báo
                    this.db.SubmitChanges();
                    return Content("<script>alert('Xóa thành công!');window.location='/Admin/Producer';</script>");
                }
                catch
                {
                    return Content("<script>alert('Lỗi hệ thống. Có thể nhà sản xuất này đang chứa sản phẩm!');window.location='/Admin/Producer';</script>");
                }
            }
        }
        #endregion

        #region CreateProducer
        public ActionResult CreateProducer()
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_NhaSanXuat"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Nhà Sản Xuất !');window.location='/Admin/';</script>");

            return View();
        }

        [HttpPost]
        public ActionResult CreateProducer(FormCollection collection, NhaSanXuat nsx)
        {
            try
            {
                //Lấy giá trị ở Form CreateProducer
                string _TenNSX = collection["txt_TenNSX"];
                int _AnHien = int.Parse(collection["sl_AnHien"]);

                //Gán giá trị để thêm mới
                nsx.TenNSX = _TenNSX;
                if (_AnHien == 0)//Ở đây không thể Convert sang kiểu Bool nên gán điều kiện
                    nsx.AnHien = false;
                else
                    nsx.AnHien = true;

                //Thực hiện thêm mới
                db.NhaSanXuats.InsertOnSubmit(nsx);
                db.SubmitChanges();
                return Content("<script>alert('Thêm mới thành công!');window.location='/Admin/Producer';</script>");
            }
            catch
            {
                return Content("<script>alert('Lỗi hệ thống!');window.location='/Admin/CreateProducer';</script>");
            }
        }
        #endregion

        #region EditProducer
        public ActionResult EditProducer(int id)
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_NhaSanXuat"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Nhà Sản Xuất !');window.location='/Admin/';</script>");

            //Lấy ra thông tin Nhà Sản Xuất từ MaNSX truyền vào
            var nsx = db.NhaSanXuats.First(n => n.MaNSX == id);
            return View(nsx);
        }

        [HttpPost]
        public ActionResult EditProducer(int id, FormCollection collection)
        {
            try
            {
                //Lấy giá trị ở Form EditProducer
                string _TenNSX = collection["txt_TenNSX"];
                int _AnHien = int.Parse(collection["sl_AnHien"]);
                var nsx = db.NhaSanXuats.First(n => n.MaNSX == id);

                //Gán giá trị để chỉnh sửa
                nsx.TenNSX = _TenNSX;
                if (_AnHien == 0)//Ở đây không thể Convert sang kiểu Bool nên gán điều kiện
                    nsx.AnHien = false;
                else
                    nsx.AnHien = true;

                //Thực hiện chỉnh sửa
                UpdateModel(nsx);
                db.SubmitChanges();
                return Content("<script>alert('Chỉnh sửa thành công!');window.location='/Admin/Producer';</script>");
            }
            catch
            {
                return Content("<script>alert('Lỗi hệ thống !');window.location='/Admin/EditProducer/" + id + "';</script>");
            }
        }
        #endregion

        #region UpdateProducer
        //Hàm Ẩn hoặc Hiện Nhà sản xuất (ở đây sử dụng hàm void để Response.Write hình update lại)
        [HttpPost]
        public void UpdateProducer(int id)
        {
            //Lấy ra nhà sản xuất cần Update Ẩn Hiện
            var _nsx = (from n in db.NhaSanXuats where n.MaNSX == id select n).SingleOrDefault();

            //Tạo chuỗi _Hinh để chưa đường dẫn hình Ẩn Hiện khi Update lại
            string _Hinh = "";

            //Ẩn thì cập nhật lại thành hiện và ngược lại
            if (_nsx.AnHien == true)
            {
                _nsx.AnHien = false;
                _Hinh = "/Content/Admin/Images/icon_An.png";
            }
            else
            {
                _nsx.AnHien = true;
                _Hinh = "/Content/Admin/Images/icon_Hien.png";
            }

            //Lưu chỉnh sửa
            UpdateModel(_nsx);
            db.SubmitChanges();

            //Xuất ra (Trả về) đường dẫn hình để Update lại trên Form
            Response.Write(_Hinh);
        }
        #endregion
        #endregion

        #region Bảng sản phẩm(Product va Delete - CreateProduct - EditProduct - UpdateProduct)
        #region Product va Delete
        public ActionResult Product(int? page)
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_SanPham"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Sản Phẩm !');window.location='/Admin/';</script>");

            int PageSize = 10;//Chỉ lấy ra 10 dòng (10 Sản phẩm)
            int PageNum = (page ?? 1);

            //Lấy ra Danh sản phẩm
            var _sp = (from sp in db.SanPhams
                       orderby sp.NgayCapNhat descending
                       select sp).ToPagedList(PageNum, PageSize);
            return View(_sp);
        }

        //Hàm xóa Danh Sách Sản Phẩm trực tiếp trên Form Product
        [HttpPost]
        public ActionResult Product(int[] ckb_ID)
        {
            if (ckb_ID == null)//ckb_ID==null: Mảng ckb_ID trống, chưa chọn CheckBox nào để xóa
            {
                return Content("<script>alert('Vui lòng chọn 1 Sản Phẩm để xóa!');window.location='/Admin/Product';</script>");
            }
            else //Thực hiện các bước để xóa
            {
                try
                {
                    for (int i = 0; i < ckb_ID.Length; i++)//Duyệt vòng lặp tất cả các CheckBox đã chọn
                    {
                        //Tạo biến tạm chứa CheckBox đã chọn
                        int temp = ckb_ID[i];

                        //Lấy ra sản phẩm có mã trùng với CheckBox đã chọn
                        var sp = this.db.SanPhams.Where(s => s.MaSP == temp).SingleOrDefault();

                        //Thực hiện xóa
                        this.db.SanPhams.DeleteOnSubmit(sp);
                    }
                    //Lưu thay đổi và thông báo
                    this.db.SubmitChanges();
                    return Content("<script>alert('Xóa thành công!');window.location='/Admin/Product';</script>");
                }
                catch
                {
                    return Content("<script>alert('Lỗi hệ thống.Sản phẩm này đã có người đặt mua, không thể xóa!');window.location='/Admin/Product';</script>");
                }
            }
        }
        #endregion

        #region CreateProduct
        public ActionResult CreateProduct()
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_SanPham"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Sản Phẩm !');window.location='/Admin/';</script>");

            //Lấy danh sách nhà sản xuất cho DropDownList
            ViewData["sl_NhaSX"] = new SelectList(db.NhaSanXuats, "MaNSX", "TenNSX");

            return View();
        }

        [HttpPost]
        [ValidateInput(false)]//Cho phép nhập kiểu HTML
        public ActionResult CreateProduct(FormCollection collection, SanPham sp)
        {
            try
            {
                //Lấy giá trị ở Form CreateProduct
                string _TenSP = collection["txt_TenSP"];
                string _UrlHinh = collection["txt_UrlHinh"];
                string _UrlHinh360 = collection["txt_UrlHinh360"];
                decimal _GiaHT = decimal.Parse(collection["txt_GiaHienTai"]);
                decimal _GiaCu = decimal.Parse(collection["txt_GiaCu"]);
                string _MoTa = collection["txt_MoTa"];
                string _MoTaCT = collection["txt_MoTaCT"];
                string _DanhGiaCT = collection["txt_DanhGiaCT"];
                int _MaNSX = int.Parse(collection["sl_NhaSX"]);
                int _SoLuongTon = int.Parse(collection["txt_SLTon"]);
                int _SoLuongBan = int.Parse(collection["txt_SLDaBan"]);
                float _KhuyenMai = float.Parse(collection["txt_KhuyenMai"]);
                int _LuotXem = int.Parse(collection["txt_LuotXem"]);
                DateTime _NgayCapNhat = Convert.ToDateTime(collection["txt_NgayCapNhat"]);
                int _AnHien = int.Parse(collection["sl_AnHien"]);

                //Gán giá trị để thêm mới
                sp.TenSP = _TenSP;
                sp.UrlHinh = _UrlHinh;
                sp.Code1 = "<a class=\"tgdd360\"  data-tgdd360-options=\"autospin: infinite; autospin-direction: anticlockwise; autospin-start: load,click;columns:36\"><img src=\"";
                sp.UrlHinh360 = _UrlHinh360;
                sp.Code2 = "\" /> </a>";
                sp.GiaHienTai = _GiaHT;
                sp.GiaCu = _GiaCu;
                sp.MoTa = _MoTa;
                sp.MoTaCT = _MoTaCT;
                sp.DanhGiaCT = _DanhGiaCT;
                sp.MaNSX = _MaNSX;
                sp.SoLuongTon = _SoLuongTon;
                sp.SLDaBan = _SoLuongBan;
                sp.KhuyenMai = _KhuyenMai;
                sp.LuotXem = _LuotXem;
                sp.NgayCapNhat = _NgayCapNhat;
                if (_AnHien == 0)//Ở đây không thể Convert sang kiểu Bool nên gán điều kiện
                    sp.AnHien = false;
                else
                    sp.AnHien = true;

                //Thực hiện thêm mới
                db.SanPhams.InsertOnSubmit(sp);
                db.SubmitChanges();
                return Content("<script>alert('Thêm mới thành công!');window.location='/Admin/Product';</script>");
            }
            catch
            {
                return Content("<script>alert('Lỗi hệ thống!');window.location='/Admin/CreateProduct';</script>");
            }
        }
        #endregion

        #region EditProduct
        public ActionResult EditProduct(int id)
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_SanPham"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Sản Phẩm !');window.location='/Admin/';</script>");

            //Lấy ra thông tin Sản phẩm từ MaSP truyền vào
            var sp = db.SanPhams.First(s => s.MaSP == id);

            //Lấy danh sách nhà sản xuất cho DropDownList
            ViewData["sl_NhaSX"] = new SelectList(db.NhaSanXuats, "MaNSX", "TenNSX", sp.MaNSX);
            return View(sp);
        }

        [HttpPost]
        [ValidateInput(false)]//Cho phép nhập kiểu HTML
        public ActionResult EditProduct(int id, FormCollection collection)
        {
            try
            {
                //Lấy giá trị ở Form EditProduct
                string _TenSP = collection["txt_TenSP"];
                string _UrlHinh = collection["txt_UrlHinh"];
                string _UrlHinh360 = collection["txt_UrlHinh360"];
                decimal _GiaHT = decimal.Parse(collection["txt_GiaHienTai"]);
                decimal _GiaCu = decimal.Parse(collection["txt_GiaCu"]);
                string _MoTa = collection["txt_MoTa"];
                string _MoTaCT = collection["txt_MoTaCT"];
                string _DanhGiaCT = collection["txt_DanhGiaCT"];
                int _MaNSX = int.Parse(collection["sl_NhaSX"]);
                int _SoLuongTon = int.Parse(collection["txt_SLTon"]);
                int _SoLuongBan = int.Parse(collection["txt_SLDaBan"]);
                float _KhuyenMai = float.Parse(collection["txt_KhuyenMai"]);
                int _LuotXem = int.Parse(collection["txt_LuotXem"]);
                DateTime _NgayCapNhat = Convert.ToDateTime(collection["txt_NgayCapNhat"]);
                int _AnHien = int.Parse(collection["sl_AnHien"]);
                //Lấy ra thông tin Sản phẩm từ MaSP truyền vào
                var sp = db.SanPhams.First(s => s.MaSP == id);

                //Gán giá trị để chỉnh sửa
                sp.TenSP = _TenSP;
                sp.UrlHinh = _UrlHinh;
                sp.Code1 = "<a class=\"tgdd360\"  data-tgdd360-options=\"autospin: infinite; autospin-direction: anticlockwise; autospin-start: load,click;columns:36\"><img src=\"";
                sp.UrlHinh360 = _UrlHinh360;
                sp.Code2 = "\" /> </a>";
                sp.GiaHienTai = _GiaHT;
                sp.GiaCu = _GiaCu;
                sp.MoTa = _MoTa;
                sp.MoTaCT = _MoTaCT;
                sp.DanhGiaCT = _DanhGiaCT;
                sp.MaNSX = _MaNSX;
                sp.SoLuongTon = _SoLuongTon;
                sp.SLDaBan = _SoLuongBan;
                sp.KhuyenMai = _KhuyenMai;
                sp.LuotXem = _LuotXem;
                sp.NgayCapNhat = _NgayCapNhat;
                if (_AnHien == 0)//Ở đây không thể Convert sang kiểu Bool nên gán điều kiện
                    sp.AnHien = false;
                else
                    sp.AnHien = true;

                //Thực hiện thêm mới
                UpdateModel(sp);
                db.SubmitChanges();
                return Content("<script>alert('Chỉnh sửa thành công!');window.location='/Admin/Product';</script>");
            }
            catch
            {
                return Content("<script>alert('Lỗi hệ thống!');window.location='/Admin/Product';</script>");
            }
        }
        #endregion

        #region UpdateProduct
        //Hàm Ẩn hoặc Hiện Sản phẩm (ở đây sử dụng hàm void để Response.Write hình update lại)
        [HttpPost]
        public void UpdateProduct(int id)
        {
            //Lấy ra sản phẩm cần Update Ẩn Hiện
            var _sp = (from s in db.SanPhams where s.MaSP == id select s).SingleOrDefault();

            //Tạo chuỗi _Hinh để chưa đường dẫn hình Ẩn Hiện khi Update lại
            string _Hinh = "";

            //Ẩn thì cập nhật lại thành hiện và ngược lại
            if (_sp.AnHien == true)
            {
                _sp.AnHien = false;
                _Hinh = "/Content/Admin/Images/icon_An.png";
            }
            else
            {
                _sp.AnHien = true;
                _Hinh = "/Content/Admin/Images/icon_Hien.png";
            }

            //Lưu chỉnh sửa
            UpdateModel(_sp);
            db.SubmitChanges();

            //Xuất ra (Trả về) đường dẫn hình để Update lại trên Form
            Response.Write(_Hinh);
        }
        #endregion
        #endregion

        #region Bảng khách hàng (Member và Delete - UpdateMember)
        #region Danh sách Member và Delete
        public ActionResult Member(int? page)
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_KhachHang"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Khách Hàng !');window.location='/Admin/';</script>"); ;

            int PageSize = 10;//Chỉ lấy ra 10 dòng (10 Member)
            int PageNum = (page ?? 1);

            //Lấy ra Danh sách Member
            var _KH = (from s in db.KhachHangs
                       orderby s.NgayDangKy //descending
                       select s).ToPagedList(PageNum, PageSize);
            return View(_KH);
        }

        //Hàm xóa Danh Sách Member trực tiếp trên Form Member
        [HttpPost]
        public ActionResult Member(int[] ckb_ID)
        {
            if (ckb_ID == null)//ckb_ID==null: Mảng ckb_ID trống, chưa chọn CheckBox nào để xóa
            {
                return Content("<script>alert('Vui lòng chọn 1 khách hàng để xóa!');window.location='/Admin/Member';</script>");
            }
            else //Thực hiện các bước để xóa
            {
                try
                {
                    for (int i = 0; i < ckb_ID.Length; i++)//Duyệt vòng lặp tất cả các CheckBox đã chọn
                    {
                        //Tạo biến tạm chứa CheckBox đã chọn
                        int temp = ckb_ID[i];

                        //Lấy ra nhà sản xuất có mã trùng với CheckBox đã chọn
                        var kh = this.db.KhachHangs.Where(k => k.MaKH == temp).SingleOrDefault();

                        //Thực hiện xóa
                        this.db.KhachHangs.DeleteOnSubmit(kh);
                    }
                    //Lưu thay đổi và thông báo
                    this.db.SubmitChanges();
                    return Content("<script>alert('Xóa thành công!');window.location='/Admin/Member';</script>");
                }
                catch
                {
                    return Content("<script>alert('Lỗi hệ thống! khách hàng này có thể đã mua hàng, gửi tin nhắn cho admin. Bạn Vui lòng xóa những gì liên quan đến khách hàng này trước!');window.location='/Admin/Member';</script>");
                }
            }
        }
        #endregion

        #region UpdateMember
        //Hàm khóa hoặc mở khóa tài khoản khách hàng (ở đây sử dụng hàm void để Response.Write hình update lại)
        [HttpPost]
        public void UpdateMember(int id)
        {
            //Lấy ra tài khoản khách hàng cần Update Ẩn Hiện
            var _KH = (from kh in db.KhachHangs where kh.MaKH == id select kh).SingleOrDefault();

            //Tạo chuỗi _Hinh để chưa đường dẫn hình Ẩn Hiện khi Update lại
            string _Hinh = "";

            //Ẩn thì cập nhật lại thành hiện và ngược lại
            if (_KH.AnHien == true)
            {
                _KH.AnHien = false;
                _Hinh = "/Content/Admin/Images/icon_An.png";
            }
            else
            {
                _KH.AnHien = true;
                _Hinh = "/Content/Admin/Images/icon_Hien.png";
            }

            //Lưu chỉnh sửa
            UpdateModel(_KH);
            db.SubmitChanges();

            //Xuất ra (Trả về) đường dẫn hình để Update lại trên Form
            Response.Write(_Hinh);
        }
        #endregion
        #endregion

        #region Bảng đơn hàng (Order - UpdateOrder)
        #region Danh sách đơn hàng
        public ActionResult Order(int? page)
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_DonHang"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Đơn Hàng !');window.location='/Admin/';</script>");

            int PageSize = 10;//Chỉ lấy ra 10 dòng (10 đơn hàng )
            int PageNum = (page ?? 1);

            //Lấy ra Danh sách đơn hàng
            var _DH = (from d in db.DonHangs
                       orderby d.NgayMua descending
                       select d).ToPagedList(PageNum, PageSize);
            return View(_DH);
        }
        #endregion

        #region UpdateOrder
        //Hàm Update trạng thái đã giao | chưa giao của đơn hàng (ở đây sử dụng hàm void để Response.Write hình update lại)
        [HttpPost]
        public void UpdateOrder(int id)
        {
            //Lấy ra đơn hàng cần Update Trạng thái giao hàng
            var _DH = (from d in db.DonHangs where d.MaDH == id select d).SingleOrDefault();

            //Tạo chuỗi _Hinh để chưa đường dẫn hình Ẩn Hiện khi Update lại
            string _Hinh = "";

            //Ẩn thì cập nhật lại thành hiện và ngược lại
            if (_DH.Dagiao == true)
            {
                _DH.Dagiao = false;
                _Hinh = "/Content/Admin/Images/icon_An.png";
            }
            else
            {
                _DH.Dagiao = true;
                _Hinh = "/Content/Admin/Images/icon_Hien.png";
            }

            //Lưu chỉnh sửa
            UpdateModel(_DH);
            db.SubmitChanges();

            //Xuất ra (Trả về) đường dẫn hình để Update lại trên Form
            Response.Write(_Hinh);
        }
        #endregion
        #endregion

        #region Bảng Chi tiết đơn hàng (OrderDetail)
        public ActionResult OrderDetail(int id)
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_DonHang"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Đơn Hàng !');window.location='/Admin/';</script>");

            //Lấy ra thông tin Chi tiết đơn hàng từ mã đơn hàng truyền vào
            //Ở đây 1 đơn hàng có thể có nhiều chi tiết ĐH(mua nhiều SP), nên dùng where như trang sản phẩm theo nhà sản xuất
            var CT_DH = (from c in db.CT_DonHangs
                         where c.MaDH == id
                         select c).ToList();
            return View(CT_DH);
        }
        #endregion

        #region Tin tức (News và Delete - CreateNews - EditNews - UpdateNews)
        #region News và Delete
        public ActionResult News(int? page)
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_TinTuc"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Tin Tức !');window.location='/Admin/';</script>");

            int PageSize = 10;//Chỉ lấy ra 10 dòng (10 tin tức)
            int PageNum = (page ?? 1);

            //Lấy ra Danh sách tin tức
            var _tt = (from tt in db.TinTucs
                       orderby tt.NgayCapNhat descending
                       select tt).ToPagedList(PageNum, PageSize);
            return View(_tt);
        }

        //Hàm xóa Danh Sách Nhà Sản Xuất trực tiếp trên Form Producer
        [HttpPost]
        public ActionResult News(int[] ckb_ID)
        {
            if (ckb_ID == null)//ckb_ID==null: Mảng ckb_ID trống, chưa chọn CheckBox nào để xóa
            {
                return Content("<script>alert('Vui lòng chọn 1 Tin Tức để xóa!');window.location='/Admin/News';</script>");
            }
            else //Thực hiện các bước để xóa
            {
                try
                {
                    for (int i = 0; i < ckb_ID.Length; i++)//Duyệt vòng lặp tất cả các CheckBox đã chọn
                    {
                        //Tạo biến tạm chứa CheckBox đã chọn
                        int temp = ckb_ID[i];

                        //Lấy ra tin tức có mã trùng với CheckBox đã chọn
                        var tt = this.db.TinTucs.Where(t => t.MaTin == temp).SingleOrDefault();

                        //Thực hiện xóa
                        this.db.TinTucs.DeleteOnSubmit(tt);
                    }
                    //Lưu thay đổi và thông báo
                    this.db.SubmitChanges();
                    return Content("<script>alert('Xóa thành công!');window.location='/Admin/News';</script>");
                }
                catch
                {
                    return Content("<script>alert('Lỗi hệ thống!');window.location='/Admin/News';</script>");
                }
            }
        }
        #endregion

        #region CreateNews
        public ActionResult CreateNews()
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_TinTuc"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Tin Tức !');window.location='/Admin/';</script>");

            return View();
        }

        [HttpPost]
        [ValidateInput(false)]//Cho phép nhập kiểu HTML
        public ActionResult CreateNews(FormCollection collection, TinTuc tt)
        {
            try
            {
                //Lấy giá trị ở Form CreateNews
                string _TieuDe = collection["txt_TieuDe"];
                string _TomTat = collection["txt_TomTat"];
                string _UrlHinh = collection["txt_UrlHinh"];
                string _NoiDung = collection["txt_NoiDung"];
                int _LuotXem = int.Parse(collection["txt_LuotXem"]);
                string _NgayCapNhat = collection["txt_NgayCapNhat"];
                int _AnHien = int.Parse(collection["sl_AnHien"]);

                //Gán giá trị để thêm mới
                tt.TieuDe = _TieuDe;
                tt.TomTat = _TomTat;
                tt.UrlHinh = _UrlHinh;
                tt.NoiDung = _NoiDung;
                tt.LuotXem = _LuotXem;
                tt.NgayCapNhat = Convert.ToDateTime(_NgayCapNhat);

                if (_AnHien == 0)//Ở đây không thể Convert sang kiểu Bool nên gán điều kiện
                    tt.AnHien = false;
                else
                    tt.AnHien = true;

                //Thực hiện thêm mới
                db.TinTucs.InsertOnSubmit(tt);
                db.SubmitChanges();
                return Content("<script>alert('Thêm mới thành công!');window.location='/Admin/News';</script>");
            }
            catch
            {
                return Content("<script>alert('Lỗi hệ thống!');window.location='/Admin/CreateNews';</script>");
            }
        }
        #endregion

        #region EditNews
        public ActionResult EditNews(int id)
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_TinTuc"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Tin Tức !');window.location='/Admin/';</script>");

            //Lấy ra thông tin Tin Tức từ MaTin truyền vào
            var tt = db.TinTucs.First(t => t.MaTin == id);
            return View(tt);
        }

        [HttpPost]
        [ValidateInput(false)]//Cho phép nhập kiểu HTML
        public ActionResult EditNews(int id, FormCollection collection)
        {
            try
            {
                //Lấy giá trị ở Form EditNews
                string _TieuDe = collection["txt_TieuDe"];
                string _TomTat = collection["txt_TomTat"];
                string _UrlHinh = collection["txt_UrlHinh"];
                string _NoiDung = collection["txt_NoiDung"];
                int _LuotXem = int.Parse(collection["txt_LuotXem"]);
                string _NgayCapNhat = collection["txt_NgayCapNhat"];
                int _AnHien = int.Parse(collection["sl_AnHien"]);
                var tt = db.TinTucs.First(t => t.MaTin == id);

                //Gán giá trị để chỉnh sửa
                tt.TieuDe = _TieuDe;
                tt.TomTat = _TomTat;
                tt.UrlHinh = _UrlHinh;
                tt.NoiDung = _NoiDung;
                tt.LuotXem = _LuotXem;
                tt.NgayCapNhat = Convert.ToDateTime(_NgayCapNhat);

                if (_AnHien == 0)//Ở đây không thể Convert sang kiểu Bool nên gán điều kiện
                    tt.AnHien = false;
                else
                    tt.AnHien = true;

                //Thực hiện chỉnh sửa
                UpdateModel(tt);
                db.SubmitChanges();
                return Content("<script>alert('Chỉnh sửa thành công!');window.location='/Admin/News';</script>");
            }
            catch
            {
                return Content("<script>alert('Lỗi hệ thống !');window.location='/Admin/EditNews/" + id + "';</script>");
            }
        }
        #endregion

        #region UpdateNews
        //Hàm Ẩn hoặc Hiện Tin tức (ở đây sử dụng hàm void để Response.Write hình update lại)
        [HttpPost]
        public void UpdateNews(int id)
        {
            //Lấy ra tin tức cần Update Ẩn Hiện
            var _tt = (from t in db.TinTucs where t.MaTin == id select t).SingleOrDefault();

            //Tạo chuỗi _Hinh để chưa đường dẫn hình Ẩn Hiện khi Update lại
            string _Hinh = "";

            //Ẩn thì cập nhật lại thành hiện và ngược lại
            if (_tt.AnHien == true)
            {
                _tt.AnHien = false;
                _Hinh = "/Content/Admin/Images/icon_An.png";
            }
            else
            {
                _tt.AnHien = true;
                _Hinh = "/Content/Admin/Images/icon_Hien.png";
            }

            //Lưu chỉnh sửa
            UpdateModel(_tt);
            db.SubmitChanges();

            //Xuất ra (Trả về) đường dẫn hình để Update lại trên Form
            Response.Write(_Hinh);
        }
        #endregion
        #endregion

        #region Bảng Quảng cáo (Advertisement và Delete - CreateAdvertisement - EditAdvertisement)
        #region Advertisement và Delete
        public ActionResult Advertisement(int? page)
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_QuangCao"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Quảng Cáo !');window.location='/Admin/';</script>");

            int PageSize = 10;//Chỉ lấy ra 10 dòng (10 Quảng cáo)
            int PageNum = (page ?? 1);

            //Lấy ra Danh sách Quảng cáo
            var _QC = (from q in db.QuangCaos
                       orderby q.MaQC
                       select q).ToPagedList(PageNum, PageSize);
            return View(_QC);
        }
        //Hàm xóa Danh Sách quảng cáo trực tiếp trên Form QuangCao
        [HttpPost]
        public ActionResult Advertisement(int[] ckb_ID)
        {
            if (ckb_ID == null)//ckb_ID==null: Mảng ckb_ID trống, chưa chọn CheckBox nào để xóa
            {
                return Content("<script>alert('Vui lòng chọn 1 quảng cáo để xóa!');window.location='/Admin/Advertisement';</script>");
            }
            else //Thực hiện các bước để xóa
            {
                try
                {
                    for (int i = 0; i < ckb_ID.Length; i++)//Duyệt vòng lặp tất cả các CheckBox đã chọn
                    {
                        //Tạo biến tạm chứa CheckBox đã chọn
                        int temp = ckb_ID[i];

                        //Lấy ra quảng cáo có mã trùng với CheckBox đã chọn
                        var QC = this.db.QuangCaos.Where(m => m.MaQC == temp).SingleOrDefault();

                        //Thực hiện xóa
                        this.db.QuangCaos.DeleteOnSubmit(QC);
                    }
                    //Lưu thay đổi và thông báo
                    this.db.SubmitChanges();
                    return Content("<script>alert('Xóa thành công!');window.location='/Admin/Advertisement';</script>");
                }
                catch
                {
                    return Content("<script>alert('Lỗi hệ thống!');window.location='/Admin/Advertisement';</script>");
                }
            }
        }

        #endregion

        #region CreateAdvertisement
        public ActionResult CreateAdvertisement()
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_QuangCao"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Quảng Cáo !');window.location='/Admin/';</script>");

            return View();
        }
        [HttpPost]
        public ActionResult CreateAdvertisement(FormCollection collection, QuangCao QC)
        {
            try
            {
                //Lấy giá trị ở Form CreateAdvertisement
                string _TenQC = collection["txt_TenQC"];
                string _TenCty = collection["txt_TenCty"];
                string _UrlHinh = collection["txt_UrlHinh"];
                string _LinkUrl = collection["txt_LinkUrl"];
                string _ViTri = collection["txt_ViTri"];
                string _NgayBatDau = collection["txt_NgayBatDau"];
                string _NgayHetHan = collection["txt_NgayHetHan"];
                int _AnHien = int.Parse(collection["sl_AnHien"]);

                //Gán giá trị để thêm mới
                QC.TenQC = _TenQC;
                QC.TenCty = _TenCty;
                QC.UrlHinh = _UrlHinh;
                QC.LinkUrl = _LinkUrl;
                QC.vitri = _ViTri;
                QC.Ngaybatdau = Convert.ToDateTime(_NgayBatDau);
                QC.Ngayhethan = Convert.ToDateTime(_NgayHetHan);
                if (_AnHien == 0)//Ở đây không thể Convert sang kiểu Bool nên gán điều kiện
                    QC.AnHien = false;
                else
                    QC.AnHien = true;

                //Thực hiện thêm mới
                db.QuangCaos.InsertOnSubmit(QC);
                db.SubmitChanges();
                return Content("<script>alert('Thêm mới thành công!');window.location='/Admin/Advertisement';</script>");
            }
            catch
            {
                return Content("<script>alert('Lỗi hệ thống!');window.location='/Admin/CreateAdvertisement';</script>");
            }
        }
        #endregion

        #region EditAdvertisement
        public ActionResult EditAdvertisement(int id)
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_QuangCao"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Quảng Cáo !');window.location='/Admin/';</script>");

            //Lấy thông tin quảng cáo từ MaQC truyền vào
            var QC = db.QuangCaos.First(q => q.MaQC == id);
            return View(QC);
        }

        [HttpPost]
        public ActionResult EditAdvertisement(int id, FormCollection collection)
        {
            try
            {
                //Lấy giá trị ở Form EditAdvertisement
                string _TenQC = collection["txt_TenQC"];
                string _TenCty = collection["txt_TenCty"];
                string _UrlHinh = collection["txt_UrlHinh"];
                string _LinkUrl = collection["txt_LinkUrl"];
                string _ViTri = collection["txt_ViTri"];
                string _NgayBatDau = collection["txt_NgayBatDau"];
                string _NgayHetHan = collection["txt_NgayHetHan"];
                int _AnHien = int.Parse(collection["sl_AnHien"]);
                var QC = db.QuangCaos.First(q => q.MaQC == id);

                //Gán giá trị để chỉnh sửa
                QC.TenQC = _TenQC;
                QC.TenCty = _TenCty;
                QC.UrlHinh = _UrlHinh;
                QC.LinkUrl = _LinkUrl;
                QC.vitri = _ViTri;
                QC.Ngaybatdau = Convert.ToDateTime(_NgayBatDau);
                QC.Ngayhethan = Convert.ToDateTime(_NgayHetHan);
                if (_AnHien == 0)//Ở đây không thể Convert sang kiểu Bool nên gán điều kiện
                    QC.AnHien = false;
                else
                    QC.AnHien = true;

                //Thực hiện chỉnh sửa
                UpdateModel(QC);
                db.SubmitChanges();
                return Content("<script>alert('Chỉnh sửa thành công!');window.location='/Admin/Advertisement';</script>");
            }
            catch
            {
                return Content("<script>alert('Lỗi hệ thống !');window.location='/Admin/EditAdvertisement/" + id + "';</script>");
            }
        }

        #endregion

        #region UpdateAdvertisement
        //Hàm Ẩn hoặc Hiện Quảng cáo (ở đây sử dụng hàm void để Response.Write hình update lại)
        [HttpPost]
        public void UpdateAdvertisement(int id)
        {
            //Lấy ra quảng cáo cần Update Ẩn Hiện
            var _qc = (from q in db.QuangCaos where q.MaQC == id select q).SingleOrDefault();

            //Tạo chuỗi _Hinh để chưa đường dẫn hình Ẩn Hiện khi Update lại
            string _Hinh = "";

            //Ẩn thì cập nhật lại thành hiện và ngược lại
            if (_qc.AnHien == true)
            {
                _qc.AnHien = false;
                _Hinh = "/Content/Admin/Images/icon_An.png";
            }
            else
            {
                _qc.AnHien = true;
                _Hinh = "/Content/Admin/Images/icon_Hien.png";
            }

            //Lưu chỉnh sửa
            UpdateModel(_qc);
            db.SubmitChanges();

            //Xuất ra (Trả về) đường dẫn hình để Update lại trên Form
            Response.Write(_Hinh);
        }
        #endregion
        #endregion

        #region Bảng Liên hệ (Contact và Delete - Reply)
        #region Contact và Delete
        public ActionResult Contact(int? page)
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_LienHe"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Liên Hệ !');window.location='/Admin/';</script>");

            int PageSize = 10;//Chỉ lấy ra 10 dòng (10 tin nhắn mới nhất)
            int PageNum = (page ?? 1);

            //Lấy ra liên hệ mới nhất của từng khách hàng (Code LINQ ở đây hơi phức tạp T_T )
            var _LienHe = (from _lh in db.LienHes
                           where (from _Lh1 in db.LienHes
                                  group _Lh1 by new { _Lh1.MaKH } into g
                                  select new { _Column = (int?)g.Max(p => p.MaLH) }).Contains(new { _Column = (System.Int32?)_lh.MaLH })
                           orderby _lh.NgayGui descending
                           select _lh).ToPagedList(PageNum, PageSize);

            return View(_LienHe);
        }

        //Hàm xóa Liên hệ trực tiếp trên Form Contact
        [HttpPost]
        public ActionResult Contact(int[] ckb_ID)
        {
            if (ckb_ID == null)//ckb_ID==null: Mảng ckb_ID trống, chưa chọn CheckBox nào để xóa
            {
                return Content("<script>alert('Vui lòng chọn 1 liên hệ để xóa!');window.location='/Admin/Contact';</script>");
            }
            else //Thực hiện các bước để xóa
            {
                try
                {
                    for (int i = 0; i < ckb_ID.Length; i++)//Duyệt vòng lặp tất cả các CheckBox đã chọn
                    {
                        //Tạo biến tạm chứa CheckBox đã chọn
                        int temp = ckb_ID[i];

                        //Lấy ra liên hệ có mã trùng với CheckBox đã chọn
                        var lh = this.db.LienHes.Where(l => l.MaLH == temp).SingleOrDefault();

                        //Thực hiện xóa
                        this.db.LienHes.DeleteOnSubmit(lh);
                    }
                    //Lưu thay đổi và thông báo
                    this.db.SubmitChanges();
                    return Content("<script>alert('Xóa thành công!');window.location='/Admin/Contact';</script>");
                }
                catch
                {
                    return Content("<script>alert('Lỗi hệ thống!');window.location='/Admin/Contact';</script>");
                }
            }
        }
        #endregion

        #region Reply
        public ActionResult Reply(int id, int? page)
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_LienHe"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Liên Hệ !');window.location='/Admin/';</script>");

            int PageSize = 4;//Chỉ lấy ra 4 dòng (4 tin nhắn mới nhất)
            int PageNum = (page ?? 1);

            //Load dữ liệu tin nhắn
            var _LienHe = (from lh in db.LienHes
                           where lh.MaKH == id && lh.Anhien == true
                           orderby lh.MaLH descending
                           select lh).ToPagedList(PageNum, PageSize);

            //Kiểm tra trong hộp thư có tin nhắn thì thực hiện tiếp, không thì ở hàm _LayLH sẽ bị lỗi vì ko tìm đc liên hệ để lấy ra
            var _DemLH = db.LienHes.Where(m => m.MaKH == id);
            if (_DemLH.Count() > 0)
            {
                //Hàm lấy ra 1 Liên hệ mới nhất để xem Khách hàng đã đọc tin nhắn đấy chưa
                var _LayLH = (from lh in db.LienHes
                              where lh.MaKH == id && lh.Anhien == true
                              orderby lh.MaLH descending
                              select lh).First();

                //Hàm Kiểm tra chưa đọc thì Update lại là khách hàng đã đọc
                if (_LayLH != null)//Kiểm tra trong hộp thư có tin nhắn thì thực hiện tiếp
                {
                    if (_LayLH.LuotGui == false && _LayLH.DaDocAD == false)//Lượt gửi Là Khách hàng và Admin chưa đọc
                    {
                        //Vào đây thì đã đọc tin nhắn => Cập nhật tin nhắn đã đọc của Admin là true
                        _LayLH.DaDocAD = true;

                        //Thực hiện cập nhật
                        UpdateModel(_LayLH);
                        db.SubmitChanges();
                    }
                }
            }
            return View(_LienHe);
        }

        [HttpPost]
        [ValidateInput(false)]//Cho phép nhập kiểu HTML
        public ActionResult Reply(int id, FormCollection collection, LienHe lh)
        {
            try
            {
                //Lấy giá trị ở Form Contact
                int _MaKH = id;
                string _Avatar = Session["Avatar_Admin"].ToString();
                string _TenNguoiGui = Session["HoTen_Admin"].ToString();
                string _NoiDung = collection["txt_NoiDung"];

                //Bắt lỗi không cho Liên hệ trống
                if (_NoiDung == "")
                    return Content("<script>alert('Vui lòng nhập nội dung để gửi liên hệ!');window.location='/Admin/Reply/" + id + "';</script>");

                //Gán giá trị để thêm mới
                lh.MaKH = _MaKH;
                lh.Avatar = _Avatar;
                lh.TenNguoiGui = _TenNguoiGui;
                lh.NoiDung = _NoiDung;
                lh.NgayGui = DateTime.Now;//Ngày hiện tại
                lh.LuotGui = true;//ADmin gửi
                lh.DaDocKH = false;//Khách hàng chưa đọc
                lh.DaDocAD = true;//Admin đã đọc
                lh.Anhien = true;//Hiện

                //Thực hiện thêm mới
                db.LienHes.InsertOnSubmit(lh);
                db.SubmitChanges();
                return Content("<script>alert('Gửi liên hệ đến khách hàng thành công!');window.location='/Admin/Reply/" + id + "';</script>");
            }
            catch
            {
                return Content("<script>alert('Lỗi hệ thống!');window.location='/Admin/Reply/" + id + "';</script>");
            }
        }
        #endregion
        #endregion

        #region Bảng Giới thiệu (About và Update)
        public ActionResult About()
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_GioiThieu"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Giới Thiệu !');window.location='/Admin/';</script>"); ;

            //Lấy ra thông tin giới thiệu website, ở đây chỉ 1 hàng dữ liệu nên gán cứng MaTT=1
            var _TT = db.ThongTins.First(t => t.MaTT == 1);
            return View(_TT);
        }

        [HttpPost]
        [ValidateInput(false)]//Cho phép nhập kiểu HTML
        public ActionResult About(FormCollection collection)
        {
            try
            {
                //Lấy giá trị ở Form About
                string _GioiThieu = collection["txt_GioiThieu"];
                var _TT = db.ThongTins.First(t => t.MaTT == 1);//Lấy ra thông tin giới thiệu website, ở đây chỉ 1 hàng dữ liệu nên gán cứng MaTT=1

                //Bắt lỗi không cho Giới thiệu trống
                if (_GioiThieu == "")
                    return Content("<script>alert('Vui lòng nhập nội dung giới thiệu cho Website!');window.location='/Admin/About';</script>");

                //Gán giá trị để chỉnh sửa
                _TT.GioiThieu = _GioiThieu;

                //Thực hiện chỉnh sửa
                UpdateModel(_TT);
                db.SubmitChanges();
                return Content("<script>alert('Chỉnh sửa thành công!');window.location='/Admin/About';</script>");
            }
            catch
            {
                return Content("<script>alert('Lỗi hệ thông!');window.location='/Admin/About';</script>");
            }
        }
        #endregion

        #region Bảng Admin (ListAdmin và Delete - CreateAdmin-  Các hàm Update trạng thái, Quyền quản trị website)
        #region ListAdmin và Delete
        public ActionResult ListAdmin(int? page)
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_QuanTriAdmin"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Administrator !');window.location='/Admin/';</script>");

            int PageSize = 3;//Chỉ lấy ra 3 dòng (3 Admin)
            int PageNum = (page ?? 1);

            //Lấy ra Danh sách Admin
            var _PQ = (from pq in db.PhanQuyen_Admins
                       orderby pq.MaAdmin descending
                       select pq).ToPagedList(PageNum, PageSize);
            return View(_PQ);
        }

        //Hàm xóa Danh Sách Admin trực tiếp trên Form ListAdmin
        [HttpPost]
        public ActionResult ListAdmin(int[] ckb_ID)
        {
            if (ckb_ID == null)//ckb_ID==null: Mảng ckb_ID trống, chưa chọn CheckBox nào để xóa
            {
                return Content("<script>alert('Vui lòng chọn 1 Administrator để xóa!');window.location='/Admin/ListAdmin';</script>");
            }
            else //Thực hiện các bước để xóa
            {
                try
                {
                    for (int i = 0; i < ckb_ID.Length; i++)//Duyệt vòng lặp tất cả các CheckBox đã chọn
                    {
                        //Tạo biến tạm chứa CheckBox đã chọn
                        int temp = ckb_ID[i];

                        //Kiểm tra mã Admin muốn xóa không được trung với Mã Admin đang đăng nhập (Tức không thể xóa TK của chính mình)
                        if (temp != int.Parse(Session["MaAdmin"].ToString()))
                        {

                            //Thực hiện xóa bảng phân quyền trước rồi xóa bảng Admin
                            //Lấy ra Phân quyền có mã Admin trùng với CheckBox đã chọn
                            var _PQ = this.db.PhanQuyen_Admins.Where(p => p.MaAdmin == temp).SingleOrDefault();

                            //Thực hiện xóa bảng phân quyền
                            this.db.PhanQuyen_Admins.DeleteOnSubmit(_PQ);

                            //Tiếp đến xóa bảng Admin
                            //Lấy ra Admin có mã Admin trùng với CheckBox đã chọn
                            var _AD = this.db.Admins.Where(a => a.MaAdmin == temp).SingleOrDefault();

                            //Thực hiện xóa bảng Admin
                            this.db.Admins.DeleteOnSubmit(_AD);
                        }
                        else
                        {
                            return Content("<script>alert('Bạn không thể tự xóa tài khoản của mình !');window.location='/Admin/ListAdmin';</script>");
                        }
                    }
                    //Lưu thay đổi và thông báo
                    this.db.SubmitChanges();
                    return Content("<script>alert('Xóa thành công !');window.location='/Admin/ListAdmin';</script>");
                }
                catch
                {
                    return Content("<script>alert('Lỗi hệ thống !);window.location='/Admin/ListAdmin';</script>");
                }
            }
        }
        #endregion

        #region CreateAdmin
        public ActionResult CreateAdmin()
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_QuanTriAdmin"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị Administrator !');window.location='/Admin/';</script>");

            return View();
        }

        [HttpPost]
        public ActionResult CreateAdmin(FormCollection collection, Admin ad, PhanQuyen_Admin pq)
        {
            try
            {
                //Lấy giá trị ở Form Register         
                string _Username = collection["txt_Username"];
                string _Password = collection["txt_Password"]; string _RePassword = collection["txt_NhapLaiPass"];
                string _Email = collection["txt_Email"];
                string _HoTen = collection["txt_HoTen"];
                string _DiaChi = collection["txt_DiaChi"];
                string _DienThoai = collection["txt_DienThoai"];
                string _Cmnd = collection["txt_Cmnd"];
                string _NgaySinh = collection["txt_Ngay"] + "/" + collection["dl_Thang"] + "/" + collection["txt_Nam"];
                int _GioiTinh = int.Parse(collection["sl_GioiTinh"]);

                //Gán giá trị để Đăng Ký mới tài khoản

                //Kiểm tra xem tài khoản đã có người sử dụng chưa?
                var _CheckUser = db.Admins.FirstOrDefault(a => a.Username == _Username);
                if (_CheckUser != null)
                    return Content("<script>alert('Tên đăng nhập đã có người sử dụng!');window.location='/Admin/CreateAdmin';</script>");
                else
                    ad.Username = _Username;

                //Kiểm tra Mật khẩu nhập lại có giống Mật khẩu đăng ký không?
                if (_RePassword != _Password)
                    return Content("<script>alert('Mật khẩu nhập lại không đúng!');window.location='/Admin/CreateAdmin';</script>");
                else
                    ad.Password = _Password;

                //Kiểm tra xem Email đã có người sử dụng chưa?
                var _CheckEmail = db.Admins.FirstOrDefault(a => a.Email == _Email);
                if (_CheckEmail != null)
                    return Content("<script>alert('Email đã có người sử dụng!');window.location='/Admin/CreateAdmin';</script>");
                else
                    ad.Email = _Email;

                ad.HoTen = _HoTen;
                ad.DiaChi = _DiaChi;
                ad.DienThoai = _DienThoai;
                ad.CMND = _Cmnd;
                ad.NgaySinh = Convert.ToDateTime(_NgaySinh);

                if (_GioiTinh == 0)//Ở đây không thể Convert sang kiểu Bool nên gán điều kiện
                    ad.GioiTinh = false;
                else
                    ad.GioiTinh = true;

                ad.NgayDangKy = DateTime.Now;

                //Khai báo _FileUpload ở <input type="file" id="_FileUpload" name="_FileUpload" /> trên Form CreateAdmin
                HttpPostedFileBase _FileUpload = Request.Files["_FileUpload"];
                if (_FileUpload != null && _FileUpload.ContentLength > 0)//Kiểm tra đã chọn 1 file Upload để thực hiện tiếp
                {
                    //khai báo biến _FileName là tên File
                    string _FileName = Path.GetFileName(_FileUpload.FileName);

                    //Khai báo biến _Path là đường dẫn Upload File
                    string _Path = Path.Combine(Server.MapPath("~/Content/Images/Upload/"), _FileName);

                    //Kiểm tra chỉ cho Upload File có kính thước < 1 MB
                    if (_FileUpload.ContentLength > 1 * 1024 * 1024)
                    {
                        return Content("<script>alert('Kích thước của tập tin không được vượt quá 1 MB!');window.location='/Admin/CreateAdmin';</script>");
                    }

                    //Ngoài hạn chế dung lượng File Upload lên Server thì quan trọng nhất là chỉ cho phép User Upload được dạng File ảnh lên
                    //Vì nếu cho Upload được tất cả các File thì User có thể Upload File Backdoor, Shell lên Server dẫn đến Site bị hacker tấn công

                    //Khai báo mảng chứa các đuôi file hợp lệ cho Upload
                    var _DuoiFile = new[] { "jpg", "jpeg", "png", "gif" };

                    //Khai báo biến _FileExt: trong đó GetExtension là lấy phần mở rộng (đuôi File), Substring(1): lấy từ vị trí thứ nhất => Tức sẽ lấy ra đuôi File
                    var _FileExt = Path.GetExtension(_FileUpload.FileName).Substring(1);

                    //Kiểm tra trong mảng _DuoiFile KHÔNG chứa phần đuôi file của tập tin User upload lên
                    if (!_DuoiFile.Contains(_FileExt))
                    {
                        return Content("<script>alert('Bảo mật Website! Chỉ được Upload tập tin hình ảnh dạng (.jpg, .jpeg, .png, .gif)!');window.location='/Admin/CreateAdmin';</script>");
                    }

                    //Thực thi Upload tập tin lên Server
                    _FileUpload.SaveAs(_Path);

                    //Gán giá trị Avatar là đường dẫn của tập tin vừa Upload để Update trong Database
                    ad.Avatar = "/Content/Images/Upload/" + _FileName;
                }
                else
                {
                    //Ở đây, mặc định trong CSDL khi Thêm mới thì Avatar Admin đã mặc định sẵn là "/Content/Images/Upload/Avatar_Admin.jpg"
                    //Nhưng thêm trên Form xuống CSDL nếu KHÔNG chọn Avatar thì nó gán là Null => không có Avatar và Session["Avatar"] cũng = Null
                    //Session["Avatar"] = Null => sẽ bị lỗi ở _PHeader (K có ảnh Avatar để Show lên)
                    //Nên nếu khách hàng không chọn Avatar ta sẽ Lưu Url Avatar mặc định cho khách hàng.
                    ad.Avatar = "/Content/Images/Upload/Avatar_Admin.jpg";
                }

                ad.AnHien = true;//Mặc định cho tài khoản là Hiện

                //Thực hiện thêm mới vào bảng Admin
                db.Admins.InsertOnSubmit(ad);
                db.SubmitChanges();

                //=====================================================================
                //Sau khi đã thêm mới tài khoản thì tiếp tục thêm mới cho bảng phân quyền
                pq.MaAdmin = ad.MaAdmin;//Lấy mã Admin vừa tạo mới

                //Gán các giá trị phân quyền trên Form vào bảng phân quyền
                if (collection["ckb_PhanQuyen1"] == "DaChon")
                    pq.PQ_Menu = true;
                else
                    pq.PQ_Menu = false;

                if (collection["ckb_PhanQuyen2"] == "DaChon")
                    pq.PQ_Slider = true;
                else
                    pq.PQ_Slider = false;

                if (collection["ckb_PhanQuyen3"] == "DaChon")
                    pq.PQ_NhaSanXuat = true;
                else
                    pq.PQ_NhaSanXuat = false;

                if (collection["ckb_PhanQuyen4"] == "DaChon")
                    pq.PQ_SanPham = true;
                else
                    pq.PQ_SanPham = false;

                if (collection["ckb_PhanQuyen5"] == "DaChon")
                    pq.PQ_KhachHang = true;
                else
                    pq.PQ_KhachHang = false;

                if (collection["ckb_PhanQuyen6"] == "DaChon")
                    pq.PQ_DonHang = true;
                else
                    pq.PQ_DonHang = false;

                if (collection["ckb_PhanQuyen7"] == "DaChon")
                    pq.PQ_TinTuc = true;
                else
                    pq.PQ_TinTuc = false;

                if (collection["ckb_PhanQuyen8"] == "DaChon")
                    pq.PQ_QuangCao = true;
                else
                    pq.PQ_QuangCao = false;

                if (collection["ckb_PhanQuyen9"] == "DaChon")
                    pq.PQ_LienHe = true;
                else
                    pq.PQ_LienHe = false;

                if (collection["ckb_PhanQuyen10"] == "DaChon")
                    pq.PQ_GioiThieu = true;
                else
                    pq.PQ_GioiThieu = false;

                if (collection["ckb_PhanQuyen11"] == "DaChon")
                    pq.PQ_QuanTriAdmin = true;
                else
                    pq.PQ_QuanTriAdmin = false;

                if (collection["ckb_PhanQuyen12"] == "DaChon")
                    pq.PQ_SiteMap = true;
                else
                    pq.PQ_SiteMap = false;

                //Thực hiện thêm mới vào bảng Phân quyền
                db.PhanQuyen_Admins.InsertOnSubmit(pq);
                db.SubmitChanges();

                return Content("<script>alert('Thêm mới tài khoản quản trị thành công !');window.location='/Admin/ListAdmin';</script>");
            }
            catch
            {
                return Content("<script>alert('Đăng ký thất bại.Vui lòng kiểm tra Ngày/Tháng/Năm sinh đã hợp lệ chưa?!');window.location='/Admin/CreateAdmin';</script>");
            }
        }
        #endregion

        #region UpdateTrangThai
        //Hàm khóa hoặc mở khóa tài khoản Admin (ở đây sử dụng hàm void để Response.Write hình update lại)
        [HttpPost]
        public void UpdateTrangThai(int id)
        {
            //Lấy ra tài khoản Admin cần Update Ẩn Hiện
            var _AD = (from ad in db.Admins where ad.MaAdmin == id select ad).SingleOrDefault();

            //Tạo chuỗi _Hinh để chứa đường dẫn hình Ẩn Hiện khi Update lại
            string _Hinh = "";

            //Ẩn thì cập nhật lại thành hiện và ngược lại
            if (_AD.AnHien == true)
            {
                _AD.AnHien = false;
                _Hinh = "/Content/Admin/Images/icon_An.png";
            }
            else
            {
                _AD.AnHien = true;
                _Hinh = "/Content/Admin/Images/icon_Hien.png";
            }

            //Lưu chỉnh sửa
            UpdateModel(_AD);
            db.SubmitChanges();

            //Xuất ra (Trả về) đường dẫn hình để Update lại trên Form
            Response.Write(_Hinh);
        }
        #endregion

        #region UpdatePQ_Menu
        //Hàm Update Phân quyền cho quản trị:(ở đây sử dụng hàm void để Response.Write hình update lại)
        [HttpPost]
        public void UpdatePQ_Menu(int id)
        {
            //Lấy ra tài khoản Admin trong bảng phân quyền cần Update quyền quản trị
            var _PQ = (from ad in db.PhanQuyen_Admins where ad.MaAdmin == id select ad).SingleOrDefault();

            //Tạo chuỗi _Hinh để chứa đường dẫn hình Ẩn Hiện khi Update lại
            string _Hinh = "";

            //Không có quyền  thì cập nhật lại thành có và ngược lại
            if (_PQ.PQ_Menu == true)
            {
                _PQ.PQ_Menu = false;
                _Hinh = "/Content/Admin/Images/block.png";
            }
            else
            {
                _PQ.PQ_Menu = true;
                _Hinh = "/Content/Admin/Images/accept.png";
            }

            //Lưu chỉnh sửa
            UpdateModel(_PQ);
            db.SubmitChanges();

            //Xuất ra (Trả về) đường dẫn hình để Update lại trên Form
            Response.Write(_Hinh);
        }
        #endregion

        #region UpdatePQ_Slider
        //Hàm Update Phân quyền cho quản trị:(ở đây sử dụng hàm void để Response.Write hình update lại)
        [HttpPost]
        public void UpdatePQ_Slider(int id)
        {
            //Lấy ra tài khoản Admin trong bảng phân quyền cần Update quyền quản trị
            var _PQ = (from ad in db.PhanQuyen_Admins where ad.MaAdmin == id select ad).SingleOrDefault();

            //Tạo chuỗi _Hinh để chứa đường dẫn hình Ẩn Hiện khi Update lại
            string _Hinh = "";

            //Không có quyền  thì cập nhật lại thành có và ngược lại
            if (_PQ.PQ_Slider == true)
            {
                _PQ.PQ_Slider = false;
                _Hinh = "/Content/Admin/Images/block.png";
            }
            else
            {
                _PQ.PQ_Slider = true;
                _Hinh = "/Content/Admin/Images/accept.png";
            }

            //Lưu chỉnh sửa
            UpdateModel(_PQ);
            db.SubmitChanges();

            //Xuất ra (Trả về) đường dẫn hình để Update lại trên Form
            Response.Write(_Hinh);
        }
        #endregion

        #region UpdatePQ_NhaSanXuat
        //Hàm Update Phân quyền cho quản trị:(ở đây sử dụng hàm void để Response.Write hình update lại)
        [HttpPost]
        public void UpdatePQ_NhaSanXuat(int id)
        {
            //Lấy ra tài khoản Admin trong bảng phân quyền cần Update quyền quản trị
            var _PQ = (from ad in db.PhanQuyen_Admins where ad.MaAdmin == id select ad).SingleOrDefault();

            //Tạo chuỗi _Hinh để chứa đường dẫn hình Ẩn Hiện khi Update lại
            string _Hinh = "";

            //Không có quyền  thì cập nhật lại thành có và ngược lại
            if (_PQ.PQ_NhaSanXuat == true)
            {
                _PQ.PQ_NhaSanXuat = false;
                _Hinh = "/Content/Admin/Images/block.png";
            }
            else
            {
                _PQ.PQ_NhaSanXuat = true;
                _Hinh = "/Content/Admin/Images/accept.png";
            }

            //Lưu chỉnh sửa
            UpdateModel(_PQ);
            db.SubmitChanges();

            //Xuất ra (Trả về) đường dẫn hình để Update lại trên Form
            Response.Write(_Hinh);
        }
        #endregion

        #region UpdatePQ_SanPham
        //Hàm Update Phân quyền cho quản trị:(ở đây sử dụng hàm void để Response.Write hình update lại)
        [HttpPost]
        public void UpdatePQ_SanPham(int id)
        {
            //Lấy ra tài khoản Admin trong bảng phân quyền cần Update quyền quản trị
            var _PQ = (from ad in db.PhanQuyen_Admins where ad.MaAdmin == id select ad).SingleOrDefault();

            //Tạo chuỗi _Hinh để chứa đường dẫn hình Ẩn Hiện khi Update lại
            string _Hinh = "";

            //Không có quyền thì cập nhật lại thành có và ngược lại
            if (_PQ.PQ_SanPham == true)
            {
                _PQ.PQ_SanPham = false;
                _Hinh = "/Content/Admin/Images/block.png";
            }
            else
            {
                _PQ.PQ_SanPham = true;
                _Hinh = "/Content/Admin/Images/accept.png";
            }

            //Lưu chỉnh sửa
            UpdateModel(_PQ);
            db.SubmitChanges();

            //Xuất ra (Trả về) đường dẫn hình để Update lại trên Form
            Response.Write(_Hinh);
        }
        #endregion

        #region UpdatePQ_KhachHang
        //Hàm Update Phân quyền cho quản trị:(ở đây sử dụng hàm void để Response.Write hình update lại)
        [HttpPost]
        public void UpdatePQ_KhachHang(int id)
        {
            //Lấy ra tài khoản Admin trong bảng phân quyền cần Update quyền quản trị
            var _PQ = (from ad in db.PhanQuyen_Admins where ad.MaAdmin == id select ad).SingleOrDefault();

            //Tạo chuỗi _Hinh để chứa đường dẫn hình Ẩn Hiện khi Update lại
            string _Hinh = "";

            //Không có quyền thì cập nhật lại thành có và ngược lại
            if (_PQ.PQ_KhachHang == true)
            {
                _PQ.PQ_KhachHang = false;
                _Hinh = "/Content/Admin/Images/block.png";
            }
            else
            {
                _PQ.PQ_KhachHang = true;
                _Hinh = "/Content/Admin/Images/accept.png";
            }

            //Lưu chỉnh sửa
            UpdateModel(_PQ);
            db.SubmitChanges();

            //Xuất ra (Trả về) đường dẫn hình để Update lại trên Form
            Response.Write(_Hinh);
        }
        #endregion

        #region UpdatePQ_DonHang
        //Hàm Update Phân quyền cho quản trị:(ở đây sử dụng hàm void để Response.Write hình update lại)
        [HttpPost]
        public void UpdatePQ_DonHang(int id)
        {
            //Lấy ra tài khoản Admin trong bảng phân quyền cần Update quyền quản trị
            var _PQ = (from ad in db.PhanQuyen_Admins where ad.MaAdmin == id select ad).SingleOrDefault();

            //Tạo chuỗi _Hinh để chứa đường dẫn hình Ẩn Hiện khi Update lại
            string _Hinh = "";

            //Không có quyền thì cập nhật lại thành có và ngược lại
            if (_PQ.PQ_DonHang == true)
            {
                _PQ.PQ_DonHang = false;
                _Hinh = "/Content/Admin/Images/block.png";
            }
            else
            {
                _PQ.PQ_DonHang = true;
                _Hinh = "/Content/Admin/Images/accept.png";
            }

            //Lưu chỉnh sửa
            UpdateModel(_PQ);
            db.SubmitChanges();

            //Xuất ra (Trả về) đường dẫn hình để Update lại trên Form
            Response.Write(_Hinh);
        }
        #endregion

        #region UpdatePQ_TinTuc
        //Hàm Update Phân quyền cho quản trị:(ở đây sử dụng hàm void để Response.Write hình update lại)
        [HttpPost]
        public void UpdatePQ_TinTuc(int id)
        {
            //Lấy ra tài khoản Admin trong bảng phân quyền cần Update quyền quản trị
            var _PQ = (from ad in db.PhanQuyen_Admins where ad.MaAdmin == id select ad).SingleOrDefault();

            //Tạo chuỗi _Hinh để chứa đường dẫn hình Ẩn Hiện khi Update lại
            string _Hinh = "";

            //Không có quyền thì cập nhật lại thành có và ngược lại
            if (_PQ.PQ_TinTuc == true)
            {
                _PQ.PQ_TinTuc = false;
                _Hinh = "/Content/Admin/Images/block.png";
            }
            else
            {
                _PQ.PQ_TinTuc = true;
                _Hinh = "/Content/Admin/Images/accept.png";
            }

            //Lưu chỉnh sửa
            UpdateModel(_PQ);
            db.SubmitChanges();

            //Xuất ra (Trả về) đường dẫn hình để Update lại trên Form
            Response.Write(_Hinh);
        }
        #endregion

        #region UpdatePQ_QuangCao
        //Hàm Update Phân quyền cho quản trị:(ở đây sử dụng hàm void để Response.Write hình update lại)
        [HttpPost]
        public void UpdatePQ_QuangCao(int id)
        {
            //Lấy ra tài khoản Admin trong bảng phân quyền cần Update quyền quản trị
            var _PQ = (from ad in db.PhanQuyen_Admins where ad.MaAdmin == id select ad).SingleOrDefault();

            //Tạo chuỗi _Hinh để chứa đường dẫn hình Ẩn Hiện khi Update lại
            string _Hinh = "";

            //Không có quyền thì cập nhật lại thành có và ngược lại
            if (_PQ.PQ_QuangCao == true)
            {
                _PQ.PQ_QuangCao = false;
                _Hinh = "/Content/Admin/Images/block.png";
            }
            else
            {
                _PQ.PQ_QuangCao = true;
                _Hinh = "/Content/Admin/Images/accept.png";
            }

            //Lưu chỉnh sửa
            UpdateModel(_PQ);
            db.SubmitChanges();

            //Xuất ra (Trả về) đường dẫn hình để Update lại trên Form
            Response.Write(_Hinh);
        }
        #endregion

        #region UpdatePQ_LienHe
        //Hàm Update Phân quyền cho quản trị:(ở đây sử dụng hàm void để Response.Write hình update lại)
        [HttpPost]
        public void UpdatePQ_LienHe(int id)
        {
            //Lấy ra tài khoản Admin trong bảng phân quyền cần Update quyền quản trị
            var _PQ = (from ad in db.PhanQuyen_Admins where ad.MaAdmin == id select ad).SingleOrDefault();

            //Tạo chuỗi _Hinh để chứa đường dẫn hình Ẩn Hiện khi Update lại
            string _Hinh = "";

            //Không có quyền thì cập nhật lại thành có và ngược lại
            if (_PQ.PQ_LienHe == true)
            {
                _PQ.PQ_LienHe = false;
                _Hinh = "/Content/Admin/Images/block.png";
            }
            else
            {
                _PQ.PQ_LienHe = true;
                _Hinh = "/Content/Admin/Images/accept.png";
            }

            //Lưu chỉnh sửa
            UpdateModel(_PQ);
            db.SubmitChanges();

            //Xuất ra (Trả về) đường dẫn hình để Update lại trên Form
            Response.Write(_Hinh);
        }
        #endregion

        #region UpdatePQ_GioiThieu
        //Hàm Update Phân quyền cho quản trị:(ở đây sử dụng hàm void để Response.Write hình update lại)
        [HttpPost]
        public void UpdatePQ_GioiThieu(int id)
        {
            //Lấy ra tài khoản Admin trong bảng phân quyền cần Update quyền quản trị
            var _PQ = (from ad in db.PhanQuyen_Admins where ad.MaAdmin == id select ad).SingleOrDefault();

            //Tạo chuỗi _Hinh để chứa đường dẫn hình Ẩn Hiện khi Update lại
            string _Hinh = "";

            //Không có quyền thì cập nhật lại thành có và ngược lại
            if (_PQ.PQ_GioiThieu == true)
            {
                _PQ.PQ_GioiThieu = false;
                _Hinh = "/Content/Admin/Images/block.png";
            }
            else
            {
                _PQ.PQ_GioiThieu = true;
                _Hinh = "/Content/Admin/Images/accept.png";
            }

            //Lưu chỉnh sửa
            UpdateModel(_PQ);
            db.SubmitChanges();

            //Xuất ra (Trả về) đường dẫn hình để Update lại trên Form
            Response.Write(_Hinh);
        }
        #endregion

        #region UpdatePQ_QuanTriAdmin
        //Hàm Update Phân quyền cho quản trị:(ở đây sử dụng hàm void để Response.Write hình update lại)
        [HttpPost]
        public void UpdatePQ_QuanTriAdmin(int id)
        {
            //Lấy ra tài khoản Admin trong bảng phân quyền cần Update quyền quản trị
            var _PQ = (from ad in db.PhanQuyen_Admins where ad.MaAdmin == id select ad).SingleOrDefault();

            //Tạo chuỗi _Hinh để chứa đường dẫn hình Ẩn Hiện khi Update lại
            string _Hinh = "";

            //Không có quyền thì cập nhật lại thành có và ngược lại
            if (_PQ.PQ_QuanTriAdmin == true)
            {
                _PQ.PQ_QuanTriAdmin = false;
                _Hinh = "/Content/Admin/Images/block.png";
            }
            else
            {
                _PQ.PQ_QuanTriAdmin = true;
                _Hinh = "/Content/Admin/Images/accept.png";
            }

            //Lưu chỉnh sửa
            UpdateModel(_PQ);
            db.SubmitChanges();

            //Xuất ra (Trả về) đường dẫn hình để Update lại trên Form
            Response.Write(_Hinh);
        }
        #endregion

        #region UpdatePQ_SiteMap
        //Hàm Update Phân quyền cho quản trị:(ở đây sử dụng hàm void để Response.Write hình update lại)
        [HttpPost]
        public void UpdatePQ_SiteMap(int id)
        {
            //Lấy ra tài khoản Admin trong bảng phân quyền cần Update quyền quản trị
            var _PQ = (from ad in db.PhanQuyen_Admins where ad.MaAdmin == id select ad).SingleOrDefault();

            //Tạo chuỗi _Hinh để chứa đường dẫn hình Ẩn Hiện khi Update lại
            string _Hinh = "";

            //Không có quyền thì cập nhật lại thành có và ngược lại
            if (_PQ.PQ_SiteMap == true)
            {
                _PQ.PQ_SiteMap = false;
                _Hinh = "/Content/Admin/Images/block.png";
            }
            else
            {
                _PQ.PQ_SiteMap = true;
                _Hinh = "/Content/Admin/Images/accept.png";
            }

            //Lưu chỉnh sửa
            UpdateModel(_PQ);
            db.SubmitChanges();

            //Xuất ra (Trả về) đường dẫn hình để Update lại trên Form
            Response.Write(_Hinh);
        }
        #endregion
        #endregion

        #region Bảng SiteMap (SiteMap và Update)
        public ActionResult SiteMap()
        {
            if (Session["Username_Admin"] == null)//Chưa đăng nhập => Login
                return RedirectToAction("Login");
            else
                if (bool.Parse(Session["PQ_SiteMap"].ToString()) == false)//Không đủ quyền hạn vào ku vực này => thông báo
                    return Content("<script>alert('Bạn không đủ quyền hạn vào khu vực quản trị SiteMap !');window.location='/Admin/';</script>");

            //Lấy ra thông tin SiteMap, ở đây chỉ 1 hàng dữ liệu nên gán cứng MaTT=1
            var _TT = db.ThongTins.First(t => t.MaTT == 1);
            return View(_TT);
        }

        [HttpPost]
        [ValidateInput(false)]//Cho phép nhập kiểu HTML
        public ActionResult SiteMap(FormCollection collection)
        {
            try
            {
                //Lấy giá trị ở Form SiteMap
                string _SiteMap = collection["txt_SiteMap"];
                var _TT = db.ThongTins.First(t => t.MaTT == 1);//Lấy ra thông tin bản đồ website, ở đây chỉ 1 hàng dữ liệu nên gán cứng MaTT=1

                //Bắt lỗi không cho Site map trống
                if (_SiteMap == "")
                    return Content("<script>alert('Vui lòng code SiteMap cho bản đồ Website!');window.location='/Admin/SiteMap';</script>");

                //Gán giá trị để chỉnh sửa
                _TT.sitemap = _SiteMap;

                //Thực hiện chỉnh sửa
                UpdateModel(_TT);
                db.SubmitChanges();
                return Content("<script>alert('Cập nhật Site map thành công !');window.location='/Admin/SiteMap';</script>");
            }
            catch
            {
                return Content("<script>alert('Lỗi hệ thông!');window.location='/Admin/About';</script>");
            }
        }
        #endregion
    }
}
