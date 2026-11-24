# ZIP Sharing

Projects can be exported as password-protected ZIP files and shared with others — even across different machines.

---

## Why ZIP?

A project ZIP is one password-protected file (AES-256-GCM) that contains the whole project. It is the safe way
to share a project, to move it to another PC and to back it up — especially if
[local encryption](../reference/encryption.md) is on, because an encrypted project folder can only be opened
by you on your PC.

---

## Exporting a ZIP

1. **File → Export Project ZIP...**
2. Choose a save location and filename.
3. A password dialog opens — enter and confirm a password.
4. The tool:
   - Encrypts `project.julveh` with the password (AES-256-GCM + PBKDF2)
   - Encrypts the binary files of all vehicles the same way (leftovers of deleted vehicles are not included)
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
   - Stores the files like the original project did (encrypted for your account only if the sender had local encryption on)
   - Saves the project ready to open

---

## Encryption Details

| Context | Cipher | Key derivation |
|---------|--------|---------------|
| Local storage | None by default, optional DPAPI | Machine/user bound, automatic |
| ZIP export/import | AES-256-GCM | PBKDF2-SHA256, 600 000 iterations (ZIPs from 1.0 with 100 000 still import) |

See [Encryption Reference](../reference/encryption.md) for full technical details.

---

## Use Cases

| Scenario | Solution |
|----------|----------|
| Backup to cloud storage | Export ZIP, store in Google Drive / OneDrive etc. |
| Hand off to a team member | Export ZIP, send file + password separately |
| Move to a different PC | Export ZIP, import on new machine |
| Version snapshots | Export ZIP after each milestone |
