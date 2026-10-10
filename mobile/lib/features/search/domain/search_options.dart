/// Whose listings to show. [apiValue] is what the backend expects.
enum SellerType {
  all('all'),
  store('store'),
  individual('individual');

  const SellerType(this.apiValue);
  final String apiValue;
}

/// How results are ordered. [apiValue] is what the backend expects.
enum SearchSort {
  date('date'),
  priceAsc('price_asc'),
  priceDesc('price_desc');

  const SearchSort(this.apiValue);
  final String apiValue;
}
