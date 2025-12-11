import ReactDOM from "react-dom/client";
import App from "./App";
import "./index.css";

// StrictMode dev'de double-mount yapıp SignalR bağlantısını iki kez başlatıyor.
ReactDOM.createRoot(document.getElementById("root")).render(<App />);

