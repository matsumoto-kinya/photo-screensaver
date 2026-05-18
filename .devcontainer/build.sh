#!/bin/bash
# コンテナ内でWindowsターゲット向けにビルド
dotnet publish -c Release -r win-x64 --self-contained false -o ./publish
echo "ビルド完了: ./publish/ を Windows 上で実行してください"
