#!/bin/bash
function upfind() {
    pushd $PWD > /dev/null
    while [[ $PWD != / ]] ; do
        find "$PWD" -maxdepth 1 -name "$1"
        cd ..
    done
    popd > /dev/null
}

function evil_git_dirty {
  [[ $(git diff --shortstat 2> /dev/null | tail -n1) != "" ]] && echo "*"
}

APPNAME=ToolBelt
SLNDIR=$(dirname $(upfind $APPNAME.sln))
pushd $SLNDIR
if [[ $(evil_git_dirty) == "*" ]]; then {
    echo "error: You have uncommitted changes! Please stash or commit them first."
    exit 1
}; fi
vamper -u
git add . 
mkdir scratch 2> /dev/null
git commit -F Scratch/$APPNAME.version.txt
popd