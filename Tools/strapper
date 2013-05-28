#!/bin/bash
pushd $(dirname $0) > /dev/null
SCRIPTDIR=$(pwd)
popd > /dev/null
SCRIPTNAME=$(basename $0)
APPNAME=$(tr '[:lower:]' '[:upper:]' <<< ${SCRIPTNAME:0:1})${SCRIPTNAME:1}
mono --gc=sgen $SCRIPTDIR/$APPNAME.app/$APPNAME.exe $*
