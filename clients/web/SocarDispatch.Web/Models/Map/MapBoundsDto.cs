namespace SocarDispatch.Web.Models.Map;

public class MapBoundsDto
{
    public double SouthWestLat { get; set; }
    public double SouthWestLng { get; set; }
    public double NorthEastLat { get; set; }
    public double NorthEastLng { get; set; }

    public MapBoundsDto() { }

    public MapBoundsDto(double southWestLat, double southWestLng, double northEastLat, double northEastLng)
    {
        SouthWestLat = southWestLat;
        SouthWestLng = southWestLng;
        NorthEastLat = northEastLat;
        NorthEastLng = northEastLng;
    }

    /// <summary>
    /// Default operational bounding box encompassing SOCAR Aliaga / STAR Refinery.
    /// </summary>
    public static MapBoundsDto DefaultFacilityBounds => new(38.7650, 26.8900, 38.8350, 26.9750);
}
