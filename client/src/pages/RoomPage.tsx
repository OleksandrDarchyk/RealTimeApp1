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

    // для poke — просто поле вводу
    const [targetConnId, setTargetConnId] = useState("");

    const canJoin = useMemo(() => !!roomId && !!stream.connectionId, [roomId, stream.connectionId]);

    // 1) Підписки на SSE події (як у вчителя)
    useEffect(() => {
        if (!roomId) return;

        const offMsg = stream.on<{ message: string; from?: string }>(
            roomId,
            "messageHasBeenReceived",
            (dto) => {
                setItems((prev) => [...prev, { type: "msg", message: dto.message, from: dto.from }]);
            }
        );

        const offSystem = stream.on<{ message: string; kind: string }>(
            roomId,
            "SystemMessage",
            (dto) => {
                setItems((prev) => [...prev, { type: "system", message: dto.message, kind: dto.kind }]);
            }
        );

        const offHistory = stream.on<{ roomId: string; messages: any[] }>(
            "direct",
            "RoomHistory",
            (dto) => {
                // dto.messages: [{ id, content, from, createdAt }]
                const mapped: ChatItem[] = (dto.messages ?? []).map((m: any) => ({
                    type: "msg",
                    message: m.content,
                    from: m.from,
                }));
                setItems(mapped);
            }
        );

        const offPoke = stream.on<{ message: string }>("direct", "PokeResponse", (dto) => {
            alert(dto.message);
        });

        return () => {
            offMsg();
            offSystem();
            offHistory();
            offPoke();
        };
    }, [roomId]);

    // 2) Авто-join коли з’явився connectionId
    useEffect(() => {
        if (!canJoin) return;

        realtimeClient
            .joinRoom(roomId!, { connectionId: stream.connectionId! })
            .catch(() => navigate("/"));
    }, [canJoin]);

    if (!roomId) return null;

    return (
        <div style={{ display: "grid", gap: 12, padding: 16 }}>
            <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
                <button onClick={() => navigate("/")}>Back</button>
                <h2 style={{ margin: 0 }}>Room: {roomId}</h2>
                <div style={{ marginLeft: "auto", fontSize: 12 }}>
                    connectionId: {stream.connectionId ?? "(connecting...)"}
                </div>
            </div>

            <div style={{ border: "1px solid #ccc", padding: 12, minHeight: 240 }}>
                {items.map((x, i) => (
                    <div key={i} style={{ marginBottom: 6 }}>
                        {x.type === "msg" ? (
                            <span>
                <b>{x.from ?? "Anonymous"}:</b> {x.message}
              </span>
                        ) : (
                            <i>
                                [{x.kind}] {x.message}
                            </i>
                        )}
                    </div>
                ))}
            </div>

            <div style={{ display: "flex", gap: 8 }}>
                <input
                    style={{ flex: 1 }}
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
                    onClick={async () => {
                        if (!text.trim()) return;
                        await realtimeClient.sendMessage(roomId, { content: text });
                        setText("");
                    }}
                >
                    Send
                </button>
            </div>

            <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
                <input
                    style={{ flex: 1 }}
                    placeholder="target connectionId for poke"
                    value={targetConnId}
                    onChange={(e) => setTargetConnId(e.target.value)}
                />
                <button
                    onClick={async () => {
                        if (!targetConnId.trim()) return;
                        await realtimeClient.poke({ targetConnectionId: targetConnId });
                        alert("poke sent");
                    }}
                >
                    Poke
                </button>
                <button
                    onClick={async () => {
                        if (!stream.connectionId) return;
                        await realtimeClient.leaveRoom(roomId, { connectionId: stream.connectionId });
                        navigate("/");
                    }}
                >
                    Leave
                </button>
            </div>

            <div style={{ fontSize: 12 }}>
                Важливо: <b>Send/Poke</b> працює тільки якщо ти залогінився (бо [Authorize]).
            </div>
        </div>
    );
}
