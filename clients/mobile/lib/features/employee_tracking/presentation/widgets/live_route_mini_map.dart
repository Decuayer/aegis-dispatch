import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import '../../../../core/constants/app_colors.dart';

class LiveRouteMiniMap extends StatefulWidget {
  final LatLng incidentLocation;
  final LatLng? teamLocation;
  final List<LatLng> routePoints;
  final double? distanceKm;
  final int? etaMinutes;
  final bool isFallback;

  const LiveRouteMiniMap({
    super.key,
    required this.incidentLocation,
    this.teamLocation,
    this.routePoints = const [],
    this.distanceKm,
    this.etaMinutes,
    this.isFallback = false,
  });

  @override
  State<LiveRouteMiniMap> createState() => _LiveRouteMiniMapState();
}

class _LiveRouteMiniMapState extends State<LiveRouteMiniMap> {
  final MapController _mapController = MapController();

  @override
  void didUpdateWidget(covariant LiveRouteMiniMap oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.teamLocation != oldWidget.teamLocation ||
        widget.routePoints.length != oldWidget.routePoints.length) {
      _fitMapBounds();
    }
  }

  void _fitMapBounds() {
    if (widget.teamLocation != null) {
      final points = widget.routePoints.isNotEmpty
          ? widget.routePoints
          : [widget.teamLocation!, widget.incidentLocation];

      if (points.length >= 2) {
        final bounds = LatLngBounds.fromPoints(points);
        _mapController.fitCamera(
          CameraFit.bounds(
            bounds: bounds,
            padding: const EdgeInsets.symmetric(horizontal: 40, vertical: 40),
          ),
        );
      }
    } else {
      _mapController.move(widget.incidentLocation, 15);
    }
  }

  @override
  Widget build(BuildContext context) {
    return ClipRRect(
      borderRadius: BorderRadius.circular(16),
      child: Stack(
        children: [
          SizedBox(
            height: 220,
            width: double.infinity,
            child: FlutterMap(
              mapController: _mapController,
              options: MapOptions(
                initialCenter: widget.incidentLocation,
                initialZoom: 14.5,
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
                        color: widget.isFallback ? AppColors.warning : AppColors.primary,
                      ),
                    ],
                  ),
                MarkerLayer(
                  markers: [
                    // Incident Pin Marker
                    Marker(
                      point: widget.incidentLocation,
                      width: 40,
                      height: 40,
                      child: Container(
                        decoration: BoxDecoration(
                          color: AppColors.accent,
                          shape: BoxShape.circle,
                          border: Border.all(color: Colors.white, width: 2.5),
                          boxShadow: const [
                            BoxShadow(color: Colors.black38, blurRadius: 6, offset: Offset(0, 2)),
                          ],
                        ),
                        child: const Icon(Icons.location_on, color: Colors.white, size: 22),
                      ),
                    ),

                    // Team Dynamic Marker (if location is available)
                    if (widget.teamLocation != null)
                      Marker(
                        point: widget.teamLocation!,
                        width: 44,
                        height: 44,
                        child: Container(
                          decoration: BoxDecoration(
                            color: AppColors.secondary,
                            shape: BoxShape.circle,
                            border: Border.all(color: Colors.white, width: 2.5),
                            boxShadow: const [
                              BoxShadow(color: Colors.black38, blurRadius: 6, offset: Offset(0, 2)),
                            ],
                          ),
                          child: const Icon(Icons.directions_car_rounded, color: Colors.white, size: 22),
                        ),
                      ),
                  ],
                ),
              ],
            ),
          ),

          // Live ETA & Proximity Overlay Badge
          if (widget.teamLocation != null && widget.distanceKm != null)
            Positioned(
              top: 12,
              left: 12,
              child: Container(
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                decoration: BoxDecoration(
                  color: Colors.black87,
                  borderRadius: BorderRadius.circular(20),
                  boxShadow: const [
                    BoxShadow(color: Colors.black26, blurRadius: 4, offset: Offset(0, 2)),
                  ],
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const Icon(Icons.speed_rounded, color: Colors.greenAccent, size: 16),
                    const SizedBox(width: 6),
                    Text(
                      widget.etaMinutes != null
                          ? 'ETA: ~${widget.etaMinutes} min'
                          : 'Proximity',
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 12,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    const SizedBox(width: 8),
                    Text(
                      '(${widget.distanceKm!.toStringAsFixed(1)} km)',
                      style: const TextStyle(
                        color: Colors.white70,
                        fontSize: 11,
                      ),
                    ),
                  ],
                ),
              ),
            ),

          // Center on team button
          if (widget.teamLocation != null)
            Positioned(
              bottom: 12,
              right: 12,
              child: FloatingActionButton.small(
                heroTag: 'recenter_map_btn',
                backgroundColor: Colors.white,
                foregroundColor: AppColors.primary,
                onPressed: _fitMapBounds,
                child: const Icon(Icons.crop_free_rounded, size: 20),
              ),
            ),
        ],
      ),
    );
  }
}
