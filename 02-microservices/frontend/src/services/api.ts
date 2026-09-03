const API_BASE = '/api';

export interface Product {
  id: string;
  name: string;
  description: string;
  price: number;
  stockQuantity: number;
  category: string;
}

export interface Order {
  id: string;
  customerId: string;
  items: OrderItem[];
  totalAmount: number;
  status: string;
  createdAt: string;
  completedAt: string | null;
}

export interface OrderItem {
  productId: string;
  quantity: number;
  unitPrice: number;
}

export interface SagaLog {
  step: string;
  status: string;
  message: string;
  timestamp: string;
}

export const api = {
  getProducts: async (): Promise<Product[]> => {
    const res = await fetch(`${API_BASE}/products`);
    return res.json();
  },

  getProduct: async (id: string): Promise<Product> => {
    const res = await fetch(`${API_BASE}/products/${id}`);
    return res.json();
  },

  createOrder: async (customerId: string, items: { productId: string; quantity: number }[]): Promise<Order> => {
    const res = await fetch(`${API_BASE}/orders`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ customerId, items }),
    });
    return res.json();
  },

  getOrders: async (): Promise<Order[]> => {
    const res = await fetch(`${API_BASE}/orders`);
    return res.json();
  },

  getOrder: async (id: string): Promise<Order> => {
    const res = await fetch(`${API_BASE}/orders/${id}`);
    return res.json();
  },

  getOrderSaga: async (id: string): Promise<SagaLog[]> => {
    const res = await fetch(`${API_BASE}/orders/${id}/saga`);
    return res.json();
  },
};
