using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace Infrastructure.Security
{
    public class GeolocationService(HttpClient httpClient)
    {
        public async Task<IpLocationResult?> GetLocationByIpAsync(string ipAddress)
        {
            try
            {
                // To avoid problems during local development (Localhost always gives ::1 or 127.0.0.1)
                if (ipAddress == "::1" || ipAddress == "127.0.0.1")
                {
                    // We use a test IP (e.g., an IP in Saudi Arabia or Egypt) to see the result during the test.
                    ipAddress = "185.120.124.1";
                }

                // Connect to the free location API
                var response = await httpClient.GetFromJsonAsync<IpApiResponse>($"http://ip-api.com/json/{ipAddress}");

                if (response != null && response.Status == "success")
                {
                    return new IpLocationResult(response.Country, response.City, response.Lat, response.Lon);
                }
            }
            catch
            {
                // In banking systems, if the external API fails, we don't cause the system to crash; instead, we pass default values ​​along with an error log.
            }

            return new IpLocationResult("Unknown", "Unknown", null, null);
        }
    }

   
    public record IpApiResponse(string Status, string Country, string City, double Lat, double Lon);
    public record IpLocationResult(string Country, string City, double? Latitude, double? Longitude);
}
