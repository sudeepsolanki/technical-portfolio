import { BrowserRouter, Routes, Route } from 'react-router-dom'
import ProductList from './components/ProductList'
import OrderForm from './components/OrderForm'
import OrderList from './components/OrderList'
import OrderDetail from './components/OrderDetail'

function App() {
  return (
    <BrowserRouter>
      <div style={{ fontFamily: 'system-ui, sans-serif', maxWidth: 1200, margin: '0 auto', padding: '20px' }}>
        <h1>Microservices Demo</h1>
        <p style={{ color: '#666' }}>gRPC + RabbitMQ + Saga Pattern</p>
        <nav style={{ marginBottom: 20 }}>
          <a href="/products" style={{ marginRight: 15 }}>Products</a>
          <a href="/orders" style={{ marginRight: 15 }}>Orders</a>
          <a href="/orders/new" style={{ marginRight: 15 }}>New Order</a>
        </nav>
        <Routes>
          <Route path="/products" element={<ProductList />} />
          <Route path="/orders" element={<OrderList />} />
          <Route path="/orders/new" element={<OrderForm />} />
          <Route path="/orders/:id" element={<OrderDetail />} />
        </Routes>
      </div>
    </BrowserRouter>
  )
}

export default App
