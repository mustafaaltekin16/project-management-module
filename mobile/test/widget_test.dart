import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'package:ozveri_mobile/app.dart';

void main() {
  testWidgets('App builds and shows the login screen when logged out', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(const ProviderScope(child: OzveriMobileApp()));
    await tester.pumpAndSettle();

    expect(find.text('Özveri-0047'), findsOneWidget);
    expect(find.text('Giriş Yap'), findsOneWidget);
    expect(find.text('E-posta'), findsOneWidget);
    expect(find.text('Şifre'), findsOneWidget);
  });
}
