#!/bin/sh -x
NUNIT_DIR=$(find packages -type d -name NUnit.Runners\* -maxdepth 1)/tools
mono --runtime=v4.0 $NUNIT_DIR/nunit-console.exe -noxml -nodots -labels -stoponerror $@
exit $?