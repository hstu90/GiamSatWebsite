using System.Net;
using System.Net.Http.Json;
using System.Text;

class ThongBaoTelegram
{
    private const int SoLanThuToiDa = 3;
    private const int ThoiGianNghiGiay = 3;

    private readonly HttpClient MayKhachHttp = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly string MaBot;
    private readonly string MaCuocTroChuyen;

    public ThongBaoTelegram(string MaBot, string MaCuocTroChuyen)
    {
        this.MaBot = MaBot;
        this.MaCuocTroChuyen = MaCuocTroChuyen;
    }

    // Gop tat ca website doi trang thai trong luot nay thanh 1 tin
    public static string TaoNoiDung(List<ThayDoiTrangThai> DanhSach, Dictionary<string, WebsiteCauHinh> TheoMa)
    {
        var BayGioViet = DateTime.UtcNow.AddHours(7);
        var NoiDung = new StringBuilder();
        NoiDung.AppendLine($"<b>GiamSatWebsite</b> - {BayGioViet:dd/MM/yyyy HH:mm} (GMT+7)");

        foreach (var ThayDoi in DanhSach)
        {
            var KetQua = ThayDoi.KetQua;
            var Website = TheoMa[KetQua.MaDinhDanh];
            var Ten = WebUtility.HtmlEncode(Website.TenWebsite);
            var Url = WebUtility.HtmlEncode(Website.Url);

            NoiDung.AppendLine();
            if (KetQua.HoatDong)
            {
                NoiDung.AppendLine($"🟢 <b>PHỤC HỒI</b>: {Ten}");
                NoiDung.AppendLine(Url);
                NoiDung.AppendLine($"Đã ngừng hoạt động {DinhDangThoiGian(ThayDoi.ThoiGianTrangThaiTruoc)}, phản hồi {KetQua.ThoiGianPhanHoiMs}ms");
            }
            else
            {
                NoiDung.AppendLine($"🔴 <b>NGỪNG HOẠT ĐỘNG</b>: {Ten}");
                NoiDung.AppendLine(Url);
                NoiDung.AppendLine($"Lỗi: {KetQua.LoaiLoi} | Http: {KetQua.MaTrangThaiHttp?.ToString() ?? "-"} | đã thử {KetQua.SoLanThu} lần");
            }
        }

        return NoiDung.ToString();
    }

    // Tra ve true neu gui thanh cong. Khong in noi dung tin, token hay chat id ra log
    public async Task<bool> GuiAsync(string NoiDung)
    {
        var DuongDan = $"https://api.telegram.org/bot{MaBot}/sendMessage";
        var DuLieuGui = new
        {
            chat_id = MaCuocTroChuyen,
            text = NoiDung,
            parse_mode = "HTML",
            disable_web_page_preview = true
        };

        for (var LanThu = 1; LanThu <= SoLanThuToiDa; LanThu++)
        {
            try
            {
                using var PhanHoi = await MayKhachHttp.PostAsJsonAsync(DuongDan, DuLieuGui);
                if (PhanHoi.IsSuccessStatusCode) return true;

                var Ma = (int)PhanHoi.StatusCode;
                Console.WriteLine($"Telegram tra ve Http{Ma} (lan {LanThu})");

                // 4xx (tru 429) la loi token/chat id/noi dung, thu lai khong co y nghia
                if (Ma is >= 400 and < 500 and not 429) return false;
            }
            catch (Exception Loi) when (Loi is HttpRequestException or TaskCanceledException)
            {
                // Khong in Loi.Message vi co the chua duong dan co token
                Console.WriteLine($"Telegram loi ket noi (lan {LanThu})");
            }

            if (LanThu < SoLanThuToiDa)
                await Task.Delay(TimeSpan.FromSeconds(ThoiGianNghiGiay));
        }

        return false;
    }

    private static string DinhDangThoiGian(TimeSpan ThoiGian) =>
        ThoiGian.TotalHours >= 1
            ? $"{(int)ThoiGian.TotalHours} giờ {ThoiGian.Minutes} phút"
            : $"{(int)ThoiGian.TotalMinutes} phút";
}