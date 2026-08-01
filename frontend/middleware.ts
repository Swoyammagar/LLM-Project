import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

// Only these are accessible without a session. Everything else requires auth.
const PUBLIC_ROUTES = ["/", "/login", "/register", "/verify-email", "/forgot-password"];
const AUTH_ROUTES = ["/login", "/register"]; // logged-in users shouldn't see these

function isPublicRoute(pathname: string): boolean {
  return PUBLIC_ROUTES.some((route) =>
    route === "/" ? pathname === "/" : pathname === route || pathname.startsWith(`${route}/`)
  );
}

export function middleware(request: NextRequest) {
  const accessToken = request.cookies.get("accessToken")?.value;
  const refreshToken = request.cookies.get("refreshToken")?.value;
  const { pathname } = request.nextUrl;

  const hasSession = !!(accessToken || refreshToken);
  const isAuthRoute = AUTH_ROUTES.some((route) => pathname.startsWith(route));

  // Logged in and trying to view login/register -> bounce to dashboard
  if (isAuthRoute && accessToken) {
    return NextResponse.redirect(new URL("/dashboard", request.url));
  }

  // No session, trying to view anything not explicitly public -> bounce to login
  if (!isPublicRoute(pathname) && !hasSession) {
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set("redirect", pathname);
    return NextResponse.redirect(loginUrl);
  }

  return NextResponse.next();
}

export const config = {
  // Run on everything except static assets, images, favicon, and Next internals.
  // This is the one list you actually need to maintain — and it almost never changes.
  matcher: ["/((?!_next/static|_next/image|favicon.ico|.*\\.(?:svg|png|jpg|jpeg|gif|webp)$).*)"],
};