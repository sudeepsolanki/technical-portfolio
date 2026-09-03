import { useState, useEffect } from 'react';
import { Product, Category, CreateProductRequest, ApiError } from '../types';
import { createProduct, updateProduct } from '../api';

interface ProductFormProps {
  product: Product | null;
  categories: Category[];
  onCancel: () => void;
  onSuccess: () => void;
}

interface FormErrors {
  name?: string;
  description?: string;
  price?: string;
  sku?: string;
  stockQuantity?: string;
  imageUrl?: string;
  categoryId?: string;
  general?: string;
}

function ProductForm({ product, categories, onCancel, onSuccess }: ProductFormProps) {
  const [formData, setFormData] = useState<CreateProductRequest>({
    name: '',
    description: '',
    price: 0,
    sku: '',
    stockQuantity: 0,
    imageUrl: '',
    categoryId: 0,
  });
  const [errors, setErrors] = useState<FormErrors>({});
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (product) {
      setFormData({
        name: product.name,
        description: product.description,
        price: product.price,
        sku: product.sku,
        stockQuantity: product.stockQuantity,
        imageUrl: product.imageUrl,
        categoryId: product.categoryId,
      });
    }
  }, [product]);

  const validate = (): boolean => {
    const newErrors: FormErrors = {};

    if (!formData.name.trim()) {
      newErrors.name = 'Name is required';
    } else if (formData.name.length > 200) {
      newErrors.name = 'Name must be 200 characters or less';
    }

    if (!formData.sku.trim()) {
      newErrors.sku = 'SKU is required';
    } else if (formData.sku.length > 50) {
      newErrors.sku = 'SKU must be 50 characters or less';
    }

    if (formData.price < 0) {
      newErrors.price = 'Price must be 0 or greater';
    }

    if (formData.stockQuantity < 0) {
      newErrors.stockQuantity = 'Stock quantity must be 0 or greater';
    }

    if (formData.categoryId <= 0) {
      newErrors.categoryId = 'Please select a category';
    }

    if (formData.description && formData.description.length > 2000) {
      newErrors.description = 'Description must be 2000 characters or less';
    }

    if (formData.imageUrl && formData.imageUrl.length > 500) {
      newErrors.imageUrl = 'Image URL must be 500 characters or less';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;

    setSubmitting(true);
    setErrors({});

    try {
      if (product) {
        await updateProduct(product.id, formData);
      } else {
        await createProduct(formData);
      }
      onSuccess();
    } catch (err: any) {
      const apiError = err as ApiError;
      if (apiError.errors) {
        const fieldErrors: FormErrors = {};
        apiError.errors.forEach((e) => {
          fieldErrors[e.field as keyof FormErrors] = e.message;
        });
        setErrors(fieldErrors);
      } else {
        setErrors({ general: apiError.message || 'An error occurred' });
      }
    } finally {
      setSubmitting(false);
    }
  };

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]:
        name === 'price' || name === 'stockQuantity' || name === 'categoryId'
          ? Number(value)
          : value,
    }));
    if (errors[name as keyof FormErrors]) {
      setErrors((prev) => ({ ...prev, [name]: undefined }));
    }
  };

  return (
    <div className="product-form">
      <h2>{product ? 'Edit Product' : 'Create Product'}</h2>

      {errors.general && <div className="alert alert-error">{errors.general}</div>}

      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label htmlFor="name">Name *</label>
          <input
            id="name"
            name="name"
            type="text"
            value={formData.name}
            onChange={handleChange}
            className={errors.name ? 'input-error' : ''}
            maxLength={200}
          />
          {errors.name && <span className="field-error">{errors.name}</span>}
        </div>

        <div className="form-row">
          <div className="form-group">
            <label htmlFor="sku">SKU *</label>
            <input
              id="sku"
              name="sku"
              type="text"
              value={formData.sku}
              onChange={handleChange}
              className={errors.sku ? 'input-error' : ''}
              maxLength={50}
            />
            {errors.sku && <span className="field-error">{errors.sku}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="categoryId">Category *</label>
            <select
              id="categoryId"
              name="categoryId"
              value={formData.categoryId}
              onChange={handleChange}
              className={errors.categoryId ? 'input-error' : ''}
            >
              <option value={0}>Select a category</option>
              {categories.map((cat) => (
                <option key={cat.id} value={cat.id}>
                  {cat.name}
                </option>
              ))}
            </select>
            {errors.categoryId && (
              <span className="field-error">{errors.categoryId}</span>
            )}
          </div>
        </div>

        <div className="form-row">
          <div className="form-group">
            <label htmlFor="price">Price *</label>
            <input
              id="price"
              name="price"
              type="number"
              step="0.01"
              min="0"
              value={formData.price}
              onChange={handleChange}
              className={errors.price ? 'input-error' : ''}
            />
            {errors.price && <span className="field-error">{errors.price}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="stockQuantity">Stock Quantity *</label>
            <input
              id="stockQuantity"
              name="stockQuantity"
              type="number"
              min="0"
              value={formData.stockQuantity}
              onChange={handleChange}
              className={errors.stockQuantity ? 'input-error' : ''}
            />
            {errors.stockQuantity && (
              <span className="field-error">{errors.stockQuantity}</span>
            )}
          </div>
        </div>

        <div className="form-group">
          <label htmlFor="description">Description</label>
          <textarea
            id="description"
            name="description"
            value={formData.description}
            onChange={handleChange}
            className={errors.description ? 'input-error' : ''}
            rows={4}
            maxLength={2000}
          />
          {errors.description && (
            <span className="field-error">{errors.description}</span>
          )}
        </div>

        <div className="form-group">
          <label htmlFor="imageUrl">Image URL</label>
          <input
            id="imageUrl"
            name="imageUrl"
            type="url"
            value={formData.imageUrl}
            onChange={handleChange}
            className={errors.imageUrl ? 'input-error' : ''}
            placeholder="https://example.com/image.jpg"
            maxLength={500}
          />
          {errors.imageUrl && <span className="field-error">{errors.imageUrl}</span>}
        </div>

        <div className="form-actions">
          <button
            type="button"
            className="btn btn-secondary"
            onClick={onCancel}
            disabled={submitting}
          >
            Cancel
          </button>
          <button type="submit" className="btn btn-primary" disabled={submitting}>
            {submitting ? 'Saving...' : product ? 'Update Product' : 'Create Product'}
          </button>
        </div>
      </form>
    </div>
  );
}

export default ProductForm;
