# 編譯並執行 legacy-oracle 語料產生器(必須以 .NET Framework csc + /platform:x86)。
# 產物 DRGOracle.exe / DRGOracle.exe.config 落在部署根(與 rddt_lib.dll 同目錄,native SQL CE 才解析得到),
# 兩者皆 gitignore;輸出語料寫入 tests/Drg.Parity.Tests/GoldenCorpus/legacy_oracle.json。
$ErrorActionPreference = 'Stop'
$root = 'C:\med\S_DRGService_3420'
$csc = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe'

& $csc /nologo /platform:x86 /target:exe /out:"$root\DRGOracle.exe" `
    /reference:"$root\rddt_lib.dll" /reference:"$root\System.Data.SqlServerCe.dll" `
    "$root\tools\LegacyOracle\Oracle.cs"
if ($LASTEXITCODE -ne 0) { throw "csc 失敗 ($LASTEXITCODE)" }

Copy-Item "$root\tools\LegacyOracle\DRGOracle.exe.config" "$root\DRGOracle.exe.config" -Force
& "$root\DRGOracle.exe"
