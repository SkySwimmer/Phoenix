#!/bin/bash

if [ -f scriptdir ]; then
    cd ..
fi

echo Building project...
dotnet build || exit $?

cDir="$PWD"

rm -rf Build
mkdir Build
mkdir Build/Assemblies
mkdir Build/Documentation
mkdir Build/Projects

function copyBuild() {
    echo Copying outputs of "$1" libraries...
    cd "$1"
    if [ ! -d "$cDir/Build/Projects/$1" ]; then
        mkdir "$cDir/Build/Projects/$1"
    fi
    for project in */*.csproj ; do
        proj="$(dirname "$project")"
        if [ ! -d "$cDir/Build/Projects/$1/$proj" ]; then
            mkdir "$cDir/Build/Projects/$1/$proj"
        fi
        cd "$(dirname "$project")"
		if [ "$(find . -name '*.cs' -not -path './obj/*')" == "./Extensions.cs" ]; then cd .. ; continue; fi
        echo '  'Copying outputs of "$proj"...
        for framework in bin/Debug/*/ ; do
            frameworkName="$(basename "$framework")"
            if [ ! -d "$cDir/Build/Assemblies/$frameworkName" ]; then
                mkdir "$cDir/Build/Assemblies/$frameworkName"
            fi
            if [ ! -d "$cDir/Build/Projects/$1/$proj/$frameworkName" ]; then
                mkdir "$cDir/Build/Projects/$1/$proj/$frameworkName"
            fi
            cd "$framework"
            for file in *.* ; do
                if [ "$file" == "*.*" ]; then break ; fi
                ext="${file##*.}"
                msgpref=""
                for i in $(seq "${#file}" 60) ; do
                    msgpref="$msgpref "
                done
                if [ "$ext" == "dll" ]; then
                    if [ ! -f "$cDir/Build/Assemblies/$frameworkName/$file" ]; then
                        echo '    '"$file$msgpref"' ->   '"/Build/Assemblies/$frameworkName/$file"
                        cp "$file" "$cDir/Build/Assemblies/$frameworkName/$file"
                    fi
                fi
            done
            for file in *.* ; do
                if [ "$file" == "*.*" ]; then break ; fi
                ext="${file##*.}"
                msgpref=""
                for i in $(seq "${#file}" 60) ; do
                    msgpref="$msgpref "
                done
                if [ "$ext" != "pdb" ] && [ "$ext" != "xml" ]; then
                    if [ ! -f "$cDir/Build/$1/$proj/$frameworkName/$file" ]; then
                        echo '    '"$file$msgpref"' ->   '"/Build/Projects/$1/$proj/$frameworkName/$file"
                        cp "$file" "$cDir/Build/Projects/$1/$proj/$frameworkName/$file"
                    fi
                fi
            done
            for file in *.* ; do
                if [ "$file" == "*.*" ]; then break ; fi
                ext="${file##*.}"
                msgpref=""
                for i in $(seq "${#file}" 60) ; do
                    msgpref="$msgpref "
                done
                if [ "$ext" == "xml" ]; then
                    if [ ! -f "$cDir/Build/Documentation/$file" ]; then
                        echo '    '"$file$msgpref"' ->   '"/Build/Documentation/$file"
                        cp "$file" "$cDir/Build/Documentation/$file"
                    fi
					if [ ! -f "$cDir/Build/Assemblies/$frameworkName/$file" ]; then
                        echo '    '"$file$msgpref"' ->   '"/Build/Assemblies/$frameworkName/$file"
                        cp "$file" "$cDir/Build/Assemblies/$frameworkName/$file"
					fi
                fi
            done
            cd ../../..
        done
        cd ..
    done
    cd ..
}

copyBuild Common
copyBuild Server
copyBuild Client
copyBuild Debug

echo Copying utility projects...
cd Utils
copyBuild ChartZ
cd ..

echo Copying unity bindings...
mkdir Build/Unity
mkdir Build/Unity/Projects
mkdir Build/Unity/Complete
cp -rfv Unity/* Build/Unity/Projects
cp -rfv Unity/*/* Build/Unity/Complete
cp -rfv Build/Assemblies/net472/* Build/Unity/Complete/Assets/Libraries
rm -v Build/Unity/Complete/Assets/Libraries/Newtonsoft.Json.dll

echo Done.
