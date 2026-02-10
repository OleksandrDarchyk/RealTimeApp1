import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import toast from "react-hot-toast";
import Login from "./Login";
import type { RoomDto } from "../generated-ts-client";
import { realtimeClient } from "../clients";

export default function RoomsPage() {
    const [rooms, setRooms] = useState<RoomDto[]>([]);
    const [roomId, setRoomId] = useState("room1");
    const [loading, setLoading] = useState(true);
    const [creating, setCreating] = useState(false);
    const navigate = useNavigate();

    useEffect(() => {
        realtimeClient
            .getRooms()
            .then(setRooms)
            .catch((e: any) => toast.error(e?.message ?? "Failed to load rooms"))
            .finally(() => setLoading(false));
    }, []);

    return (
        <div className="min-h-screen bg-base-200">
            <div className="navbar bg-base-100 shadow-sm">
                <div className="flex-1">
                    <div className="font-bold text-lg">Realtime Chat</div>
                </div>
                <div className="text-xs opacity-70">Public reads • Auth writes</div>
            </div>

            <div className="p-4">
                <div className="grid grid-cols-1 lg:grid-cols-[420px_1fr] gap-4">
                    <div className="card bg-base-100 shadow">
                        <div className="card-body gap-4">
                            <div>
                                <div className="font-bold mb-2">Auth</div>
                                <Login />
                            </div>

                            <div className="divider my-0"></div>

                            <div>
                                <div className="font-bold mb-2">Create room</div>
                                <div className="flex gap-2">
                                    <input
                                        className="input input-bordered w-full"
                                        value={roomId}
                                        onChange={(e) => setRoomId(e.target.value)}
                                        placeholder="room id (e.g. room1)"
                                        disabled={creating}
                                    />
                                    <button
                                        className="btn btn-primary"
                                        disabled={creating}
                                        onClick={async () => {
                                            if (!roomId.trim()) return;

                                            setCreating(true);
                                            try {
                                                await realtimeClient.createRoom({ roomId });
                                                toast.success("Room created");
                                                const r = await realtimeClient.getRooms();
                                                setRooms(r);
                                            } catch (e: any) {
                                                toast.error(e?.message ?? "Failed to create room");
                                            } finally {
                                                setCreating(false);
                                            }
                                        }}
                                    >
                                        {creating ? (
                                            <>
                                                <span className="loading loading-spinner loading-sm"></span>
                                                Create
                                            </>
                                        ) : (
                                            "Create"
                                        )}
                                    </button>
                                </div>
                                <div className="text-xs opacity-70 mt-2">
                                    Anyone can join and read. Login is required to send messages.
                                </div>
                            </div>
                        </div>
                    </div>

                    <div className="card bg-base-100 shadow">
                        <div className="card-body">
                            <div className="flex items-center justify-between">
                                <div className="font-bold text-lg">Rooms</div>
                                <div className="badge badge-neutral">{rooms.length}</div>
                            </div>

                            <div className="divider my-2"></div>

                            {loading ? (
                                <div className="flex items-center gap-3">
                                    <span className="loading loading-spinner"></span>
                                    <span className="text-sm opacity-70">Loading rooms...</span>
                                </div>
                            ) : rooms.length === 0 ? (
                                <div className="alert">
                                    <span>No rooms yet. Create one on the left.</span>
                                </div>
                            ) : (
                                <div className="overflow-x-auto">
                                    <table className="table">
                                        <thead>
                                        <tr>
                                            <th>Room</th>
                                            <th className="w-32"></th>
                                        </tr>
                                        </thead>
                                        <tbody>
                                        {rooms.map((r) => (
                                            <tr key={r.id}>
                                                <td className="font-mono">{r.id}</td>
                                                <td>
                                                    <button
                                                        className="btn btn-sm btn-outline"
                                                        onClick={() => navigate(`/rooms/${r.id}`)}
                                                    >
                                                        Join
                                                    </button>
                                                </td>
                                            </tr>
                                        ))}
                                        </tbody>
                                    </table>
                                </div>
                            )}
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
