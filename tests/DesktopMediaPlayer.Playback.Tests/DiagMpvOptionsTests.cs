#if DMP_DIAG
using DesktopMediaPlayer.Engine.Mpv;
using Xunit;

namespace DesktopMediaPlayer.Playback.Tests;

// DIAG only: compiled and run with -p:DmpDiag=true.
public sealed class DiagMpvOptionsTests
{
    [Theory]
    [InlineData(" gpu-policy-force-adapter =  NVIDIA GeForce RTX 4070 ", "NVIDIA GeForce RTX 4070")]
    [InlineData("gpu-policy-force-adapter=", null)]
    [InlineData("gpu-policy-force-adapter=a\nb", null)]
    public void PolicyForceAdapterKey_Parse_NotPassthrough(string line, string? expected)
    {
        var r = DiagMpvOptions.Parse(new[] { line });
        Assert.Equal(expected, r.PolicyForceAdapter);
        Assert.Null(r.PolicyAdapterOverride);
        Assert.Empty(r.Accepted);
        Assert.Equal(expected is null ? 1 : 0, r.Rejected.Count);
        Assert.Null(DiagMpvOptions.Parse(new[] { "gpu-policy-force-adapter=" + new string('a', 257) }).PolicyForceAdapter);
    }

    [Fact]
    public void ApplyForceAdapter_OnlyWhenPolicyNone()
    {
        Assert.Equal(new GpuAdapterDecision("NV", "diag_force"), DiagMpvOptions.ApplyForceAdapter(new GpuAdapterDecision(null, "default"), "NV"));
        var selected = new GpuAdapterDecision("Intel", "selected");
        Assert.Same(selected, DiagMpvOptions.ApplyForceAdapter(selected, "NV"));
        var none = new GpuAdapterDecision(null, "default");
        Assert.Same(none, DiagMpvOptions.ApplyForceAdapter(none, null));
    }

    [Fact]
    public void FormatProbe_NamesOnly_OrFailed()
    {
        Assert.Equal("default=Intel UHD hp=[NVIDIA RTX,Intel UHD]", DiagMpvOptions.FormatProbe(new[] { "NVIDIA RTX", "Intel UHD" }, "Intel UHD", false));
        Assert.Equal("default=none hp=[]", DiagMpvOptions.FormatProbe(Array.Empty<string>(), null, false));
        Assert.Equal("failed=1", DiagMpvOptions.FormatProbe(Array.Empty<string>(), null, true));
    }

    [Theory]
    [InlineData("Device Name: NVIDIA GeForce RTX 4070\n", true, "NVIDIA GeForce RTX 4070")]
    [InlineData("  Device Name:   Intel(R) UHD Graphics  ", true, "Intel(R) UHD Graphics")]
    [InlineData("Device Name:", false, "")]
    [InlineData("Driver version: 31.0", false, "")]
    [InlineData(null, false, "")]
    public void TryParseDeviceName_MatchesOnlyDeviceLines(string? text, bool ok, string name)
    {
        Assert.Equal(ok, DiagMpvOptions.TryParseDeviceName(text, out var parsed));
        Assert.Equal(name, parsed);
    }

    [Theory]
    [InlineData(false, true, true, "no_policy_adapter")]
    [InlineData(true, true, true, "retried")]
    [InlineData(true, false, true, "first_frame")]
    [InlineData(true, false, false, null)]
    public void FaultIgnoredReason_Priority(bool applied, bool retried, bool firstFrame, string? expected)
    {
        Assert.Equal(expected, DiagMpvOptions.FaultIgnoredReason(applied, retried, firstFrame));
    }

    [Fact]
    public void PolicyAdapterKey_AcceptedTrimmed_NotPassthrough_NotD3d11Override()
    {
        var r = DiagMpvOptions.Parse(new[] { " gpu-policy-adapter-override =  NVIDIA GeForce RTX 4070 (x) " });
        Assert.Equal("NVIDIA GeForce RTX 4070 (x)", r.PolicyAdapterOverride);
        Assert.Empty(r.Accepted);
        Assert.Empty(r.Rejected);
        Assert.DoesNotContain(DiagMpvOptions.PolicyAdapterKey, DiagMpvOptions.AllowedKeys);
        Assert.DoesNotContain(r.Accepted, p => p.Key == "d3d11-adapter");
    }

    [Theory]
    [InlineData("gpu-policy-adapter-override=")]
    [InlineData("gpu-policy-adapter-override=  ")]
    [InlineData("gpu-policy-adapter-override=a\nb")]
    public void PolicyAdapterKey_EmptyOrMultiLine_Rejected(string line)
    {
        var r = DiagMpvOptions.Parse(new[] { line });
        Assert.Null(r.PolicyAdapterOverride);
        Assert.Equal("bad_value", Assert.Single(r.Rejected).Value);
    }

    [Fact]
    public void PolicyAdapterKey_LengthLimit256()
    {
        Assert.Equal(256, DiagMpvOptions.Parse(new[] { "gpu-policy-adapter-override=" + new string('a', 256) }).PolicyAdapterOverride?.Length);
        Assert.Null(DiagMpvOptions.Parse(new[] { "gpu-policy-adapter-override=" + new string('a', 257) }).PolicyAdapterOverride);
    }

