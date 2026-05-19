; MyPhotoScreensaver - Inno Setup インストーラースクリプト
; ビルド方法: Inno Setup 6 で本ファイルを開いてコンパイル、または
;             iscc installer.iss を実行
; 事前に以下のコマンドでシングルファイルパブリッシュを行うこと:
;   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish-installer

#define AppName    "MyPhotoScreensaver"
#define AppVersion "1.0"
#define AppPublisher "matsumoto"
#define ScrFile    "MyPhotoScreensaver.scr"
#define SrcExe     "publish-installer\MyPhotoScreensaver.exe"

[Setup]
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppId={{E7A3F1B2-4C8D-4E9A-B3F2-1A2B3C4D5E6F}
VersionInfoVersion={#AppVersion}

; 管理者権限必須（System32 への書き込みに必要）
PrivilegesRequired=admin

; インストール先はユーザーに選ばせない（System32 固定）
DefaultDirName={sys}
CreateAppDir=no
DirExistsWarning=no

; アンインストーラーを %ProgramData% に配置
UninstallFilesDir={commonappdata}\{#AppName}

; 出力設定
OutputDir=Output
OutputBaseFilename=MyPhotoScreensaverSetup
SetupIconFile=

; 圧縮設定
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes

; ウィンドウ設定
WizardStyle=modern
WizardSmallImageFile=

DisableWelcomePage=no
; スタートメニューフォルダー選択ページを非表示
DisableProgramGroupPage=yes

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Messages]
; ウィザードの文言をカスタマイズ
WelcomeLabel1=MyPhotoScreensaver セットアップへようこそ
WelcomeLabel2=このウィザードは MyPhotoScreensaver をインストールします。%n%n続行する前に、他のアプリケーションをすべて終了することをお勧めします。
FinishedLabel=MyPhotoScreensaver のインストールが完了しました。%n%nスクリーンセーバーの設定は、デスクトップを右クリック → 「個人用設定」 → 「ロック画面」 → 「スクリーンセーバーの設定」 から行えます。

[Files]
; スクリーンセーバー本体（.exe → .scr にリネームして System32 へ）
Source: "{#SrcExe}"; DestDir: "{sys}"; DestName: "{#ScrFile}"; Flags: ignoreversion
; WPF ネイティブ DLL（PublishSingleFile でもバンドル不可のため個別配置）
Source: "publish-installer\*.dll"; DestDir: "{sys}"; Flags: ignoreversion

[UninstallDelete]
; アンインストール時に .scr と WPF ネイティブ DLL を削除
Type: files; Name: "{sys}\{#ScrFile}"
Type: files; Name: "{sys}\D3DCompiler_47_cor3.dll"
Type: files; Name: "{sys}\PenImc_cor3.dll"
Type: files; Name: "{sys}\PresentationNative_cor3.dll"
Type: files; Name: "{sys}\vcruntime140_cor3.dll"
Type: files; Name: "{sys}\wpfgfx_cor3.dll"
; 設定ファイルも削除（任意）
Type: filesandordirs; Name: "{userappdata}\{#AppName}"


[Run]
; インストール完了後にスクリーンセーバー設定を開くオプション
Filename: "rundll32.exe"; \
  Parameters: "shell32.dll,Control_RunDLL desk.cpl,,1"; \
  Description: "スクリーンセーバーの設定を開く"; \
  Flags: postinstall nowait skipifsilent unchecked

