import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:image_picker/image_picker.dart';
import '../../../../core/constants/app_colors.dart';
import '../../data/models/tracked_incident_model.dart';
import '../bloc/employee_tracking_bloc.dart';
import '../bloc/employee_tracking_event.dart';
import '../bloc/employee_tracking_state.dart';

class EditIncidentView extends StatefulWidget {
  final TrackedIncidentModel incident;

  const EditIncidentView({super.key, required this.incident});

  @override
  State<EditIncidentView> createState() => _EditIncidentViewState();
}

class _EditIncidentViewState extends State<EditIncidentView> {
  late final TextEditingController _descController;
  final List<File> _newMediaFiles = [];
  final ImagePicker _picker = ImagePicker();

  @override
  void initState() {
    super.initState();
    _descController = TextEditingController(
      text: widget.incident.description ?? '',
    );
  }

  @override
  void dispose() {
    _descController.dispose();
    super.dispose();
  }

  Future<void> _pickImage(ImageSource source) async {
    try {
      final photo = await _picker.pickImage(source: source, imageQuality: 85);
      if (photo != null) {
        setState(() {
          _newMediaFiles.add(File(photo.path));
        });
      }
    } catch (_) {}
  }

  Future<void> _pickMultipleImages() async {
    try {
      final photos = await _picker.pickMultiImage(imageQuality: 85);
      if (photos.isNotEmpty) {
        setState(() {
          _newMediaFiles.addAll(photos.map((p) => File(p.path)));
        });
      }
    } catch (_) {}
  }

  void _showImageSourcePicker() {
    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
      ),
      builder:
          (ctx) => SafeArea(
            child: Wrap(
              children: [
                ListTile(
                  leading: const Icon(Icons.camera_alt_outlined),
                  title: const Text('Take Photo'),
                  onTap: () {
                    Navigator.pop(ctx);
                    _pickImage(ImageSource.camera);
                  },
                ),
                ListTile(
                  leading: const Icon(Icons.photo_library_outlined),
                  title: const Text('Choose from Gallery'),
                  onTap: () {
                    Navigator.pop(ctx);
                    _pickMultipleImages();
                  },
                ),
              ],
            ),
          ),
    );
  }

  void _submitUpdate() {
    context.read<EmployeeTrackingBloc>().add(
      UpdateIncidentDetailsRequested(
        incidentId: widget.incident.id,
        description: _descController.text.trim(),
        newFiles: _newMediaFiles,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(title: const Text('Edit Incident Details')),
      body: BlocConsumer<EmployeeTrackingBloc, EmployeeTrackingState>(
        listener: (context, state) {
          if (state is EmployeeTrackingLoaded) {
            if (state.updateSuccessMessage != null) {
              Navigator.pop(context);
            }
          }
        },
        builder: (context, state) {
          final isUpdating =
              state is EmployeeTrackingLoaded && state.isUpdating;

          return SingleChildScrollView(
            padding: const EdgeInsets.all(18),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Incident Category Info
                Text(
                  '${widget.incident.category} (${widget.incident.emergencyCode})',
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    color: AppColors.textPrimary,
                  ),
                ),
                const SizedBox(height: 16),

                // Description Input
                const Text(
                  'Update Situation Description',
                  style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600),
                ),
                const SizedBox(height: 8),
                TextField(
                  controller: _descController,
                  maxLines: 5,
                  decoration: InputDecoration(
                    hintText:
                        'Enter any new details, hazards, or changes on the scene...',
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                    filled: true,
                    fillColor: Colors.white,
                  ),
                ),
                const SizedBox(height: 20),

                // Attachments Section
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text(
                      'Attach Supplementary Photos',
                      style: TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    TextButton.icon(
                      icon: const Icon(Icons.add_a_photo_outlined, size: 18),
                      label: const Text('Add'),
                      onPressed: isUpdating ? null : _showImageSourcePicker,
                    ),
                  ],
                ),
                const SizedBox(height: 8),

                if (_newMediaFiles.isNotEmpty)
                  SizedBox(
                    height: 90,
                    child: ListView.separated(
                      scrollDirection: Axis.horizontal,
                      itemCount: _newMediaFiles.length,
                      separatorBuilder: (_, __) => const SizedBox(width: 8),
                      itemBuilder: (context, index) {
                        final file = _newMediaFiles[index];
                        return Stack(
                          children: [
                            ClipRRect(
                              borderRadius: BorderRadius.circular(10),
                              child: Image.file(
                                file,
                                width: 90,
                                height: 90,
                                fit: BoxFit.cover,
                              ),
                            ),
                            Positioned(
                              top: 2,
                              right: 2,
                              child: GestureDetector(
                                onTap:
                                    isUpdating
                                        ? null
                                        : () {
                                          setState(() {
                                            _newMediaFiles.removeAt(index);
                                          });
                                        },
                                child: Container(
                                  decoration: const BoxDecoration(
                                    color: Colors.black54,
                                    shape: BoxShape.circle,
                                  ),
                                  padding: const EdgeInsets.all(4),
                                  child: const Icon(
                                    Icons.close,
                                    size: 14,
                                    color: Colors.white,
                                  ),
                                ),
                              ),
                            ),
                          ],
                        );
                      },
                    ),
                  )
                else
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: AppColors.surfaceMuted,
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: const Center(
                      child: Text(
                        'No new photos attached yet.',
                        style: TextStyle(
                          fontSize: 13,
                          color: AppColors.textMuted,
                        ),
                      ),
                    ),
                  ),

                const SizedBox(height: 30),

                // Save Changes Button
                ElevatedButton(
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppColors.primary,
                    foregroundColor: Colors.white,
                    padding: const EdgeInsets.symmetric(vertical: 14),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(14),
                    ),
                  ),
                  onPressed: isUpdating ? null : _submitUpdate,
                  child:
                      isUpdating
                          ? const SizedBox(
                            height: 20,
                            width: 20,
                            child: CircularProgressIndicator(
                              strokeWidth: 2.5,
                              color: Colors.white,
                            ),
                          )
                          : const Text(
                            'Save Changes',
                            style: TextStyle(
                              fontSize: 15,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}
