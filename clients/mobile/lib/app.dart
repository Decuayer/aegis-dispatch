import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'core/permissions/permission_handler_service.dart';
import 'core/services/fcm_notification_service.dart';
import 'core/theme/app_theme.dart';
import 'features/auth/data/repositories/auth_repository.dart';
import 'features/auth/presentation/bloc/auth_bloc.dart';
import 'features/auth/presentation/bloc/auth_event.dart';
import 'features/auth/presentation/bloc/auth_state.dart';
import 'features/auth/presentation/views/login_view.dart';
import 'features/employee_tracking/data/repositories/employee_incident_repository.dart';
import 'features/employee_tracking/data/services/employee_tracking_hub_service.dart';
import 'features/employee_tracking/presentation/bloc/employee_tracking_bloc.dart';
import 'features/home/presentation/views/employee_home_view.dart';
import 'features/home/presentation/views/operator_notice_view.dart';
import 'features/home/presentation/views/team_home_view.dart';
import 'features/incident_reporting/data/repositories/incident_repository.dart';
import 'features/incident_reporting/presentation/bloc/incident_report_bloc.dart';
import 'features/incident_reporting/presentation/bloc/rapid_incident_cubit.dart';
import 'features/incident_reporting/services/location_service.dart';
import 'features/incident_reporting/services/media_picker_service.dart';
import 'features/incident_reporting/services/rapid_dispatch_service.dart';
import 'features/onboarding/data/onboarding_repository.dart';
import 'features/onboarding/presentation/bloc/onboarding_bloc.dart';
import 'features/onboarding/presentation/bloc/onboarding_event.dart';
import 'features/onboarding/presentation/views/onboarding_gate_view.dart';
import 'features/onboarding/services/onboarding_permission_service.dart';
import 'features/profile/data/models/user_model.dart';
import 'features/profile/data/repositories/media_repository.dart';
import 'features/profile/data/repositories/profile_repository.dart';
import 'features/profile/presentation/cubit/profile_cubit.dart';
import 'features/splash/presentation/views/splash_view.dart';
import 'features/team_portal/data/repositories/team_portal_repository.dart';
import 'features/team_portal/presentation/bloc/team_portal_bloc.dart';
import 'features/team_tasks/data/repositories/task_repository.dart';
import 'features/team_tasks/presentation/bloc/task_bloc.dart';
import 'features/team_tasks/presentation/bloc/task_event.dart';
import 'features/team_tasks/services/route_service.dart';
import 'features/tracking/data/location_stream_repository.dart';
import 'core/storage/map_settings_repository.dart';
import 'features/profile/presentation/cubit/map_settings_cubit.dart';
import 'features/feedback/data/feedback_repository.dart';


final GlobalKey<NavigatorState> rootNavigatorKey = GlobalKey<NavigatorState>();

class SocarDispatchApp extends StatelessWidget {
  final AuthRepository authRepository;
  final ProfileRepository profileRepository;
  final MediaRepository mediaRepository;
  final IncidentRepository incidentRepository;
  final TaskRepository taskRepository;
  final LocationService locationService;
  final PermissionHandlerService permissionService;
  final MediaPickerService mediaPickerService;
  final RouteService routeService;
  final FcmNotificationService fcmNotificationService;
  final LocationStreamRepository locationStreamRepository;
  final OnboardingRepository onboardingRepository;
  final MapSettingsRepository mapSettingsRepository;
  final OnboardingPermissionService onboardingPermissionService;
  final TeamPortalRepository teamPortalRepository;
  final EmployeeIncidentRepository employeeIncidentRepository;
  final EmployeeTrackingHubService employeeTrackingHubService;
  final FeedbackRepository feedbackRepository;

  const SocarDispatchApp({
    super.key,
    required this.authRepository,
    required this.profileRepository,
    required this.mediaRepository,
    required this.incidentRepository,
    required this.taskRepository,
    required this.locationService,
    required this.permissionService,
    required this.mediaPickerService,
    required this.routeService,
    required this.fcmNotificationService,
    required this.locationStreamRepository,
    required this.onboardingRepository,
    required this.mapSettingsRepository,
    required this.onboardingPermissionService,
    required this.teamPortalRepository,
    required this.employeeIncidentRepository,
    required this.employeeTrackingHubService,
    required this.feedbackRepository,
  });

