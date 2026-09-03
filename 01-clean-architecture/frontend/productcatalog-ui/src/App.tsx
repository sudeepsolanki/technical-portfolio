import { useState, useEffect, useCallback } from 'react';
import ProductList from './components/ProductList';
import ProductForm from './components/ProductForm';
import CategoryList from './components/CategoryList';
import { Product, Category, PaginatedResponse, ProductQueryParams } from './types';
import { getProducts, getCategories, deleteProduct } from './api';

type View = 'list' | 'create' | 'edit';

function App() {
  const [view, setView] = useState<View>('list');
  const [products, setProducts] = useState<PaginatedResponse<Product> | null>(null);
  const [categories, setCategories] = useState<Category[]>([]);
  const [selectedProduct, setSelectedProduct] = useState<Product | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [query, setQuery] = useState<ProductQueryParams>({
    page: 1,
    pageSize: 10,
    sortBy: 'name',
    sortDescending: false,
  });

  const fetchProducts = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getProducts(query);
      setProducts(data);
    } catch (err: any) {
      setError(err.message || 'Failed to fetch products');
    } finally {
      setLoading(false);
    }
  }, [query]);

  const fetchCategories = async () => {
    try {
      const data = await getCategories();
      setCategories(data);
    } catch {
      // Silently fail - categories are optional
    }
  };

  useEffect(() => {
    fetchProducts();
  }, [fetchProducts]);

  useEffect(() => {
    fetchCategories();
  }, []);

  const handleDelete = async (id: number) => {
    if (!window.confirm('Are you sure you want to delete this product?')) return;
    try {
      await deleteProduct(id);
      fetchProducts();
    } catch (err: any) {
      alert(err.message || 'Failed to delete product');
    }
  };

  const handleEdit = (product: Product) => {
    setSelectedProduct(product);
    setView('edit');
  };

  const handleFormSuccess = () => {
    setView('list');
    setSelectedProduct(null);
    fetchProducts();
  };

  const handleCategoryFilter = (categoryId: number | null) => {
    setQuery((prev) => ({
      ...prev,
      categoryId: categoryId ?? undefined,
      page: 1,
    }));
  };

  return (
    <div className="app">
      <header className="app-header">
        <div className="header-content">
          <h1 onClick={() => setView('list')} style={{ cursor: 'pointer' }}>
            Product Catalog
          </h1>
          {view === 'list' && (
            <button className="btn btn-primary" onClick={() => setView('create')}>
              + Add Product
            </button>
          )}
        </div>
      </header>

      <main className="app-main">
        {view === 'list' && (
          <div className="content-layout">
            <aside className="sidebar">
              <CategoryList
                categories={categories}
                selectedCategoryId={query.categoryId ?? null}
                onSelect={handleCategoryFilter}
              />
            </aside>
            <section className="content">
              {error && <div className="alert alert-error">{error}</div>}
              <ProductList
                data={products}
                loading={loading}
                query={query}
                onQueryChange={setQuery}
                onEdit={handleEdit}
                onDelete={handleDelete}
              />
            </section>
          </div>
        )}

        {(view === 'create' || view === 'edit') && (
          <div className="form-container">
            <ProductForm
              product={selectedProduct}
              categories={categories}
              onCancel={() => {
                setView('list');
                setSelectedProduct(null);
              }}
              onSuccess={handleFormSuccess}
            />
          </div>
        )}
      </main>
    </div>
  );
}

export default App;
