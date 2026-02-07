import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { StreamProvider } from "./useStream.tsx";
import { API_BASE_URL } from "./config";

createRoot(document.getElementById('root')!).render(
    <StreamProvider config={{
        urlForStreamEndpoint: `${API_BASE_URL}/connect`,
        connectEvent: "ConnectionResponse",
    }}>
        <App />
    </StreamProvider>,
);
