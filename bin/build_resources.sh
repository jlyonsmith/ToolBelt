#!/bin/bash
function upfind() {
	pushd $PWD > /dev/null
	while [[ $PWD != / ]] ; do
    	R=$(find "$PWD" -maxdepth 1 -type d -name "$1")
    	if [[ ${#R} != 0 ]] ; then
    		echo $R
    		exit
    	fi
    	cd ..
	done
	popd > /dev/null
}

PACKAGES_DIR=$(upfind packages)

if [[ $PACKAGES_DIR == / ]] ; then
	exit 1
fi

CSHARPTOOLS_DIR=$(find $PACKAGES_DIR -name CSharpTools\* -maxdepth 1 -type d)/lib/net45

for F in $(find . -name \*.resx -or -name \*.strings) ; do
	mono $CSHARPTOOLS_DIR/Strapper.exe $F -i -n:ToolBelt -w:ToolBelt.Message
done
