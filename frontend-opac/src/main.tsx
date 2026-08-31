import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { App } from '@/App';
import '@/styles.css';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // Trang tra cứu chủ yếu là đọc, dữ liệu đổi chậm: giữ kết quả một phút để chuyển qua lại
      // giữa các trang không phải gọi lại máy chủ.
      staleTime: 60_000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </QueryClientProvider>
  </React.StrictMode>,
);
