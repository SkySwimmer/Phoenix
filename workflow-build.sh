#!/bin/bash

cd PX\ Framework
echo Building framework...
bash Scripts/buildframework-$1.sh || exit 1

echo Creating templates...
mkdir Build/Project\ Template
bash Scripts/createproject.sh Build/Project\ Template PhoenixExample ignore || exit 1
