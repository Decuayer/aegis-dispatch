import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../../core/constants/app_colors.dart';
import '../../../../core/utils/validators.dart';
import '../../../auth/presentation/bloc/auth_bloc.dart';
import '../../../auth/presentation/bloc/auth_event.dart';
import '../../../auth/presentation/views/widgets/custom_text_field.dart';
import '../../data/models/user_model.dart';
import '../cubit/profile_cubit.dart';
import '../cubit/profile_state.dart';
import 'widgets/avatar_picker_widget.dart';
import '../widgets/map_settings_tile.dart';
import '../widgets/feedback_navigation_tile.dart';
import '../widgets/google_account_tile.dart';


class ProfileView extends StatefulWidget {
  final UserModel currentUser;

  const ProfileView({super.key, required this.currentUser});

  @override
  State<ProfileView> createState() => _ProfileViewState();
}

class _ProfileViewState extends State<ProfileView> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _firstNameController;
  late final TextEditingController _lastNameController;
  late final TextEditingController _emailController;
  late final TextEditingController _phoneController;
  late final TextEditingController _departmentController;
  late final TextEditingController _subRoleController;

  @override
  void initState() {
    super.initState();
    _firstNameController = TextEditingController(text: widget.currentUser.firstName);
    _lastNameController = TextEditingController(text: widget.currentUser.lastName);
    _emailController = TextEditingController(text: widget.currentUser.email);
    _phoneController = TextEditingController(text: widget.currentUser.phone);
    _departmentController = TextEditingController(text: widget.currentUser.department);
    _subRoleController = TextEditingController(text: widget.currentUser.subRole ?? '');

    context.read<ProfileCubit>().loadProfile(initialUser: widget.currentUser);
  }

  @override
  void dispose() {
    _firstNameController.dispose();
    _lastNameController.dispose();
    _emailController.dispose();
    _phoneController.dispose();
    _departmentController.dispose();
    _subRoleController.dispose();
    super.dispose();
  }

  void _onSavePressed(UserModel activeUser) {
    if (_formKey.currentState?.validate() ?? false) {
      context.read<ProfileCubit>().saveProfileDetails(
            currentUser: activeUser,
            firstName: _firstNameController.text.trim(),
            lastName: _lastNameController.text.trim(),
            phone: _phoneController.text.trim(),
            department: _departmentController.text.trim(),
            subRole: _subRoleController.text.trim().isNotEmpty
                ? _subRoleController.text.trim()
                : null,
          );
    }
  }

    void _showLogoutConfirmation(BuildContext context) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Sign Out'),
        content: const Text('Are you sure you want to end your current session?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('Cancel'),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.error,
              minimumSize: const Size(90, 40),
            ),
            onPressed: () {
              Navigator.pop(ctx);
              Navigator.of(context).popUntil((route) => route.isFirst); 
              context.read<AuthBloc>().add(const AuthLogoutRequested());
            },
            child: const Text('Sign Out'),
          ),
        ],
      ),
    );
  }


  String _getInitials(UserModel user) {
    final first = user.firstName.isNotEmpty ? user.firstName[0] : '';
    final last = user.lastName.isNotEmpty ? user.lastName[0] : '';
    return '$first$last'.toUpperCase();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('User Profile'),
        actions: [
          IconButton(
            icon: const Icon(Icons.logout_rounded, color: AppColors.error),
            tooltip: 'Sign Out',
            onPressed: () => _showLogoutConfirmation(context),
          ),
        ],
      ),
      body: BlocConsumer<ProfileCubit, ProfileState>(
        listener: (context, state) {
          if (state is ProfileUpdateSuccess) {
            context.read<AuthBloc>().add(AuthUserUpdated(state.user));
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(
                content: Text(state.message),
                backgroundColor: AppColors.success,
                behavior: SnackBarBehavior.floating,
              ),
            );
          } else if (state is ProfileError) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(
                content: Text(state.message),
                backgroundColor: AppColors.error,
                behavior: SnackBarBehavior.floating,
              ),
            );
          }
        },
        builder: (context, state) {
          UserModel user = widget.currentUser;
          bool isUpdating = false;
          bool isAvatarUpdating = false;

          if (state is ProfileLoaded) {
            user = state.user;
          } else if (state is ProfileUpdating) {
            user = state.user;
            isUpdating = true;
            isAvatarUpdating = state.isAvatarUpdating;
          } else if (state is ProfileUpdateSuccess) {
            user = state.user;
          } else if (state is ProfileError && state.cachedUser != null) {
            user = state.cachedUser!;
          }

          return SingleChildScrollView(
            padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
            child: Form(
              key: _formKey,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  // Avatar & Role Badges
                  AvatarPickerWidget(
                    avatarUrl: user.avatarUrl,
                    initials: _getInitials(user),
                    isUploading: isAvatarUpdating,
                    onImageSelected: (image) {
                      context.read<ProfileCubit>().updateAvatar(image, user);
                    },
                  ),
                  const SizedBox(height: 12),
                  Text(
                    user.fullName,
                    textAlign: TextAlign.center,
                    style: const TextStyle(
                      fontSize: 20,
                      fontWeight: FontWeight.w700,
                      color: AppColors.textPrimary,
                    ),
                  ),
                  const SizedBox(height: 6),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(
                          color: AppColors.primary.withValues(alpha: 0.1),
                          borderRadius: BorderRadius.circular(20),
                        ),
                        child: Text(
                          user.roleType.name.toUpperCase(),
                          style: const TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.bold,
                            color: AppColors.primary,
                          ),
                        ),
                      ),
                      if (user.department.isNotEmpty) ...[
                        const SizedBox(width: 8),
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                          decoration: BoxDecoration(
                            color: AppColors.secondary.withValues(alpha: 0.1),
                            borderRadius: BorderRadius.circular(20),
                          ),
                          child: Text(
                            user.department,
                            style: const TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.bold,
                              color: AppColors.secondary,
                            ),
                          ),
                        ),
                      ],
                    ],
                  ),
                  const SizedBox(height: 24),

                  // Form Fields
                  Container(
                    padding: const EdgeInsets.all(20),
                    decoration: BoxDecoration(
                      color: AppColors.surface,
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: AppColors.border),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Personal Details',
                          style: TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                            color: AppColors.textPrimary,
                          ),
                        ),
                        const SizedBox(height: 16),
                        Row(
                          children: [
                            Expanded(
                              child: CustomTextField(
                                controller: _firstNameController,
                                labelText: 'First Name',
                                prefixIcon: Icons.person_outline_rounded,
                                validator: (v) => Validators.validateRequired(v, 'First name'),
                                enabled: !isUpdating,
                              ),
                            ),
                            const SizedBox(width: 12),
                            Expanded(
                              child: CustomTextField(
                                controller: _lastNameController,
                                labelText: 'Last Name',
                                prefixIcon: Icons.person_outline_rounded,
                                validator: (v) => Validators.validateRequired(v, 'Last name'),
                                enabled: !isUpdating,
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 16),
                        CustomTextField(
                          controller: _emailController,
                          labelText: 'Email Address (Read-only)',
                          prefixIcon: Icons.alternate_email_rounded,
                          enabled: false,
                        ),
                        const SizedBox(height: 16),
                        CustomTextField(
                          controller: _phoneController,
                          labelText: 'Phone Number',
                          prefixIcon: Icons.phone_outlined,
                          keyboardType: TextInputType.phone,
                          validator: Validators.validatePhone,
                          enabled: !isUpdating,
                        ),
                        const SizedBox(height: 16),
                        CustomTextField(
                          controller: _departmentController,
                          labelText: 'Department',
                          prefixIcon: Icons.business_outlined,
                          validator: (v) => Validators.validateRequired(v, 'Department'),
                          enabled: !isUpdating,
                        ),
                        const SizedBox(height: 16),
                        CustomTextField(
                          controller: _subRoleController,
                          labelText: 'Sub-Role / Specialization',
                          hintText: 'e.g. Lead Firefighter, EMT Specialist',
                          prefixIcon: Icons.badge_outlined,
                          enabled: !isUpdating,
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),
                  const MapSettingsTile(),
                  const SizedBox(height: 16),
                  GoogleAccountTile(user: user, isUpdating: isUpdating),
                  const SizedBox(height: 16),
                  const FeedbackNavigationTile(),
                  const SizedBox(height: 24),

                  // Save Button
                  ElevatedButton(
                    onPressed: isUpdating ? null : () => _onSavePressed(user),
                    child: isUpdating
                        ? const SizedBox(
                            height: 22,
                            width: 22,
                            child: CircularProgressIndicator(
                              color: Colors.white,
                              strokeWidth: 2.2,
                            ),
                          )
                        : const Text('Save Changes'),
                  ),
                  const SizedBox(height: 16),
                ],
              ),
            ),
          );
        },
      ),
    );
  }
}
