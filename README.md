 [![Build (Linux)](https://github.com/SkySwimmer/Phoenix/actions/workflows/build-linux.yml/badge.svg)](https://github.com/SkySwimmer/Phoenix/actions/workflows/build-linux.yml) [![Build (Windows)](https://github.com/SkySwimmer/Phoenix/actions/workflows/build-windows.yml/badge.svg)](https://github.com/SkySwimmer/Phoenix/actions/workflows/build-windows.yml) [![Build (OSX)](https://github.com/SkySwimmer/Phoenix/actions/workflows/build-osx.yml/badge.svg)](https://github.com/SkySwimmer/Phoenix/actions/workflows/build-osx.yml) [![License: LGPL 3.0](https://img.shields.io/badge/License-LGPL%203.0-darkgreen.svg)](https://www.gnu.org/licenses/lgpl-3.0.html) [![Stage: pre-alpha](https://img.shields.io/badge/Stage-pre--alpha-red)](https://github.com/SkySwimmer/Phoenix) [![Phoenix Stable: unreleased](https://img.shields.io/badge/Phoenix%20Stable-unreleased-darkred)](https://github.com/SkySwimmer/Phoenix/tree/main) [![Phoenix Prerelease: unreleased](https://img.shields.io/badge/Phoenix%20Prerelease-unreleased-darkred)](https://github.com/SkySwimmer/Phoenix/tree/main) [![Phoenix Dev: unreleased](https://img.shields.io/badge/Phoenix%20Dev-unreleased-darkred)](https://github.com/SkySwimmer/Phoenix/tree/main) [![Documentation](https://img.shields.io/badge/Documentation-Latest-darkblue)](https://aerialworks.ddns.net/phoenix/docs)

<br/>

# Early Access Notice: Dev-only Alpha Testing
Phoenix is currently not fully released, we are still in pre-alpha and the project is subject to change.

Please note that we have not yet started closed early access testing, the project is still highly work-in-progress. We plan to perform open beta testing after two months of closed early access. Currently we have not yet decided on how to register game IDs at our service API.

However, the library side of Phoenix will be open source, you can already try the framework out but note that for quite some things you will need to have a developer token on the API server.

We hope to launch documentation for the API server soon so you can get an understanding of how it works. Feel free to write custom API servers for Phoenix when the docs launch, it might take some work but you can change the API endpoints in the framework without changing source code.
<br/>
<br/>
<br/>

# The Phoenix Framework
Welcome to Phoenix! Phoenix is a in-development game library designed to help aid in developing multiplayer games! We provide a game server runtime, authentication systems and various other utilities.


## What is Phoenix?
Phoenix is a work-in-progress collection of libraries designed to implement networking and player authentication as well as some complex systems such as scene replication.

## What engines does it work with?
Currently, client-side it only works with Unity. Phoenix is written in C#, Godot support should be theoretically possible but is currently not in development.

Server-side, we use our own runtime, specifically designed for Phoenix (written with .NET 7.0). It provides asset encryption and, if you want it, mod loading on the server. Client-side too has mod loading, its only enabled if you specify that.

<br/>

# Using Phoenix

## Building Phoenix
Phoenix is build with .NET 7.0, we have included some scripts to aid with building and project creation.

After cloning the project, in bash (git bash on windows), run the following commands to build the project:
```bash
cd "PX Framework"
chmod +x Scripts/buildframework-debug.sh
chmod +x Scripts/buildframework-release.sh
chmod +x Scripts/createproject.sh

# Debug build
./Scripts/buildframework-debug.sh

# Build in release mode
# Scripts/buildframework-release.sh
```
This should build the project and output in the Build directory. Note that on Windows this may take some time to complete due to it having to copy all libraries, on Windows, this is a lot slower.

## Creating a project
After building, you can create a project using the convenience scripts. This will set up a server and client environment. Note that at the time of writing, this only prepares the client-side Unity assets, it does not actually create the project itself. You might need to fiddle with unity for it to fully work.

Phoenix creates a shared (Common) project for both client and server, and also will create the server project files. It will also copy the basics needed for a unity client but currently it needs manual setup on the unity side.


Create a project by running the following in bash (or git bash on Windows):
```bash
# Mark the script as executable
chmod +x Scripts/createproject.sh

# Create a project
./Scripts/createproject.sh
```
Note: project setup might take quite some time to complete.

## Documentation
You can find the documentation at https://aerialworks.ddns.net/phoenix/docs

This project is still a heavy WIP, we hope to document things better in the future.
