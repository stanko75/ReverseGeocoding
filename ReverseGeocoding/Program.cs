using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace ReverseGeocoding
{
  class Program
  {
    static void Main(string[] args)
    {
      IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("jsconfig.json", true, true).Build();
      string googleApiKey = configuration["gapikey"];

      string lat = "45.140400";
      string lng = "19.911776";

      string url = UrlBuilder(lat, lng, googleApiKey);
      string reverseGeocodingJson = GetJson(url);

      List<Country> countries = new List<Country>();
      List<City> cities = new List<City>();
      ParseJsonAndWriteToList(lat, lng, countries, cities, reverseGeocodingJson);
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

    private static void ParseJsonAndWriteToList(string lat, string lng, List<Country> countries, List<City> cities, string reverseGeocodingJson)
    {
      JObject reverseGeocodingJObject = JObject.Parse(reverseGeocodingJson);
      IEnumerable<JToken> addressComponentsList = reverseGeocodingJObject.SelectTokens("$..address_components");

      foreach (JToken addressComponents in addressComponentsList)
      {
        foreach (JToken addressComponent in addressComponents)
        {
          List<string> types = addressComponent["types"].ToObject<List<string>>();

          if (types.Contains("locality") && types.Contains("political"))
          {
            City city = new City(lat, lng, addressComponent["long_name"].ToString());

            if (!cities.Contains(city))
            {
              cities.Add(city);
            }
          }
          else if (types.Contains("country") && types.Contains("political"))
          {
            Country country = new Country(lat, lng, addressComponent["long_name"].ToString());

            if (!countries.Contains(country))
            {
              countries.Add(country);
            }
          }
        }

      }

    }

  }
}