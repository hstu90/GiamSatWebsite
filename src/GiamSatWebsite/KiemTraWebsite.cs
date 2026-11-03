using System.Diagnostics;

record KetQuaKiemTra(
    string MaDinhDanh,
    bool HoatDong,
    int? MaTrangThaiHttp,
    long ThoiGianPhanHoiMs,
    string? LoaiLoi,
    int SoLanThu);

class KiemTraWebsite
{
    private const int ThoiGianChoGiay = 15;
    private const int SoLanThuLai = 2;
    private const int ThoiGianNghiGiay = 3;

    private readonly HttpClient MayKhachHttp;

    public KiemTraWebsite()
    {
        MayKhachHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(ThoiGianChoGiay) };
        MayKhachHttp.DefaultRequestHeaders.UserAgent.ParseAdd("GiamSatWebsite/1.0");
    }

    // Kiem tra tat ca website song song
    public Task<KetQuaKiemTra[]> KiemTraTatCaAsync(List<WebsiteCauHinh> DanhSach)
        => Task.WhenAll(DanhSach.Select(KiemTraMotWebsiteAsync));

    // Thu toi da (1 + SoLanThuLai) lan, chi ket luan loi khi tat ca cac lan deu loi
    private async Task<KetQuaKiemTra> KiemTraMotWebsiteAsync(WebsiteCauHinh Website)
    {
        for (var LanThu = 1; ; LanThu++)
        {
            var KetQua = await ThuMotLanAsync(Website.Url);

            if (KetQua.LoaiLoi is null || LanThu > SoLanThuLai)
            {
                return new KetQuaKiemTra(
                    Website.MaDinhDanh,
                    KetQua.LoaiLoi is null,
                    KetQua.MaTrangThai,
                    KetQua.ThoiGianMs,
                    KetQua.LoaiLoi,
                    LanThu);
            }

            await Task.Delay(TimeSpan.FromSeconds(ThoiGianNghiGiay));
        }
    }

    // Mot lan goi HTTP. Chi tra ve ma loai loi, khong tra ve Exception.Message (tranh lo domain trong log cong khai)
    private async Task<(int? MaTrangThai, string? LoaiLoi, long ThoiGianMs)> ThuMotLanAsync(string Url)
    {
        var DongHo = Stopwatch.StartNew();
        try
        {
            using var PhanHoi = await MayKhachHttp.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead);
            var Ma = (int)PhanHoi.StatusCode;
            var LoaiLoi = Ma is >= 200 and < 400 ? null : $"Http{Ma}";
            return (Ma, LoaiLoi, DongHo.ElapsedMilliseconds);
        }
        catch (TaskCanceledException)
        {
            // Khong co CancellationToken ben ngoai nen day chinh la timeout cua HttpClient
            return (null, "HetThoiGian", DongHo.ElapsedMilliseconds);
        }
        catch (HttpRequestException Loi)
        {
            return (null, PhanLoaiLoi(Loi.HttpRequestError), DongHo.ElapsedMilliseconds);
        }
        catch (Exception Loi) when (Loi is InvalidOperationException or UriFormatException)
        {
            return (null, "UrlKhongHopLe", DongHo.ElapsedMilliseconds);
        }
    }

    private static string PhanLoaiLoi(HttpRequestError Loi) => Loi switch
    {
        HttpRequestError.NameResolutionError => "LoiDns",
        HttpRequestError.ConnectionError => "KhongKetNoi",
        HttpRequestError.SecureConnectionError => "LoiSsl",
        _ => "LoiKhac"
    };
}