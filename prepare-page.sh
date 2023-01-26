#!/bin/sh

rm -rf bin obj

dotnet publish -p:WasmEnableThreads=true -c Release -f net8.0
dotnet publish -p:WasmEnableThreads=true -c Release -f net7.0

cp static-files-page/index.html bin/toplevel-index.html

git checkout page

rm -rf net7
rm -rf net8

cp -R bin/Release/net7.0/browser-wasm/AppBundle net7
cp -R bin/Release/net8.0/browser-wasm/AppBundle net8
cp bin/toplevel-index.html index.html
