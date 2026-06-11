# Stage 1:由 icd10.sdf 匯出 Phase A 五張表為 TSV(NULL→\N、UTF-8 無 BOM)。32 位元 PS 執行。
$ErrorActionPreference = 'Stop'
$root = 'C:\med\S_DRGService_3420'
Set-Location $root
[Environment]::CurrentDirectory = $root
Add-Type -Path (Join-Path $root 'System.Data.SqlServerCe.dll')

$outDir = Join-Path $root 'migration'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$inv = [System.Globalization.CultureInfo]::InvariantCulture

# table -> 欄位清單(僅匯出引擎需要者)
$spec = [ordered]@{
    'RDDT_XICD_V'       = @('ICD_OP_TYPE','ICD_CODE','SEX_CHK','AGE_CHK','PRM_ICD_CHK','OR_NOR','SEX_NO')
    'RDDT_ECC_V'        = @('ICD_NO_1','TYPE','ICD_NO_GROUP')
    'RDDT_ECC_GROUP_V'  = @('ICD_NO_GROUP','ICD_NO')
    'RDDT_PDX_MDC_V'    = @('ICD_NO','MDC_CODE','CC','OP')
    'RDDT_MDC_DRGWGT_V' = @('TREE_MDC_NO','TREE_NO','TREE_DRG','TREE_WGT','DEP','AVG_EXP','COMBO_NO','CC_MARK')
}

$cs = "Data Source=$root\icd10.sdf;Max Database Size=2048;"
$conn = New-Object System.Data.SqlServerCe.SqlCeConnection $cs
$conn.Open()
try {
    foreach ($t in $spec.Keys) {
        $cols = $spec[$t]
        $sel = ($cols -join ', ')
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = "SELECT $sel FROM [$t]"
        $rd = $cmd.ExecuteReader()

        $path = Join-Path $outDir ($t + '.tsv')
        $sw = New-Object System.IO.StreamWriter($path, $false, (New-Object System.Text.UTF8Encoding($false)))
        $sw.WriteLine(($cols -join "`t"))   # header
        $n = 0
        $fc = $cols.Count
        while ($rd.Read()) {
            $vals = New-Object string[] $fc
            for ($i = 0; $i -lt $fc; $i++) {
                if ($rd.IsDBNull($i)) { $vals[$i] = '\N' }
                else { $vals[$i] = [Convert]::ToString($rd.GetValue($i), $inv) }
            }
            $sw.WriteLine(($vals -join "`t"))
            $n++
        }
        $rd.Close()
        $sw.Dispose()
        Write-Output ("{0}`t{1} rows" -f $t, $n)
    }
}
finally { $conn.Close() }
