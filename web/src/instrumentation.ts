/**
 * Every error the server hits while answering a page is written as one line of JSON to the error output, with the
 * address of the page and its kind. `docker compose logs web` shows them, and a log collector can pick them up.
 * Nothing about the visitor (headers, cookies, body) is written.
 */
export function onRequestError(
  error: unknown,
  request: { path: string; method: string },
  context: { routerKind: string; routePath: string; routeType: string },
) {
  const e = error as { message?: string; digest?: string; stack?: string };
  console.error(
    JSON.stringify({
      level: "error",
      time: new Date().toISOString(),
      where: "web",
      method: request.method,
      path: request.path.split("?")[0],
      route: context.routePath,
      kind: context.routeType,
      digest: e.digest,
      message: e.message,
      stack: e.stack?.split("\n").slice(0, 6).join("\n"),
    }),
  );
}
