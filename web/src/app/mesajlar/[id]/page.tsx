import { notFound, redirect } from "next/navigation";
import { ChatPane } from "@/components/messages/ChatPane";
import { getConversations, getMessages } from "@/lib/personal";
import { getSession } from "@/lib/session";
import type { Conversation } from "@/lib/types";

export default async function ChatPage({ params }: PageProps<"/mesajlar/[id]">) {
  const { id } = await params;
  const session = await getSession();
  if (!session) redirect(`/giris?next=/mesajlar/${id}`);

  const [messages, list] = await Promise.all([getMessages(id, 1, 30), getConversations(1, 50)]);
  if (!messages.ok) {
    if (messages.status === 401) redirect(`/giris?next=/mesajlar/${id}`);
    notFound();
  }

  let conversation: Conversation | undefined = list.ok ? list.data.items.find((c) => c.id === id) : undefined;
  // A conversation past the first page of the inbox: look further back.
  for (let page = 2; !conversation && list.ok && page <= list.data.totalPages && page <= 5; page++) {
    const more = await getConversations(page, 50);
    conversation = more.ok ? more.data.items.find((c) => c.id === id) : undefined;
  }
  if (!conversation) notFound();

  return (
    <ChatPane
      key={id}
      conversation={conversation}
      initial={messages.data.items}
      hasMore={messages.data.page < messages.data.totalPages}
      me={session.userId}
    />
  );
}
