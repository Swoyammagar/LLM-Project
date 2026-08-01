"use client";

import { GoogleLogin } from "@react-oauth/google";
import { toast } from "sonner";
import { useRouter, useSearchParams } from "next/navigation";
import { useAppDispatch } from "@/store/hooks";
import { googleLogin } from "@/store/slices/authSlice";

export function GoogleButton() {
  const dispatch = useAppDispatch();
  const router = useRouter();
  const searchParams = useSearchParams();

  return (
    <div className="w-full flex justify-center">
      <GoogleLogin
        onSuccess={async (credentialResponse) => {
          if (!credentialResponse.credential) {
            toast.error("Google sign-in returned no credential.");
            return;
          }
          const result = await dispatch(googleLogin(credentialResponse.credential));
          if (googleLogin.fulfilled.match(result)) {
            const redirect = searchParams.get("redirect");
            router.push(redirect || "/dashboard");
          } else {
            toast.error(result.payload || "Google sign-in failed.");
          }
        }}
        onError={() => toast.error("Google sign-in failed. Please try again.")}
        width="320"
      />
    </div>
  );
}