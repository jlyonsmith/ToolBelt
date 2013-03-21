import os
import subprocess

def writeFile(fileName, contents):
    with open(fileName, "w") as f:
        f.write(contents)

os.makedirs('Scratch', exist_ok=True)
os.chdir('Scratch')

# Fake root
writeFile('test_chalk.sln', 'Nothing')

# Version file
writeFile('test_chalk.version', '''Major: 1
Minor: 0
Build: 0
Revision: 0
StartYear: 2012
Files:
- test_chalk.py
- test_chalk.cs
''')

# C# File
writeFile('test_chalk.cs', '''[assembly: AssemblyVersion("0.0.0.0")]
[assembly: AssemblyFileVersion("0.0.0.0")]
''')

# Python File
writeFile('test_chalk.py', '''__version__ = "1.0.0.0"''')

# First call should set build
subprocess.call(("/usr/local/bin/python3", "../../chalk.py"))

# Second call should increment revision
subprocess.call(("/usr/local/bin/python3", "../../chalk.py"))

os.chdir('..')
