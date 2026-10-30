#if DMP_DIAG
using DesktopMediaPlayer.Engine.Mpv;
using Xunit;

namespace DesktopMediaPlayer.Playback.Tests;

// DIAG only: compiled and run with -p:DmpDiag=true.
public sealed class DiagMpvOptionsTests
{
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
    [InlineData("gpu-policy-adapter-override=a;b")]
    public void PolicyAdapterKey_BadValue_Rejected(string line)
    {
        var r = DiagMpvOptions.Parse(new[] { line });
        Assert.Null(r.PolicyAdapterOverride);
        Assert.Equal("bad_value", Assert.Single(r.Rejected).Value);
    }

    [Fact]
    public void ResolveFile_DataRootFirst_ThenExe_ThenNone()
    {
        var data = System.IO.Path.Combine("root", DiagMpvOptions.FileName);
        var exe = System.IO.Path.Combine("exe", DiagMpvOptions.FileName);
        Assert.Equal((data, "data"), DiagMpvOptions.ResolveFile("root", "exe", _ => true));
        Assert.Equal((exe, "exe"), DiagMpvOptions.ResolveFile("root", "exe", p => p == exe));
        Assert.Equal((exe, "none"), DiagMpvOptions.ResolveFile(null, "exe", _ => false));
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
