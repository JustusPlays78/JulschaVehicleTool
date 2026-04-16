# ZIP Sharing

Projects can be exported as password-protected ZIP files and shared with others — even across different machines.

---

## Why ZIP?

Project files are stored with **DPAPI encryption** — bound to your Windows user account. Another user on another machine cannot open them directly. The ZIP export re-encrypts everything with a password-based cipher (AES-256-GCM) so the project can be safely shared.

---

## Exporting a ZIP

1. **File → Export Project ZIP...**
2. Choose a save location and filename.
3. A password dialog opens — enter and confirm a password.
4. The tool:
   - Re-encrypts the `project.julveh` file (DPAPI → AES-256-GCM + PBKDF2)
   - Re-encrypts all binary files in `vehicles/` the same way
   - Packages everything into a standard ZIP

Share the `.zip` and the password separately (don't include the password in the same message).

---

## Importing a ZIP

1. **File → Import Project ZIP...**
2. Select the `.zip` file.
3. Choose a **target folder** for the extracted project.
4. Enter the password.
5. The tool:
   - Extracts and decrypts with the password
   - Re-encrypts with your local DPAPI (bound to your account)
   - Saves the project ready to open

---

## Encryption Details

| Context | Cipher | Key derivation |
|---------|--------|---------------|
| Local storage | DPAPI (Windows) | Machine/user bound, automatic |
| ZIP export/import | AES-256-GCM | PBKDF2-SHA256, 100 000 iterations |

See [Encryption Reference](../reference/encryption.md) for full technical details.

---

## Use Cases

| Scenario | Solution |
|----------|----------|
| Backup to cloud storage | Export ZIP, store in Google Drive / OneDrive etc. |
| Hand off to a team member | Export ZIP, send file + password separately |
| Move to a different PC | Export ZIP, import on new machine |
| Version snapshots | Export ZIP after each milestone |
