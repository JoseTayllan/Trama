$ErrorActionPreference = 'Stop'
$taskRoot = 'C:\Users\joset\www\IDEA'
$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0
try {
    foreach ($stem in @('Trama_Documentacao_do_Produto_v1', 'Trama_Modelo_de_Documentacao')) {
        $source = Join-Path $taskRoot "docs\$stem.docx"
        $target = Join-Path $taskRoot "docs\qa\$stem.pdf"
        $document = $word.Documents.Open($source, $false, $true)
        try {
            $document.Repaginate()
            $document.ExportAsFixedFormat($target, 17)
            Write-Output "$stem pages=$($document.ComputeStatistics(2))"
        } finally {
            $document.Close(0)
            [void][Runtime.InteropServices.Marshal]::ReleaseComObject($document)
        }
    }
} finally {
    $word.Quit()
    [void][Runtime.InteropServices.Marshal]::ReleaseComObject($word)
}
