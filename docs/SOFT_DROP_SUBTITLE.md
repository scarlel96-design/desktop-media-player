# Soft Drop Subtitle File (S55 Soft)

Shell Soft: dropping a subtitle file (.srt/.ass/.ssa/.vtt/.sub, case-insensitive, same list as the Sub button filter) on the window loads it as external subtitle Soft via existing _facade.LoadExternalSubtitle(path) + PopulateTrackList(MediaTrackKind.Subtitle) Soft. Multiple subtitles Soft: first one only.
Branch Soft: only when TryGetDroppedMediaPaths is false (no media in drop). Media or mixed drops Soft still go to existing OpenMediaFiles Soft unchanged; TryGetDroppedMediaPaths / MediaExtensions Soft unchanged.
DragOver Soft uses the same predicate as Drop Soft: Copy for media, or for subtitle-only when facade exists and state is not Idle/Opening/Stopped/Error; otherwise Effects None Soft (other file types stay None).
Soft no-op Soft if no facade / no loaded media / exception Soft.
Shell only Soft: existing LoadExternalSubtitle / PopulateTrackList only Soft; no Facade/Engine/Contracts change.
Soft != Windows PASS. KI-022/024 WINDOWS PENDING.
