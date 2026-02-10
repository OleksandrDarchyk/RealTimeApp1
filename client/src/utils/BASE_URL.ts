const local = "http://localhost:5264";
const prod = "https://server-realtime-api.fly.dev";

const isProd = import.meta.env.PROD;
export const BASE_URL = isProd ? prod : local;
