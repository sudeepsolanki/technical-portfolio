import { useEffect, useState } from 'react'
import { api, Product } from '../services/api'

export default function ProductList() {
  const [products, setProducts] = useState<Product[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    api.getProducts().then(setProducts).finally(() => setLoading(false))
  }, [])

  if (loading) return <p>Loading products...</p>

  return (
    <div>
      <h2>Products</h2>
      <table style={{ width: '100%', borderCollapse: 'collapse' }}>
        <thead>
          <tr style={{ borderBottom: '2px solid #ddd', textAlign: 'left' }}>
            <th style={{ padding: 8 }}>ID</th>
            <th style={{ padding: 8 }}>Name</th>
            <th style={{ padding: 8 }}>Price</th>
            <th style={{ padding: 8 }}>Stock</th>
            <th style={{ padding: 8 }}>Category</th>
          </tr>
        </thead>
        <tbody>
          {products.map(p => (
            <tr key={p.id} style={{ borderBottom: '1px solid #eee' }}>
              <td style={{ padding: 8, fontFamily: 'monospace' }}>{p.id}</td>
              <td style={{ padding: 8 }}>{p.name}</td>
              <td style={{ padding: 8 }}>${p.price.toFixed(2)}</td>
              <td style={{ padding: 8 }}>{p.stockQuantity}</td>
              <td style={{ padding: 8 }}>{p.category}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
