import os
import subprocess

def bytesToHex(bytes):
    return ' '.join(["%02X" % ord(x) for x in bytes])

def readFile(fileName):
    with open(fileName, "r") as f:
        return f.read()

def writeFile(fileName, contents):
    with open(fileName, "w") as f:
        f.write(contents)

os.makedirs('Scratch', exist_ok=True)
os.chdir('Scratch')

contents = '''    a
\tb
 \t   c
  d
\t  e
\t@""
    @"
\t1
    2"
f'''

writeFile('test_chisel.txt', contents)
print(bytesToHex(contents))

subprocess.call(("/usr/local/bin/python3", "../../chisel.py", "test_chisel.txt", "-m", "t", "-o", "test_chisel_result.txt"))

contents = readFile('test_chisel_result.txt')
print(bytesToHex(contents))
