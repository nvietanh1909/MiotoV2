using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

public class DrivingLicenseController : Controller
{
    private readonly HttpClient _apiService;

    public DrivingLicenseController()
    {
        _apiService = new HttpClient();
    }

    [HttpPost]
    public async Task<ActionResult> CheckLicense(string session, string donViXuLy, string ngayNhan1, string ngayNhan2)
    {
        string apiUrl = "ADAPTER_URL/mapi/g";

        var requestBody = new
        {
            session = session,
            service = "DanhSachHoSo",
            donViXuLy = donViXuLy,
            ngayNhan1 = ngayNhan1,
            ngayNhan2 = ngayNhan2
        };

        var jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);

        var httpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        _apiService.DefaultRequestHeaders.Clear();
        _apiService.DefaultRequestHeaders.Add("Charset", "utf-8");

        try
        {
            var response = await _apiService.PostAsync(apiUrl, httpContent);

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();

                return Content(responseData); 
            }
            else
            {
                return Content($"Error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            return Content($"Exception: {ex.Message}");
        }
    }
}
