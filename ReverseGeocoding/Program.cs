using Microsoft.Extensions.Configuration;

namespace ReverseGeocoding
{
  class Program
  {
    static void Main(string[] args)
    {
      IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("jsconfig.json", true, true).Build();
      string googleApiKey = configuration["gapikey"];
      string url = UrlBuilder("45.140400", "19.911776", googleApiKey);
      string reverseGeocodingJson = GetJson(url);
    }

    private static string UrlBuilder(string lat, string lng, string googleApiKey)
    {
      return $@"https://maps.googleapis.com/maps/api/geocode/json?latlng={lat},{lng}&language=en&key={googleApiKey}";
    }

    private static string GetJson(string url)
    {
      string doc = string.Empty;
      using (System.Net.WebClient client = new System.Net.WebClient())
      {
        doc = client.DownloadString(url);
      }

      return doc;
    }
  }
}