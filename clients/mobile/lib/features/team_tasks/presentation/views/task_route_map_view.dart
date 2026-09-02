import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import '../../../../core/constants/app_colors.dart';

class TaskRouteMapView extends StatefulWidget {
  final LatLng teamLocation;
  final LatLng incidentLocation;
  final List<LatLng> routePoints;
  final double distanceKm;
  final int estimatedMinutes;
  final bool isRouteFallback;

  const TaskRouteMapView({
    super.key,
    required this.teamLocation,
    required this.incidentLocation,
    required this.routePoints,
    required this.distanceKm,
    required this.estimatedMinutes,
    this.isRouteFallback = false,
  });

  @override
  State<TaskRouteMapView> createState() => _TaskRouteMapViewState();
}

class _TaskRouteMapViewState extends State<TaskRouteMapView> {
  final MapController _mapController = MapController();

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _fitRouteBounds());
  }

    void _fitRouteBounds() {
    try {
      final points = widget.routePoints.isNotEmpty
          ? widget.routePoints
          : [widget.teamLocation, widget.incidentLocation];

      if (points.length >= 2) {
        final bounds = LatLngBounds.fromPoints(points);
        final latDelta = (bounds.northEast.latitude - bounds.southWest.latitude).abs();
        final lngDelta = (bounds.northEast.longitude - bounds.southWest.longitude).abs();

        // Prevent division-by-zero Infinity zoom when points are identical or extremely close
        if (latDelta < 0.0005 && lngDelta < 0.0005) {
          _mapController.move(widget.incidentLocation, 15.0);
        } else {
          _mapController.fitCamera(
            CameraFit.bounds(
              bounds: bounds,
              padding: const EdgeInsets.symmetric(horizontal: 40, vertical: 50),
              maxZoom: 16.0,
            ),
          );
        }
      } else {
        _mapController.move(widget.incidentLocation, 15.0);
      }
    } catch (_) {
      _mapController.move(widget.incidentLocation, 15.0);
    }
  }


  void _centerOnTeam() {
    _mapController.move(widget.teamLocation, 16);
  }

  @override
  Widget build(BuildContext context) {
    return ClipRRect(
      borderRadius: BorderRadius.circular(18),
      child: Stack(
        children: [
          SizedBox(
            height: 240,
            width: double.infinity,
            child: FlutterMap(
              mapController: _mapController,
              options: MapOptions(
                initialCenter: widget.incidentLocation,
                initialZoom: 14.5,
                minZoom: 3.0,
                maxZoom: 18.0,
              ),
              children: [
                TileLayer(
                  urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                  userAgentPackageName: 'com.socar.dispatch',
                ),
                if (widget.routePoints.isNotEmpty)
                  PolylineLayer(
                    polylines: [
                      Polyline(
                        points: widget.routePoints,
                        strokeWidth: 4.5,
                        color: widget.isRouteFallback ? AppColors.warning : AppColors.primary,
                      ),
                    ],
                  ),
                MarkerLayer(
                  markers: [
                    // Responder Marker
                    Marker(
                      point: widget.teamLocation,
                      width: 44,
                      height: 44,
                      child: Container(
                        decoration: BoxDecoration(
                          color: AppColors.primary,
                          shape: BoxShape.circle,
                          border: Border.all(color: Colors.white, width: 2.5),
                          boxShadow: const [
                            BoxShadow(color: Colors.black26, blurRadius: 6, offset: Offset(0, 2)),
                          ],
                        ),
                        child: const Icon(Icons.navigation_rounded, color: Colors.white, size: 22),
                      ),
                    ),
                    // Incident Marker
                    Marker(
                      point: widget.incidentLocation,
                      width: 44,
                      height: 44,
                      child: Container(
                        decoration: BoxDecoration(
                          color: AppColors.accent,
                          shape: BoxShape.circle,
                          border: Border.all(color: Colors.white, width: 2.5),
                          boxShadow: const [
                            BoxShadow(color: Colors.black26, blurRadius: 6, offset: Offset(0, 2)),
                          ],
                        ),
                        child: const Icon(Icons.local_fire_department, color: Colors.white, size: 24),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
          // Route ETA Banner
          Positioned(
            top: 10,
            left: 10,
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
              decoration: BoxDecoration(
                color: Colors.black87,
                borderRadius: BorderRadius.circular(20),
              ),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Icon(Icons.timer_outlined, color: Colors.white, size: 14),
                  const SizedBox(width: 4),
                  Text(
                    '${widget.estimatedMinutes} min (${widget.distanceKm} km)',
                    style: const TextStyle(color: Colors.white, fontSize: 12, fontWeight: FontWeight.bold),
                  ),
                ],
              ),
            ),
          ),
          // Quick Recenter Controls
          Positioned(
            bottom: 8,
            right: 8,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                FloatingActionButton.small(
                  heroTag: 'fit_bounds_btn',
                  backgroundColor: AppColors.surface,
                  foregroundColor: AppColors.textPrimary,
                  onPressed: _fitRouteBounds,
                  child: const Icon(Icons.crop_free, size: 18),
                ),
                const SizedBox(height: 6),
                FloatingActionButton.small(
                  heroTag: 'my_location_btn',
                  backgroundColor: AppColors.surface,
                  foregroundColor: AppColors.primary,
                  onPressed: _centerOnTeam,
                  child: const Icon(Icons.my_location, size: 18),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
