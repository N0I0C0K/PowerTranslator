# runFullTrust Justification — Partner Center Submission

This is the text to paste into Partner Center → Submission → restricted
capabilities → runFullTrust gate, when prompted for "Why do you need
runFullTrust" and "How will you use it in the product".

Use the **English** versions unless Partner Center forces Chinese input.

Updated for each submission only if the manifest's COM CLSID or set of
called APIs changes.

---

## Q1 — Why do you need to use runFullTrust capability?

```
PowerTranslator is an extension for Microsoft PowerToys Command Palette.
The Command Palette extension model requires every extension to be
hosted as an out-of-process COM server, declared via <com:ComServer> in
the package manifest and activated on demand by the Command Palette host
process.

Per Microsoft's own documentation, registering an out-of-process COM
server from a packaged app — the feature officially called "Packaged
COM" — requires the runFullTrust restricted capability:

  "...to be able to register out-of-process COM servers for
  inter-process communication (IPC), a packaged app needs runFullTrust.
  That feature is known as Packaged COM."
  — https://learn.microsoft.com/windows/uwp/packaging/app-capability-declarations#special-and-restricted-capabilities

Without runFullTrust, Windows refuses to register the COM class
(CLSID c87ef62e-5d58-41c4-8da8-af88434680a4) declared in this package,
and Command Palette cannot discover or activate the extension. This
capability is therefore a hard architectural requirement of every
Command Palette extension, not a request for additional privileges.
```

## Q2 — How will you use it in the product?

```
The capability is used solely to allow Windows to start
PowerTranslatorExtension.exe as a Packaged COM server (CLSID
c87ef62e-5d58-41c4-8da8-af88434680a4) under the Command Palette host.
The extension's runtime behavior is equivalent to a sandboxed app and
exercises only the following APIs:

  - System.Net.Http.HttpClient — outbound HTTPS to public translation
    endpoints (api.youdao.com, fanyi.youdao.com, www2.deepl.com,
    fanyi.baidu.com, aidemo.youdao.com). Network access is already
    declared via the internetClient capability.
  - Windows.ApplicationModel.DataTransfer.Clipboard — user-initiated
    reads (translate clipboard content when the query box is empty)
    and writes (copy a translation result back).
  - Windows.Media.Playback.MediaPlayer — streaming TTS audio from
    dict.youdao.com for the optional "Read aloud" command.
  - System.Security.Cryptography (MD5 for request signing, AES for
    decrypting the Youdao API response payload).
  - Microsoft.CommandPalette.Extensions.Toolkit.JsonSettingsManager —
    persists user preferences to the package's own LocalAppData
    container.

The extension does not:
  - write outside its own package container;
  - modify system settings, registry, or files belonging to other
    packages or to Windows;
  - load arbitrary or downloaded code;
  - invoke native Win32 APIs beyond what the Windows App SDK runtime
    uses internally;
  - elevate privileges, use FullTrustProcessLauncher, or spawn child
    processes.

In short, runFullTrust is used here only as the prerequisite for
Packaged COM registration, not to perform any privileged operation.
```
