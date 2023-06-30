#!/bin/sh

has_changes=$(git status --porcelain)
if [ "z$has_changes" != "z" ]; then
    echo "There are changes in the working directory. Please commit or stash them before running this script."
    exit 1
fi
commit=$(git rev-parse HEAD)
echo "Running with commit $commit"


rm -rf bin obj

dotnet publish -c Release -f net8.0
dotnet publish -c Release -f net7.0

cp static-files-page/index.html bin/toplevel-index.html

git checkout page

rm -rf net7
rm -rf net8

cp -R bin/Release/net7.0/browser-wasm/AppBundle net7
cp -R bin/Release/net8.0/browser-wasm/AppBundle net8
cp bin/toplevel-index.html index.html

git add net7 net8 index.html
git commit -m "Update page with commit $commit"
