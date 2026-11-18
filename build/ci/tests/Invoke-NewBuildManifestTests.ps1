<#
.SYNOPSIS
  CIF-01b-3 unit checks for build/ci/New-BuildManifest.ps1 (errata v1.0.2 E8-3). Plain pwsh, no Pester.
.DESCRIPTION
  - E8 positive fixtures (fixtures/normal, fixtures/build-fail, fixtures/missing-imageversion)
    and edge case fixtures/test-exit-missing (ci, build 0, Test exit code empty => Test fields STEP_NOT_RUN):
    generator exit 0, Test-Json against build/manifest/build-manifest.schema.json passes,
    -CheckManifest self-check passes, missingFields equals the expected list.
  - Negative generation cases (fixtures/negative/*.case.json): expected non-zero exit + message, no file written.
  - Negative static manifests (fixtures/negative/*.manifest.json): schema passes (schema cannot see it)
    but the self-check rejects them with the expected message.
  Inputs are synthetic fixtures. A throw-away git repo is built in -OutDir from copies of the
  repository files the generator reads; nothing outside -OutDir is written. A stub `dotnet`
  (prints the fixture SDK version only) is put first on PATH. Requires pwsh >= 7.4 (Test-Json 2020-12).
  Exit 0 when every case matches its expectation, 1 otherwise.
#>
param([string]$OutDir = (Join-Path ([IO.Path]::GetTempPath()) "nbm-tests-$([guid]::NewGuid().ToString('N'))"))

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion -lt [version]'7.4') { [Console]::Error.WriteLine("BLOCKED: pwsh >= 7.4 required, running $($PSVersionTable.PSVersion)"); exit 1 }

$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$gen = Join-Path $repo 'build/ci/New-BuildManifest.ps1'
$schema = Join-Path $repo 'build/manifest/build-manifest.schema.json'
$fx = Join-Path $PSScriptRoot 'fixtures'
$envNames = 'GITHUB_REPOSITORY', 'GITHUB_EVENT_NAME', 'GITHUB_REF', 'GITHUB_SHA', 'GITHUB_EVENT_PATH', 'GITHUB_RUN_ID',
'GITHUB_RUN_ATTEMPT', 'GITHUB_SERVER_URL', 'GITHUB_WORKFLOW_REF', 'RUNNER_OS', 'ImageOS', 'ImageVersion'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

# throw-away repo with copies of the files the generator reads
$tmpRepo = Join-Path $OutDir 'repo'
foreach ($rel in 'global.json', 'Directory.Build.props', '.github/workflows/ci-windows.yml', 'native/win-x64/native-lock.json', 'native/win-x64/README.md') {
    $dst = Join-Path $tmpRepo $rel
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dst) | Out-Null
    Copy-Item -LiteralPath (Join-Path $repo $rel) -Destination $dst
}
$env:GIT_AUTHOR_DATE = '2099-01-01T00:00:00Z'; $env:GIT_COMMITTER_DATE = '2099-01-01T00:00:00Z'
& git -C $tmpRepo init -q
& git -C $tmpRepo add -A
& git -C $tmpRepo -c user.name=fixture -c user.email=fixture@invalid -c commit.gpgsign=false commit -q -m fixture
if ($LASTEXITCODE -ne 0) { throw 'fixture git repo setup failed' }

$stubDir = Join-Path $OutDir 'stub'
New-Item -ItemType Directory -Force -Path $stubDir | Out-Null
$env:PATH = $stubDir + [IO.Path]::PathSeparator + $env:PATH
function Set-DotnetStub([string]$ver) {
    if ($IsWindows) {
        Set-Content -LiteralPath (Join-Path $stubDir 'dotnet.cmd') -Value "@echo off`r`nif `"%~1`"==`"--version`" (echo $ver& exit /b 0)`r`nexit /b 1`r`n"
    } else {
        $p = Join-Path $stubDir 'dotnet'
        Set-Content -LiteralPath $p -Value "#!/bin/sh`nif [ `"`$1`" = `"--version`" ]; then echo $ver; exit 0; fi`nexit 1`n"
        & chmod +x $p
    }
}

function Test-Schema([string]$path) {
    $e = $null
    $ok = Test-Json -Json (Get-Content -LiteralPath $path -Raw) -SchemaFile $schema -ErrorAction SilentlyContinue -ErrorVariable e
    return @{ Ok = [bool]$ok; Errors = @($e | ForEach-Object { "$_" }) }
}

function Invoke-Gen([System.Collections.IDictionary]$params, [string]$logBase) {
    # named parameters are passed as a JSON-splatted hashtable so values such as '-p:DmpDiag=true' or '' bind as values
    $params | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath "$logBase.args.json"
    $sb = { param($g, $j) $h = Get-Content -LiteralPath $j -Raw | ConvertFrom-Json -AsHashtable; & $g @h; exit $LASTEXITCODE }
    & pwsh -NoProfile -OutputFormat Text -Command $sb -args $gen, "$logBase.args.json" 1> "$logBase.out.txt" 2> "$logBase.err.txt"
    return @{ Exit = $LASTEXITCODE; Out = (Get-Content -LiteralPath "$logBase.out.txt" -Raw); Err = (Get-Content -LiteralPath "$logBase.err.txt" -Raw) }
}

function Invoke-Case([string]$name, [string]$caseDir, $case, $overrideArgs, [string[]]$unsetEnv) {
    foreach ($n in $envNames) { [Environment]::SetEnvironmentVariable($n, $null) }
    foreach ($p in $case.env.PSObject.Properties) { [Environment]::SetEnvironmentVariable($p.Name, [string]$p.Value) }
    if ($case.PSObject.Properties['eventPayload']) { $env:GITHUB_EVENT_PATH = Join-Path $caseDir $case.eventPayload }
    foreach ($n in @($unsetEnv)) { if ($n) { [Environment]::SetEnvironmentVariable($n, $null) } }
    Set-DotnetStub $case.dotnetVersion
    $work = Join-Path $OutDir $name
    $art = Join-Path $work 'artifacts'
    New-Item -ItemType Directory -Force -Path $art | Out-Null
    Copy-Item -Path (Join-Path $caseDir 'evidence/*') -Destination $art -Recurse
    $out = Join-Path $art "build-manifest-$($case.flavor).json"
    $a = [ordered]@{}
    foreach ($p in $case.args.PSObject.Properties) { $a[$p.Name] = [string]$p.Value }
    if ($overrideArgs) { foreach ($p in $overrideArgs.PSObject.Properties) { $a[$p.Name] = [string]$p.Value } }
    $params = [ordered]@{ Kind = $case.kind; Flavor = $case.flavor; RepoRoot = $tmpRepo; ArtifactsDir = $art; OutFile = $out; SchemaFile = $schema }
    foreach ($k in $a.Keys) { $params[$k] = $a[$k] }
    $r = Invoke-Gen $params (Join-Path $work 'gen')
    $r['OutFile'] = $out
    return $r
}

$rows = [System.Collections.Generic.List[object]]::new()
function Add-Row([string]$case, [string]$kind, $expExit, $actExit, [string[]]$checks, [string[]]$problems) {
    $rows.Add([ordered]@{ case = $case; type = $kind; expectedExit = $expExit; actualExit = $actExit; checks = ($checks -join '; '); result = $(if (@($problems).Count -eq 0) { 'OK' } else { 'MISMATCH' }); problems = @($problems) })
}

# --- positive E8 fixtures
foreach ($name in 'normal', 'build-fail', 'missing-imageversion', 'test-exit-missing') {
    $dir = Join-Path $fx $name
    $case = Get-Content -LiteralPath (Join-Path $dir 'case.json') -Raw | ConvertFrom-Json
    $r = Invoke-Case $name $dir $case $null @()
    $probs = [System.Collections.Generic.List[string]]::new(); $checks = [System.Collections.Generic.List[string]]::new()
    if ($r.Exit -ne $case.expectExit) { $probs.Add("exit $($r.Exit): $($r.Err)") }
    if (Test-Path -LiteralPath $r.OutFile) {
        $s = Test-Schema $r.OutFile
        $checks.Add("Test-Json=$($s.Ok)"); if (-not $s.Ok) { $probs.Add("schema: $($s.Errors -join ' | ')") }
        $c = Invoke-Gen ([ordered]@{ CheckManifest = $r.OutFile }) (Join-Path $OutDir "$name/check")
        $checks.Add("self-check exit=$($c.Exit)"); if ($c.Exit -ne 0) { $probs.Add("self-check: $($c.Err)") }
        $m = Get-Content -LiteralPath $r.OutFile -Raw | ConvertFrom-Json -AsHashtable
        $act = @($m['missingFields'] | ForEach-Object { "$($_['field']):$($_['reason'])" } | Sort-Object)
        $exp = @($case.expectMissingFields | Sort-Object)
        $checks.Add("missingFields=[$($act -join ',')]")
        if (($act -join '|') -cne ($exp -join '|')) { $probs.Add("missingFields expected [$($exp -join ',')]") }
        $checks.Add("buildId=$($m['buildId'])")
    } else { $probs.Add('manifest not written') }
    Add-Row $name 'E8 positive' $case.expectExit $r.Exit $checks $probs
}

# --- negative generation cases
foreach ($f in @(Get-ChildItem -LiteralPath (Join-Path $fx 'negative') -Filter '*.case.json' | Sort-Object Name)) {
    $neg = Get-Content -LiteralPath $f.FullName -Raw | ConvertFrom-Json
    $dir = Join-Path $fx $neg.baseCase
    $case = Get-Content -LiteralPath (Join-Path $dir 'case.json') -Raw | ConvertFrom-Json
    $unset = if ($neg.PSObject.Properties['unsetEnv']) { @($neg.unsetEnv) } else { @() }
    $ov = if ($neg.PSObject.Properties['args']) { $neg.args } else { $null }
    $name = $f.Name -replace '\.case\.json$', ''
    $r = Invoke-Case $name $dir $case $ov $unset
    $probs = [System.Collections.Generic.List[string]]::new()
    if ($r.Exit -ne $neg.expectExit) { $probs.Add("exit $($r.Exit)") }
    if (-not ([string]$r.Err).Contains($neg.expectStderr)) { $probs.Add("stderr lacks expected message; got: $($r.Err)") }
    if (Test-Path -LiteralPath $r.OutFile) { $probs.Add('manifest was written') }
    Add-Row $name 'negative generate' $neg.expectExit $r.Exit @("stderr=$(([string]$r.Err).Trim())", "fileWritten=$(Test-Path -LiteralPath $r.OutFile)") $probs
}

# --- negative static manifests
$expect = Get-Content -LiteralPath (Join-Path $fx 'negative/static-expectations.json') -Raw | ConvertFrom-Json
foreach ($p in $expect.PSObject.Properties) {
    $path = Join-Path $fx "negative/$($p.Name)"
    $probs = [System.Collections.Generic.List[string]]::new()
    $s = Test-Schema $path
    if ($s.Ok -ne [bool]$p.Value.schemaValid) { $probs.Add("Test-Json=$($s.Ok) expected $($p.Value.schemaValid): $($s.Errors -join ' | ')") }
    $c = Invoke-Gen ([ordered]@{ CheckManifest = $path }) (Join-Path $OutDir ($p.Name -replace '\.json$', '.check'))
    if ($c.Exit -ne $p.Value.expectExit) { $probs.Add("self-check exit $($c.Exit)") }
    if ($p.Value.expectStderr -and -not ([string]$c.Err).Contains($p.Value.expectStderr)) { $probs.Add("stderr lacks expected message; got: $($c.Err)") }
    $msg = if ($c.Exit -eq 0) { ([string]$c.Out).Trim() } else { ([string]$c.Err).Trim() }
    Add-Row ($p.Name -replace '\.manifest\.json$', '') 'static self-check' $p.Value.expectExit $c.Exit @("Test-Json=$($s.Ok)", "msg=$msg") $probs
}

foreach ($n in $envNames) { [Environment]::SetEnvironmentVariable($n, $null) }
$rows | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $OutDir 'results.json')
Write-Output "pwsh $($PSVersionTable.PSVersion); OutDir $OutDir"
foreach ($row in $rows) { Write-Output ("[{0}] {1} ({2}) expected exit {3}, actual {4} :: {5}{6}" -f $row.result, $row.case, $row.type, $row.expectedExit, $row.actualExit, $row.checks, $(if ($row.problems.Count) { ' :: ' + ($row.problems -join ' | ') } else { '' })) }
$bad = @($rows | Where-Object { $_.result -ne 'OK' }).Count
Write-Output "cases $($rows.Count), mismatches $bad"
exit $(if ($bad -eq 0) { 0 } else { 1 })
