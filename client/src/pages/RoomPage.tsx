import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useStream } from "../useStream";
import { realtimeClient } from "../clients";

type RoomParams = { roomId: string };

type ChatItem =
    | { type: "msg"; message: string; from?: string }
    | { type: "system"; message: string; kind: string };

export default function RoomPage() {
    const { roomId } = useParams<RoomParams>();
    const navigate = useNavigate();
    const stream = useStream();

    const [items, setItems] = useState<ChatItem[]>([]);
    const [text, setText] = useState("");
    const [targetConnId, setTargetConnId] = useState("");

    const canJoin = useMemo(
        () => !!roomId && !!stream.connectionId,
        [roomId, stream.connectionId]
    );

    useEffect(() => {
        if (!roomId) return;

        const offMsg = stream.on<{ message: string; from?: string }>(
            roomId,
            "messageHasBeenReceived",
            (dto) => {
                setItems((prev) => [
                    ...prev,
                    { type: "msg", message: dto.message, from: dto.from },
                ]);
            }
        );

        const offSystem = stream.on<{ message: string; kind: string }>(
            roomId,
            "SystemMessage",
            (dto) => {
                setItems((prev) => [
                    ...prev,
                    { type: "system", message: dto.message, kind: dto.kind },
                ]);
            }
        );

        const offHistory = stream.on<{ roomId: string; messages: any[] }>(
            "direct",
            "RoomHistory",
            (dto) => {
                const mapped: ChatItem[] = (dto.messages ?? []).map((m: any) => ({
                    type: "msg",
                    message: m.content,
                    from: m.from,
                }));
                setItems(mapped);
            }
        );

        const offPoke = stream.on<{ message: string }>(
            "direct",
            "PokeResponse",
            (dto) => {
                alert(dto.message);
            }
        );

        return () => {
            offMsg();
            offSystem();
            offHistory();
            offPoke();
        };
    }, [roomId]);

    useEffect(() => {
        if (!canJoin) return;

        realtimeClient
            .joinRoom(roomId!, { connectionId: stream.connectionId! })
            .catch(() => navigate("/"));
    }, [canJoin, roomId, stream.connectionId, navigate]);

    if (!roomId) return null;

    return (
        <div className="min-h-screen bg-base-200">
            <div className="navbar bg-base-100 shadow-sm">
                <div className="flex-1 gap-2">
                    <button className="btn btn-ghost" onClick={() => navigate("/")}>
                        Back
                    </button>
                    <div className="font-bold">Room: {roomId}</div>
                </div>

                <div className="text-xs opacity-70">
                    connectionId:{" "}
                    <span className="font-mono">
            {stream.connectionId ?? "(connecting...)"}
          </span>
                </div>
            </div>

            <div className="p-4">
                <div className="card bg-base-100 shadow">
                    <div className="card-body gap-3">
                        <div className="h-[55vh] overflow-auto border border-base-200 rounded-box p-3">
                            {items.map((x, i) => (
                                <div key={i} className="mb-2">
                                    {x.type === "msg" ? (
                                        <div className="chat chat-start">
                                            <div className="chat-header opacity-70">
                                                {x.from ?? "Anonymous"}
                                            </div>
                                            <div className="chat-bubble">{x.message}</div>
                                        </div>
                                    ) : (
                                        <div className="alert alert-info py-2">
                      <span className="text-sm">
                        [{x.kind}] {x.message}
                      </span>
                                        </div>
                                    )}
                                </div>
                            ))}
                        </div>

                        <div className="flex gap-2">
                            <input
                                className="input input-bordered w-full"
                                placeholder="message..."
                                value={text}
                                onChange={(e) => setText(e.target.value)}
                                onKeyDown={async (e) => {
                                    if (e.key === "Enter" && text.trim()) {
                                        await realtimeClient.sendMessage(roomId, { content: text });
                                        setText("");
                                    }
                                }}
                            />
                            <button
                                className="btn btn-primary"
                                onClick={async () => {
                                    if (!text.trim()) return;
                                    await realtimeClient.sendMessage(roomId, { content: text });
                                    setText("");
                                }}
                            >
                                Send
                            </button>
                        </div>

                        <div className="flex flex-col sm:flex-row gap-2">
                            <input
                                className="input input-bordered w-full"
                                placeholder="target connectionId for poke"
                                value={targetConnId}
                                onChange={(e) => setTargetConnId(e.target.value)}
                            />
                            <button
                                className="btn btn-secondary"
                                onClick={async () => {
                                    if (!targetConnId.trim()) return;
                                    await realtimeClient.poke({ targetConnectionId: targetConnId });
                                    alert("poke sent");
                                }}
                            >
                                Poke
                            </button>
                            <button
                                className="btn btn-outline"
                                onClick={async () => {
                                    if (!stream.connectionId) return;
                                    await realtimeClient.leaveRoom(roomId, {
                                        connectionId: stream.connectionId,
                                    });
                                    navigate("/");
                                }}
                            >
                                Leave
                            </button>
                        </div>

                        <div className="text-xs opacity-70">
                            Send/Poke працює тільки якщо ти залогінився (бо [Authorize]).
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
