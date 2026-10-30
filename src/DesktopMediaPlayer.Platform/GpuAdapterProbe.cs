using System.Runtime.InteropServices;

namespace DesktopMediaPlayer.Platform;

/// <summary>A5-a: adapter descriptions in high-performance order plus the default (EnumAdapters1(0)) adapter.</summary>
public sealed record GpuAdapterList(IReadOnlyList<string> HighPerformanceOrder, string? DefaultAdapter, bool Failed)
{
    public static readonly GpuAdapterList ProbeFailed = new(Array.Empty<string>(), null, true);
}

public interface IGpuAdapterProbe
{
    /// <summary>Never throws; any failure returns <see cref="GpuAdapterList.ProbeFailed"/> (empty list).</summary>
    GpuAdapterList Probe();
}

/// <summary>DXGI via raw vtable calls (IDXGIFactory6.EnumAdapterByGpuPreference, IDXGIFactory1.EnumAdapters1, IDXGIAdapter.GetDesc).</summary>
public sealed class DxgiGpuAdapterProbe : IGpuAdapterProbe
{
    private const int VtEnumAdapters1 = 12, VtEnumAdapterByGpuPreference = 29, VtAdapterGetDesc = 8;
    private const int DxgiGpuPreferenceHighPerformance = 1;
    private const int MaxAdapters = 16;
    private static Guid IidFactory1 = new("770aae78-f26f-4dba-a829-253c83d1b387");
    private static readonly Guid IidFactory6 = new("c1b6694f-ff09-44a9-b03c-77900a0a1d17");
    private static Guid IidAdapter1 = new("29038f61-3839-4626-91fd-086879011a05");

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int EnumAdapters1Fn(nint self, uint index, out nint adapter);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int EnumByPreferenceFn(nint self, uint index, int preference, ref Guid riid, out nint adapter);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetDescFn(nint self, nint desc);

    [DllImport("dxgi.dll")]
    private static extern int CreateDXGIFactory1(ref Guid riid, out nint factory);

    public GpuAdapterList Probe()
    {
        nint factory = 0, factory6 = 0;
        try
        {
            if (CreateDXGIFactory1(ref IidFactory1, out factory) < 0 || factory == 0)
            {
                return GpuAdapterList.ProbeFailed;
            }

            string? defaultAdapter = null;
            if (Vt<EnumAdapters1Fn>(factory, VtEnumAdapters1)(factory, 0, out var first) >= 0 && first != 0)
            {
                defaultAdapter = DescribeAndRelease(first);
            }

            var iid6 = IidFactory6;
            if (Marshal.QueryInterface(factory, ref iid6, out factory6) < 0 || factory6 == 0)
            {
                return GpuAdapterList.ProbeFailed; // pre-1803 Windows: no IDXGIFactory6
            }

            var list = new List<string>();
            var enumByPref = Vt<EnumByPreferenceFn>(factory6, VtEnumAdapterByGpuPreference);
            for (uint i = 0; i < MaxAdapters; i++)
            {
                if (enumByPref(factory6, i, DxgiGpuPreferenceHighPerformance, ref IidAdapter1, out var adapter) < 0 || adapter == 0)
                {
                    break; // DXGI_ERROR_NOT_FOUND ends the enumeration
                }

                var desc = DescribeAndRelease(adapter);
                if (!string.IsNullOrEmpty(desc))
                {
                    list.Add(desc);
                }
            }

            return new GpuAdapterList(list, defaultAdapter, false);
        }
        catch (Exception)
        {
            return GpuAdapterList.ProbeFailed;
        }
        finally
        {
            if (factory6 != 0)
            {
                Marshal.Release(factory6);
            }

            if (factory != 0)
            {
                Marshal.Release(factory);
            }
        }
    }

    private static T Vt<T>(nint com, int slot) where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(com), slot * nint.Size));

    private static string? DescribeAndRelease(nint adapter)
    {
        var desc = Marshal.AllocHGlobal(512); // DXGI_ADAPTER_DESC is 304 bytes on x64; Description is WCHAR[128] at offset 0
        try
        {
            return Vt<GetDescFn>(adapter, VtAdapterGetDesc)(adapter, desc) < 0 ? null : Marshal.PtrToStringUni(desc, 128).Split('\0')[0];
        }
        finally
        {
            Marshal.FreeHGlobal(desc);
            Marshal.Release(adapter);
        }
    }
}
