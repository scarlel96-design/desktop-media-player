<#
.SYNOPSIS
  CIF-01b-3: Build Manifest (M-B) generator.
.DESCRIPTION
  Design: CIF-01b-BUILD-MANIFEST-DESIGN v1.0 (bf7eca51...04a9) section 3.1/4.1,
  errata v1.0.1 (69c93f0c...9d7d) E1-E3, errata v1.0.2 (c96654fd...e990) E7-E9.
  Measured values only. No verdicts (PASS/FAIL/Gate) are written.

  - A measured field that cannot be obtained is null and listed once in missingFields
    (reason NOT_PROVIDED_BY_ENV | SOURCE_FILE_MISSING | SOURCE_PARSE_FAILED | STEP_NOT_RUN).
  - Optional (O) fields that cannot be obtained are omitted.
  - local R(ci) fields (run.runId, run.runAttempt, toolchain.runner.*, toolchain.actions)
    are null and not listed (E2-3).
  - Generated fields (schemaVersion, manifestKind, buildId, flavor, evidenceClass, generator,
    native.policy, warnings.method, missingFields) are never null. If one cannot be produced
    the script exits non-zero without writing (E2-2).
  - Self-check runs before writing (E3): null <-> missingFields both ways, no duplicate field,
    ci: buildId == "CI-{runId}-{runAttempt}-{flavor}", run.startedAtUtc <= run.finishedAtUtc when both present.
  - run.startedAtUtc / run.finishedAtUtc come from trx Times @start / @finish (UTC).
  - ci: an empty -BuildExitCode / -TestExitCode means that step did not run; its fields are STEP_NOT_RUN.
  - Runner image values come only from env ImageOS / ImageVersion (E9).

  Exit codes: 0 written; 1 generation failure; 2 self-check failure; 3 schema check failure.
  Nothing is written unless the exit code is 0.

  -CheckManifest <path> runs the self-check only on an existing manifest file (unit fixtures).
