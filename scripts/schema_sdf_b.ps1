# 傾印遷移批次 2(combo)五張表的欄位 schema(32 位元 PS)。
$ErrorActionPreference = 'Stop'
$root = 'C:\med\S_DRGService_3420'
Set-Location $root
[Environment]::CurrentDirectory = $root
Add-Type -Path (Join-Path $root 'System.Data.SqlServerCe.dll')

$tables = @('RDDT_MDC_DRG_XICD_V','RDDT_MDC_DRG_XICD_00_V','RDDT_MDC_DRG_XICD_NOTIN_V','RDDT_MDC_DRG_XICD_UN_V','RDDT_DRG_MDC02_V')
$cs = "Data Source=$root\icd10.sdf;Max Database Size=2048;"
$conn = New-Object System.Data.SqlServerCe.SqlCeConnection $cs
$conn.Open()
try {
    foreach ($t in $tables) {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = "SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '$t' ORDER BY ORDINAL_POSITION"
        $rd = $cmd.ExecuteReader()
        Write-Output ("=== " + $t + " ===")
        while ($rd.Read()) {
            $len = if ($rd.IsDBNull(2)) { '' } else { $rd.GetValue(2) }
            Write-Output ("{0}`t{1}`t{2}`t{3}" -f $rd.GetValue(0), $rd.GetValue(1), $len, $rd.GetValue(3))
        }
        $rd.Close()
    }
}
finally { $conn.Close() }
