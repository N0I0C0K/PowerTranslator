# PowerTranslator - Testing Instructions for Microsoft Store Certification

PowerTranslator is a **Microsoft PowerToys Command Palette extension**, not a
standalone application. It does not have its own window or shortcut. It
registers itself with Command Palette via the
`com.microsoft.commandpalette` app extension category and shows up as
commands inside Command Palette after installation.

## Prerequisites

- Windows 11, or Windows 10 version 2004 (build 19041) or newer
- x64 or ARM64 processor
- **Microsoft PowerToys must be installed**, and the Command Palette tool
  must be enabled
  - PowerToys: <https://apps.microsoft.com/detail/xp89dcgq3k6vld>
  - Command Palette: <https://apps.microsoft.com/detail/9N1F85V9T8BN>
- Internet connection (the extension calls public translation services)

## How to verify the extension after install

1. **Install PowerToys + Command Palette** from the links above if not
   already present. Make sure Command Palette is enabled in PowerToys
   settings.
2. **Install this package.** Once the package is installed, Command Palette
   will discover the extension automatically via the manifest's
   `windows.appExtension` declaration (Name: `com.microsoft.commandpalette`,
   CLSID `c87ef62e-5d58-41c4-8da8-af88434680a4`).
3. **Open Command Palette.** Default hotkey is `Win + Alt + Space` (it can
   be changed in PowerToys → Command Palette settings).
4. **Reload extensions.** In the Command Palette search box, type
   `Reload`. Choose the entry whose subtitle is *"Reload Command Palette
   Extensions"* and press `Enter`. This is required so Command Palette
   re-enumerates installed extensions and discovers PowerTranslator.
5. **Find the extension.** Type `Translator` (or scroll to it). You should
   see three top-level commands provided by this extension:
   - **Translator** — main translation page
   - **Translation history**
   - **Supported languages**

## Suggested test cases

### 1. Basic translation
1. Open Command Palette, select **Translator**, press `Enter`.
2. Type `hello` and wait ~1 second.
3. Expected: one or more translation results appear (e.g. Chinese rendering
   of "hello"). The first result is sourced from Youdao Web API; if the
   primary service is unreachable the extension falls back to DeepL or a
   backup Youdao endpoint.

### 2. Target a specific language
1. In the Translator page, type `apple -> ja` and wait.
2. Expected: the result row shows the Japanese translation of "apple".
3. Other accepted targets include `zhs` (Simplified Chinese), `zht`
   (Traditional Chinese), `en`, `ko`, `ru`, `fr`, `es`, `de`, `it`, `ar`,
   `he`. The full list is visible under the **Supported languages**
   command.

### 3. Empty-query / clipboard helper
1. Copy any phrase to clipboard (e.g. `Microsoft`).
2. Open Command Palette → **Translator** without typing anything.
3. Expected: the clipboard text is translated automatically, followed by
   three navigation rows: *History*, *Supported languages*, *Find help*.

### 4. Per-result secondary actions (Ctrl+K)
1. With a translation result selected, press `Ctrl + K`.
2. Expected menu items: *Copy subtitle*, *Read aloud*, and (if enabled in
   settings) *Jump to dictionary*. *Read aloud* streams audio from
   `dict.youdao.com` via `Windows.Media.Playback.MediaPlayer`.

### 5. History page
1. Run several translations in sequence.
2. Open Command Palette → **Translation history**.
3. Expected: recently-translated phrases listed newest-first, capped at 20
   entries. History is in-memory only and resets when Command Palette
   restarts.

### 6. Settings
1. In Command Palette, open Command Palette's settings page and locate the
   **Translator** entry (the extension contributes a settings form).
2. Expected configurable options: default target language, second target
   language, search suggestions toggle, auto-read toggle, show original
   query, jump-to-dictionary toggle + dictionary choice, system proxy
   toggle, snake/camel-case word translation toggle.

## Expected network endpoints

The extension performs outbound HTTPS requests only to the following public
endpoints. They are required for translation results to appear:

- `https://dict.youdao.com/webtranslate` — Youdao web translation API (primary)
- `https://dict.youdao.com/dictvoice` — Youdao TTS audio for the optional *Read aloud* action
- `https://fanyi.youdao.com/translate_o` — Youdao legacy translation API (fallback)
- `https://aidemo.youdao.com/trans` — Youdao backup translation endpoint (fallback)
- `https://www2.deepl.com/jsonrpc` — DeepL web translation API (fallback)
- `https://fanyi.baidu.com/sug` — Baidu autocomplete suggestions (only when *Enable search suggestions* is on)

If outbound network access is blocked, the extension will display
*"result is null, some error happen in translate. check out your
network!"* and a help row. This is the expected error path, not a crash.

## Capabilities used and why

- `internetClient` — outbound HTTPS to the endpoints above
- `runFullTrust` — required by the Command Palette extension architecture:
  the extension is hosted as an out-of-process COM server (declared in
  `Package.appxmanifest` via `com:ComServer`) and activated by the Command
  Palette host. All Command Palette extensions need this capability.

The extension does not write outside its own LocalAppData container, does
not modify system settings, does not load arbitrary code, and does not
call native Win32 APIs beyond what the Windows App SDK runtime uses
internally.

## Troubleshooting

| Symptom | Cause / Fix |
| --- | --- |
| Translator does not appear in Command Palette | Command Palette has not re-enumerated extensions. Run `Reload` in Command Palette. |
| All translations return the error row | Outbound HTTPS to `*.youdao.com`, `*.deepl.com`, `*.baidu.com` is blocked on the test machine. |
| `Read aloud` produces no sound | `dict.youdao.com/dictvoice` is unreachable, or audio output device is muted. |
| `Translator` row shows *"initializing other apis... please try later"* | First-run initialization is in flight. Type the same query again after ~2 seconds. |

## Contact

If certification needs more detail or a reproduction step is unclear,
please reach out via the project's GitHub issue tracker:
<https://github.com/N0I0C0K/PowerTranslator/issues>
