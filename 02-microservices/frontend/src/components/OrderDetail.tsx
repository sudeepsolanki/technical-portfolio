import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { api, Order, SagaLog } from '../services/api'

const statusColors: Record<string, string> = {
  Pending: '#ffc107',
  Started: '#17a2b8',
  StockReserved: '#17a2b8',
  Completed: '#28a745',
  Failed: '#dc3545',
  Compensating: '#fd7e14',
  Compensated: '#6c757d',
}

export default function OrderDetail() {
  const { id } = useParams<{ id: string }>()
  const [order, setOrder] = useState<Order | null>(null)
  const [sagaLogs, setSagaLogs] = useState<SagaLog[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!id) return
    Promise.all([api.getOrder(id), api.getOrderSaga(id)])
      .then(([o, logs]) => { setOrder(o); setSagaLogs(logs); })
      .finally(() => setLoading(false))
  }, [id])

  if (loading) return <p>Loading order...</p>
  if (!order) return <p>Order not found</p>

  return (
    <div>
      <Link to="/orders">&larr; Back to orders</Link>
      <h2>Order {order.id}</h2>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 20, marginBottom: 20 }}>
        <div>
          <h3>Details</h3>
          <p><strong>Customer:</strong> {order.customerId}</p>
          <p><strong>Status:</strong>
            <span style={{
              marginLeft: 8,
              padding: '2px 8px',
              borderRadius: 4,
              color: 'white',
              backgroundColor: statusColors[order.status] || '#6c757d'
            }}>
              {order.status}
            </span>
          </p>
          <p><strong>Total:</strong> ${order.totalAmount.toFixed(2)}</p>
          <p><strong>Created:</strong> {new Date(order.createdAt).toLocaleString()}</p>
          {order.completedAt && (
            <p><strong>Completed:</strong> {new Date(order.completedAt).toLocaleString()}</p>
          )}
        </div>

        <div>
          <h3>Items</h3>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ borderBottom: '2px solid #ddd', textAlign: 'left' }}>
                <th style={{ padding: 8 }}>Product</th>
                <th style={{ padding: 8 }}>Qty</th>
                <th style={{ padding: 8 }}>Price</th>
              </tr>
            </thead>
            <tbody>
              {order.items.map((item, i) => (
                <tr key={i} style={{ borderBottom: '1px solid #eee' }}>
                  <td style={{ padding: 8, fontFamily: 'monospace' }}>{item.productId}</td>
                  <td style={{ padding: 8 }}>{item.quantity}</td>
                  <td style={{ padding: 8 }}>${(item.unitPrice * item.quantity).toFixed(2)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <div>
        <h3>Saga Execution Log</h3>
        {sagaLogs.length === 0 ? (
          <p>No saga logs yet</p>
        ) : (
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ borderBottom: '2px solid #ddd', textAlign: 'left' }}>
                <th style={{ padding: 8 }}>Step</th>
                <th style={{ padding: 8 }}>Status</th>
                <th style={{ padding: 8 }}>Message</th>
                <th style={{ padding: 8 }}>Time</th>
              </tr>
            </thead>
            <tbody>
              {sagaLogs.map((log, i) => (
                <tr key={i} style={{ borderBottom: '1px solid #eee' }}>
                  <td style={{ padding: 8 }}>{log.step}</td>
                  <td style={{ padding: 8 }}>
                    <span style={{
                      padding: '2px 8px',
                      borderRadius: 4,
                      color: 'white',
                      backgroundColor: statusColors[log.status] || '#6c757d'
                    }}>
                      {log.status}
                    </span>
                  </td>
                  <td style={{ padding: 8 }}>{log.message}</td>
                  <td style={{ padding: 8 }}>{new Date(log.timestamp).toLocaleTimeString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}