  @override
  Widget build(BuildContext context) {
    return MultiRepositoryProvider(
      providers: [
        RepositoryProvider.value(value: authRepository),
        RepositoryProvider.value(value: profileRepository),
        RepositoryProvider.value(value: mediaRepository),
        RepositoryProvider.value(value: incidentRepository),
        RepositoryProvider.value(value: taskRepository),
        RepositoryProvider.value(value: locationService),
        RepositoryProvider.value(value: permissionService),
        RepositoryProvider.value(value: mediaPickerService),
        RepositoryProvider.value(value: routeService),
        RepositoryProvider.value(value: fcmNotificationService),
        RepositoryProvider.value(value: locationStreamRepository),
        RepositoryProvider.value(value: onboardingRepository),
        RepositoryProvider.value(value: mapSettingsRepository),
        RepositoryProvider.value(value: onboardingPermissionService),
        RepositoryProvider.value(value: teamPortalRepository),
        RepositoryProvider.value(value: employeeIncidentRepository),
        RepositoryProvider.value(value: employeeTrackingHubService),
        RepositoryProvider.value(value: feedbackRepository),
      ],
      child: MultiBlocProvider(
        providers: [
          BlocProvider(
            create: (ctx) => AuthBloc(
              authRepository: authRepository,
            )..add(const AuthCheckRequested()),
          ),
          BlocProvider(
            create: (ctx) => ProfileCubit(
              profileRepository: profileRepository,
              mediaRepository: mediaRepository,
            ),
          ),
          BlocProvider(
            create: (ctx) => IncidentReportBloc(
              incidentRepository: incidentRepository,
              locationService: locationService,
            ),
          ),
          BlocProvider(
            create: (ctx) => RapidIncidentCubit(
              dispatchService: RapidDispatchService(
                incidentRepository: incidentRepository,
                locationService: locationService,
              ),
            ),
          ),
          BlocProvider(
            create: (ctx) => TaskBloc(
              taskRepository: taskRepository,
              routeService: routeService,
              locationService: locationService,
              locationStreamRepository: locationStreamRepository,
            ),
          ),
          BlocProvider(
            create: (ctx) => TeamPortalBloc(
              repository: teamPortalRepository,
            ),
          ),
          BlocProvider(
            create: (ctx) => OnboardingBloc(
              onboardingRepository: onboardingRepository,
              permissionService: onboardingPermissionService,
            )..add(const OnboardingCheckRequested()),
          ),
          BlocProvider(
            create: (ctx) => MapSettingsCubit(
              repository: mapSettingsRepository,
            ),
          ),
          BlocProvider(
            create: (ctx) => EmployeeTrackingBloc(
              repository: employeeIncidentRepository,
              routeService: routeService,
              hubService: employeeTrackingHubService,
            ),
          ),
        ],
        child: MaterialApp(
          navigatorKey: rootNavigatorKey,
          title: 'SOCAR Dispatch',
          debugShowCheckedModeBanner: false,
          theme: AppTheme.lightTheme,
          home: OnboardingGateView(
            child: AuthGate(
              fcmService: fcmNotificationService,
              trackingHubService: employeeTrackingHubService,
            ),
          ),
        ),
      ),
    );
  }
}

class AuthGate extends StatefulWidget {
  final FcmNotificationService fcmService;
  final EmployeeTrackingHubService trackingHubService;

  const AuthGate({
    super.key,
    required this.fcmService,
    required this.trackingHubService,
  });

  @override
  State<AuthGate> createState() => _AuthGateState();
}

class _AuthGateState extends State<AuthGate> {
  bool _fcmInitialized = false;

  void _setupNotifications(BuildContext context, UserModel user) {
    if (_fcmInitialized) return;
    _fcmInitialized = true;

    widget.fcmService.initialize(
      onNotificationAction: (incidentId, data) {
        if (user.roleType == RoleType.team) {
          final teamId = data['teamId']?.toString();
          if (teamId != null) {
            context.read<TaskBloc>().add(LoadActiveTask(teamId, isRefresh: true));
          }
        }
      },
    );

    widget.trackingHubService.initialize();
  }

  @override
  Widget build(BuildContext context) {
    return BlocConsumer<AuthBloc, AuthState>(
      listenWhen: (previous, current) => current is Unauthenticated || current is Authenticated,
      listener: (context, state) {
        if (state is Unauthenticated) {
          _fcmInitialized = false;
          context.read<LocationStreamRepository>().stopTracking();
          widget.trackingHubService.dispose();
          rootNavigatorKey.currentState?.popUntil((route) => route.isFirst);
        } else if (state is Authenticated) {
          _setupNotifications(context, state.user);
        }
      },
      buildWhen: (previous, current) {
        return current is Authenticated || current is Unauthenticated || current is AuthInitial;
      },
      builder: (context, state) {
        if (state is Authenticated) {
          switch (state.user.roleType) {
            case RoleType.employee:
              return EmployeeHomeView(user: state.user);
            case RoleType.team:
              return TeamHomeView(user: state.user);
            case RoleType.operator:
              return OperatorNoticeView(user: state.user);
          }
        } else if (state is AuthInitial) {
          return const SplashView();
        } else {
          return const LoginView();
        }
      },
    );
  }
}
