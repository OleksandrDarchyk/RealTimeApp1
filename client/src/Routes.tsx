import { createBrowserRouter, RouterProvider } from "react-router-dom";
import RoomsPage from "./pages/RoomsPage";
import RoomPage from "./pages/RoomPage";

export default function Routes() {
    return (
        <RouterProvider
            router={createBrowserRouter([
                { path: "/", element: <RoomsPage /> },
                { path: "/rooms/:roomId", element: <RoomPage /> },
            ])}
        />
    );
}
