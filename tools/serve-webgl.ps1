# Servidor local para testar o build WebGL do Legion Rush 3D no navegador.
# Uso: powershell -ExecutionPolicy Bypass -File D:\legion-rush\tools\serve-webgl.ps1 [-Root <pasta>] [-Port 8080]
param(
    [string]$Root = (Join-Path (Split-Path -Parent $PSScriptRoot) "Builds\WebGL"),
    [int]$Port = 8080,
    [switch]$NoBrowser
)

$ErrorActionPreference = "Stop"
$Root = (Resolve-Path $Root).Path
if (-not (Test-Path (Join-Path $Root "index.html"))) {
    Write-Host "index.html nao encontrado em $Root. Gere o build WebGL primeiro." -ForegroundColor Red
    exit 1
}

$mime = @{
    ".html" = "text/html; charset=utf-8"; ".js" = "application/javascript"; ".wasm" = "application/wasm"
    ".data" = "application/octet-stream"; ".css" = "text/css"; ".png" = "image/png"; ".jpg" = "image/jpeg"
    ".ico" = "image/x-icon"; ".json" = "application/json"; ".txt" = "text/plain"; ".svg" = "image/svg+xml"
}
$gzipTypes = @{ ".gz" = "gzip"; ".br" = "br" }

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://localhost:$Port/")
$listener.Prefixes.Add("http://127.0.0.1:$Port/")
try {
    $listener.Start()
} catch {
    Write-Host "Nao foi possivel abrir a porta $Port ($($_.Exception.Message)). Tente: -Port 8090" -ForegroundColor Red
    exit 1
}

$url = "http://localhost:$Port/"
Write-Host "Legion Rush 3D rodando em $url" -ForegroundColor Green
Write-Host "Pasta: $Root"
Write-Host "Feche esta janela ou aperte Ctrl+C para parar."
if (-not $NoBrowser) { Start-Process $url }

try {
    while ($listener.IsListening) {
        $ctx = $listener.GetContext()
        $response = $ctx.Response
        $relative = [Uri]::UnescapeDataString($ctx.Request.Url.AbsolutePath.TrimStart('/'))
        if ([string]::IsNullOrWhiteSpace($relative)) { $relative = "index.html" }
        $status = 200
        try {
            $file = [System.IO.Path]::GetFullPath((Join-Path $Root $relative))
            if (-not $file.StartsWith($Root, [StringComparison]::OrdinalIgnoreCase) -or -not (Test-Path $file -PathType Leaf)) {
                $status = 404
                $response.StatusCode = 404
            } else {
                $ext = [System.IO.Path]::GetExtension($file).ToLowerInvariant()
                if ($gzipTypes.ContainsKey($ext)) {
                    $response.AddHeader("Content-Encoding", $gzipTypes[$ext])
                    $ext = [System.IO.Path]::GetExtension([System.IO.Path]::GetFileNameWithoutExtension($file)).ToLowerInvariant()
                }
                $response.ContentType = if ($mime.ContainsKey($ext)) { $mime[$ext] } else { "application/octet-stream" }
                $response.AddHeader("Cache-Control", "no-cache")
                $bytes = [System.IO.File]::ReadAllBytes($file)
                $response.ContentLength64 = $bytes.Length
                $response.OutputStream.Write($bytes, 0, $bytes.Length)
            }
        } catch {
            $status = 500
            Write-Host "Erro em /$relative : $($_.Exception.Message)" -ForegroundColor Yellow
            try { $response.StatusCode = 500 } catch { }
        } finally {
            try { $response.OutputStream.Close() } catch { }
        }
        $color = if ($status -eq 200) { "DarkGray" } else { "Yellow" }
        Write-Host ("{0} {1} /{2}" -f (Get-Date -Format "HH:mm:ss"), $status, $relative) -ForegroundColor $color
    }
} finally {
    $listener.Stop()
    $listener.Close()
}
