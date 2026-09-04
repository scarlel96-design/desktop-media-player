using System.Reflection;
using System.Runtime.InteropServices;

namespace DesktopMediaPlayer.Engine.Mpv.Native;

/// <summary>libmpv client API P/Invoke (x64 cdecl).</summary>
internal static partial class MpvNative
{
    public const string DllName = "libmpv-2";

    static MpvNative()
    {
        NativeLibrary.SetDllImportResolver(typeof(MpvNative).Assembly, Resolve);
    }

    private static nint Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!libraryName.Equals(DllName, StringComparison.OrdinalIgnoreCase)
            && !libraryName.Equals("libmpv-2.dll", StringComparison.OrdinalIgnoreCase))
        {
            return nint.Zero;
        }

        foreach (var candidate in EnumerateCandidates())
        {
            if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out var handle))
            {
                return handle;
            }
        }

        if (NativeLibrary.TryLoad("libmpv-2.dll", assembly, searchPath, out var fallback))
        {
            return fallback;
        }

        return nint.Zero;
    }

    private static IEnumerable<string> EnumerateCandidates()
    {
        var env = Environment.GetEnvironmentVariable("DMP_LIBMPV_PATH");
        if (!string.IsNullOrWhiteSpace(env))
        {
            if (Directory.Exists(env))
            {
                yield return Path.Combine(env, "libmpv-2.dll");
            }
            else
            {
                yield return env;
            }
        }

        var baseDir = AppContext.BaseDirectory;
        yield return Path.Combine(baseDir, "libmpv-2.dll");
        yield return Path.Combine(baseDir, "native", "win-x64", "libmpv-2.dll");

        var dir = new DirectoryInfo(baseDir);
        for (var i = 0; i < 8 && dir is not null; i++, dir = dir.Parent)
        {
            yield return Path.Combine(dir.FullName, "native", "win-x64", "libmpv-2.dll");
        }
    }

    public static bool TryProbeLibrary(out string? loadedPath, out string? error)
    {
        loadedPath = null;
        error = null;
        try
        {
            foreach (var candidate in EnumerateCandidates())
            {
                if (!File.Exists(candidate))
                {
                    continue;
                }

                if (NativeLibrary.TryLoad(candidate, out var handle))
                {
                    loadedPath = candidate;
                    _ = handle;
                    return true;
                }
            }

            if (NativeLibrary.TryLoad("libmpv-2.dll", typeof(MpvNative).Assembly, null, out var h))
            {
                loadedPath = "libmpv-2.dll (default probe)";
                _ = h;
                return true;
            }

            error = "libmpv-2.dll not found. Place LGPL binaries under native/win-x64 or set DMP_LIBMPV_PATH.";
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    [LibraryImport(DllName, EntryPoint = "mpv_create")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial nint mpv_create();

    [LibraryImport(DllName, EntryPoint = "mpv_initialize")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial int mpv_initialize(nint mpv);

    [LibraryImport(DllName, EntryPoint = "mpv_terminate_destroy")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial void mpv_terminate_destroy(nint mpv);

    [LibraryImport(DllName, EntryPoint = "mpv_command_string", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial int mpv_command_string(nint mpv, string command);

    [LibraryImport(DllName, EntryPoint = "mpv_set_option_string", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial int mpv_set_option_string(nint mpv, string name, string data);

    [LibraryImport(DllName, EntryPoint = "mpv_set_property_string", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial int mpv_set_property_string(nint mpv, string name, string data);

    [LibraryImport(DllName, EntryPoint = "mpv_get_property_string", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial nint mpv_get_property_string(nint mpv, string name);

    [LibraryImport(DllName, EntryPoint = "mpv_free")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial void mpv_free(nint data);

    [LibraryImport(DllName, EntryPoint = "mpv_wait_event")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial nint mpv_wait_event(nint mpv, double timeout);

    [LibraryImport(DllName, EntryPoint = "mpv_request_log_messages", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial int mpv_request_log_messages(nint mpv, string minLevel);

    [LibraryImport(DllName, EntryPoint = "mpv_error_string")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial nint mpv_error_string(int error);

    [LibraryImport(DllName, EntryPoint = "mpv_observe_property", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial int mpv_observe_property(nint mpv, ulong replyUserdata, string name, int format);

    public static string? PtrToUtf8(nint ptr)
    {
        if (ptr == nint.Zero)
        {
            return null;
        }

        return Marshal.PtrToStringUTF8(ptr);
    }

    public static string GetErrorString(int error)
    {
        var p = mpv_error_string(error);
        return PtrToUtf8(p) ?? $"mpv_error({error})";
    }

    public static string? GetPropertyAndFree(nint mpv, string name)
    {
        var p = mpv_get_property_string(mpv, name);
        if (p == nint.Zero)
        {
            return null;
        }

        try
        {
            return Marshal.PtrToStringUTF8(p);
        }
        finally
        {
            mpv_free(p);
        }
    }
}
