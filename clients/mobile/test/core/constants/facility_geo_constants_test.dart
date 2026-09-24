import 'package:flutter_test/flutter_test.dart';
import 'package:latlong2/latlong.dart';
import 'package:socar_dispatch_mobile/core/constants/facility_geo_constants.dart';

void main() {
  group('FacilityGeoConstants Tests', () {
    test(
      'facilityBounds encompasses expected refinery coordinate boundaries',
      () {
        final bounds = FacilityGeoConstants.facilityBounds;

        expect(bounds.south, equals(38.7650));
        expect(bounds.west, equals(26.8900));
        expect(bounds.north, equals(38.8350));
        expect(bounds.east, equals(26.9750));
      },
    );

    test('facilityCenter is within facility bounds', () {
      const center = FacilityGeoConstants.facilityCenter;
      expect(FacilityGeoConstants.isWithinFacility(center), isTrue);
    });

    test('isWithinFacility returns true for internal refinery coordinates', () {
      // Test points within STAR Refinery / Petkim grounds
      const point1 = LatLng(38.8050, 26.9200);
      const point2 = LatLng(38.7700, 26.9000);

      expect(FacilityGeoConstants.isWithinFacility(point1), isTrue);
      expect(FacilityGeoConstants.isWithinFacility(point2), isTrue);
    });

    test(
      'isWithinFacility returns false for points outside facility grounds',
      () {
        // Coordinates outside the Aliaga facility (e.g. Izmir center or Aegean sea)
        const outsidePoint1 = LatLng(38.4237, 27.1428); // Izmir center
        const outsidePoint2 = LatLng(38.9000, 26.8000); // North-west Aegean sea

        expect(FacilityGeoConstants.isWithinFacility(outsidePoint1), isFalse);
        expect(FacilityGeoConstants.isWithinFacility(outsidePoint2), isFalse);
      },
    );

    test('zoom constraint constants are properly configured', () {
      expect(
        FacilityGeoConstants.lockedMinZoom,
        greaterThan(FacilityGeoConstants.unlockedMinZoom),
      );
      expect(
        FacilityGeoConstants.maxZoom,
        greaterThan(FacilityGeoConstants.lockedMinZoom),
      );
    });
  });
}
