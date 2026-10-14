"use client";

import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { createContext, useContext, useEffect, useMemo, useRef } from "react";
import type { ChatMessage } from "@/lib/types";

export type RealtimeEvent =
  | { type: "message"; message: ChatMessage }
  | { type: "read"; conversationId: string; readByUserId: string; readAtUtc: string }
  | { type: "typing"; conversationId: string; userId: string; typing: boolean }
  /** The connection came back after a drop: anything may have been missed. */
  | { type: "reconnected" };

type Listener = (event: RealtimeEvent) => void;

type Realtime = {
  subscribe: (listener: Listener) => () => void;
  startTyping: (conversationId: string) => void;
  stopTyping: (conversationId: string) => void;
};

const Context = createContext<Realtime>({
  subscribe: () => () => {},
  startTyping: () => {},
  stopTyping: () => {},
});

async function readToken(): Promise<string> {
  const res = await fetch("/bff/hub-token", { cache: "no-store" });
  if (!res.ok) throw new Error("no token");
  return ((await res.json()) as { token: string }).token;
}

/**
 * One live connection for the messages area. The token is fetched when the
 * connection (re)starts and never stored; events go to whoever listens.
 */
export function RealtimeProvider({ children }: { children: React.ReactNode }) {
  const listeners = useRef(new Set<Listener>());
  const connection = useRef<HubConnection | null>(null);

  useEffect(() => {
    const hub = new HubConnectionBuilder()
      .withUrl("/hubs/conversations", { accessTokenFactory: readToken })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 20000])
      .configureLogging(LogLevel.None)
      .build();
    connection.current = hub;

    const emit = (event: RealtimeEvent) => listeners.current.forEach((l) => l(event));

    hub.on("MessageCreated", (message: ChatMessage) => emit({ type: "message", message }));
    hub.on("MessagesRead", (e: { conversationId: string; readByUserId: string; readAtUtc: string }) =>
      emit({ type: "read", ...e }),
    );
    hub.on("TypingStarted", (e: { conversationId: string; userId: string }) =>
      emit({ type: "typing", conversationId: e.conversationId, userId: e.userId, typing: true }),
    );
    hub.on("TypingStopped", (e: { conversationId: string; userId: string }) =>
      emit({ type: "typing", conversationId: e.conversationId, userId: e.userId, typing: false }),
    );

    const beat = () => {
      if (hub.state === HubConnectionState.Connected) void hub.invoke("Heartbeat").catch(() => {});
    };
    hub.onreconnected(() => {
      beat();
      emit({ type: "reconnected" });
    });

    let closed = false;
    const start = () =>
      hub
        .start()
        .then(beat)
        .catch(() => {
          // The first connection failed: try again in a while.
          if (!closed) setTimeout(start, 5000);
        });
    void start();
    const timer = setInterval(beat, 20_000);

    return () => {
      closed = true;
      clearInterval(timer);
      void hub.stop();
      connection.current = null;
    };
  }, []);

  const value = useMemo<Realtime>(
    () => ({
      subscribe: (listener) => {
        listeners.current.add(listener);
        return () => {
          listeners.current.delete(listener);
        };
      },
      startTyping: (id) => {
        const hub = connection.current;
        if (hub?.state === HubConnectionState.Connected) void hub.invoke("StartTyping", id).catch(() => {});
      },
      stopTyping: (id) => {
        const hub = connection.current;
        if (hub?.state === HubConnectionState.Connected) void hub.invoke("StopTyping", id).catch(() => {});
      },
    }),
    [],
  );

  return <Context.Provider value={value}>{children}</Context.Provider>;
}

export const useRealtime = () => useContext(Context);
