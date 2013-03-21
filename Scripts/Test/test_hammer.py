import os
import subprocess

def writeFile(fileName, contents):
    with open(fileName, "w") as f:
        f.write(contents)

os.makedirs('Scratch', exist_ok=True)
os.chdir('Scratch')

nameAndContents = [
    ("cr.txt", "\r"),
    ("lf.txt", "\n"),
    ("crlf.txt", "\r\n"),
    ("mixed1.txt", "\n\r\n\r"),
    ("mixed2.txt", "\n\n\r\n\r"),
    ("mixed3.txt", "\n\r\n\r\r"),
    ("mixed4.txt", "\n\r\n\r\r\n")
]

for nameAndContent in nameAndContents:
    writeFile(*nameAndContent)

argLists = [
    ("-h",),
    ("cr.txt",),
    ("-q", "lf.txt",),
    ("-q", "crlf.txt",),
    ("-q", "mixed1.txt",),
    ("-q", "mixed2.txt",),
    ("-q", "mixed3.txt",),
    ("-q", "mixed4.txt",),
    ("-q", "-f", "lf", "-o", "cr2lf.txt", "cr.txt"),
    ("-q", "-f", "cr", "-o", "lf2cr.txt", "lf.txt"),
    ("-q", "-f", "lf", "-o", "crlf2lf.txt", "crlf.txt"),
    ("-q", "-f", "cr", "-o", "crlf2cr.txt", "crlf.txt"),
	("-q", "-f", "auto", "mixed1.txt"),
	("-q", "-f", "auto", "mixed2.txt"),
	("-q", "-f", "auto", "mixed3.txt"),
	("-q", "-f", "auto", "mixed4.txt")
]

for argList in argLists:
    subprocess.call(("/usr/local/bin/python3", "../../hammer.py") + argList)

os.chdir('..')
#os.rmdir('Scratch')