import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, Order } from '../services/api'

const statusColors: Record<string, string> = {
  Pending: '#ffc107',
  StockReserved: '#17a2b8',
  Completed: '#28a745',
  Failed: '#dc3545',
  Compensating: '#fd7e14',
  Compensated: '#6c757d',
}

export default function OrderList() {
  const [orders, setOrders] = useState<Order[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    api.getOrders().then(setOrders).finally(() => setLoading(false))
  }, [])

  if (loading) return <p>Loading orders...</p>

  return (
    <div>
      <h2>Orders</h2>
      <table style={{ width: '100%', borderCollapse: 'collapse' }}>
        <thead>
          <tr style={{ borderBottom: '2px solid #ddd', textAlign: 'left' }}>
            <th style={{ padding: 8 }}>ID</th>
            <th style={{ padding: 8 }}>Customer</th>
            <th style={{ padding: 8 }}>Total</th>
            <th style={{ padding: 8 }}>Status</th>
            <th style={{ padding: 8 }}>Created</th>
          </tr>
        </thead>
        <tbody>
          {orders.map(o => (
            <tr key={o.id} style={{ borderBottom: '1px solid #eee' }}>
              <td style={{ padding: 8 }}>
                <Link to={`/orders/${o.id}`}>{o.id}</Link>
              </td>
              <td style={{ padding: 8 }}>{o.customerId}</td>
              <td style={{ padding: 8 }}>${o.totalAmount.toFixed(2)}</td>
              <td style={{ padding: 8 }}>
                <span style={{
                  padding: '2px 8px',
                  borderRadius: 4,
                  color: 'white',
                  backgroundColor: statusColors[o.status] || '#6c757d'
                }}>
                  {o.status}
                </span>
              </td>
              <td style={{ padding: 8 }}>{new Date(o.createdAt).toLocaleString()}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
