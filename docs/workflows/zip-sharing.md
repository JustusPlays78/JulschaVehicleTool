# Workflow: Share a Project via ZIP

This workflow covers exporting a project as a password-protected ZIP file and importing it on another machine.

---

## Why ZIP Sharing Exists

A ZIP export packs the whole project into one password-protected archive (AES-256-GCM with PBKDF2 key
derivation). It is portable to any PC — unlike a project folder with [local encryption](../reference/encryption.md)
switched on, which only you can open on your PC.

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
- Stores the files like the original project did (encrypted for the recipient's account only if local encryption was on)
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
| Backup to an external drive | ZIP can be stored on any external media and opened on any PC |
| Moving to a new machine | Export ZIP from old machine, import on new machine |
| Handing off a finished pack | Recipient only needs to import and export as FiveM resource; no edits required |

---

## Security Notes

- The password is never stored by the tool. If the password is lost, the archive cannot be recovered.
- AES-256-GCM authenticated encryption is used. A tampered archive will fail to import.
- The decrypted content exists in memory only during import; it is not written to disk in unencrypted form.
- After import the files are stored like in the original project (plain, or DPAPI-encrypted if local encryption was on). The archive itself stays encrypted and can be deleted once the import succeeded.
