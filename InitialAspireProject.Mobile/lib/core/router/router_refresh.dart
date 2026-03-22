
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Converts a Riverpod provider stream into a [Listenable] that GoRouter
/// can use for its `refreshListenable` parameter.
class RouterRefresh extends ChangeNotifier {
  late final ProviderSubscription _subscription;

  RouterRefresh(Ref ref, ProviderListenable provider) {
    _subscription = ref.listen(provider, (_, __) {
      notifyListeners();
    });
  }

  @override
  void dispose() {
    _subscription.close();
    super.dispose();
  }
}
