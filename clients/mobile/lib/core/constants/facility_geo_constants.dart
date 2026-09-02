import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';

class FacilityGeoConstants {
  FacilityGeoConstants._();

  // SOCAR Aliaga / STAR Refinery geofence bounds
  static const double southWestLat = 38.7650;
  static const double southWestLng = 26.8900;
  static const double northEastLat = 38.8350;
  static const double northEastLng = 26.9750;

  static const LatLng southWest = LatLng(southWestLat, southWestLng);
  static const LatLng northEast = LatLng(northEastLat, northEastLng);

  static final LatLngBounds facilityBounds = LatLngBounds(southWest, northEast);
  static const LatLng facilityCenter = LatLng(38.8000, 26.9325);

  // Zoom levels
  static const double defaultZoom = 14.5;
  static const double lockedMinZoom = 13.0;
  static const double unlockedMinZoom = 3.0;
  static const double maxZoom = 18.0;

  // Local storage preference keys
  static const String prefKeyMapBoundaryLock = 'map_facility_boundary_lock';

  /// Validates whether a given point is within refinery facility boundaries
  static bool isWithinFacility(LatLng point) {
    return facilityBounds.contains(point);
  }
}
