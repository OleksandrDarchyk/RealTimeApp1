import { useMemo, useState } from "react";
import type { LoginRequest } from "../generated-ts-client";
import { authClient } from "../clients";

export default function Login() {
    const [form, setForm] = useState<LoginRequest>({
        username: "test",
        password: "pass",
    });

    const [busy, setBusy] = useState<"login" | "register" | null>(null);
    const [token, setToken] = useState<string | null>(localStorage.getItem("jwt"));
    const isLoggedIn = useMemo(() => !!token, [token]);

    return (
        <div className="card bg-base-200">
            <div className="card-body p-4 gap-3">
                <div className="flex items-center justify-between">
                    <div className="font-bold">Account</div>
                    {isLoggedIn ? (
                        <div className="badge badge-success">Logged in</div>
                    ) : (
                        <div className="badge badge-ghost">Guest</div>
                    )}
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                    <input
                        className="input input-bordered w-full"
                        placeholder="username"
                        value={form.username ?? ""}
                        onChange={(e) => setForm({ ...form, username: e.target.value })}
                        disabled={busy !== null}
                    />
                    <input
                        className="input input-bordered w-full"
                        placeholder="password"
                        type="password"
                        value={form.password ?? ""}
                        onChange={(e) => setForm({ ...form, password: e.target.value })}
                        disabled={busy !== null}
                    />
                </div>

                <div className="flex flex-wrap gap-2">
                    <button
                        className="btn btn-primary"
                        disabled={busy !== null}
                        onClick={async () => {
                            setBusy("login");
                            try {
                                const r = await authClient.login(form);
                                const t = r.token ?? "";
                                localStorage.setItem("jwt", t);
                                setToken(t);
                            } finally {
                                setBusy(null);
                            }
                        }}
                    >
                        {busy === "login" ? (
                            <>
                                <span className="loading loading-spinner loading-sm"></span>
                                Login
                            </>
                        ) : (
                            "Login"
                        )}
                    </button>

                    <button
                        className="btn btn-secondary"
                        disabled={busy !== null}
                        onClick={async () => {
                            setBusy("register");
                            try {
                                const r = await authClient.register(form);
                                const t = r.token ?? "";
                                localStorage.setItem("jwt", t);
                                setToken(t);
                            } finally {
                                setBusy(null);
                            }
                        }}
                    >
                        {busy === "register" ? (
                            <>
                                <span className="loading loading-spinner loading-sm"></span>
                                Register
                            </>
                        ) : (
                            "Register"
                        )}
                    </button>

                    <button
                        className="btn btn-outline"
                        disabled={busy !== null || !isLoggedIn}
                        onClick={() => {
                            localStorage.removeItem("jwt");
                            setToken(null);
                        }}
                    >
                        Logout
                    </button>
                </div>

                <div className="text-xs opacity-70">
                    Token is stored in <span className="font-mono">localStorage.jwt</span>.
                </div>
            </div>
        </div>
    );
}
