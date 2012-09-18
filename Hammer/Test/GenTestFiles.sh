mcs GenTestFiles.cs
mono GenTestFiles.exe
HAMMER="../bin/Debug/Hammer.exe -q"
mono $HAMMER cr.txt
mono $HAMMER lf.txt
mono $HAMMER crlf.txt
mono $HAMMER mixed1.txt
mono $HAMMER mixed2.txt
mono $HAMMER mixed3.txt
mono $HAMMER mixed4.txt
mono $HAMMER -f:lf -o:cr.lf.txt cr.txt
mono $HAMMER -f:cr -o:lf.cr.txt lf.txt
mono $HAMMER -f:lf -o:crlf.lf.txt crlf.txt
mono $HAMMER -f:auto mixed1.txt
mono $HAMMER -f:auto mixed2.txt
mono $HAMMER -f:auto mixed3.txt
mono $HAMMER -f:auto mixed4.txt
