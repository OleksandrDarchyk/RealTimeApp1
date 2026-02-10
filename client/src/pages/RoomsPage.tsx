import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import Login from "./Login";
import type { RoomDto } from "../generated-ts-client";
import { realtimeClient } from "../clients";

export default function RoomsPage() {
    const [rooms, setRooms] = useState<RoomDto[]>([]);
    const [roomId, setRoomId] = useState("room1");
    const navigate = useNavigate();

    useEffect(() => {
        realtimeClient.getRooms().then(setRooms);
    }, []);

    return (
        <div style={{ display: "grid", gap: 16, padding: 16 }}>
            <div>
                <h3>Auth</h3>
                <Login />
            </div>

            <div>
                <h3>Create room</h3>
                <input value={roomId} onChange={(e) => setRoomId(e.target.value)} />
                <button
                    onClick={async () => {
                        if (!roomId.trim()) return;
                        await realtimeClient.createRoom({ roomId });
                        // перезавантажити список
                        const r = await realtimeClient.getRooms();
                        setRooms(r);
                    }}
                >
                    Create
                </button>
            </div>

            <div>
                <h3>Rooms</h3>
                {rooms.map((r) => (
                    <div key={r.id} style={{ display: "flex", gap: 8, alignItems: "center" }}>
                        <div style={{ minWidth: 220 }}>{r.id}</div>
                        <button onClick={() => navigate(`/rooms/${r.id}`)}>Join</button>
                    </div>
                ))}
            </div>
        </div>
    );
}
