using System.Text.Json;

// Doc danh sach website tu GitHub Secret
var chuoiJson = Environment.GetEnvironmentVariable("DANH_SACH_WEBSITE");
if (string.IsNullOrWhiteSpace(chuoiJson))
{
    Console.Error.WriteLine("Thieu secret DANH_SACH_WEBSITE.");
    return 1;
}

List<WebsiteCauHinh>? danhSach;
try
{
    danhSach = JsonSerializer.Deserialize<List<WebsiteCauHinh>>(
        chuoiJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
}
catch (JsonException)
{
    // Khong in chi tiet loi vi co the chua noi dung URL
    Console.Error.WriteLine("DANH_SACH_WEBSITE khong phai JSON hop le.");
    return 1;
}

if (danhSach is null || danhSach.Count == 0)
{
    Console.Error.WriteLine("DANH_SACH_WEBSITE rong.");
    return 1;
}

// Che URL va ten trong log cong khai cua Actions
foreach (var website in danhSach)
{
    Console.WriteLine($"::add-mask::{website.Url}");
    Console.WriteLine($"::add-mask::{website.TenWebsite}");
}

// Log chi ghi ma dinh danh, khong ghi URL/ten
Console.WriteLine($"Da doc {danhSach.Count} website tu secret.");
foreach (var website in danhSach)
{
    Console.WriteLine($"{website.MaDinhDanh}: da nap");
}

return 0;

record WebsiteCauHinh(string MaDinhDanh, string TenWebsite, string Url);