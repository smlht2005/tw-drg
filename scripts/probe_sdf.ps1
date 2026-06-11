# 以 32 位元 Windows PowerShell 執行(SQL CE 4.0 native 為 x86)。
$ErrorActionPreference = 'Stop'
$root = 'C:\med\S_DRGService_3420'
Set-Location $root
[Environment]::CurrentDirectory = $root
Add-Type -Path (Join-Path $root 'System.Data.SqlServerCe.dll')

$cs = "Data Source=$root\icd10.sdf;Max Database Size=2048;"
$conn = New-Object System.Data.SqlServerCe.SqlCeConnection $cs
$conn.Open()
try {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES ORDER BY TABLE_NAME"
    $rd = $cmd.ExecuteReader()
    $tables = @()
    while ($rd.Read()) { $tables += $rd.GetString(0) }
    $rd.Close()

    Write-Output ("TOTAL TABLES: " + $tables.Count)
    foreach ($t in $tables) {
        $c = $conn.CreateCommand()
        $c.CommandText = "SELECT COUNT(*) FROM [$t]"
        $n = $c.ExecuteScalar()
        Write-Output ("{0}`t{1}" -f $t, $n)
    }
}
finally { $conn.Close() }
