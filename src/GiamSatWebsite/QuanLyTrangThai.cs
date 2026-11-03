using System.Text.Json;

record TrangThaiWebsite(bool HoatDong, DateTime ThoiDiemDoiTrangThai);

record ThayDoiTrangThai(KetQuaKiemTra KetQua, TimeSpan ThoiGianTrangThaiTruoc);

class DuLieuTrangThai
{
    public DateTime LanGhiCuoi { get; set; }
    public Dictionary<string, TrangThaiWebsite> DanhSachWebsite { get; set; } = new();
}

class QuanLyTrangThai
{
    private const int SoGioGhiDinhKy = 24;
    private static readonly JsonSerializerOptions TuyChonJson = new() { WriteIndented = true };

    private readonly string DuongDanTep;
    private readonly DuLieuTrangThai DuLieuCu;

    public QuanLyTrangThai(string DuongDanTep)
    {
        this.DuongDanTep = DuongDanTep;
        DuLieuCu = File.Exists(DuongDanTep)
            ? JsonSerializer.Deserialize<DuLieuTrangThai>(File.ReadAllText(DuongDanTep)) ?? new DuLieuTrangThai()
            : new DuLieuTrangThai();
    }

    // So sanh ket qua moi voi trang thai cu, tra ve cac website doi trang thai.
    // Chi ghi tep khi noi dung doi hoac da qua SoGioGhiDinhKy gio (de repo luon co hoat dong)
    public List<ThayDoiTrangThai> CapNhatVaGhi(KetQuaKiemTra[] KetQuaTatCa)
    {
        var BayGio = DateTime.UtcNow;
        BayGio = new DateTime(BayGio.Ticks - BayGio.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);

        var DuLieuMoi = new DuLieuTrangThai();
        var DanhSachThayDoi = new List<ThayDoiTrangThai>();

        foreach (var KetQua in KetQuaTatCa)
        {
            var CoTrangThaiCu = DuLieuCu.DanhSachWebsite.TryGetValue(KetQua.MaDinhDanh, out var TrangThaiCu);

            // Cung trang thai: giu nguyen ban ghi cu
            if (CoTrangThaiCu && TrangThaiCu!.HoatDong == KetQua.HoatDong)
            {
                DuLieuMoi.DanhSachWebsite[KetQua.MaDinhDanh] = TrangThaiCu!;
                continue;
            }

            DuLieuMoi.DanhSachWebsite[KetQua.MaDinhDanh] = new TrangThaiWebsite(KetQua.HoatDong, BayGio);

            // Website moi chua co trang thai cu: xem nhu truoc do dang UP, chi bao khi dang loi
            if (CoTrangThaiCu)
                DanhSachThayDoi.Add(new ThayDoiTrangThai(KetQua, BayGio - TrangThaiCu!.ThoiDiemDoiTrangThai));
            else if (!KetQua.HoatDong)
                DanhSachThayDoi.Add(new ThayDoiTrangThai(KetQua, TimeSpan.Zero));
        }

        var CoThayDoiNoiDung =
            JsonSerializer.Serialize(DuLieuCu.DanhSachWebsite, TuyChonJson) !=
            JsonSerializer.Serialize(DuLieuMoi.DanhSachWebsite, TuyChonJson);
        var DenHanGhiDinhKy = BayGio - DuLieuCu.LanGhiCuoi >= TimeSpan.FromHours(SoGioGhiDinhKy);

        if (CoThayDoiNoiDung || DenHanGhiDinhKy)
        {
            DuLieuMoi.LanGhiCuoi = BayGio;
            File.WriteAllText(DuongDanTep, JsonSerializer.Serialize(DuLieuMoi, TuyChonJson));
        }

        return DanhSachThayDoi;
    }
}