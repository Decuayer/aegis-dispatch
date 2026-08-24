namespace SocarDispatch.Web.Utils;

public static class GeoUtils
{
    private const double EarthRadiusMeters = 6371000.0;

    /// Calculates distance in meters between two GPS coordinates using the Haversine formula.
    public static double CalculateDistanceInMeters(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    /// Overload for decimal and nullable coordinate types commonly used in DTOs.
    public static double? CalculateDistanceInMeters(decimal? lat1, decimal? lon1, decimal? lat2, decimal? lon2)
    {
        if (!lat1.HasValue || !lon1.HasValue || !lat2.HasValue || !lon2.HasValue)
        {
            return null;
        }

        return CalculateDistanceInMeters(
            (double)lat1.Value,
            (double)lon1.Value,
            (double)lat2.Value,
            (double)lon2.Value);
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
