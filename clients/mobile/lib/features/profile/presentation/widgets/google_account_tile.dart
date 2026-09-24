import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../../core/constants/app_colors.dart';
import '../../../../core/services/google_auth_service.dart';
import '../../data/models/user_model.dart';
import '../cubit/profile_cubit.dart';

class GoogleAccountTile extends StatefulWidget {
  final UserModel user;
  final bool isUpdating;

  const GoogleAccountTile({
    super.key,
    required this.user,
    this.isUpdating = false,
  });

  @override
  State<GoogleAccountTile> createState() => _GoogleAccountTileState();
}

class _GoogleAccountTileState extends State<GoogleAccountTile> {
  bool _isActionInProgress = false;

  Future<void> _linkGoogleAccount() async {
    setState(() => _isActionInProgress = true);
    try {
      final idToken = await GoogleAuthService.signInAndGetIdToken();
      if (idToken == null) {
        setState(() => _isActionInProgress = false);
        return;
      }
      if (!mounted) return;
      await context.read<ProfileCubit>().linkGoogleAccount(
        widget.user,
        idToken,
      );
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            'Google bağlantı hatası: ${e.toString().replaceAll('Exception: ', '')}',
          ),
          backgroundColor: AppColors.error,
          behavior: SnackBarBehavior.floating,
        ),
      );
    } finally {
      if (mounted) {
        setState(() => _isActionInProgress = false);
      }
    }
  }

  Future<void> _unlinkGoogleAccount() async {
    final shouldUnlink = await showDialog<bool>(
      context: context,
      builder:
          (ctx) => AlertDialog(
            title: const Text('Google Bağlantısını Kaldır'),
            content: const Text(
              'Google hesabı bağlantısını kaldırmak istediğinize emin misiniz? Kaldırdıktan sonra Google ile giriş yapamayacaksınız.',
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(ctx, false),
                child: const Text('İptal'),
              ),
              ElevatedButton(
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.error,
                ),
                onPressed: () => Navigator.pop(ctx, true),
                child: const Text('Bağlantıyı Kaldır'),
              ),
            ],
          ),
    );

    if (shouldUnlink != true) return;
    if (!mounted) return;

    setState(() => _isActionInProgress = true);
    try {
      await context.read<ProfileCubit>().unlinkGoogleAccount(widget.user);
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            'Bağlantı kaldırılamadı: ${e.toString().replaceAll('Exception: ', '')}',
          ),
          backgroundColor: AppColors.error,
          behavior: SnackBarBehavior.floating,
        ),
      );
    } finally {
      if (mounted) {
        setState(() => _isActionInProgress = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final isLinked = widget.user.isGoogleLinked;
    final busy = widget.isUpdating || _isActionInProgress;

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color:
                      isLinked
                          ? AppColors.success.withValues(alpha: 0.12)
                          : AppColors.textMuted.withValues(alpha: 0.1),
                  shape: BoxShape.circle,
                ),
                child: Icon(
                  isLinked
                      ? Icons.check_circle_outline_rounded
                      : Icons.link_rounded,
                  color: isLinked ? AppColors.success : AppColors.textMuted,
                  size: 22,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'Google Hesabı',
                      style: TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.bold,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      isLinked
                          ? (widget.user.googleEmail ?? 'Bağlandı')
                          : 'Bağlı Değil',
                      style: TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w600,
                        color:
                            isLinked
                                ? AppColors.success
                                : AppColors.textSecondary,
                      ),
                    ),
                  ],
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                decoration: BoxDecoration(
                  color:
                      isLinked
                          ? AppColors.success.withValues(alpha: 0.1)
                          : AppColors.textMuted.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  isLinked ? 'BAĞLI' : 'BAĞLANMADI',
                  style: TextStyle(
                    fontSize: 10,
                    fontWeight: FontWeight.w700,
                    color: isLinked ? AppColors.success : AppColors.textMuted,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Text(
            isLinked
                ? 'Bu hesaba Google hesabınız bağlıdır. Hem mobil uygulamaya hem web panele Google ile tek tıkla şifresiz giriş yapabilirsiniz.'
                : 'Google hesabınızı bu kullanıcıya bağlayarak hem mobil uygulamaya hem de web panele Google ile tek tıkla giriş yapabilirsiniz.',
            style: const TextStyle(
              fontSize: 12,
              color: AppColors.textSecondary,
              height: 1.4,
            ),
          ),
          const SizedBox(height: 14),
          if (isLinked)
            OutlinedButton.icon(
              style: OutlinedButton.styleFrom(
                minimumSize: const Size.fromHeight(42),
                foregroundColor: AppColors.error,
                side: BorderSide(color: AppColors.error.withValues(alpha: 0.4)),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(10),
                ),
              ),
              onPressed: busy ? null : _unlinkGoogleAccount,
              icon:
                  busy
                      ? const SizedBox(
                        width: 16,
                        height: 16,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: AppColors.error,
                        ),
                      )
                      : const Icon(Icons.link_off_rounded, size: 18),
              label: Text(busy ? 'İşleniyor...' : 'Google Bağlantısını Kaldır'),
            )
          else
            OutlinedButton.icon(
              style: OutlinedButton.styleFrom(
                minimumSize: const Size.fromHeight(44),
                backgroundColor: AppColors.surface,
                side: const BorderSide(color: AppColors.border, width: 1.2),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(10),
                ),
              ),
              onPressed: busy ? null : _linkGoogleAccount,
              icon:
                  busy
                      ? const SizedBox(
                        width: 16,
                        height: 16,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                      : const Icon(
                        Icons.g_mobiledata_rounded,
                        size: 28,
                        color: AppColors.primaryDark,
                      ),
              label: Text(
                busy ? 'Bağlanıyor...' : 'Google Hesabını Bağla',
                style: const TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w600,
                  color: AppColors.textPrimary,
                ),
              ),
            ),
        ],
      ),
    );
  }
}
