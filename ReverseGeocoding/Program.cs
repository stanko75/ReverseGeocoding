using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace ReverseGeocoding
{
  class Program
  {
    static void Main(string[] args)
    {
      IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("jsconfig.json", true, true).Build();
      string googleApiKey = configuration["gapikey"];
      string connectionString = configuration["connectionString"];
      string jsonsConf = configuration["jsons"];
      string[] jsons = jsonsConf.Split(';');

      JsonSerializer serializer = new JsonSerializer();
      LatLngFileName latLngFileName;

      MySqlConnection mySqlConnection = new MySqlConnection();
      mySqlConnection.ConnectionString = connectionString;

      foreach (string json in jsons)
      {
        using (FileStream s = File.Open(json, FileMode.Open))
        using (StreamReader sr = new StreamReader(s))
        using (JsonReader reader = new JsonTextReader(sr))
        {
          while (reader.Read())
          {
            if (reader.TokenType == JsonToken.StartObject)
            {
              latLngFileName = serializer.Deserialize<LatLngFileName>(reader);

              Console.WriteLine($"Geocoding: {latLngFileName.Latitude} ,{latLngFileName.Longitude}, FileName: {latLngFileName.FileName}");

              string url = UrlBuilder(latLngFileName.Latitude, latLngFileName.Longitude, googleApiKey);
              string reverseGeocodingJson = GetJson(url);

              ParseJsonAndWriteToDB(latLngFileName.Latitude, latLngFileName.Longitude, latLngFileName.FileName, reverseGeocodingJson, mySqlConnection);

            }
          }
        }
      }

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

    private static void ParseJsonAndWriteToDB(string lat, string lng, string fileName, string reverseGeocodingJson, MySqlConnection mySqlConnection)
    {
      JObject reverseGeocodingJObject = JObject.Parse(reverseGeocodingJson);
      IEnumerable<JToken> addressComponentsList = reverseGeocodingJObject.SelectTokens("$..address_components");

      string city = string.Empty;
      string country = string.Empty;
      fileName = fileName.Replace(@"\", @"\\");

      foreach (JToken addressComponents in addressComponentsList)
      {
        foreach (JToken addressComponent in addressComponents)
        {
          List<string> types = addressComponent["types"].ToObject<List<string>>();

          if (types.Contains("locality") && types.Contains("political")) //city
          {
            city = addressComponent["long_name"].ToString();
            city = city.Replace("'", "''");
            AddCityToDb(city, mySqlConnection);
          }
          else if (types.Contains("country") && types.Contains("political")) //country
          {
            country = addressComponent["long_name"].ToString();
            country = country.Replace("'", "''");
            AddCountryToDb(country, mySqlConnection);
          }
        }
      }
      AddGpsLocationToDB(city, country, lat, lng, fileName, mySqlConnection);

    }

    private static void AddGpsLocationToDB(string city, string country, string lat, string lng, string fileName, MySqlConnection mySqlConnection)
    {
      fileName = fileName.Replace("\\\\", "/");
      fileName = fileName.Replace("\\", "/");

      fileName = "file:///" + fileName.Replace("\\", "/");

      using (mySqlConnection)
      {
        mySqlConnection.Open();

        MySqlCommand mySqlCommandCountry = new MySqlCommand();
        mySqlCommandCountry.CommandText = $"select * from countries where Name = '{country}' ";
        mySqlCommandCountry.Connection = mySqlConnection;

        int countryID = 0;
        if (!string.IsNullOrWhiteSpace(country))
        {
          using (MySqlDataReader mySqlDataReaderCountry = mySqlCommandCountry.ExecuteReader())
          {
            if (mySqlDataReaderCountry.HasRows)
            {
              while (mySqlDataReaderCountry.Read())
              {
                countryID = (int)mySqlDataReaderCountry["ID"];
              }
            }
            else
            {
              throw new Exception($"Couldn't find the country: {country}!");
            }
          }
        }

        MySqlCommand mySqlCommandCity = new MySqlCommand();
        mySqlCommandCity.CommandText = $"select * from cities where Name = '{city}' ";
        mySqlCommandCity.Connection = mySqlConnection;

        int cityID = 0;
        if (!string.IsNullOrWhiteSpace(city))
        {
          using (MySqlDataReader mySqlDataReaderCity = mySqlCommandCity.ExecuteReader())
          {
            if (mySqlDataReaderCity.HasRows)
            {
              while (mySqlDataReaderCity.Read())
              {
                cityID = (int)mySqlDataReaderCity["ID"];
              }
            }
            else
            {
              throw new Exception($"Couldn't find the city: {city}!");
            }
          }
        }

        MySqlCommand mySqlCommandLatLngChckIfExists = new MySqlCommand();
        mySqlCommandLatLngChckIfExists.CommandText = $"select * from reversegeocoding.gpslocations where Latitude = '{lat}' and Longitude = '{lng}' ";
        mySqlCommandLatLngChckIfExists.Connection = mySqlConnection;

        bool latLngNotExists = true;
        using (MySqlDataReader mySqlDataReaderLatLngChckIfExists = mySqlCommandLatLngChckIfExists.ExecuteReader())
        {
          latLngNotExists = !mySqlDataReaderLatLngChckIfExists.HasRows;
        }

        if (latLngNotExists)
        {
          string insertSql = $"INSERT INTO reversegeocoding.gpslocations (Latitude, Longitude, FileName, CityID, CountryID) VALUES ('{lat}', '{lng}', '{fileName}', '{cityID}', '{countryID}');";

          try
          {
            MySqlCommand mySqlCommandLatLng = new MySqlCommand();
            mySqlCommandLatLng.Connection = mySqlConnection;

            mySqlCommandLatLng.CommandText = insertSql;
              
            mySqlCommandLatLng.ExecuteNonQuery();
          }
          catch (Exception e)
          {
            throw new Exception($"Error: {e.Message}, SQL: {insertSql}");
          }
        }
      }
    }

    private static void AddCountryToDb(string country, MySqlConnection mySqlConnection)
    {
      using (mySqlConnection)
      {
        mySqlConnection.Open();

        MySqlCommand mySqlCommand = new MySqlCommand();
        mySqlCommand.CommandText = $"select * from countries where Name = '{country}' ";
        mySqlCommand.Connection = mySqlConnection;

        MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();

        if (!mySqlDataReader.HasRows)
        {
          //Add to db
          mySqlDataReader.Close();

          mySqlCommand.CommandText = $"INSERT INTO reversegeocoding.countries (Name) VALUES ('{country}');";
          mySqlCommand.ExecuteNonQuery();
        }
      }
    }

    private static void AddCityToDb(string city, MySqlConnection mySqlConnection)
    {
      using (mySqlConnection)
      {
        mySqlConnection.Open();

        MySqlCommand mySqlCommand = new MySqlCommand();
        mySqlCommand.CommandText = $"select * from cities where Name = '{city}' ";
        mySqlCommand.Connection = mySqlConnection;

        MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
        if (!mySqlDataReader.HasRows)
        {
          mySqlDataReader.Close();

          mySqlCommand.CommandText = $"INSERT INTO reversegeocoding.cities (Name) VALUES ('{city}');";
          mySqlCommand.ExecuteNonQuery();
        }
      }

    }
  }
}