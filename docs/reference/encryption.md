# Encryption

Projects are stored **unencrypted by default**. Local encryption is optional; password encryption is used
for sharing projects as ZIP.

---

## Local Storage — optional DPAPI

| Property | Value |
|----------|-------|
| Default | **Off** — `project.julveh` is plain JSON, vehicle files are the original GTA V files |
| Switch | **File > Encrypt Project on this PC** (converts all existing files) |
| Algorithm | Windows Data Protection API (DPAPI) |
| Scope | `CurrentUser` — bound to your Windows user account |
| Files encrypted | `project.julveh`, all files in `vehicles/` |

With encryption on, **only you, on this PC**, can open the project. After reinstalling Windows, on another
PC or under another user account the project **cannot be opened** — keep a password-protected
[project ZIP](../export/zip-sharing.md) as backup.

The tool recognises encrypted files by their header, so projects from version 1.0 (always encrypted)
open as before and stay encrypted until you switch encryption off.

---

## Safe Saving

`project.julveh` is written to `project.julveh.tmp` first and then swapped in; the previous version is kept
as `project.julveh.bak`. If the project file is damaged (e.g. power loss while saving), the tool opens the
backup automatically and tells you so.

---

## ZIP Export/Import — AES-256-GCM

| Property | Value |
|----------|-------|
| Algorithm | AES-256-GCM (authenticated encryption) |
| Key derivation | PBKDF2-SHA256, **600 000 iterations**, random 16-byte salt per export |
| Authentication | 16-byte GCM tag (detects a wrong password or tampering) |
| Nonce | 12-byte random nonce per file |

File format:

```
"JVE2" | iterations (int32) | salt (16) | nonce (12) | tag (16) | ciphertext
```

All files of one export share the salt, so the expensive key derivation runs once per export, not once per
file. ZIPs created with version 1.0 (no `JVE2` header, 100 000 iterations) can still be imported.

---

## Security Considerations

- **ZIP password strength matters** — PBKDF2 slows down brute force, but a weak password can still be cracked offline.
- **Passwords are never written to disk.** Derived keys are kept in memory only during an export/import and wiped afterwards.
- **This is not DRM** — anyone with the project (or the ZIP password) can export the original binary files.
