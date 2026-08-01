import type { Metadata } from "next";
import { Inter, Geist } from "next/font/google";
import "./globals.css";
import { cn } from "@/lib/utils";
import { ThemeProvider } from "@/contexts/theme-context";
import { DarkModeWrapper } from "@/components/layout/DarkModeWrapper";
import { ToasterProvider } from "@/components/layout/ToasterProvider";
import { Providers } from "./providers";

const geist = Geist({subsets:['latin'],variable:'--font-sans'});

const inter = Inter({
  subsets: ["latin"],
  variable: "--font-inter",
  display: "swap",
});

export const metadata: Metadata = {
  title: "DocuAI — AI Document Q&A Platform",
  description: "Chat with your documents using AI. Upload PDFs, DOCX, and TXT files.",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en" suppressHydrationWarning className={cn("font-sans", geist.variable)}>
      <body className={`${inter.variable} font-sans antialiased`}>
        <Providers>
        <ThemeProvider>
          <DarkModeWrapper>
            <ToasterProvider/> 
              {children}
          </DarkModeWrapper>
        </ThemeProvider>
        </Providers>
      </body>
    </html>
  );
}

