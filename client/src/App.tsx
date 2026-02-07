
import './App.css'
import {useStream} from "./useStream.tsx";
import {useEffect} from "react";
import { API_BASE_URL } from "./config";





function App() {

    const stream = useStream();



    useEffect(() => {
        // 1) Regular room messages (chat messages)
        const offMsg = stream.on<{ message: string; from?: string }>(
            "room1",
            "messageHasBeenReceived",
            (dto) => {
                console.log("MSG:", dto.message, dto.from);
            }
        );

        // 2) System messages (join/leave/disconnect notifications)
        const offSystem = stream.on<{ message: string; kind: string }>(
            "room1",
            "SystemMessage",
            (dto) => {
                console.log("SYSTEM:", dto.kind, dto.message);
            }
        );

        // 3) Direct messages (e.g., poke) - requires server to map null-group to "direct"
        const offPoke = stream.on<{ message: string }>(
            "direct",
            "PokeResponse",
            (dto) => {
                alert(dto.message);
            }
        );

        // Cleanup subscriptions when the component unmounts
        return () => {
            offMsg();
            offSystem();
            offPoke();
        };
    }, []);



    return (
        <>

            <button onClick={() => {
                fetch(`${API_BASE_URL}/rooms/room1/join`,{
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({connectionId: stream.connectionId}),

                })

            }}>JOIN ROOM</button>

            <button onClick={() => {

                fetch(`${API_BASE_URL}/rooms/room1/leave`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ connectionId: stream.connectionId }),
                });
            }}>LEAVE ROOM</button>

            <button onClick={() => {
                fetch(`${API_BASE_URL}/rooms/room1/messages`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ content:'hello', from:'me' }),
                });
            }}>SEND MESSAGE ROOM</button>

            <button onClick={async () => {
                const target = prompt("Target connectionId?");
                if (!target) return;

                const res = await fetch(`${API_BASE_URL}/poke`, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ targetConnectionId: target }),
                });

                console.log("POKE status:", res.status);
            }}>
                POKE
            </button>

        </>
    )
}

export default App
//