    [Theory]
    [InlineData("data", "data")]
    [InlineData("app", "app")]
    [InlineData("none", "none")]
    public void ResolveFile_FirstExistingOnly_DataThenApp(string existing, string expectedSource)
    {
        var dirs = new[] { "data", "app" };
        var (path, source) = DiagMpvOptions.ResolveFile(dirs, p => p.StartsWith(existing) || (existing == "data" && p.StartsWith("app")));
        Assert.Equal(expectedSource, source);
        Assert.Equal(existing == "none" ? null : System.IO.Path.Combine(existing, DiagMpvOptions.FileName), path);
    }

    [Fact]
    public void ApplyAdapterOverride_ReplacesOnlyPolicyAdapter()
    {
        var applied = DiagMpvOptions.ApplyAdapterOverride(new GpuAdapterDecision("Intel", "selected"), "NVIDIA X");
        Assert.Equal(new GpuAdapterDecision("NVIDIA X", "diag_policy_override"), applied.Decision);
        Assert.False(applied.Ignored);
        var ignored = DiagMpvOptions.ApplyAdapterOverride(new GpuAdapterDecision(null, "default"), "NVIDIA X");
        Assert.Equal(new GpuAdapterDecision(null, "default"), ignored.Decision);
        Assert.True(ignored.Ignored);
        Assert.False(DiagMpvOptions.ApplyAdapterOverride(new GpuAdapterDecision(null, "default"), null).Ignored);
    }

    [Theory]
    [InlineData("gpu-policy-fault=vo", true, 0)]
    [InlineData("gpu-policy-fault=VO", false, 1)]
    [InlineData("gpu-policy-fault=", false, 1)]
    [InlineData("gpu-policy-fault=media", false, 1)]
    public void PolicyFaultKey_OnlyVo_NotPassthrough(string line, bool fault, int rejected)
    {
        var r = DiagMpvOptions.Parse(new[] { line });
        Assert.Equal(fault, r.PolicyFaultVo);
        Assert.Equal(rejected, r.Rejected.Count);
        Assert.Empty(r.Accepted);
        Assert.DoesNotContain(DiagMpvOptions.PolicyFaultKey, DiagMpvOptions.AllowedKeys);
    }

    [Fact]
    public void Parse_ValidLines_Accepted()
    {
        var r = DiagMpvOptions.Parse(new[] { "dither-depth=8", " d3d11-adapter = NVIDIA ", "scale=bilinear" });
        Assert.Equal(new[] { "dither-depth", "d3d11-adapter", "scale" }, r.Accepted.Select(p => p.Key));
        Assert.Equal("NVIDIA", r.Accepted[1].Value);
        Assert.Empty(r.Rejected);
    }

    [Fact]
    public void Parse_KeyOutsideAllowlist_RejectedNotAllowed()
    {
        var r = DiagMpvOptions.Parse(new[] { "vo=null", "script=evil.lua" });
        Assert.Empty(r.Accepted);
        Assert.All(r.Rejected, p => Assert.Equal("not_allowed", p.Value));
    }

    [Theory]
    [InlineData("dither=")]
    [InlineData("scale=bi linear")]
    [InlineData("scale=a;b")]
    [InlineData("hwdec=../x/y")]
    public void Parse_BadValue_Rejected(string line)
    {
        var r = DiagMpvOptions.Parse(new[] { line });
        Assert.Empty(r.Accepted);
        Assert.Equal("bad_value", Assert.Single(r.Rejected).Value);
    }

    [Fact]
    public void Parse_ValueOver64_Rejected()
    {
        var r = DiagMpvOptions.Parse(new[] { "scale=" + new string('a', 65) });
        Assert.Equal("bad_value", Assert.Single(r.Rejected).Value);
    }

    [Fact]
    public void Parse_NoEquals_RejectedFormat()
    {
        Assert.Equal("format", Assert.Single(DiagMpvOptions.Parse(new[] { "dither" }).Rejected).Value);
    }

    [Fact]
    public void Parse_DuplicateKey_LastWins()
    {
        var r = DiagMpvOptions.Parse(new[] { "cscale=lanczos", "cscale=bilinear" });
        var only = Assert.Single(r.Accepted);
        Assert.Equal("bilinear", only.Value);
    }

    [Fact]
    public void Parse_EmptyOrMissing_NoOp()
    {
        Assert.Empty(DiagMpvOptions.Parse(null).Accepted);
        var r = DiagMpvOptions.Parse(new[] { "", "   " });
        Assert.Empty(r.Accepted);
        Assert.Empty(r.Rejected);
    }

    [Fact]
    public void OverridesCscale_OnlyWhenCscaleApplied()
    {
        Assert.True(DiagMpvOptions.OverridesCscale(new[] { "dither", "cscale" }));
        Assert.False(DiagMpvOptions.OverridesCscale(new[] { "scale", "dscale" }));
    }

    [Fact]
    public void SumFreshAvgMs_SumsFreshOnly()
    {
        const string json = "{\"fresh\":[{\"desc\":\"a\",\"avg\":1500000},{\"desc\":\"b\",\"avg\":500000}],\"redraw\":[{\"avg\":9000000}]}";
        Assert.Equal(2.0, DiagMpvOptions.SumFreshAvgMs(json));
        Assert.Null(DiagMpvOptions.SumFreshAvgMs("not json"));
        Assert.Null(DiagMpvOptions.SumFreshAvgMs(null));
    }

    [Fact]
    public void CpuPercent_NormalizesByCores()
    {
        Assert.Equal(25.0, DiagMpvOptions.CpuPercent(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), 4));
        Assert.Null(DiagMpvOptions.CpuPercent(TimeSpan.FromSeconds(1), TimeSpan.Zero, 4));
    }
}
#endif