#>
[CmdletBinding(DefaultParameterSetName = 'Generate')]
param(
    [Parameter(ParameterSetName = 'Generate', Mandatory)][ValidateSet('ci', 'local')][string]$Kind,
    [Parameter(ParameterSetName = 'Generate', Mandatory)][ValidateSet('release', 'diag')][string]$Flavor,
    [Parameter(ParameterSetName = 'Generate', Mandatory)][string]$OutFile,
    [Parameter(ParameterSetName = 'Generate')][string]$BuildId,
    [Parameter(ParameterSetName = 'Generate')][string]$Configuration,
    [Parameter(ParameterSetName = 'Generate')][AllowEmptyString()][string]$MsbuildExtra,
    [Parameter(ParameterSetName = 'Generate')][string]$RepoRoot,
    [Parameter(ParameterSetName = 'Generate')][string]$ArtifactsDir,
    [Parameter(ParameterSetName = 'Generate')][string]$TrxFile,
    [Parameter(ParameterSetName = 'Generate')][string]$WarningsFile,
    [Parameter(ParameterSetName = 'Generate')][string]$BuildLogFile,
    [Parameter(ParameterSetName = 'Generate')][AllowEmptyString()][string]$BuildExitCode,
    [Parameter(ParameterSetName = 'Generate')][AllowEmptyString()][string]$TestExitCode,
    [Parameter(ParameterSetName = 'Generate')][string[]]$PackageFiles,
    [Parameter(ParameterSetName = 'Generate')][string]$SchemaFile,
    [Parameter(ParameterSetName = 'Check', Mandatory)][string]$CheckManifest
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

$PolicyConst = 'no-download; not-in-repo; provenance via native-lock'
$WarnMethodConst = "unique-lines regex 'warning [A-Z]+[0-9]+' in build log"
$WarnRegex = 'warning [A-Z]+[0-9]+'
$DefaultWorkflowFile = '.github/workflows/ci-windows.yml'
$LockRelPath = 'native/win-x64/native-lock.json'
$Reasons = @('NOT_PROVIDED_BY_ENV', 'SOURCE_FILE_MISSING', 'SOURCE_PARSE_FAILED', 'STEP_NOT_RUN')
$LocalCiOnlyFields = @('run.runId', 'run.runAttempt', 'toolchain.runner.os', 'toolchain.runner.imageName',
    'toolchain.runner.imageVersion', 'toolchain.runner.label', 'toolchain.actions')

class GenerateFailure : System.Exception { GenerateFailure([string]$m) : base($m) {} }

function Write-Err([string]$m) { [Console]::Error.WriteLine($m) }

function Get-EnvValue([string]$name) {
    $v = [Environment]::GetEnvironmentVariable($name)
    if ([string]::IsNullOrEmpty($v)) { return $null }
    return $v
}

function Get-Sha256([string]$path) { (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() }

function Get-RelPath([string]$base, [string]$path) {
    [IO.Path]::GetRelativePath($base, $path).Replace('\', '/')
}

function Resolve-Under([string]$base, [string]$path) {
    if ([IO.Path]::IsPathRooted($path)) { return [IO.Path]::GetFullPath($path) }
    return [IO.Path]::GetFullPath((Join-Path $base $path))
}

# ---------------- self-check (E3) ----------------
function Get-NullPaths($node, [string]$prefix, [System.Collections.Generic.List[string]]$acc) {
    foreach ($k in @($node.Keys)) {
        $p = if ($prefix) { "$prefix.$k" } else { [string]$k }
        $v = $node[$k]
        if ($null -eq $v) { $acc.Add($p) }
        elseif ($v -is [System.Collections.IDictionary]) { Get-NullPaths $v $p $acc }
        # arrays are not descended (E1: fields inside arrays are not recorded)
    }
}

function Resolve-Path-InManifest($m, [string]$field) {
    $cur = $m
    foreach ($seg in $field.Split('.')) {
        if (-not ($cur -is [System.Collections.IDictionary]) -or -not $cur.Contains($seg)) {
            return @{ Found = $false; Value = $null }
        }
        $cur = $cur[$seg]
    }
    return @{ Found = $true; Value = $cur }
}

function Test-ManifestSelfCheck($m) {
    $errs = [System.Collections.Generic.List[string]]::new()
    if (-not ($m -is [System.Collections.IDictionary])) { $errs.Add('manifest root is not an object'); return $errs }

    # E2-2 generated fields: never null
    if ($m['schemaVersion'] -cne '1.0') { $errs.Add("generated field schemaVersion invalid: '$($m['schemaVersion'])'") }
    $kind = $m['manifestKind']
    if ($kind -cnotin @('ci', 'local')) { $errs.Add("generated field manifestKind invalid: '$kind'") }
    if ($m['buildId'] -isnot [string] -or [string]::IsNullOrEmpty($m['buildId'])) { $errs.Add('generated field buildId is null/empty') }
    if ($m['flavor'] -cnotin @('release', 'diag')) { $errs.Add("generated field flavor invalid: '$($m['flavor'])'") }
    $expClass = if ($kind -ceq 'ci') { 'CI' } elseif ($kind -ceq 'local') { 'LOCAL' } else { $null }
    if ($null -eq $m['evidenceClass'] -or $m['evidenceClass'] -cne $expClass) { $errs.Add("generated field evidenceClass invalid: '$($m['evidenceClass'])'") }
    $gen = $m['generator']
    if (-not ($gen -is [System.Collections.IDictionary]) -or [string]::IsNullOrEmpty([string]$gen['script']) -or
        ([string]$gen['scriptSha256']) -cnotmatch '^[0-9a-f]{64}$') { $errs.Add('generated field generator is null/invalid') }
    $nat = $m['native']
    if (-not ($nat -is [System.Collections.IDictionary]) -or $nat['policy'] -cne $PolicyConst) { $errs.Add('generated field native.policy is null/invalid') }
    $warn = $m['warnings']
    if (-not ($warn -is [System.Collections.IDictionary]) -or $warn['method'] -cne $WarnMethodConst) { $errs.Add('generated field warnings.method is null/invalid') }
    $mf = $m['missingFields']
    if ($null -eq $mf -or $mf -is [string] -or $mf -is [System.Collections.IDictionary] -or -not ($mf -is [System.Collections.IEnumerable])) {
        $errs.Add('generated field missingFields is null or not an array'); return $errs
    }

    # missingFields entries: format, uniqueness, listed => present and null
    $listed = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($e in @($mf)) {
        if (-not ($e -is [System.Collections.IDictionary]) -or [string]::IsNullOrEmpty([string]$e['field'])) { $errs.Add('missingFields entry without field'); continue }
        $f = [string]$e['field']
        if ([string]$e['reason'] -cnotin $Reasons) { $errs.Add("missingFields '$f': invalid reason '$($e['reason'])'") }
        if (-not $listed.Add($f)) { $errs.Add("missingFields duplicate field '$f'"); continue }
        $r = Resolve-Path-InManifest $m $f
        if (-not $r.Found) { $errs.Add("missingFields '$f': field not present in manifest") }
        elseif ($null -ne $r.Value) { $errs.Add("missingFields '$f': listed but has a value") }
        if ($kind -ceq 'local' -and $f -cin $LocalCiOnlyFields) { $errs.Add("missingFields '$f': local R(ci) field must not be listed (E2-3)") }
    }

    # null => listed
    $nulls = [System.Collections.Generic.List[string]]::new()
    Get-NullPaths $m '' $nulls
    foreach ($p in $nulls) {
        if ($kind -ceq 'local' -and $p -cin $LocalCiOnlyFields) { continue }
        if (-not $listed.Contains($p)) { $errs.Add("field '$p' is null but not listed in missingFields") }
    }

    # ci: buildId == CI-{runId}-{runAttempt}-{flavor}
    if ($kind -ceq 'ci') {
        $run = $m['run']
        $rid = if ($run -is [System.Collections.IDictionary]) { $run['runId'] } else { $null }
        $rat = if ($run -is [System.Collections.IDictionary]) { $run['runAttempt'] } else { $null }
        $isInt = { param($x) $x -is [int] -or $x -is [long] }
        if (-not (& $isInt $rid) -or -not (& $isInt $rat)) {
            $errs.Add("ci buildId check: run.runId/run.runAttempt not integers (buildId '$($m['buildId'])')")
        } else {
            $exp = "CI-$rid-$rat-$($m['flavor'])"
            if ($m['buildId'] -cne $exp) { $errs.Add("ci buildId mismatch: buildId '$($m['buildId'])' != expected '$exp'") }
        }
    }

    # run.startedAtUtc <= run.finishedAtUtc when both present
    $runObj = $m['run']
    if ($runObj -is [System.Collections.IDictionary] -and $null -ne $runObj['startedAtUtc'] -and $null -ne $runObj['finishedAtUtc']) {
        $toUtc = {
            param($x)
            if ($x -is [DateTime]) { return $x.ToUniversalTime() }
            $d = [DateTimeOffset]::MinValue
            if ($x -is [string] -and [DateTimeOffset]::TryParse($x, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::None, [ref]$d)) { return $d.UtcDateTime }
            return $null
        }
        $s0 = & $toUtc $runObj['startedAtUtc']; $f0 = & $toUtc $runObj['finishedAtUtc']
        if ($null -eq $s0 -or $null -eq $f0) { $errs.Add('run.startedAtUtc/run.finishedAtUtc not parseable as UTC timestamps') }
        elseif ($s0 -gt $f0) { $errs.Add("run.startedAtUtc '$($s0.ToString('o'))' is later than run.finishedAtUtc '$($f0.ToString('o'))'") }
    }
    return $errs
}

if ($PSCmdlet.ParameterSetName -eq 'Check') {
    try {
        $m = Get-Content -LiteralPath $CheckManifest -Raw | ConvertFrom-Json -AsHashtable
    } catch {
        Write-Err "SELF-CHECK FAIL: cannot read/parse '$CheckManifest': $($_.Exception.Message)"; exit 2
    }
    $errs = @(Test-ManifestSelfCheck $m)
    if ($errs.Count -gt 0) { foreach ($e in $errs) { Write-Err "SELF-CHECK FAIL: $e" }; exit 2 }
    Write-Output "SELF-CHECK OK: $CheckManifest"
    exit 0
}

# ---------------- generation ----------------
$missing = [System.Collections.Generic.List[object]]::new()
function Add-Missing([string]$field, [string]$reason, [string]$detail) {
    $e = [ordered]@{ field = $field; reason = $reason }
    if ($detail) { $e['detail'] = $detail }
    $missing.Add($e)
}
# measured value or null + missingFields entry
function Set-Measured($obj, [string]$key, [string]$path, $value, [string]$reason, [string]$detail) {
    $obj[$key] = $value
    if ($null -eq $value) { Add-Missing $path $reason $detail }
}

function Invoke-Tool([string]$exe, [string[]]$toolArgs, [string]$cwd) {
    if (-not (Get-Command $exe -CommandType Application -ErrorAction SilentlyContinue)) { return @{ Found = $false; ExitCode = -1; Lines = @() } }
    Push-Location -LiteralPath $cwd
    try {
        $out = & $exe @toolArgs 2>$null
        $code = $LASTEXITCODE
    } catch { $out = $null; $code = -1 } finally { Pop-Location }
    $lines = @($out | ForEach-Object { ([string]$_).TrimEnd() } | Where-Object { $_ -ne '' })
    return @{ Found = $true; ExitCode = $code; Lines = $lines }
}

function Read-IntParam([string]$name, [string]$raw) {
    if ([string]::IsNullOrEmpty($raw)) { return $null }
    $n = 0
    if (-not [int]::TryParse($raw, [ref]$n)) { throw [GenerateFailure]::new("-$name '$raw' is not an integer") }
    return $n
}

try {
    if (-not $RepoRoot) { $RepoRoot = (Get-Location).Path }
    $RepoRoot = [IO.Path]::GetFullPath($RepoRoot)
    if (-not (Test-Path -LiteralPath $RepoRoot -PathType Container)) { throw [GenerateFailure]::new("RepoRoot '$RepoRoot' not found") }
    if (-not $ArtifactsDir) { $ArtifactsDir = Join-Path $RepoRoot 'artifacts' }
    $ArtifactsDir = Resolve-Under $RepoRoot $ArtifactsDir
    $OutFull = Resolve-Under (Get-Location).Path $OutFile
    if (-not $TrxFile) { $TrxFile = "test-results/test-$Flavor.trx" }
    if (-not $WarningsFile) { $WarningsFile = "warnings-$Flavor.txt" }
    if (-not $BuildLogFile) { $BuildLogFile = "build-$Flavor.log" }
    $buildExit = Read-IntParam 'BuildExitCode' $BuildExitCode
    $testExit = Read-IntParam 'TestExitCode' $TestExitCode
    $env_ = @{}
    foreach ($n in 'GITHUB_REPOSITORY', 'GITHUB_EVENT_NAME', 'GITHUB_REF', 'GITHUB_SHA', 'GITHUB_EVENT_PATH', 'GITHUB_RUN_ID',
        'GITHUB_RUN_ATTEMPT', 'GITHUB_SERVER_URL', 'GITHUB_WORKFLOW_REF', 'RUNNER_OS', 'ImageOS', 'ImageVersion') { $env_[$n] = Get-EnvValue $n }

    # --- generated fields (E2-2): failure => exit 1, nothing written
    $runId = $null; $runAttempt = $null
    if ($Kind -eq 'ci') {
        $rid = 0L; $rat = 0L
        if ($null -eq $env_.GITHUB_RUN_ID -or -not [long]::TryParse($env_.GITHUB_RUN_ID, [ref]$rid)) { throw [GenerateFailure]::new('buildId cannot be produced: GITHUB_RUN_ID missing or not an integer') }
        if ($null -eq $env_.GITHUB_RUN_ATTEMPT -or -not [long]::TryParse($env_.GITHUB_RUN_ATTEMPT, [ref]$rat)) { throw [GenerateFailure]::new('buildId cannot be produced: GITHUB_RUN_ATTEMPT missing or not an integer') }
        $runId = $rid; $runAttempt = $rat
        $genBuildId = "CI-$runId-$runAttempt-$Flavor"
        # an explicitly passed -BuildId is kept as given; the self-check compares it
        $finalBuildId = if ($PSBoundParameters.ContainsKey('BuildId')) { $BuildId } else { $genBuildId }
    } else {
        if ([string]::IsNullOrEmpty($BuildId)) { throw [GenerateFailure]::new('buildId cannot be produced: local requires -BuildId (BLD-...)') }
        $finalBuildId = $BuildId
    }
    $scriptPath = $PSCommandPath
    if (-not $scriptPath -or -not (Test-Path -LiteralPath $scriptPath)) { throw [GenerateFailure]::new('generator cannot be produced: script path unknown') }
    $scriptRel = Get-RelPath $RepoRoot $scriptPath
    $generator = [ordered]@{ script = $(if ($scriptRel.StartsWith('..')) { $scriptPath.Replace('\', '/') } else { $scriptRel }); scriptSha256 = (Get-Sha256 $scriptPath) }

    $m = [ordered]@{}
    $m['schemaVersion'] = '1.0'
    $m['manifestKind'] = $Kind
    $m['buildId'] = $finalBuildId
    $m['flavor'] = $Flavor
    Set-Measured $m 'configuration' 'configuration' $(if ($Configuration) { $Configuration } else { $null }) 'NOT_PROVIDED_BY_ENV' '-Configuration not supplied'

    # msbuildProperties from matrix extra
    if (-not $PSBoundParameters.ContainsKey('MsbuildExtra')) {
        Set-Measured $m 'msbuildProperties' 'msbuildProperties' $null 'NOT_PROVIDED_BY_ENV' '-MsbuildExtra not supplied'
    } else {
        $props = [ordered]@{}; $ok = $true
        foreach ($tok in @($MsbuildExtra -split '\s+' | Where-Object { $_ -ne '' })) {
            if ($tok -match '^[-/]p(?:roperty)?:([^=]+)=(.*)$') { $props[$Matches[1]] = $Matches[2] } else { $ok = $false }
        }
        Set-Measured $m 'msbuildProperties' 'msbuildProperties' $(if ($ok) { $props } else { $null }) 'SOURCE_PARSE_FAILED' "cannot parse -MsbuildExtra '$MsbuildExtra'"
    }

    # platform / targetFramework from Directory.Build.props
    $propsPath = Join-Path $RepoRoot 'Directory.Build.props'
    foreach ($pair in @(@('platform', 'Platform'), @('targetFramework', 'TargetFramework'))) {
        if (-not (Test-Path -LiteralPath $propsPath)) { Set-Measured $m $pair[0] $pair[0] $null 'SOURCE_FILE_MISSING' 'Directory.Build.props not found'; continue }
        $val = $null
        try {
            [xml]$x = Get-Content -LiteralPath $propsPath -Raw
            $nodes = @($x.SelectNodes("/Project/PropertyGroup[not(@Condition)]/$($pair[1])") | ForEach-Object { $_.InnerText.Trim() } | Sort-Object -Unique)
            if ($nodes.Count -eq 1 -and $nodes[0]) { $val = $nodes[0] }
        } catch { $val = $null }
        Set-Measured $m $pair[0] $pair[0] $val 'SOURCE_PARSE_FAILED' "Directory.Build.props unconditional <$($pair[1])> not found or ambiguous"
    }

    # source
    $src = [ordered]@{}
    Set-Measured $src 'repository' 'source.repository' $env_.GITHUB_REPOSITORY 'NOT_PROVIDED_BY_ENV' 'GITHUB_REPOSITORY not set'
    Set-Measured $src 'event' 'source.event' $env_.GITHUB_EVENT_NAME 'NOT_PROVIDED_BY_ENV' 'GITHUB_EVENT_NAME not set'
    Set-Measured $src 'ref' 'source.ref' $env_.GITHUB_REF 'NOT_PROVIDED_BY_ENV' 'GITHUB_REF not set'
    $isPr = $env_.GITHUB_EVENT_NAME -in @('pull_request', 'pull_request_target')
    $headSha = $null; $headReason = 'NOT_PROVIDED_BY_ENV'; $headDetail = 'GITHUB_SHA not set'; $baseSha = $null
    if ($isPr) {
        if ($null -eq $env_.GITHUB_EVENT_PATH -or -not (Test-Path -LiteralPath $env_.GITHUB_EVENT_PATH)) {
            $headReason = 'SOURCE_FILE_MISSING'; $headDetail = 'GITHUB_EVENT_PATH payload not found'
        } else {
            try {
                $ev = Get-Content -LiteralPath $env_.GITHUB_EVENT_PATH -Raw | ConvertFrom-Json -AsHashtable
                $pr = $ev['pull_request']
                if ($pr -is [System.Collections.IDictionary]) {
                    if ($pr['head'] -is [System.Collections.IDictionary]) { $headSha = [string]$pr['head']['sha'] }
                    if ($pr['base'] -is [System.Collections.IDictionary]) { $baseSha = [string]$pr['base']['sha'] }
                }
            } catch { $headSha = $null }
            $headReason = 'SOURCE_PARSE_FAILED'; $headDetail = 'pull_request.head.sha not readable from event payload'
        }
    } else { $headSha = $env_.GITHUB_SHA }
    if ($headSha -and $headSha -cnotmatch '^[0-9a-f]{40}$') { $headSha = $null; $headReason = 'SOURCE_PARSE_FAILED'; $headDetail = 'head sha is not 40 lowercase hex' }
    Set-Measured $src 'headSha' 'source.headSha' $(if ($headSha) { $headSha } else { $null }) $headReason $headDetail
    $git = Invoke-Tool 'git' @('rev-parse', 'HEAD') $RepoRoot
    $co = if ($git.Found -and $git.ExitCode -eq 0 -and $git.Lines.Count -eq 1 -and $git.Lines[0] -cmatch '^[0-9a-f]{40}$') { $git.Lines[0] } else { $null }
    Set-Measured $src 'checkoutSha' 'source.checkoutSha' $co $(if ($git.Found) { 'SOURCE_PARSE_FAILED' } else { 'NOT_PROVIDED_BY_ENV' }) 'git rev-parse HEAD failed'
    if ($isPr -and $baseSha -and $baseSha -cmatch '^[0-9a-f]{40}$') { $src['baseSha'] = $baseSha }
    $st = Invoke-Tool 'git' @('status', '--porcelain') $RepoRoot
    $clean = if ($st.Found -and $st.ExitCode -eq 0) { $st.Lines.Count -eq 0 } else { $null }
    Set-Measured $src 'treeClean' 'source.treeClean' $clean $(if ($st.Found) { 'SOURCE_PARSE_FAILED' } else { 'NOT_PROVIDED_BY_ENV' }) 'git status --porcelain failed'
    $m['source'] = $src

    # tests (trx) -- parsed first: run.startedAtUtc / run.finishedAtUtc come from trx Times
    # ci: empty -TestExitCode => Test step not run (regardless of build); local: only when build failed
    $testsNotRun = if ($Kind -eq 'ci') { $null -eq $testExit } else { ($null -eq $testExit) -and ($null -ne $buildExit) -and ($buildExit -ne 0) }
    $buildNotRun = ($Kind -eq 'ci') -and ($null -eq $buildExit)
    $bxText = if ($null -eq $buildExit) { 'not supplied' } else { [string]$buildExit }
    $testNotRunDetail = "Test exit code not supplied (build exit code $bxText); Test step not run"
    $trxFull = Resolve-Under $ArtifactsDir $TrxFile
    $trx = $null; $trxReason = $null; $trxDetail = $null
    if ($testsNotRun) { $trxReason = 'STEP_NOT_RUN'; $trxDetail = $testNotRunDetail }
    elseif (-not (Test-Path -LiteralPath $trxFull -PathType Leaf)) { $trxReason = 'SOURCE_FILE_MISSING'; $trxDetail = "trx not found: $(Get-RelPath $ArtifactsDir $trxFull)" }
    else {
        try { [xml]$trx = Get-Content -LiteralPath $trxFull -Raw } catch { $trx = $null; $trxReason = 'SOURCE_PARSE_FAILED'; $trxDetail = 'trx is not valid XML' }
    }
    $ns = $null
    if ($trx) { $ns = [System.Xml.XmlNamespaceManager]::new($trx.NameTable); $ns.AddNamespace('t', $trx.DocumentElement.NamespaceURI) }
    function Get-TrxAttr([string]$xpath, [string]$attr) {
        if (-not $trx) { return $null }
        $n = $trx.SelectSingleNode($xpath, $ns)
        if ($null -eq $n -or $null -eq $n.Attributes[$attr]) { return $null }
        return $n.Attributes[$attr].Value
    }

    $tests = [ordered]@{}
    if ($trx) {
        $fw = @($trx.SelectNodes('//t:TestDefinitions/t:UnitTest/t:TestMethod', $ns) | ForEach-Object { $_.GetAttribute('adapterTypeName') } | Where-Object { $_ } | Sort-Object -Unique)
        if ($fw.Count -eq 1) { $tests['framework'] = $fw[0] }
        $tests['trxFile'] = Get-RelPath $ArtifactsDir $trxFull
        $tests['trxSha256'] = Get-Sha256 $trxFull
    } else {
        Set-Measured $tests 'trxFile' 'tests.trxFile' $null $trxReason $trxDetail
        Set-Measured $tests 'trxSha256' 'tests.trxSha256' $null $trxReason $trxDetail
    }
    foreach ($c in 'total', 'executed', 'passed', 'failed', 'notExecuted', 'error', 'timeout', 'aborted') {
        if (-not $trx) { Set-Measured $tests $c "tests.$c" $null $trxReason $trxDetail; continue }
        $raw = Get-TrxAttr '/t:TestRun/t:ResultSummary/t:Counters' $c
        $n = 0L
        $v = if ($null -ne $raw -and [long]::TryParse($raw, [ref]$n) -and $n -ge 0) { $n } else { $null }
        Set-Measured $tests $c "tests.$c" $v 'SOURCE_PARSE_FAILED' "trx Counters/@$c not readable"
    }
    if ($trx) {
        Set-Measured $tests 'outcome' 'tests.outcome' (Get-TrxAttr '/t:TestRun/t:ResultSummary' 'outcome') 'SOURCE_PARSE_FAILED' 'trx ResultSummary/@outcome not readable'
    } else { Set-Measured $tests 'outcome' 'tests.outcome' $null $trxReason $trxDetail }

    # run
    $run = [ordered]@{}
    $run['runId'] = $runId
    $run['runAttempt'] = $runAttempt
    if ($Kind -eq 'ci' -and $env_.GITHUB_SERVER_URL -and $env_.GITHUB_REPOSITORY) { $run['jobUrl'] = "$($env_.GITHUB_SERVER_URL)/$($env_.GITHUB_REPOSITORY)/actions/runs/$runId" }
    $wf = $DefaultWorkflowFile; $wfReason = $null
    if ($env_.GITHUB_WORKFLOW_REF) {
        if ($env_.GITHUB_WORKFLOW_REF -match '^[^/]+/[^/]+/([^@]+)@') { $wf = $Matches[1] } else { $wf = $null; $wfReason = 'SOURCE_PARSE_FAILED' }
    }
    Set-Measured $run 'workflowFile' 'run.workflowFile' $wf $wfReason 'GITHUB_WORKFLOW_REF not parseable'
    $wfFull = if ($wf) { Resolve-Under $RepoRoot $wf } else { $null }
    $wfExists = $wfFull -and (Test-Path -LiteralPath $wfFull -PathType Leaf)
    Set-Measured $run 'workflowSha256' 'run.workflowSha256' $(if ($wfExists) { Get-Sha256 $wfFull } else { $null }) $(if ($wf) { 'SOURCE_FILE_MISSING' } else { 'SOURCE_PARSE_FAILED' }) 'workflow file not found'
    foreach ($pair in @(@('startedAtUtc', 'start'), @('finishedAtUtc', 'finish'))) {
        if (-not $trx) { Set-Measured $run $pair[0] "run.$($pair[0])" $null $trxReason $trxDetail; continue }
        $raw = Get-TrxAttr '/t:TestRun/t:Times' $pair[1]
        $ts = $null
        $dto = [DateTimeOffset]::MinValue
        if ($raw -and [DateTimeOffset]::TryParse($raw, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::None, [ref]$dto)) {
            $ts = $dto.UtcDateTime.ToString('yyyy-MM-ddTHH:mm:ss.fffffffZ', [Globalization.CultureInfo]::InvariantCulture)
        }
        Set-Measured $run $pair[0] "run.$($pair[0])" $ts 'SOURCE_PARSE_FAILED' "trx Times/@$($pair[1]) not readable"
    }
    $m['run'] = $run

    # toolchain
    $tc = [ordered]@{}
    $gj = Join-Path $RepoRoot 'global.json'
    $req = $null; $rf = $null; $gjReason = 'SOURCE_FILE_MISSING'
    if (Test-Path -LiteralPath $gj -PathType Leaf) {
        $gjReason = 'SOURCE_PARSE_FAILED'
        try {
            $g = Get-Content -LiteralPath $gj -Raw | ConvertFrom-Json -AsHashtable
            if ($g['sdk'] -is [System.Collections.IDictionary]) {
                if ($g['sdk']['version'] -is [string] -and $g['sdk']['version']) { $req = $g['sdk']['version'] }
                if ($g['sdk']['rollForward'] -is [string] -and $g['sdk']['rollForward']) { $rf = $g['sdk']['rollForward'] }
            }
        } catch { $req = $null; $rf = $null }
    }
    Set-Measured $tc 'sdkRequested' 'toolchain.sdkRequested' $req $gjReason 'global.json sdk.version'
    Set-Measured $tc 'sdkRollForward' 'toolchain.sdkRollForward' $rf $gjReason 'global.json sdk.rollForward'
    $dv = Invoke-Tool 'dotnet' @('--version') $RepoRoot
    $resolved = if ($dv.Found -and $dv.ExitCode -eq 0 -and $dv.Lines.Count -ge 1) { $dv.Lines[-1].Trim() } else { $null }
    $resReason = if ($dv.Found) { 'SOURCE_PARSE_FAILED' } else { 'NOT_PROVIDED_BY_ENV' }
    Set-Measured $tc 'sdkResolved' 'toolchain.sdkResolved' $resolved $resReason 'dotnet --version (repo root)'
    if ($null -ne $req -and $null -ne $resolved) { $tc['sdkMatchesRequested'] = ($req -ceq $resolved) }
    else {
        $r = if ($null -eq $req) { $gjReason } else { $resReason }
        Set-Measured $tc 'sdkMatchesRequested' 'toolchain.sdkMatchesRequested' $null $r 'derived: sdkRequested or sdkResolved is null'
    }
    if ($dv.Found) {
        $mb = Invoke-Tool 'dotnet' @('msbuild', '-version') $RepoRoot
        if ($mb.ExitCode -eq 0 -and $mb.Lines.Count -ge 1) { $tc['msbuildVersion'] = $mb.Lines[-1].Trim() }
        $rt = Invoke-Tool 'dotnet' @('--list-runtimes') $RepoRoot
        if ($rt.ExitCode -eq 0 -and $rt.Lines.Count -ge 1) { $tc['runtimeVersions'] = @($rt.Lines | ForEach-Object { $_.Trim() }) }
    }
    $wfLines = if ($wfExists) { @(Get-Content -LiteralPath $wfFull) } else { $null }
    if ($Kind -eq 'ci') {
        $rn = [ordered]@{}
        Set-Measured $rn 'os' 'toolchain.runner.os' $env_.RUNNER_OS 'NOT_PROVIDED_BY_ENV' 'RUNNER_OS not set'
        Set-Measured $rn 'imageName' 'toolchain.runner.imageName' $env_.ImageOS 'NOT_PROVIDED_BY_ENV' 'env ImageOS not set'
        Set-Measured $rn 'imageVersion' 'toolchain.runner.imageVersion' $env_.ImageVersion 'NOT_PROVIDED_BY_ENV' 'env ImageVersion not set'
        $label = $null
        if ($wfLines) {
            $ro = @($wfLines | Where-Object { $_ -match '^\s*runs-on:\s*(\S.*)$' } | ForEach-Object { ($_ -replace '^\s*runs-on:\s*', '' -replace '\s+#.*$', '').Trim().Trim('"', "'") })
            if ($ro.Count -eq 1 -and $ro[0] -and $ro[0] -notmatch '\$\{\{') { $label = $ro[0] }
        }
        Set-Measured $rn 'label' 'toolchain.runner.label' $label $(if ($wfLines) { 'SOURCE_PARSE_FAILED' } else { 'SOURCE_FILE_MISSING' }) 'workflow runs-on: (single constant) not found'
        $tc['runner'] = $rn
        $acts = $null
        if ($wfLines) {
            $acts = [System.Collections.Generic.List[object]]::new()
            foreach ($l in $wfLines) {
                if ($l -match '^\s*(?:-\s*)?uses:\s*([^@\s#]+)@([^\s#]+)\s*(?:#\s*(\S+))?') {
                    $acts.Add([ordered]@{ name = $Matches[1]; sha = $Matches[2]; tag = $(if ($Matches[3]) { $Matches[3] } else { '' }) })
                }
            }
            $acts = @($acts)
        }
        Set-Measured $tc 'actions' 'toolchain.actions' $acts 'SOURCE_FILE_MISSING' 'workflow file not found'
    } else {
        $tc['runner'] = [ordered]@{ os = $null; imageName = $null; imageVersion = $null; label = $null }
        $tc['actions'] = $null
    }
    $m['toolchain'] = $tc
    $m['tests'] = $tests

    # warnings
    $w = [ordered]@{}
    $wFull = Resolve-Under $ArtifactsDir $WarningsFile
    $wc = $null; $wReason = 'SOURCE_FILE_MISSING'
    if (Test-Path -LiteralPath $wFull -PathType Leaf) {
        $wReason = 'SOURCE_PARSE_FAILED'
        $hit = @(Get-Content -LiteralPath $wFull | Where-Object { $_ -match '^\s*warnings=(\d+)\s*$' })
        if ($hit.Count -eq 1 -and $hit[0] -match '^\s*warnings=(\d+)\s*$') { $wc = [long]$Matches[1] }
    }
    Set-Measured $w 'count' 'warnings.count' $wc $wReason "warnings file: $(Get-RelPath $ArtifactsDir $wFull)"
    $w['method'] = $WarnMethodConst
    $logFull = Resolve-Under $ArtifactsDir $BuildLogFile
    if (Test-Path -LiteralPath $logFull -PathType Leaf) {
        $codes = [ordered]@{}
        $uniq = @(Select-String -LiteralPath $logFull -Pattern $WarnRegex -CaseSensitive | ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique -CaseSensitive)
        foreach ($u in $uniq) {
            if ($u -cmatch 'warning ([A-Z]+[0-9]+)') { $k = $Matches[1]; $codes[$k] = $(if ($codes.Contains($k)) { $codes[$k] + 1 } else { 1 }) }
        }
        $w['codes'] = $codes
    }
    $m['warnings'] = $w

    $m['buildResult'] = [ordered]@{}
    if ($buildNotRun) { Set-Measured $m['buildResult'] 'exitCode' 'buildResult.exitCode' $null 'STEP_NOT_RUN' 'Build exit code not supplied; Build step not run' }
    else { Set-Measured $m['buildResult'] 'exitCode' 'buildResult.exitCode' $buildExit 'NOT_PROVIDED_BY_ENV' '-BuildExitCode not supplied' }
    $m['testResult'] = [ordered]@{}
    if ($testsNotRun) { Set-Measured $m['testResult'] 'exitCode' 'testResult.exitCode' $null 'STEP_NOT_RUN' $testNotRunDetail }
    else { Set-Measured $m['testResult'] 'exitCode' 'testResult.exitCode' $testExit 'NOT_PROVIDED_BY_ENV' '-TestExitCode not supplied' }

    # artifacts
    $art = [ordered]@{}
    if (Test-Path -LiteralPath $ArtifactsDir -PathType Container) {
        $ev = [System.Collections.Generic.List[object]]::new()
        foreach ($f in @(Get-ChildItem -LiteralPath $ArtifactsDir -Recurse -File | Sort-Object { Get-RelPath $ArtifactsDir $_.FullName } -CaseSensitive)) {
            if ([string]::Equals($f.FullName, $OutFull, [StringComparison]::OrdinalIgnoreCase)) { continue }
            $ev.Add([ordered]@{ path = (Get-RelPath $ArtifactsDir $f.FullName); bytes = [long]$f.Length; sha256 = (Get-Sha256 $f.FullName) })
        }
        $art['evidenceFiles'] = @($ev)
    } else { Set-Measured $art 'evidenceFiles' 'artifacts.evidenceFiles' $null 'SOURCE_FILE_MISSING' 'artifacts directory not found' }
    $tfm = $m['targetFramework']
    if ($tfm) {
        $srcDir = Join-Path $RepoRoot 'src'
        if (Test-Path -LiteralPath $srcDir -PathType Container) {
            $outs = @(Get-ChildItem -LiteralPath $srcDir -Directory | ForEach-Object {
                    $d = Join-Path $_.FullName "bin/x64/Release/$tfm"
                    if (Test-Path -LiteralPath $d -PathType Container) { Get-ChildItem -LiteralPath $d -File -Filter 'DesktopMediaPlayer.*.dll' }
                } | Sort-Object { Get-RelPath $RepoRoot $_.FullName } -CaseSensitive | ForEach-Object {
                    [ordered]@{ path = (Get-RelPath $RepoRoot $_.FullName); bytes = [long]$_.Length; sha256 = (Get-Sha256 $_.FullName) } })
            if ($outs.Count -gt 0) { $art['buildOutputs'] = $outs }
        }
    }
    if ($Kind -eq 'ci') { $art['packages'] = @() }
    elseif ($PackageFiles) {
        $art['packages'] = @($PackageFiles | ForEach-Object {
                $p = Resolve-Under $RepoRoot $_
                if (-not (Test-Path -LiteralPath $p -PathType Leaf)) { throw [GenerateFailure]::new("package file not found: $_") }
                [ordered]@{ name = [IO.Path]::GetFileName($p); bytes = [long](Get-Item -LiteralPath $p).Length; sha256 = (Get-Sha256 $p) } })
    }
    $m['artifacts'] = $art

    # native
    $nat = [ordered]@{}
    $nat['policy'] = $PolicyConst
    $lockFull = Join-Path $RepoRoot $LockRelPath
    $lock = $null
    if (-not (Test-Path -LiteralPath $lockFull -PathType Leaf)) {
        $nat['lockFile'] = $null; $nat['lockMissingReason'] = "$LockRelPath not found"
        Add-Missing 'native.lockFile' 'SOURCE_FILE_MISSING' "$LockRelPath not found"
    } else {
        try { $lock = Get-Content -LiteralPath $lockFull -Raw | ConvertFrom-Json -AsHashtable } catch { $lock = $null }
        if ($lock -is [System.Collections.IDictionary] -and $lock['bundleBuildId'] -is [string] -and $lock['bundleBuildId']) {
            $nat['lockFile'] = [ordered]@{ path = $LockRelPath; sha256 = (Get-Sha256 $lockFull); bundleBuildId = $lock['bundleBuildId'] }
        } else {
            $nat['lockFile'] = $null; $nat['lockMissingReason'] = "$LockRelPath unreadable or bundleBuildId missing"
            Add-Missing 'native.lockFile' 'SOURCE_PARSE_FAILED' "$LockRelPath unreadable or bundleBuildId missing"
        }
    }
    $ls = Invoke-Tool 'git' @('ls-files') $RepoRoot
    $tb = if ($ls.Found -and $ls.ExitCode -eq 0) { [long]@($ls.Lines | Where-Object { $_ -imatch '\.(dll|so|dylib|exe)$' }).Count } else { $null }
    Set-Measured $nat 'trackedBinaryCount' 'native.trackedBinaryCount' $tb $(if ($ls.Found) { 'SOURCE_PARSE_FAILED' } else { 'NOT_PROVIDED_BY_ENV' }) 'git ls-files failed'
    $natDir = Join-Path $RepoRoot 'native/win-x64'
    $natDirExists = Test-Path -LiteralPath $natDir -PathType Container
    $dlls = @()
    if ($natDirExists) { $dlls = @(Get-ChildItem -LiteralPath $natDir -File | Where-Object { $_.Extension -ieq '.dll' }) }
    Set-Measured $nat 'workspaceDllCount' 'native.workspaceDllCount' $(if ($natDirExists) { [long]$dlls.Count } else { $null }) 'SOURCE_FILE_MISSING' 'native/win-x64 directory not found'
    if ($Kind -eq 'ci') {
        $nat['verification'] = [ordered]@{ status = 'NOT_APPLICABLE'; reason = 'no native DLL in CI' }
    } elseif ($null -eq $nat['lockFile'] -or -not $natDirExists -or -not ($lock['files'] -is [System.Collections.IEnumerable])) {
        Set-Measured $nat 'verification' 'native.verification' $null 'SOURCE_FILE_MISSING' 'native-lock files[] or native/win-x64 not available'
    } else {
        $actual = @{}; foreach ($d in $dlls) { $actual[$d.Name] = Get-Sha256 $d.FullName }
        $mism = [System.Collections.Generic.List[object]]::new(); $matched = 0L; $expected = 0L
        foreach ($f in @($lock['files'])) {
            $expected++
            $a = $actual[[string]$f['name']]
            if ($a -and $a -ceq [string]$f['sha256']) { $matched++ } else { $mism.Add([ordered]@{ name = [string]$f['name']; expectedSha256 = [string]$f['sha256']; actualSha256 = $a }) }
            $actual.Remove([string]$f['name'])
        }
        foreach ($k in @($actual.Keys | Sort-Object)) { $mism.Add([ordered]@{ name = $k; expectedSha256 = $null; actualSha256 = $actual[$k] }) }
        $nat['verification'] = [ordered]@{ status = $(if ($mism.Count -eq 0) { 'MATCH' } else { 'MISMATCH' }); expected = $expected; matched = $matched; mismatches = @($mism) }
    }
    $m['native'] = $nat
    $m['generator'] = $generator
    $m['evidenceClass'] = $(if ($Kind -eq 'ci') { 'CI' } else { 'LOCAL' })
    $m['missingFields'] = @($missing)
} catch [GenerateFailure] {
    Write-Err "GENERATE FAIL: $($_.Exception.Message)"; exit 1
} catch {
    Write-Err "GENERATE FAIL: unexpected error: $($_.Exception.Message) at $($_.InvocationInfo.PositionMessage)"; exit 1
}

# ---------------- self-check before write (E3) ----------------
$errs = @(Test-ManifestSelfCheck $m)
if ($errs.Count -gt 0) { foreach ($e in $errs) { Write-Err "SELF-CHECK FAIL: $e" }; exit 2 }
$json = $m | ConvertTo-Json -Depth 32

if ($SchemaFile) {
    if ($PSVersionTable.PSVersion -lt [version]'7.4') { Write-Err "SCHEMA FAIL: BLOCKED: Test-Json (Draft 2020-12) needs pwsh >= 7.4, running $($PSVersionTable.PSVersion)"; exit 3 }
    $tjErr = $null
    $okSchema = Test-Json -Json $json -SchemaFile $SchemaFile -ErrorAction SilentlyContinue -ErrorVariable tjErr
    if (-not $okSchema) { foreach ($e in @($tjErr)) { Write-Err "SCHEMA FAIL: $e" }; if (-not $tjErr) { Write-Err 'SCHEMA FAIL' }; exit 3 }
}

$outDir = Split-Path -Parent $OutFull
if ($outDir -and -not (Test-Path -LiteralPath $outDir)) { New-Item -ItemType Directory -Force -Path $outDir | Out-Null }
[IO.File]::WriteAllText($OutFull, $json + "`n", [Text.UTF8Encoding]::new($false))
Write-Output "pwsh $($PSVersionTable.PSVersion)"
Write-Output "MANIFEST WRITTEN: $OutFull sha256=$(Get-Sha256 $OutFull) buildId=$($m['buildId'])"
foreach ($e in $m['missingFields']) { Write-Output "missingField: $($e['field']) $($e['reason'])" }
exit 0
