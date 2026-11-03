using System.Text.Json;

// Doc danh sach website tu GitHub Secret
var ChuoiJson = Environment.GetEnvironmentVariable("DANH_SACH_WEBSITE");
if (string.IsNullOrWhiteSpace(ChuoiJson))
{
    Console.Error.WriteLine("Thieu secret DANH_SACH_WEBSITE.");
    return 1;
}

List<WebsiteCauHinh>? DanhSach;
try
{
    DanhSach = JsonSerializer.Deserialize<List<WebsiteCauHinh>>(
        ChuoiJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
}
catch (JsonException)
{
    // Khong in chi tiet loi vi co the chua noi dung URL
    Console.Error.WriteLine("DANH_SACH_WEBSITE khong phai JSON hop le.");
    return 1;
}

if (DanhSach is null || DanhSach.Count == 0)
{
    Console.Error.WriteLine("DANH_SACH_WEBSITE rong.");
    return 1;
}

// Che URL va ten trong log cong khai cua Actions
foreach (var Website in DanhSach)
{
    Console.WriteLine($"::add-mask::{Website.Url}");
    Console.WriteLine($"::add-mask::{Website.TenWebsite}");
}

Console.WriteLine($"Da doc {DanhSach.Count} website tu secret.");

var KetQuaTatCa = await new KiemTraWebsite().KiemTraTatCaAsync(DanhSach);

// Log chi ghi ma dinh danh va ma loai loi, khong ghi URL/ten
foreach (var KetQua in KetQuaTatCa)
{
    var TrangThai = KetQua.HoatDong ? "UP" : "DOWN";
    var MaHttp = KetQua.MaTrangThaiHttp?.ToString() ?? "-";
    Console.WriteLine(
        $"{KetQua.MaDinhDanh}: {TrangThai} | Http={MaHttp} | {KetQua.ThoiGianPhanHoiMs}ms | Loi={KetQua.LoaiLoi ?? "-"} | SoLanThu={KetQua.SoLanThu}");
}

var SoLoi = KetQuaTatCa.Count(Muc => !Muc.HoatDong);
Console.WriteLine($"Tong: {KetQuaTatCa.Length} website, {SoLoi} loi.");

// Cap nhat trang thai, ghi TrangThai.json khi can, in cac website doi trang thai
var DuongDanTrangThai = Environment.GetEnvironmentVariable("DUONG_DAN_TRANG_THAI") ?? "TrangThai.json";
var DanhSachThayDoi = new QuanLyTrangThai(DuongDanTrangThai).CapNhatVaGhi(KetQuaTatCa);

foreach (var ThayDoi in DanhSachThayDoi)
{
    var Nhan = ThayDoi.KetQua.HoatDong ? "PHUC HOI" : "NGUNG HOAT DONG";
    Console.WriteLine(
        $"{ThayDoi.KetQua.MaDinhDanh}: {Nhan} (trang thai truoc keo dai {(int)ThayDoi.ThoiGianTrangThaiTruoc.TotalMinutes} phut)");
}

// Gui thong bao Telegram: 1 tin gop cho cac website doi trang thai trong luot nay
if (DanhSachThayDoi.Count > 0)
{
    var MaBot = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
    var MaCuocTroChuyen = Environment.GetEnvironmentVariable("TELEGRAM_CHAT_ID");
    if (string.IsNullOrWhiteSpace(MaBot) || string.IsNullOrWhiteSpace(MaCuocTroChuyen))
    {
        Console.Error.WriteLine("Thieu secret TELEGRAM_BOT_TOKEN hoac TELEGRAM_CHAT_ID.");
        return 1;
    }

    var TheoMa = DanhSach.ToDictionary(Website => Website.MaDinhDanh);
    var NoiDungTin = ThongBaoTelegram.TaoNoiDung(DanhSachThayDoi, TheoMa);
    var DaGui = await new ThongBaoTelegram(MaBot, MaCuocTroChuyen).GuiAsync(NoiDungTin);

    if (!DaGui)
    {
        // Tra ve 1 de buoc luu TrangThai.json bi bo qua, lan sau se bao lai
        Console.Error.WriteLine("Gui Telegram that bai, khong luu trang thai de lan sau gui lai.");
        return 1;
    }

    Console.WriteLine("Da gui thong bao Telegram.");
}

// Luon tra 0 khi tool chay dung, ke ca khi co website DOWN
return 0;

record WebsiteCauHinh(string MaDinhDanh, string TenWebsite, string Url);