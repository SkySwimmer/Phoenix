#!/bin/bash

echo Building project...
eval dotnet build -c Release $@ || exit 1

echo Running server build...
../server-runtime/debug/bin/Release/net6.0/Phoenix.Debug.DebugServerRunner project.json build || exit 1
