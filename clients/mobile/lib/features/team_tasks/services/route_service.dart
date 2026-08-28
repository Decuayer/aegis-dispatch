import 'package:dio/dio.dart';
import 'package:latlong2/latlong.dart';

class RouteResult {
  final List<LatLng> points;
  final double distanceKm;
  final int durationMinutes;
  final bool isFallback;

  const RouteResult({
    required this.points,
    required this.distanceKm,
    required this.durationMinutes,
    this.isFallback = false,
  });
}

class RouteService {
  final Dio _dio;

  RouteService({Dio? dio})
      : _dio = dio ??
            Dio(
              BaseOptions(
                connectTimeout: const Duration(seconds: 6),
                receiveTimeout: const Duration(seconds: 6),
              ),
            );

  Future<RouteResult> calculateRoute({
    required LatLng origin,
    required LatLng destination,
  }) async {
    final osrmUrl =
        'https://router.project-osrm.org/route/v1/driving/'
        '${origin.longitude},${origin.latitude};'
        '${destination.longitude},${destination.latitude}'
        '?overview=full&geometries=geojson';

    try {
      final response = await _dio.get(osrmUrl);

      if (response.statusCode == 200 && response.data != null) {
        final data = response.data as Map<String, dynamic>;
        final routes = data['routes'] as List<dynamic>?;

        if (routes != null && routes.isNotEmpty) {
          final firstRoute = routes.first as Map<String, dynamic>;
          final geometry = firstRoute['geometry'] as Map<String, dynamic>?;
          final coordinates = geometry?['coordinates'] as List<dynamic>?;

          if (coordinates != null && coordinates.isNotEmpty) {
            final List<LatLng> routePoints = coordinates.map((coord) {
              final lon = (coord[0] as num).toDouble();
              final lat = (coord[1] as num).toDouble();
              return LatLng(lat, lon);
            }).toList();

            final double distanceMeters =
                (firstRoute['distance'] as num?)?.toDouble() ?? 0.0;
            final double durationSeconds =
                (firstRoute['duration'] as num?)?.toDouble() ?? 0.0;

            final double distanceKm = distanceMeters / 1000.0;
            final int durationMinutes = (durationSeconds / 60.0).ceil();

            return RouteResult(
              points: routePoints,
              distanceKm: double.parse(distanceKm.toStringAsFixed(2)),
              durationMinutes: durationMinutes < 1 ? 1 : durationMinutes,
              isFallback: false,
            );
          }
        }
      }
      return _calculateFallbackRoute(origin, destination);
    } catch (_) {
      // Offline fallback: Return direct line with calculated Haversine distance
      return _calculateFallbackRoute(origin, destination);
    }
  }

  RouteResult _calculateFallbackRoute(LatLng origin, LatLng destination) {
    const distanceCalculator = Distance();
    final double distanceMeters = distanceCalculator.as(
      LengthUnit.Meter,
      origin,
      destination,
    );

    final double distanceKm = distanceMeters / 1000.0;
    // Estimated duration assuming average emergency vehicle facility speed of 40 km/h
    final int estimatedMinutes = ((distanceKm / 40.0) * 60).ceil();

    return RouteResult(
      points: [origin, destination],
      distanceKm: double.parse(distanceKm.toStringAsFixed(2)),
      durationMinutes: estimatedMinutes < 1 ? 1 : estimatedMinutes,
      isFallback: true,
    );
  }
}
