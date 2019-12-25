using System;

namespace ReverseGeocoding
{
  class CountryAndCity : IEquatable<CountryAndCity>
  {
    public string Lat { get; set; }
    public string Lng { get; set; }
    public string Name { get; set; }

    public CountryAndCity(string lat, string lng, string name)
    {
      Lat = lat;
      Lng = lng;
      Name = name;
    }

    public bool Equals(CountryAndCity other)
    {
      return Lat == other.Lat &&
               Lng == other.Lng &&
               Name == other.Name;
    }
  }

  class Country : CountryAndCity
  {
    public Country(string lat, string lng, string name) : base(lat, lng, name)
    {
    }

    public override bool Equals(object other)
    {
      return base.Equals((CountryAndCity)other);
    }

    public override int GetHashCode()
    {
      return base.GetHashCode();
    }

    public override string ToString()
    {
      return base.ToString();
    }
  }

  class City : CountryAndCity
  {
    public City(string lat, string lng, string name) : base(lat, lng, name)
    {
    }

    public override bool Equals(object obj)
    {
      return base.Equals((CountryAndCity)obj);
    }

    public override int GetHashCode()
    {
      return base.GetHashCode();
    }

    public override string ToString()
    {
      return base.ToString();
    }
  }
}