import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api, Product } from '../services/api'

export default function OrderForm() {
  const [products, setProducts] = useState<Product[]>([])
  const [customerId, setCustomerId] = useState('')
  const [items, setItems] = useState<{ productId: string; quantity: number }[]>([])
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()

  useEffect(() => {
    api.getProducts().then(setProducts)
  }, [])

  const addItem = () => {
    setItems([...items, { productId: '', quantity: 1 }])
  }

  const updateItem = (index: number, field: string, value: string | number) => {
    const newItems = [...items]
    if (field === 'productId') newItems[index].productId = value as string
    else newItems[index].quantity = value as number
    setItems(newItems)
  }

  const removeItem = (index: number) => {
    setItems(items.filter((_, i) => i !== index))
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!customerId || items.length === 0) return
    setLoading(true)
    try {
      const order = await api.createOrder(customerId, items)
      navigate(`/orders/${order.id}`)
    } catch (err) {
      alert('Failed to create order')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div>
      <h2>Create Order</h2>
      <form onSubmit={handleSubmit}>
        <div style={{ marginBottom: 15 }}>
          <label style={{ display: 'block', marginBottom: 5 }}>Customer ID</label>
          <input
            value={customerId}
            onChange={e => setCustomerId(e.target.value)}
            placeholder="cust-001"
            style={{ padding: 8, width: 300 }}
          />
        </div>

        <div style={{ marginBottom: 15 }}>
          <h3>Order Items</h3>
          {items.map((item, i) => (
            <div key={i} style={{ display: 'flex', gap: 10, marginBottom: 5 }}>
              <select
                value={item.productId}
                onChange={e => updateItem(i, 'productId', e.target.value)}
                style={{ padding: 8 }}
              >
                <option value="">Select product</option>
                {products.map(p => (
                  <option key={p.id} value={p.id}>{p.name} (${p.price})</option>
                ))}
              </select>
              <input
                type="number"
                value={item.quantity}
                onChange={e => updateItem(i, 'quantity', parseInt(e.target.value) || 1)}
                min={1}
                style={{ padding: 8, width: 80 }}
              />
              <button type="button" onClick={() => removeItem(i)} style={{ padding: 8 }}>Remove</button>
            </div>
          ))}
          <button type="button" onClick={addItem} style={{ padding: 8, marginTop: 5 }}>Add Item</button>
        </div>

        <button type="submit" disabled={loading} style={{ padding: '10px 20px', fontSize: 16 }}>
          {loading ? 'Creating...' : 'Create Order'}
        </button>
      </form>
    </div>
  )
}
