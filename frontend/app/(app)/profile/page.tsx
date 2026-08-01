"use client";

import { useState } from "react";
import { toast } from "sonner";
import { AlertCircle, SquarePen } from "lucide-react";
import { Avatar } from "@/components/ui2/Avatar";
import { Badge } from "@/components/ui2/Badge";
import { Button } from "@/components/ui2/Button";
import { CardShell } from "@/components/ui2/CardShell";
import { Input } from "@/components/ui2/Input";

export default function ProfilePage() {
  const [name, setName] = useState("Sarah Chen");
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);

  return (
    <div className="p-6 max-w-2xl mx-auto space-y-5">
      <div>
        <h1 className="text-xl font-semibold text-foreground">Profile</h1>
        <p className="text-sm text-muted-foreground mt-0.5">Manage your account details and preferences.</p>
      </div>

      <CardShell className="p-6">
        <div className="flex items-start gap-5">
          <div className="relative">
            <Avatar name={name} size="lg" />
            <button className="absolute -bottom-1 -right-1 w-6 h-6 rounded-full bg-primary flex items-center justify-center border-2 border-card shadow-sm">
              <SquarePen className="w-2.5 h-2.5 text-white" />
            </button>
          </div>
          <div className="flex-1">
            <div className="flex items-center gap-3">
              <div>
                <h2 className="text-base font-semibold text-foreground">{name}</h2>
                <p className="text-sm text-muted-foreground">sarah@acme.com</p>
              </div>
              <Badge variant="info" className="ml-2">Pro Plan</Badge>
            </div>
            <div className="flex items-center gap-4 mt-3">
              <div className="text-center"><p className="text-lg font-semibold text-foreground">47</p><p className="text-xs text-muted-foreground">Documents</p></div>
              <div className="w-px h-8 bg-border" />
              <div className="text-center"><p className="text-lg font-semibold text-foreground">1,234</p><p className="text-xs text-muted-foreground">Questions</p></div>
              <div className="w-px h-8 bg-border" />
              <div className="text-center"><p className="text-lg font-semibold text-foreground">2.8 GB</p><p className="text-xs text-muted-foreground">Storage</p></div>
            </div>
          </div>
        </div>
      </CardShell>

      <CardShell className="p-6">
        <h2 className="text-sm font-semibold text-foreground mb-4">Personal Information</h2>
        <div className="space-y-4">
          <Input label="Full Name" value={name} onChange={e => setName(e.target.value)} />
          <Input label="Email Address" type="email" value="sarah@acme.com" disabled className="opacity-60" />
        </div>
        <div className="flex justify-end mt-5">
          <Button variant="primary" size="md" onClick={() => toast.success("Profile updated successfully")}>Save Changes</Button>
        </div>
      </CardShell>

      <CardShell className="p-6">
        <h2 className="text-sm font-semibold text-foreground mb-4">Change Password</h2>
        <div className="space-y-4">
          <div>
            <label className="text-sm font-medium text-foreground block mb-1.5">Current Password</label>
            <input type="password" placeholder="••••••••" className="w-full h-9 rounded-lg border border-border bg-card text-foreground text-sm px-3 placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary/30 transition-all" />
          </div>
          <div>
            <label className="text-sm font-medium text-foreground block mb-1.5">New Password</label>
            <input type="password" placeholder="Min. 8 characters" className="w-full h-9 rounded-lg border border-border bg-card text-foreground text-sm px-3 placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary/30 transition-all" />
          </div>
        </div>
        <div className="flex justify-end mt-5">
          <Button variant="outline" size="md" onClick={() => toast.success("Password changed successfully")}>Update Password</Button>
        </div>
      </CardShell>

      <CardShell className="p-6 border-destructive/30">
        <h2 className="text-sm font-semibold text-destructive mb-1">Danger Zone</h2>
        <p className="text-xs text-muted-foreground mb-4">Permanently delete your account and all associated data.</p>
        {showDeleteConfirm ? (
          <div className="flex items-center gap-3 p-4 bg-red-50 dark:bg-red-900/10 rounded-lg border border-destructive/20">
            <AlertCircle className="w-4 h-4 text-destructive flex-shrink-0" />
            <p className="text-sm text-foreground flex-1">Are you sure? All your data will be permanently deleted.</p>
            <div className="flex gap-2">
              <Button variant="ghost" size="sm" onClick={() => setShowDeleteConfirm(false)}>Cancel</Button>
              <Button variant="danger" size="sm" onClick={() => { toast.error("Account deletion disabled in demo"); setShowDeleteConfirm(false); }}>Delete</Button>
            </div>
          </div>
        ) : (
          <Button variant="danger" size="md" onClick={() => setShowDeleteConfirm(true)}>Delete Account</Button>
        )}
      </CardShell>
    </div>
  );
}
