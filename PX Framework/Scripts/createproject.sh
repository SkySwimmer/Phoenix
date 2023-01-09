#!/bin/bash

if [ -f scriptdir ]; then
    cd ..
fi

# Read path
phoenixdir=$PWD
if [ ! -d "$phoenixdir/Build/Projects/Debug/Phoenix.Debug.DebugServerRunner" ]; then
    1>&2 echo Error: Phoenix has not been built yet
	1>&2 echo Please build the project in debug or release mode using the convenience scripts before making a project.
	exit 1
fi
path="$1"
if [ "$path" == "" ]; then
    read -rp "Project parent directory: " path
fi
if [ ! -d "$path" ]; then
	1>&2 echo Error: project parent directory does not exist!
	exit 1
fi

# Read name
cd "$path"
name="$2"
if [ "$name" == "" ]; then
	read -rp "Project name: " name
fi
if [ "$(basename "$name")" != "$name" ]; then
	1>&2 echo Error: invalid project name!
	exit 1
fi
if [ -d "$name" ] && [ "$3" != "ignore" ]; then
	1>&2 echo Warning!
	1>&2 echo A directory with the name "'$name'" already exists!
	1>&2 echo If you continue, a solution file will be made in the existing directory if it does not yet exist!
	1>&2 echo
	1>&2 echo -n "Do you wish to proceed? [Y/n] "
	read confirm
	if [ "${confirm,,}" != "y" ]; then
		exit 1
	fi
else
    if ! mkdir "$name"; then 
		1>&2 echo Error: failed to create project directory!
		exit 1
	fi
fi
cd "$name"
if [ -d "Server" ] && [ -d "Common" ] && [ -d "Client" ]; then
	1>&2 echo "Error: a Phoenix-like project already exists (found a matching directory layout)"
	exit 1
fi
if [ ! -f "${name}.sln" ]; then
    echo Creating solution...
	dotnet new sln || exit 1
fi

echo Creating directories...
mkdir -v Server
mkdir -v Common
mkdir -v Client

echo
echo Creating projects...
cp -rfv "$phoenixdir/Scripts/libs/project-template/Common/"* Common
dotnet sln add Common || exit 1
cp -rfv "$phoenixdir/Scripts/libs/project-template/Server/"* Server
cd Server
dotnet add reference ../Common || exit 1
cd ..
dotnet sln add Server || exit

echo
echo Copying required files to Unity project...
cp -rfv "$phoenixdir/Build/Unity/Complete/"* Client

echo
echo Cloning Phoenix...
git clone "$phoenixdir/.." phoenix-framework || exit 1

echo
echo Creating gitignore files...
echo Downloading unity gitignore for client...
curl https://raw.githubusercontent.com/github/gitignore/main/Unity.gitignore --output Client/.gitignore

echo
echo Generating server gitignore...
echo '
/bin/
/obj/
/.vs/
/run/
/Build/
' > Server/.gitignore

echo Generating common gitignore...
echo '
/phoenix-framework/
bin/
obj/
.vs/
' > .gitignore

echo
echo Generating configure script...
echo '#!/bin/bash
if [ ! -d phoenix-framework ]; then
	echo Downloading Phoenix...
	git clone https://github.com/SkySwimmer/Phoenix.git phoenix-framework || exit 1
else
	cd phoenix-framework
	echo Updating Phoenix...
	git pull || exit 1
fi
' > configure

echo
echo Adding Phoenix to the project...
for proj in phoenix-framework/PX\ Framework/*/ ; do
	if [ "$(basename "$proj")" == "Tests" ] || [ "$(basename "$proj")" == "Unity" ]; then
		continue
	fi
	if [ "$(basename "$proj")" == "Utils" ]; then
		for proj in "$proj"/*/ ; do
			echo "Adding $(basename "$proj") projects..."
			for projDir in "$proj"/*/ ; do
				if [ "$(basename "$projDir")" != "Phoenix.Debug.DebugServerRunner" ] && [ "$(basename "$projDir")" != "Phoenix.Server.Bootstrapper" ]; then
					echo "Adding $(basename "$projDir")..."
					dotnet sln add "$projDir"
				fi
			done
		done
		continue
	fi
	echo "Adding $(basename "$proj") projects..."
	for projDir in "$proj"/*/ ; do
		if [ "$(basename "$projDir")" != "Phoenix.Debug.DebugServerRunner" ] && [ "$(basename "$projDir")" != "Phoenix.Server.Bootstrapper" ]; then
			echo "Adding $(basename "$projDir")..."
			dotnet sln add "$projDir"
		fi
	done
done

echo
echo Copying server runtime...
mkdir server-runtime
cp -rfv "$phoenixdir/Debug/Phoenix.Debug.DebugServerRunner" server-runtime/debug || exit 1
cp -rfv "$phoenixdir/Server/Phoenix.Server.Bootstrapper" server-runtime/bootstrap || exit 1
rm -rfv server-runtime/debug/bin
rm -rfv server-runtime/debug/obj
rm -rfv server-runtime/bootstrap/bin
rm -rfv server-runtime/bootstrap/obj

echo
echo Modifying server runtime...
cp -rfv "$phoenixdir/Scripts/libs/project-template/server-runtime/bootstrap/"* server-runtime/bootstrap
cp -rfv "$phoenixdir/Scripts/libs/project-template/server-runtime/debug/"* server-runtime/debug

echo
echo Adding packages...
cd Common
dotnet add package Newtonsoft.Json || exit 1
dotnet add package BouncyCastle || exit 1
dotnet add package YamlDotNet || exit 1
cd ..
cd Server
dotnet add package Newtonsoft.Json || exit 1
dotnet add package BouncyCastle || exit 1
dotnet add package YamlDotNet || exit 1
cd ..
cd server-runtime/bootstrap
dotnet add package Newtonsoft.Json || exit 1
dotnet add package BouncyCastle || exit 1
dotnet add package YamlDotNet || exit 1
cd ../..
cd server-runtime/debug
dotnet add package Newtonsoft.Json || exit 1
dotnet add package BouncyCastle || exit 1
dotnet add package YamlDotNet || exit 1
cd ../..

echo
echo Adding server runtimes to project...
dotnet sln add server-runtime/debug || exit 1
dotnet sln add server-runtime/bootstrap || exit 1

echo
echo Copying vscode configurations...
mkdir .vscode
cp -rfv "$phoenixdir/Scripts/libs/project-template/.vscode"/* .vscode

echo
echo Generating project and debug settings...
echo '{
  "assetsFolder": "../Client/Assets/Resources/PhoenixAssets",
  "serverClass": "Server.DedicatedServer",
  "serverAssembly": "DedicatedServer.dll",
  "manifestFile": "game.json",
  "debugConfig": "debug.json",
  "assembliesDirectory": "bin/Release/net6.0"
}
' > Server/project.json
echo '{
  "title": "<game title>",
  "gameID": "<game id>",
  "version": "<game version>",
  "developmentStage": "<development stage>",
  "hasOfflineSupport":  true
}' > Server/game.json
echo '{
  "workingDirectory": "bin/Debug/net6.0",
  "arguments": [],
  "logLevel":  "trace"
}' > Server/debug.json

echo
echo Compiling server runtime...
cd server-runtime/debug
dotnet build || exit 1
cd ../..

echo
echo Moving assets...
mkdir -v Client/Assets/Resources
mv -v Server/Assets Client/Assets/Resources/PhoenixAssets

echo
echo Done.
echo Project has been created in "$(realpath "$PWD")".
