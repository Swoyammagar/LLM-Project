"use client";

import { useState } from "react";
import { toast } from "sonner";
import { Bell, ChevronDown, Globe, RefreshCw, Shield, Sun } from "lucide-react";
import { Button } from "@/components/ui2/Button";
import { CardShell } from "@/components/ui2/CardShell";
import { Toggle } from "@/components/ui2/Toggle";
import { SettingRow } from "@/components/settings/SettingRow";
import { useTheme } from "@/contexts/theme-context";

export default function SettingsPage() {
  const { darkMode, setDarkMode } = useTheme();
  const [notifications, setNotifications] = useState({
    uploadComplete: true,
    aiResponse: false,
    weeklyDigest: true,
    productUpdates: false,
  });
  const [language, setLanguage] = useState("English");

  return (
    <div className="p-6 max-w-2xl mx-auto space-y-5">
      <div>
        <h1 className="text-xl font-semibold text-foreground">Settings</h1>
        <p className="text-sm text-muted-foreground mt-0.5">Manage your application preferences.</p>
      </div>

      <CardShell className="p-5">
        <h2 className="text-sm font-semibold text-foreground mb-1 flex items-center gap-2">
          <Sun className="w-4 h-4 text-muted-foreground" /> Appearance
        </h2>
        <p className="text-xs text-muted-foreground mb-4">Customize how DocuAI looks on your device.</p>
        <SettingRow label="Dark Mode" description="Use dark theme across the application">
          <Toggle checked={darkMode} onChange={setDarkMode} />
        </SettingRow>
        <SettingRow label="Compact Mode" description="Reduce spacing for denser information display">
          <Toggle checked={false} onChange={() => toast.info("Compact mode coming soon")} />
        </SettingRow>
      </CardShell>

      <CardShell className="p-5">
        <h2 className="text-sm font-semibold text-foreground mb-1 flex items-center gap-2">
          <Globe className="w-4 h-4 text-muted-foreground" /> Language & Region
        </h2>
        <p className="text-xs text-muted-foreground mb-4">Choose your preferred language.</p>
        <SettingRow label="Interface Language" description="Language used throughout the app">
          <div className="relative">
            <select value={language} onChange={e => { setLanguage(e.target.value); toast.success(`Language set to ${e.target.value}`); }}
              className="h-9 pl-3 pr-8 bg-card border border-border rounded-lg text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary/30 appearance-none cursor-pointer">
              {["English", "Spanish", "French", "German", "Japanese", "Chinese"].map(l => <option key={l}>{l}</option>)}
            </select>
            <ChevronDown className="absolute right-2.5 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-muted-foreground pointer-events-none" />
          </div>
        </SettingRow>
      </CardShell>

      <CardShell className="p-5">
        <h2 className="text-sm font-semibold text-foreground mb-1 flex items-center gap-2">
          <Bell className="w-4 h-4 text-muted-foreground" /> Notifications
        </h2>
        <p className="text-xs text-muted-foreground mb-4">Decide what notifications you want to receive.</p>
        <SettingRow label="Upload Complete" description="Notify when document processing finishes">
          <Toggle checked={notifications.uploadComplete} onChange={v => { setNotifications(n => ({ ...n, uploadComplete: v })); toast.success(`Upload notifications ${v ? "enabled" : "disabled"}`); }} />
        </SettingRow>
        <SettingRow label="AI Response Ready" description="Notify when long queries complete">
          <Toggle checked={notifications.aiResponse} onChange={v => setNotifications(n => ({ ...n, aiResponse: v }))} />
        </SettingRow>
        <SettingRow label="Weekly Digest" description="Summary of your activity every week">
          <Toggle checked={notifications.weeklyDigest} onChange={v => setNotifications(n => ({ ...n, weeklyDigest: v }))} />
        </SettingRow>
        <SettingRow label="Product Updates" description="New features and improvements">
          <Toggle checked={notifications.productUpdates} onChange={v => setNotifications(n => ({ ...n, productUpdates: v }))} />
        </SettingRow>
      </CardShell>

      <CardShell className="p-5">
        <h2 className="text-sm font-semibold text-foreground mb-1 flex items-center gap-2">
          <Shield className="w-4 h-4 text-muted-foreground" /> API Access
        </h2>
        <p className="text-xs text-muted-foreground mb-4">Integrate DocuAI with your applications.</p>
        <SettingRow label="API Key" description="Use this key to authenticate API requests">
          <div className="flex items-center gap-2">
            <code className="text-xs bg-muted px-2.5 py-1.5 rounded font-mono text-muted-foreground">sk-docuai-••••••••a7f2</code>
            <Button variant="outline" size="sm" onClick={() => toast.success("API key copied")}>Copy</Button>
          </div>
        </SettingRow>
        <SettingRow label="Regenerate Key" description="Invalidate current key and generate a new one">
          <Button variant="outline" size="sm" icon={<RefreshCw className="w-3.5 h-3.5" />} onClick={() => toast.warning("Regenerating will invalidate the current key")}>Regenerate</Button>
        </SettingRow>
      </CardShell>
    </div>
  );
}
