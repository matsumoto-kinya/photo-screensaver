#Requires -RunAsAdministrator
<#
.SYNOPSIS
    MyPhotoScreensaver インストールスクリプト
.DESCRIPTION
    ビルド済みの exe を .scr にコピーして System32 に配置します。
    管理者権限で実行してください。
.EXAMPLE
    .\install.ps1
.EXAMPLE
    .\install.ps1 -PublishDir ".\publish" -Uninstall
#>

param(
    [string]$PublishDir = ".\publish",
    [switch]$Uninstall
)

$ErrorActionPreference = "Stop"

$scrName   = "MyPhotoScreensaver.scr"
$exeName   = "MyPhotoScreensaver.exe"
$system32  = "$env:SystemRoot\System32"
$destPath  = Join-Path $system32 $scrName
$appData   = Join-Path $env:APPDATA "MyPhotoScreensaver"

function Write-Step($msg) {
    Write-Host "[*] $msg" -ForegroundColor Cyan
}
function Write-OK($msg) {
    Write-Host "[OK] $msg" -ForegroundColor Green
}
function Write-Err($msg) {
    Write-Host "[ERR] $msg" -ForegroundColor Red
}

# ---- Uninstall ----
if ($Uninstall) {
    Write-Step "アンインストール中..."

    if (Test-Path $destPath) {
        Remove-Item $destPath -Force
        Write-OK "$destPath を削除しました"
    } else {
        Write-Host "  (インストール済みファイルが見つかりません: $destPath)"
    }

    # インストール時にコピーした依存ファイルを削除
    # (System32 本来のファイルを誤って消さないよう install.ps1 が把握しているファイルのみ)
    $srcPath = $null
    foreach ($c in @(
        (Join-Path $PublishDir $exeName),
        (Join-Path "." $exeName),
        (Join-Path ".\bin\Release\net8.0-windows\win-x64\publish" $exeName),
        (Join-Path ".\bin\Release\net8.0-windows" $exeName)
    )) {
        if (Test-Path $c) { $srcPath = (Resolve-Path $c).Path; break }
    }
    if ($srcPath) {
        $srcDir = Split-Path $srcPath
        $depFiles = Get-ChildItem "$srcDir\*" -Include "*.dll","*.json" -ErrorAction SilentlyContinue
        foreach ($f in $depFiles) {
            $target = Join-Path $system32 $f.Name
            if (Test-Path $target) {
                Remove-Item $target -Force
                Write-OK "削除: $target"
            }
        }
    }

    if (Test-Path $appData) {
        Remove-Item $appData -Recurse -Force
        Write-OK "$appData を削除しました"
    }

    Write-OK "アンインストール完了"
    exit 0
}

# ---- Install ----

# 1. ソースを探す
$srcPath = $null
$candidates = @(
    (Join-Path $PublishDir $exeName),
    (Join-Path "." $exeName),
    (Join-Path ".\bin\Release\net8.0-windows\win-x64\publish" $exeName),
    (Join-Path ".\bin\Release\net8.0-windows" $exeName)
)

foreach ($c in $candidates) {
    if (Test-Path $c) {
        $srcPath = (Resolve-Path $c).Path
        break
    }
}

if (-not $srcPath) {
    Write-Err "ビルド済みの $exeName が見つかりません。先にビルドしてください:"
    Write-Host "  dotnet publish -c Release -r win-x64 --self-contained false -o .\publish"
    exit 1
}

Write-Step "ソースファイル: $srcPath"

# 2. .scr としてコピー
Write-Step "$scrName を $system32 にコピー中..."
Copy-Item $srcPath $destPath -Force
Write-OK "コピー完了: $destPath"

# 3. 同じディレクトリに依存ファイル (.dll / .json) があればコピー
$srcDir = Split-Path $srcPath
$depFiles = Get-ChildItem "$srcDir\*" -Include "*.dll","*.json" -ErrorAction SilentlyContinue
$depCount = if ($depFiles) { $depFiles.Count } else { 0 }
Write-Step "依存ファイルを検索しました: $depCount 件"
if ($depCount -gt 0) {
    Write-Step "依存ファイルをコピー中 ($depCount ファイル)..."
    foreach ($f in $depFiles) {
        Write-Host "  - $($f.Name)"
        $dest = Join-Path $system32 $f.Name
        Copy-Item $f.FullName $dest -Force
    }
    Write-OK "依存ファイルのコピー完了"
}

# 4. 完了メッセージ
Write-Host ""
Write-OK "インストール完了！"
Write-Host ""
Write-Host "次の手順でスクリーンセーバーを設定してください:" -ForegroundColor Yellow
Write-Host "  1. デスクトップを右クリック → 「個人用設定」"
Write-Host "  2. 「ロック画面」→「スクリーンセーバー設定」"
Write-Host "  3. 一覧から「MyPhotoScreensaver」を選択"
Write-Host "  4. 「設定」ボタンで画像フォルダなどを設定"
Write-Host ""
