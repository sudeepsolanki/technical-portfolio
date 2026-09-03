import { useState } from 'react';
import { Product, PaginatedResponse, ProductQueryParams } from '../types';

interface ProductListProps {
  data: PaginatedResponse<Product> | null;
  loading: boolean;
  query: ProductQueryParams;
  onQueryChange: (query: ProductQueryParams) => void;
  onEdit: (product: Product) => void;
  onDelete: (id: number) => void;
}

function ProductList({
  data,
  loading,
  query,
  onQueryChange,
  onEdit,
  onDelete,
}: ProductListProps) {
  const [searchInput, setSearchInput] = useState(query.search || '');

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    onQueryChange({ ...query, search: searchInput, page: 1 });
  };

  const handleSort = (field: string) => {
    const isCurrentField = query.sortBy === field;
    onQueryChange({
      ...query,
      sortBy: field,
      sortDescending: isCurrentField ? !query.sortDescending : false,
      page: 1,
    });
  };

  const handlePageChange = (newPage: number) => {
    onQueryChange({ ...query, page: newPage });
  };

  const getSortIndicator = (field: string) => {
    if (query.sortBy !== field) return '';
    return query.sortDescending ? ' ▼' : ' ▲';
  };

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(price);
  };

  if (loading) {
    return (
      <div className="loading-container">
        <div className="spinner"></div>
        <p>Loading products...</p>
      </div>
    );
  }

  return (
    <div className="product-list">
      <form className="search-bar" onSubmit={handleSearch}>
        <input
          type="text"
          placeholder="Search products..."
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          className="search-input"
        />
        <button type="submit" className="btn btn-secondary">
          Search
        </button>
        {searchInput && (
          <button
            type="button"
            className="btn btn-text"
            onClick={() => {
              setSearchInput('');
              onQueryChange({ ...query, search: '', page: 1 });
            }}
          >
            Clear
          </button>
        )}
      </form>

      {!data || data.items.length === 0 ? (
        <div className="empty-state">
          <p>No products found.</p>
        </div>
      ) : (
        <>
          <div className="table-container">
            <table className="product-table">
              <thead>
                <tr>
                  <th onClick={() => handleSort('name')} className="sortable">
                    Name{getSortIndicator('name')}
                  </th>
                  <th onClick={() => handleSort('sku')} className="sortable">
                    SKU{getSortIndicator('sku')}
                  </th>
                  <th onClick={() => handleSort('price')} className="sortable">
                    Price{getSortIndicator('price')}
                  </th>
                  <th onClick={() => handleSort('stockQuantity')} className="sortable">
                    Stock{getSortIndicator('stockQuantity')}
                  </th>
                  <th>Category</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((product) => (
                  <tr key={product.id}>
                    <td className="product-name">{product.name}</td>
                    <td className="product-sku">{product.sku}</td>
                    <td className="product-price">{formatPrice(product.price)}</td>
                    <td className="product-stock">
                      <span
                        className={`stock-badge ${
                          product.stockQuantity > 0 ? 'in-stock' : 'out-of-stock'
                        }`}
                      >
                        {product.stockQuantity}
                      </span>
                    </td>
                    <td className="product-category">{product.categoryName}</td>
                    <td className="product-actions">
                      <button
                        className="btn btn-small btn-secondary"
                        onClick={() => onEdit(product)}
                      >
                        Edit
                      </button>
                      <button
                        className="btn btn-small btn-danger"
                        onClick={() => onDelete(product.id)}
                      >
                        Delete
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="pagination">
            <span className="pagination-info">
              Showing {(data.page - 1) * data.pageSize + 1} to{' '}
              {Math.min(data.page * data.pageSize, data.totalCount)} of{' '}
              {data.totalCount} products
            </span>
            <div className="pagination-controls">
              <button
                className="btn btn-small btn-secondary"
                disabled={data.page <= 1}
                onClick={() => handlePageChange(data.page - 1)}
              >
                Previous
              </button>
              {Array.from({ length: data.totalPages }, (_, i) => i + 1)
                .filter(
                  (p) =>
                    p === 1 ||
                    p === data.totalPages ||
                    Math.abs(p - data.page) <= 2
                )
                .reduce<(number | string)[]>((acc, p, i, arr) => {
                  if (i > 0 && p - (arr[i - 1] as number) > 1) {
                    acc.push('...');
                  }
                  acc.push(p);
                  return acc;
                }, [])
                .map((p, i) =>
                  typeof p === 'string' ? (
                    <span key={`ellipsis-${i}`} className="pagination-ellipsis">
                      ...
                    </span>
                  ) : (
                    <button
                      key={p}
                      className={`btn btn-small ${
                        p === data.page ? 'btn-primary' : 'btn-secondary'
                      }`}
                      onClick={() => handlePageChange(p)}
                    >
                      {p}
                    </button>
                  )
                )}
              <button
                className="btn btn-small btn-secondary"
                disabled={data.page >= data.totalPages}
                onClick={() => handlePageChange(data.page + 1)}
              >
                Next
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

export default ProductList;
