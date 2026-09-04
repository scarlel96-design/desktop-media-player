using System.Runtime.InteropServices;

namespace DesktopMediaPlayer.Engine.Mpv.Native;

internal static class MpvEventIds
{
    public const int None = 0;
    public const int Shutdown = 1;
    public const int LogMessage = 2;
    public const int GetPropertyReply = 3;
    public const int SetPropertyReply = 4;
    public const int CommandReply = 5;
    public const int StartFile = 6;
    public const int EndFile = 7;
    public const int FileLoaded = 8;
    public const int Idle = 11;
    public const int VideoReconfig = 13;
    public const int PlaybackRestart = 21;
    public const int PropertyChange = 22;
}

/// <summary>mpv_format for observe_property.</summary>
internal static class MpvFormat
{
    public const int None = 0;
    public const int String = 1;
    public const int Flag = 3;
    public const int Int64 = 4;
    public const int Double = 5;
    public const int Node = 6;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MpvEvent
{
    public int event_id;
    public int error;
    public ulong reply_userdata;
    public nint data;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MpvEventProperty
{
    public nint name;
    public int format;
    public nint data;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MpvEventEndFile
{
    public int reason;
    public int error;
}
