# Workflow: Share a Project via ZIP

This workflow covers exporting a project as a password-protected ZIP file and importing it on another machine.

---

## Why ZIP Sharing Exists

The standard project file (`project.julveh`) uses DPAPI to encrypt vehicle binary files. DPAPI is tied to the current Windows user account and machine. A project folder copied directly to another machine cannot be decrypted by a different user. The ZIP export re-encrypts the project under a symmetric password (AES-256-GCM with PBKDF2 key derivation), making the archive portable.

---

## Step 1: Export the Project as ZIP

1. Open the project you want to share.
2. Go to **File > Export as ZIP** (or the equivalent menu entry in the toolbar).
3. A password dialog appears. Enter a password and confirm it. The password protects the archive; share it separately from the file (for example, via a different channel).
4. Choose a destination for the `.zip` file.
5. The tool writes the archive. Progress is shown in the status bar.

The ZIP contains:
- The `project.julveh` descriptor (re-encrypted for the archive)
- All vehicle binary files from the `vehicles/` folder (decrypted from DPAPI, then re-encrypted under the chosen password)

---

## Step 2: Transfer the Archive

Send the `.zip` file to the recipient through any channel (file share, cloud storage, messaging). Communicate the password separately — do not include it in the same message or file transfer.

---

## Step 3: Import the ZIP on the Recipient's Machine

1. Open Julscha Vehicle Tool on the recipient's machine.
2. Go to **File > Import from ZIP** (or the equivalent menu entry).
3. Select the `.zip` file.
4. A password dialog appears (no confirm field this time). Enter the password.
5. Choose a destination folder for the project.

The tool:
- Decrypts the archive using the provided password
- Re-encrypts all binary files with the recipient's local DPAPI credentials
- Writes the project to the chosen destination folder
- Opens the imported project automatically

If the wrong password is entered, the import fails with an error. No partial files are written.

---

## Step 4: Verify After Import

After import, check the following:

- The project tree shows all resources and vehicles.
- Select a vehicle and switch to the 3D Viewer tab. The model should load without errors.
- Check the Vehicle, Handling, and Variations tabs to confirm data was preserved.

If binary files are missing or the 3D viewer shows an error, the import may have been interrupted or the source project had missing files before export. Re-export from the source machine and retry.

---

## Use Cases

| Scenario | Notes |
|---|---|
| Collaborating with a teammate | Export ZIP → share → teammate imports, edits, re-exports ZIP back |
| Backup to an external drive | ZIP can be stored on any external media; DPAPI binding is removed |
| Moving to a new machine | Export ZIP from old machine, import on new machine |
| Handing off a finished pack | Recipient only needs to import and export as FiveM resource; no edits required |

---

## Security Notes

- The password is never stored by the tool. If the password is lost, the archive cannot be recovered.
- AES-256-GCM authenticated encryption is used. A tampered archive will fail to import.
- The decrypted content exists in memory only during import; it is not written to disk in unencrypted form.
- After import, all files are re-encrypted with DPAPI and are protected by the recipient's Windows account. The original archive remains encrypted and can be safely deleted from the recipient's machine once import succeeds.
