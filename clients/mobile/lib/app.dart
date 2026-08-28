import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'core/theme/app_theme.dart';
import 'features/auth/data/repositories/auth_repository.dart';
import 'features/auth/presentation/bloc/auth_bloc.dart';
import 'features/auth/presentation/bloc/auth_event.dart';
import 'features/auth/presentation/bloc/auth_state.dart';
import 'features/auth/presentation/views/login_view.dart';
import 'features/home/presentation/views/employee_home_view.dart';
import 'features/home/presentation/views/operator_notice_view.dart';
import 'features/home/presentation/views/team_home_view.dart';
import 'features/incident_reporting/data/repositories/incident_repository.dart';
import 'features/incident_reporting/presentation/bloc/incident_report_bloc.dart';
import 'features/incident_reporting/services/location_service.dart';
import 'features/incident_reporting/services/media_picker_service.dart';
import 'features/profile/data/models/user_model.dart';
import 'features/profile/data/repositories/media_repository.dart';
import 'features/profile/data/repositories/profile_repository.dart';
import 'features/profile/presentation/cubit/profile_cubit.dart';
import 'features/splash/presentation/views/splash_view.dart';
import 'features/team_tasks/data/repositories/task_repository.dart';
import 'features/team_tasks/presentation/bloc/task_bloc.dart';
import 'features/team_tasks/services/route_service.dart';

final GlobalKey<NavigatorState> rootNavigatorKey = GlobalKey<NavigatorState>();

class SocarDispatchApp extends StatelessWidget {
  final AuthRepository authRepository;
  final ProfileRepository profileRepository;
  final MediaRepository mediaRepository;
  final IncidentRepository incidentRepository;
  final TaskRepository taskRepository;
  final LocationService locationService;
  final MediaPickerService mediaPickerService;
  final RouteService routeService;

  const SocarDispatchApp({
    super.key,
    required this.authRepository,
    required this.profileRepository,
    required this.mediaRepository,
    required this.incidentRepository,
    required this.taskRepository,
    required this.locationService,
    required this.mediaPickerService,
    required this.routeService,
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
        RepositoryProvider.value(value: mediaPickerService),
        RepositoryProvider.value(value: routeService),
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
            create: (ctx) => TaskBloc(
              taskRepository: taskRepository,
              routeService: routeService,
              locationService: locationService,
            ),
          ),
        ],
        child: MaterialApp(
          navigatorKey: rootNavigatorKey,
          title: 'SOCAR Dispatch',
          debugShowCheckedModeBanner: false,
          theme: AppTheme.lightTheme,
          home: const AuthGate(),
        ),
      ),
    );
  }
}

class AuthGate extends StatelessWidget {
  const AuthGate({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocConsumer<AuthBloc, AuthState>(
      listenWhen: (previous, current) => current is Unauthenticated,
      listener: (context, state) {
        rootNavigatorKey.currentState?.popUntil((route) => route.isFirst);
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
