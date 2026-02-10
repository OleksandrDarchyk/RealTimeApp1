import { RealtimeClient, AuthClient } from "./generated-ts-client";
import { BASE_URL } from "./utils/BASE_URL";
import { customFetch } from "./utils/customFetch";

export const realtimeClient = new RealtimeClient(BASE_URL, customFetch);
export const authClient = new AuthClient(BASE_URL, customFetch);
