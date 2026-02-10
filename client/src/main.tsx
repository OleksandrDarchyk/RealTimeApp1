import { createRoot } from "react-dom/client";
import Routes from "./Routes";
import { StreamProvider } from "./useStream";
import { BASE_URL } from "./utils/BASE_URL";
import "./styles.css";
import { Toaster } from "react-hot-toast";

createRoot(document.getElementById("root")!).render(
    <StreamProvider
        config={{
            urlForStreamEndpoint: `${BASE_URL}/connect`,
            connectEvent: "ConnectionResponse",
        }}
    >
        <Toaster position="top-right" />
        <Routes />
    </StreamProvider>
);
