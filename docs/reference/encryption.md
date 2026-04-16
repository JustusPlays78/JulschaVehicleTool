# Encryption

The tool encrypts all project data to prevent accidental access to extracted GTA V assets. Two encryption modes are used depending on context.

---

## Local Storage — DPAPI

| Property | Value |
|----------|-------|
| Algorithm | Windows Data Protection API (DPAPI) |
| Scope | `CurrentUser` — bound to your Windows user account |
| Key management | Automatic (managed by Windows, tied to your login) |
| Files encrypted | `project.julveh`, all files in `vehicles/` |

DPAPI means **no password is needed** to open a project on the same machine with the same user account. The key is derived automatically from your Windows credentials.

**Consequence:** A project folder copied to another machine or user account **cannot be opened** there. Use [ZIP export](../export/zip-sharing.md) to transfer projects.

---

## ZIP Export/Import — AES-256-GCM

| Property | Value |
|----------|-------|
| Algorithm | AES-256-GCM (authenticated encryption) |
| Key derivation | PBKDF2-SHA256, 100 000 iterations, random 16-byte salt |
| Authentication | 16-byte GCM authentication tag (prevents tampering) |
| IV | 12-byte random nonce per file |

On export, every file is individually re-encrypted:

```
PlainBytes
  → EncryptWithPassword(password, salt, iv)
  → [salt (16)] + [iv (12)] + [tag (16)] + [ciphertext]
```

On import, the process reverses. Files that fail authentication (wrong password or tampered data) throw an exception and abort the import.

---

## Security Considerations

- **DPAPI is machine-bound** — cloud sync of a project folder (Dropbox, OneDrive) will not work across machines without ZIP export.
- **ZIP password strength matters** — use a strong password for shared ZIPs since the key stretching (PBKDF2 / 100k iterations) provides defense against brute force, but a weak password can still be cracked offline.
- **The tool does not store passwords** — passwords are used transiently in memory and never written to disk.
- **GTA V binary files are not "DRM-protected"** by this tool — the encryption protects against casual access only. Anyone with the project password can export the original binary files.
