namespace DesktopMediaPlayer.Playback;

/// <summary>Startup outcome for the CLI file argument (KI-CLIARG-01).</summary>
public enum StartupOpenOutcome
{
    /// <summary>No args were given.</summary>
    NoArgs,
    /// <summary>Args were given but none was an existing file.</summary>
    NoExistingFile,
    /// <summary>libmpv probe failed, open not attempted.</summary>
    ProbeFailed,
    /// <summary>OpenMediaFiles was called and did not fail synchronously.</summary>
    Opened,
    /// <summary>Open attempt failed; reason carries the failure kind.</summary>
    OpenFailed,
}

/// <summary>Pure formatter for the single startup_args log line. Never includes the path; extension only.</summary>
public static class StartupArgsLog
{
    public static string Format(int argCount, StartupOpenOutcome outcome, string? failureReason, string? extension)
    {
        var (result, reason) = outcome switch
        {
            StartupOpenOutcome.NoArgs => ("none", "no_args"),
            StartupOpenOutcome.NoExistingFile => ("none", "no_existing_file"),
            StartupOpenOutcome.ProbeFailed => ("skipped", "probe_failed"),
            StartupOpenOutcome.Opened => ("opened", null),
            _ => ("open_failed", string.IsNullOrEmpty(failureReason) ? "exception" : failureReason),
        };

        var line = $"args.count={argCount} result={result}";
        if (reason is not null)
        {
            line += $" reason={reason}";
        }

        return line + $" ext={extension ?? string.Empty}";
    }

    /// <summary>Picks the outcome when the probe succeeded and no open was attempted.</summary>
    public static StartupOpenOutcome NoPathOutcome(int argCount) =>
        argCount == 0 ? StartupOpenOutcome.NoArgs : StartupOpenOutcome.NoExistingFile;
}
