import { useState } from "react";
import type { LoginRequest } from "../generated-ts-client";
import { authClient } from "../clients";

export default function Login() {
    const [form, setForm] = useState<LoginRequest>({
        username: "test",
        password: "pass",
    });

    return (
        <div style={{ display: "flex", gap: 8, alignItems: "center", flexWrap: "wrap" }}>
            <input
                placeholder="username"
                value={form.username ?? ""}
                onChange={(e) => setForm({ ...form, username: e.target.value })}
            />
            <input
                placeholder="password"
                type="password"
                value={form.password ?? ""}
                onChange={(e) => setForm({ ...form, password: e.target.value })}
            />
            <button
                onClick={async () => {
                    const r = await authClient.login(form);
                    localStorage.setItem("jwt", r.token ?? "");
                    alert("logged in");
                }}
            >
                Login
            </button>
            <button
                onClick={async () => {
                    const r = await authClient.register(form);
                    localStorage.setItem("jwt", r.token ?? "");
                    alert("registered");
                }}
            >
                Register
            </button>
            <button
                onClick={() => {
                    localStorage.removeItem("jwt");
                    alert("logged out");
                }}
            >
                Logout
            </button>
        </div>
    );
}
