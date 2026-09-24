import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import '../../../../core/constants/app_colors.dart';
import '../../../../core/constants/facility_geo_constants.dart';
import '../../../profile/presentation/cubit/map_settings_cubit.dart';
import '../widgets/incident_map_marker.dart';

class TaskRouteMapView extends StatefulWidget {
  final LatLng teamLocation;
  final LatLng incidentLocation;
  final List<LatLng> routePoints;
  final double distanceKm;
  final int estimatedMinutes;
  final bool isRouteFallback;
  final String? incidentId;
  final String? teamId;
  final bool? isBoundaryLocked;

  const TaskRouteMapView({
    super.key,
    required this.teamLocation,
    required this.incidentLocation,
    required this.routePoints,
    required this.distanceKm,
    required this.estimatedMinutes,
    this.isRouteFallback = false,
    this.incidentId,
    this.teamId,
    this.isBoundaryLocked,
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
      final points =
          widget.routePoints.isNotEmpty
              ? widget.routePoints
              : [widget.teamLocation, widget.incidentLocation];

      if (points.length >= 2) {
        final bounds = LatLngBounds.fromPoints(points);
        final latDelta =
            (bounds.northEast.latitude - bounds.southWest.latitude).abs();
        final lngDelta =
            (bounds.northEast.longitude - bounds.southWest.longitude).abs();

        if (latDelta < 0.0005 && lngDelta < 0.0005) {
          _mapController.move(
            widget.incidentLocation,
            FacilityGeoConstants.defaultZoom,
          );
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
        _mapController.move(
          widget.incidentLocation,
          FacilityGeoConstants.defaultZoom,
        );
      }
    } catch (_) {
      _mapController.move(
        widget.incidentLocation,
        FacilityGeoConstants.defaultZoom,
      );
    }
  }

  void _centerOnTeam() {
    _mapController.move(widget.teamLocation, 16);
  }

  bool _resolveBoundaryLock(BuildContext context) {
    if (widget.isBoundaryLocked != null) {
      return widget.isBoundaryLocked!;
    }
    try {
      return context.watch<MapSettingsCubit>().state.isBoundaryLockEnabled;
    } catch (_) {
      return true; // Fallback to locked for safety
    }
  }

  @override
  Widget build(BuildContext context) {
    final isLocked = _resolveBoundaryLock(context);

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
                initialZoom: FacilityGeoConstants.defaultZoom,
                minZoom:
                    isLocked
                        ? FacilityGeoConstants.lockedMinZoom
                        : FacilityGeoConstants.unlockedMinZoom,
                maxZoom: FacilityGeoConstants.maxZoom,
                cameraConstraint:
                    isLocked
                        ? CameraConstraint.contain(
                          bounds: FacilityGeoConstants.facilityBounds,
                        )
                        : const CameraConstraint.unconstrained(),
              ),
              children: [
                TileLayer(
                  urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                  userAgentPackageName: 'com.aegis.dispatch',
                ),
                if (widget.routePoints.isNotEmpty)
                  PolylineLayer(
                    polylines: [
                      Polyline(
                        points: widget.routePoints,
                        strokeWidth: 4.5,
                        color:
                            widget.isRouteFallback
                                ? AppColors.warning
                                : AppColors.primary,
                      ),
                    ],
                  ),
                MarkerLayer(
                  markers: [
                    // Responder Location Marker
                    Marker(
                      point: widget.teamLocation,
                      width: 90,
                      height: 68,
                      child: ResponderMapMarker(teamId: widget.teamId),
                    ),
                    // Incident Target Marker
                    Marker(
                      point: widget.incidentLocation,
                      width: 90,
                      height: 68,
                      child: IncidentMapMarker(incidentId: widget.incidentId),
                    ),
                  ],
                ),
              ],
            ),
          ),

          // Route ETA Banner (Top-Left)
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
                  const Icon(
                    Icons.timer_outlined,
                    color: Colors.white,
                    size: 14,
                  ),
                  const SizedBox(width: 4),
                  Text(
                    '${widget.estimatedMinutes} min (${widget.distanceKm} km)',
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ],
              ),
            ),
          ),

          // Facility Boundary Lock Indicator (Top-Right)
          Positioned(
            top: 10,
            right: 10,
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
              decoration: BoxDecoration(
                color: isLocked ? Colors.black87 : Colors.black54,
                borderRadius: BorderRadius.circular(20),
                border: Border.all(
                  color: isLocked ? AppColors.secondary : Colors.white24,
                  width: 1.2,
                ),
              ),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(
                    isLocked
                        ? Icons.lock_outline_rounded
                        : Icons.lock_open_rounded,
                    color: isLocked ? AppColors.secondary : Colors.white70,
                    size: 13,
                  ),
                  const SizedBox(width: 4),
                  Text(
                    isLocked ? 'Facility Lock' : 'Unrestricted',
                    style: TextStyle(
                      color: isLocked ? Colors.white : Colors.white70,
                      fontSize: 11,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ],
              ),
            ),
          ),

          // Quick Recenter Controls (Bottom-Right)
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
