"use client";

import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
import { createContext, useContext, useEffect, useMemo, useRef, useState } from "react";

export type LiveSupportMessage = {
  id: string;
  isAdmin: boolean;
  body: string;
  sentAtUtc: string;
  senderName?: string | null;
};

export type SupportEvent =
  | { type: "message"; ticketId: string; message: LiveSupportMessage }
  | { type: "ticket"; ticketId: string; subject: string; userName?: string | null }
  | { type: "changed"; ticketId: string; status: number; priority: number }
  | { type: "report"; kind: "content" | "chat"; reportId: string }
  | { type: "review"; kind: "listing" | "store"; id: string; title: string }
  | { type: "typing"; ticketId: string; userId: string; isStaff: boolean; typing: boolean }
  /** The connection came back after a drop: anything may have been missed. */
  | { type: "reconnected" };

type Listener = (event: SupportEvent) => void;

type Live = {
  subscribe: (listener: Listener) => () => void;
  startTyping: (ticketId: string) => void;
  stopTyping: (ticketId: string) => void;
};

const Context = createContext<Live>({
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
 * The live line for support and reports: new messages, "is typing", new requests and reports, changes of state.
 * The token is fetched when the connection (re)starts and never stored.
 */
export function SupportRealtimeProvider({ children }: { children: React.ReactNode }) {
  const listeners = useRef(new Set<Listener>());
  const connection = useRef<HubConnection | null>(null);

  useEffect(() => {
    const hub = new HubConnectionBuilder()
      .withUrl("/hubs/support", { accessTokenFactory: readToken })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 20000])
      .configureLogging(LogLevel.None)
      .build();
    connection.current = hub;

    const emit = (event: SupportEvent) => listeners.current.forEach((l) => l(event));

    hub.on("SupportMessageCreated", (e: { ticketId: string; message: LiveSupportMessage }) =>
      emit({ type: "message", ticketId: e.ticketId, message: e.message }),
    );
    hub.on("SupportTicketCreated", (e: { ticketId: string; subject: string; userName?: string | null }) =>
      emit({ type: "ticket", ...e }),
    );
    hub.on("SupportTicketChanged", (e: { ticketId: string; status: number; priority: number }) =>
      emit({ type: "changed", ...e }),
    );
    hub.on("ReviewWaiting", (e: { kind: "listing" | "store"; id: string; title: string }) =>
      emit({ type: "review", ...e }),
    );
    hub.on("ReportCreated", (e: { kind: "content" | "chat"; reportId: string }) => emit({ type: "report", ...e }));
    const typing = (on: boolean) => (e: { ticketId: string; userId: string; isStaff: boolean }) =>
      emit({ type: "typing", ticketId: e.ticketId, userId: e.userId, isStaff: e.isStaff, typing: on });
    hub.on("TypingStarted", typing(true));
    hub.on("TypingStopped", typing(false));

    hub.onreconnected(() => emit({ type: "reconnected" }));

    let closed = false;
    const start = () =>
      hub.start().catch(() => {
        // The first connection failed: try again in a while.
        if (!closed) setTimeout(start, 5000);
      });
    void start();

    return () => {
      closed = true;
      void hub.stop();
      connection.current = null;
    };
  }, []);

  const value = useMemo<Live>(
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

export const useSupportLive = () => useContext(Context);

/** The "is typing" state of the other side, which clears itself if the "stopped" never arrives. */
export function useTypingFrom(ticketId: string, fromStaff: boolean): boolean {
  const live = useSupportLive();
  const [typing, setTyping] = useState(false);
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    const off = live.subscribe((event) => {
      if (event.type === "message" && event.ticketId === ticketId && event.message.isAdmin === fromStaff) {
        // The message arrived: they are no longer typing it.
        setTyping(false);
      }
      if (event.type !== "typing" || event.ticketId !== ticketId || event.isStaff !== fromStaff) return;
      setTyping(event.typing);
      if (timer.current) clearTimeout(timer.current);
      if (event.typing) timer.current = setTimeout(() => setTyping(false), 6000);
    });
    return () => {
      off();
      if (timer.current) clearTimeout(timer.current);
    };
  }, [live, ticketId, fromStaff]);

  return typing;
}

/** Sends "is typing" while the box is being written in (once in 3 seconds) and "stopped" when it is emptied or sent. */
export function useTypingSender(ticketId: string) {
  const live = useSupportLive();
  const last = useRef(0);

  return useMemo(
    () => ({
      changed: (value: string) => {
        const now = Date.now();
        if (value && now - last.current > 3000) {
          last.current = now;
          live.startTyping(ticketId);
        } else if (!value && last.current) {
          last.current = 0;
          live.stopTyping(ticketId);
        }
      },
      stopped: () => {
        if (!last.current) return;
        last.current = 0;
        live.stopTyping(ticketId);
      },
    }),
    [live, ticketId],
  );
}
