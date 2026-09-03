import {
  Product,
  Category,
  PaginatedResponse,
  ProductQueryParams,
  CreateProductRequest,
} from './types';

const BASE_URL = '/api';

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: 'An error occurred' }));
    throw error;
  }
  return response.json();
}

export async function getProducts(
  params: ProductQueryParams = {}
): Promise<PaginatedResponse<Product>> {
  const searchParams = new URLSearchParams();
  if (params.page) searchParams.set('page', params.page.toString());
  if (params.pageSize) searchParams.set('pageSize', params.pageSize.toString());
  if (params.search) searchParams.set('search', params.search);
  if (params.sortBy) searchParams.set('sortBy', params.sortBy);
  if (params.sortDescending !== undefined)
    searchParams.set('sortDescending', params.sortDescending.toString());
  if (params.categoryId) searchParams.set('categoryId', params.categoryId.toString());

  const response = await fetch(`${BASE_URL}/products?${searchParams.toString()}`);
  return handleResponse<PaginatedResponse<Product>>(response);
}

export async function getProduct(id: number): Promise<Product> {
  const response = await fetch(`${BASE_URL}/products/${id}`);
  return handleResponse<Product>(response);
}

export async function createProduct(data: CreateProductRequest): Promise<Product> {
  const response = await fetch(`${BASE_URL}/products`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  });
  return handleResponse<Product>(response);
}

export async function updateProduct(
  id: number,
  data: CreateProductRequest
): Promise<Product> {
  const response = await fetch(`${BASE_URL}/products/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  });
  return handleResponse<Product>(response);
}

export async function deleteProduct(id: number): Promise<void> {
  const response = await fetch(`${BASE_URL}/products/${id}`, {
    method: 'DELETE',
  });
  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: 'An error occurred' }));
    throw error;
  }
}

export async function getCategories(): Promise<Category[]> {
  const response = await fetch(`${BASE_URL}/categories`);
  return handleResponse<Category[]>(response);
}
