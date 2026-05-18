# フォトスクリーンセーバー (MyPhotoScreensaver)

macOS のフォトスクリーンセーバー風の Windows スクリーンセーバーです。  
指定フォルダの画像をランダムに表示し、各種トランジションエフェクトをサポートします。

---

## ファイル構成

```
MyPhotoScreensaver/
├── MyPhotoScreensaver.csproj
├── App.xaml
├── App.xaml.cs
├── Models/
│   └── Settings.cs
├── Services/
│   ├── SettingsService.cs
│   └── ImageService.cs
├── Transitions/
│   └── TransitionEngine.cs
├── Windows/
│   ├── ScreensaverWindow.xaml
│   ├── ScreensaverWindow.xaml.cs
│   ├── ScreensaverManager.cs
│   ├── SettingsWindow.xaml
│   ├── SettingsWindow.xaml.cs
│   ├── PreviewWindow.xaml
│   └── PreviewWindow.xaml.cs
├── README.md
└── install.ps1
```

---

## ビルド手順

### 前提条件

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) がインストール済みであること
- Windows 10 / 11

### ビルド

```powershell
cd MyPhotoScreensaver
dotnet publish -c Release -r win-x64 --self-contained false -o ./publish
```

> **自己完結型にする場合**（.NET 未インストール環境向け）:
> ```powershell
> dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
> ```

ビルドが成功すると `./publish/MyPhotoScreensaver.exe` が生成されます。

---

## インストール手順

### 方法 1：PowerShell スクリプト（推奨）

**管理者権限の PowerShell** で以下を実行します：

```powershell
.\install.ps1
```

スクリプトが自動的に：
1. `MyPhotoScreensaver.exe` を `MyPhotoScreensaver.scr` にコピー
2. `C:\Windows\System32\` に配置
3. スクリーンセーバーとして登録

### 方法 2：手動インストール

1. ビルドで生成された `MyPhotoScreensaver.exe` を `MyPhotoScreensaver.scr` にコピー
2. `C:\Windows\System32\MyPhotoScreensaver.scr` にコピー（管理者権限が必要）
3. デスクトップを右クリック → 「個人用設定」 → 「ロック画面」 → 「スクリーンセーバー設定」
4. スクリーンセーバーの一覧から **MyPhotoScreensaver** を選択

---

## コマンドライン引数

| 引数 | 動作 |
|------|------|
| `/s` | スクリーンセーバーをフルスクリーンで起動 |
| `/c` | 設定ダイアログを開く |
| `/c:HWND` | 親ウィンドウ付きで設定ダイアログを開く |
| `/p HWND` | コントロールパネルのプレビュー用ミニウィンドウに描画 |
| なし | 設定ダイアログを開く |

---

## 設定ファイル

設定は以下のパスに JSON 形式で保存されます：

```
%APPDATA%\MyPhotoScreensaver\settings.json
```

### 設定項目

```json
{
  "ImageFolder": "C:\\Users\\YourName\\Pictures",
  "DisplaySeconds": 5,
  "Transition": "Fade",
  "TransitionSpeed": 0.8,
  "FitMode": "Letterbox",
  "Shuffle": true
}
```

| キー | 型 | 説明 |
|------|----|------|
| `ImageFolder` | string | 画像フォルダのパス |
| `DisplaySeconds` | int | 各画像の表示時間（秒、1〜60） |
| `Transition` | string | トランジションエフェクト |
| `TransitionSpeed` | float | トランジション時間（秒） |
| `FitMode` | string | `Letterbox` または `Crop` |
| `Shuffle` | bool | シャッフル再生 |

### Transition の値

| 値 | 説明 |
|----|------|
| `Fade` | フェードイン/アウト |
| `SlideLeft` | 左からスライドイン |
| `SlideRight` | 右からスライドイン |
| `SlideUp` | 上からスライドイン |
| `SlideDown` | 下からスライドイン |
| `ZoomIn` | ズームイン |
| `Dissolve` | ディゾルブ |
| `Random` | ランダムに選択 |

---

## アンインストール

```powershell
Remove-Item "C:\Windows\System32\MyPhotoScreensaver.scr" -Force
Remove-Item "$env:APPDATA\MyPhotoScreensaver" -Recurse -Force
```

---

## 対応画像形式

- JPEG (`.jpg`, `.jpeg`)
- PNG (`.png`)
- BMP (`.bmp`)
- GIF (`.gif`)

サブフォルダも再帰的に検索します。

---

## ライセンス

MIT License
