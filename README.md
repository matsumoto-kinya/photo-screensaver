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

### 方法 1：インストーラー（推奨）

#### インストーラーのビルド（Windows 上で実施）

1. [Inno Setup 6](https://jrsoftware.org/isinfo.php) をインストール
2. シングルファイルパブリッシュを実行：
   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained true `
     -p:PublishSingleFile=true -o .\publish-installer
   ```
3. `installer.iss` を Inno Setup Compiler で開いてビルド、またはコマンドラインで実行：
   ```powershell
   iscc installer.iss
   ```
4. `Output\MyPhotoScreensaverSetup.exe` が生成される

#### インストーラーの実行

`MyPhotoScreensaverSetup.exe` をダブルクリックし、ウィザードに従って進めるだけです。  
UAC プロンプトが表示されたら「はい」を選択してください。

- **配布先に .NET 8 は不要**（ランタイムを同梱済み）
- インストール完了後、"アプリと機能" からアンインストール可能

---

### 方法 2：PowerShell スクリプト

**管理者権限の PowerShell** で以下を実行します：

```powershell
.\install.ps1
```

スクリプトが自動的に：
1. `MyPhotoScreensaver.exe` を `MyPhotoScreensaver.scr` にコピー
2. `C:\Windows\System32\` に配置
3. 依存 DLL / JSON ファイルを System32 にコピー

アンインストールする場合：
```powershell
.\install.ps1 -Uninstall
```

### 方法 3：手動インストール

1. ビルドで生成された `MyPhotoScreensaver.exe` を `MyPhotoScreensaver.scr` にコピー
2. `C:\Windows\System32\MyPhotoScreensaver.scr` にコピー（管理者権限が必要）
3. 同ディレクトリの `.dll` / `.json` ファイルも System32 にコピー
4. デスクトップを右クリック → 「個人用設定」 → 「ロック画面」 → 「スクリーンセーバー設定」
5. スクリーンセーバーの一覧から **MyPhotoScreensaver** を選択

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
  "Shuffle": true,
  "PanelMode": "Conveyor"
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
| `PanelMode` | string | パネルの入れ替え方（`Transition` が `Panel` のときのみ有効） |

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
| `Panel` | 複数画像をグリッド表示して入れ替え（入れ替え方は `PanelMode` で選択） |
| `Random` | ランダムに選択（`Panel` は含まれません） |

### PanelMode の値

`Transition` が `Panel` のときだけ使われます。4行×可変列のグリッドで、どう画像を入れ替えるかを決めます。

| 値 | 説明 | 1回あたり |
|----|------|-----------|
| `Conveyor` | 行の端から流し込み、他のセルを押しやる（従来の動作） | 1枚 |
| `SingleCell` | ランダムな1セルをその場でクロスフェード | 1枚 |
| `CellZoom` | 縮小→等倍のズームで差し替え | 3枚 |
| `CellFlip` | 横に潰して開くフリップで差し替え | 2枚 |
| `DiagonalWave` | 対角線上のセルを順にクロスフェード | 行数ぶん |
| `ColumnWave` | 同じ列のセルを上から順にクロスフェード | 行数ぶん |

`Conveyor` はセルの移動とリサイズをアニメーションするため毎フレーム レイアウトパスが走ります。
それ以外はセルを動かさないぶん軽く、また1回に複数枚が入れ替わるため、
グリッド全体が一巡するまでの時間も短くなります。

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
