import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/api/api_response.dart';
import '../../../../core/theme/app_theme.dart';
import '../controllers/auth_controller.dart';
import '../widgets/login_form.dart';

class LoginScreen extends ConsumerWidget {
  const LoginScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authControllerProvider);

    String? errorMessage;
    if (authState.hasError) {
      final error = authState.error;
      errorMessage = error is ApiException
          ? error.message
          : 'Giriş yapılamadı, lütfen tekrar deneyin.';
    }

    return Scaffold(
      body: Stack(
        children: [
          Positioned.fill(child: _LoginBackdrop()),
          SafeArea(
            child: Center(
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(24),
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 380),
                  child: LoginForm(
                    isLoading: authState.isLoading,
                    errorMessage: errorMessage,
                    onSubmit: (email, password) {
                      ref
                          .read(authControllerProvider.notifier)
                          .login(email, password);
                    },
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

/// Düz zemini kırmak için temanın indigo/mor paletiyle uyumlu,
/// yumuşak gradyanlı dekoratif lekeler.
class _LoginBackdrop extends StatelessWidget {
  const _LoginBackdrop();

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Stack(
      children: [
        Positioned(
          top: -90,
          right: -70,
          child: _Blob(
            size: 260,
            opacity: isDark ? 0.22 : 0.30,
            gradient: AppGradients.primary,
          ),
        ),
        Positioned(
          bottom: -110,
          left: -80,
          child: _Blob(
            size: 300,
            opacity: isDark ? 0.16 : 0.22,
            gradient: const LinearGradient(
              colors: [Color(0xFF0D9488), Color(0xFF4F46E5)],
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
            ),
          ),
        ),
      ],
    );
  }
}

class _Blob extends StatelessWidget {
  const _Blob({
    required this.size,
    required this.opacity,
    required this.gradient,
  });

  final double size;
  final double opacity;
  final Gradient gradient;

  @override
  Widget build(BuildContext context) {
    return Opacity(
      opacity: opacity,
      child: Container(
        width: size,
        height: size,
        decoration: BoxDecoration(shape: BoxShape.circle, gradient: gradient),
      ),
    );
  }
}
