#! /bin/sh


if [ "z$1" == "z" ]; then
    framework="net7.0"
fi
framework=$1
echo "Running with framework $framework"
exec dotnet serve -h Cross-Origin-Opener-Policy:same-origin -h Cross-Origin-Embedder-Policy:require-corp --directory bin/Release/$framework/browser-wasm/AppBundle/
