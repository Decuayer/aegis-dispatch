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
import 'features/profile/data/models/user_model.dart';
import 'features/profile/data/repositories/media_repository.dart';
import 'features/profile/data/repositories/profile_repository.dart';
import 'features/profile/presentation/cubit/profile_cubit.dart';
import 'features/splash/presentation/views/splash_view.dart';

final GlobalKey<NavigatorState> rootNavigatorKey = GlobalKey<NavigatorState>();

class SocarDispatchApp extends StatelessWidget {
  final AuthRepository authRepository;
  final ProfileRepository profileRepository;
  final MediaRepository mediaRepository;

  const SocarDispatchApp({
    super.key,
    required this.authRepository,
    required this.profileRepository,
    required this.mediaRepository,
  });

  @override
  Widget build(BuildContext context) {
    return MultiRepositoryProvider(
      providers: [
        RepositoryProvider.value(value: authRepository),
        RepositoryProvider.value(value: profileRepository),
        RepositoryProvider.value(value: mediaRepository),
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
        // Pop all stacked screens when session ends
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
