# BOX-RESTORE-INVENTORY-TEMP

**STATUS: TEMPORARY · DELETE AFTER POST-UPDATE RESTORE**  
**Captured:** 2026-09-24 ~03:07 KST  
**Purpose:** Reinstall Grok Bot Computer packages/CLIs after Update Grok Bot's Computer (Settings → Computer → Update).  
**Soft:** Soft≠PASS · Soft Atomic LOCK HOLD (Near ORANGE) · Canonical Soft tip at capture `30a62ca` (S47). Soft Continuity / GitHub tips remain Soft SoT — this file is **box install environment only**.

## Forbidden in this document
- Tokens, passwords, cookies, session values, API keys  
- Re-pasting secrets into chat or this file

## What Update keeps vs removes
| Kept | Removed (reinstall) |
|------|---------------------|
| `/workspace`, `/home/box` files | apt / pip / npm packages |
| Browser logins (re-verify) | Custom CLIs / local docker images |

Prefer **Update**, never **Reset**, for this recovery path.

## OS at capture
- Debian GNU/Linux 13 (trixie), amd64

## CLI at capture
| Tool | Notes |
|------|--------|
| `gh` | Present; logged in as `scarlel96-design` (verify with `gh auth status` after reinstall) |
| `git` | Present |
| `node` / `npm` | v20.19.2 / 9.2.0 |
| `python3` / `pip3` | Present |
| `ffmpeg` | apt |
| `curl` / `wget` | apt |
| `box-doctor` / `box-chrome` | Image helpers — expect after Update |
| `docker` | Not on PATH at capture |
| `dotnet` | **Not installed** at capture — Soft Build may need SDK reinstall when Soft resumes |

## apt packages (key)
```
gh git nodejs npm python3 python3-pip python3-venv curl wget ffmpeg
```

## pip3 freeze (user-level snapshot)
```
bcrypt==4.2.0
certifi==2025.1.31
chardet==5.2.0
charset-normalizer==3.4.2
cryptography==43.0.0
Deprecated==1.2.18
gyp-next==0.16.2
idna==3.10
jwcrypto==1.5.6
numpy==2.2.4
packaging==25.0
pip==25.1.1
redis==6.1.0
requests==2.32.3
typing_extensions==4.13.2
urllib3==2.3.0
websockify==0.12.0
wheel==0.46.1
wrapt==1.15.0
```

## npm global
- Capture failed (`ENOENT` on `~/.local/lib`). Assume no critical global npm packages.

## Post-Update restore procedure (on user instruction)
1. Confirm Update finished; run `box-doctor` (expect PASS summary).
2. Local mirror (if present): `/workspace/box-restore/INVENTORY.md` + `restore.sh`.
3. Reinstall apt keys:
   ```bash
   sudo apt-get update
   sudo apt-get install -y gh git nodejs npm python3 python3-pip python3-venv curl wget ffmpeg
   ```
4. Reinstall pip packages from freeze above (or `pip3 install -r /workspace/box-restore/requirements.txt` if generated).
5. Verify: `gh auth status`, `git --version`, `node -v`, `python3 -V`, `ffmpeg -version`.
6. Soft Build only if Soft LOCK resumes: install .NET SDK + Windows targeting as Soft Build bot requires (not present at this capture).
7. Soft HOLD / Usage Near ORANGE: do **not** Soft LOCK Soft Atomics until Chief clears HOLD.
8. After restore OK + user instruction: **delete this file** and local `/workspace/box-restore/` copies; commit deletion.

## Local mirror path
`/workspace/box-restore/INVENTORY.md` (same capture; survives Update with `/workspace`)

## Delete criteria
User says restore complete → remove `docs/ops/BOX-RESTORE-INVENTORY-TEMP.md` from repo and delete `/workspace/box-restore/` temp artifacts.
