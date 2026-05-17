namespace SafeZone.Server.Helpers;

public static class GeoHelper
{
    private const double EarthRadiusKm = 6371.0;

    public static double CalculateDistanceKm(double lat1, double lng1, double lat2, double lng2)
    {
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLng = DegreesToRadians(lng2 - lng1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    public static double DegreesToRadians(double degrees)
    {
        return degrees * (Math.PI / 180.0);
    }

    public static bool IsValidCoordinate(double lat, double lng)
    {
        return lat >= -90.0 && lat <= 90.0 && lng >= -180.0 && lng <= 180.0;
    }

    public static (double MinLat, double MaxLat, double MinLng, double MaxLng) GetBoundsFromCenter(
        double centerLat, double centerLng, double radiusKm)
    {
        var latChange = radiusKm / 110.574;
        var lngChange = radiusKm / (111.320 * Math.Cos(DegreesToRadians(centerLat)));

        return (
            MinLat: centerLat - latChange,
            MaxLat: centerLat + latChange,
            MinLng: centerLng - lngChange,
            MaxLng: centerLng + lngChange
        );
    }
}
