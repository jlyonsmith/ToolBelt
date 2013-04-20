#!python
__version__ = "1.7.20401.3"

import fnmatch
import os
import datetime
import yaml
import re

class ChalkTool:
    def ProcessCommandLine(self):
        import argparse
        parser = argparse.ArgumentParser(
            "Creates and increments version information in .version file in .sln directory.")
        parser.add_argument(
            "-q", dest="doLogo", action="store_false", help="Suppress logo.")
        parser.add_argument(
            "-i", dest="doIncr", action="store_true", help="Increment build and/or revision number")
        parser.parse_args(namespace = self)

        if self.doLogo:
            print("Chalk Version Number Maintainer. Version " + __version__)
            print("Copyright (c) 2012, John Lyon-Smith.")

        return True

    def Execute(self):
        projectSln = self.GetProjectSolution()

        if projectSln is None:
            print("error: Cannot find .sln file to determine project root.")
            return

        self.rootDir = os.path.abspath(os.path.split(projectSln)[0])
        projectFileName = os.path.basename(projectSln)
        # Do string index instead of splitext because we may have multiple .'s in filename
        temp = os.path.split(projectFileName)[1]
        self.projectName = temp[:temp.index(".")]
        self.versionFile = os.path.join(self.rootDir, self.projectName + ".version")

        if not os.path.exists(self.versionFile):
            print("Error: Version file '%s' does not exist" % self.versionFile)
            return
        else:
            print("Version file is '%s'" % self.versionFile)

        # Set defaults
        self.major = 1
        self.minor = 0
        self.build = 0
        self.revision = 0
        self.startYear = datetime.date.today().year
        self.fileList = []

        self.ReadVersionFile()

        jBuild = self.JDate(self.startYear)

        if self.doIncr:
            if self.build != jBuild:
                self.revision = 0
                self.build = jBuild
            else:
                self.revision += 1

        self.versionBuildAndRevision = "%d.%d" % (self.build, self.revision)
        self.versionMajorAndMinor = "%d.%d" % (self.major, self.minor)
        self.versionMajorMinorAndBuild = "%d.%d.%d" % (
            self.major, self.minor, self.build)
        self.versionFull = "%d.%d.%d.%d" % (
            self.major, self.minor, self.build, self.revision)
        self.versionFullCsv = self.versionFull.replace('.', ',')

        if not self.doIncr:
            print("Version is %s" % (self.versionFull))
            return

        print("New version is %s" % (self.versionFull))
        print("Updating version information in files:")

        for fileName in self.fileList:
            path = os.path.join(self.rootDir, fileName)
            if not os.path.exists(path):
                print("Error: File '%s' does not exist" % path)
            else:
                self.UpdateFileVersion(path)
                print(path)

        self.WriteVersionFile()

    def GetProjectSolution(self, dir=os.curdir):
        for filename in os.listdir(dir):
            d = os.path.join(dir, filename)
            if fnmatch.fnmatch(filename, '*.sln'):
                return d

        if dir != os.path.sep:
            dir = os.path.abspath(os.path.join(dir, os.pardir))
            return self.GetProjectSolution(dir)
        else:
            return None

    def ReadVersionFile(self):
        with open(self.versionFile, 'r') as file:
            document = yaml.load(file.read())
        self.major = int(document["Major"])
        self.minor = int(document["Minor"])
        self.build = int(document["Build"])
        self.revision = int(document["Revision"])
        self.startYear = int(document["StartYear"])
        self.fileList = document["Files"]

    def WriteVersionFile(self):
        document = {}
        document["Major"] = self.major
        document["Minor"] = self.minor
        document["Build"] = self.build
        document["Revision"] = self.revision
        document["StartYear"] = self.startYear
        document["Files"] = self.fileList
        with open(self.versionFile, 'w') as file:
            file.write(yaml.dump(document, default_flow_style=False))

    def JDate(self, startYear):
        today = datetime.date.today()
        return (((today.year - startYear + 1) * 10000) + (today.month * 100) + today.day)

    def UpdateFileVersion(self, fileName):
        with open(fileName, "r") as file:
            contents = file.read()

        fn, ext = os.path.splitext(fileName)

        if ext == ".cs":
            contents = re.sub(
                "(?P<pre>AssemblyVersion\(\")[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+",
                '\g<pre>' + self.versionMajorAndMinor + ".0.0", contents)
            contents = re.sub(
                "(?P<pre>AssemblyFileVersion\(\")[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+",
                '\g<pre>' + self.versionFull, contents)

        elif ext == ".rc":
            contents = re.sub("(?P<pre>FILEVERSION\s+)[0-9]+,[0-9]+,[0-9]+,[0-9]+",
                              '\g<pre>' + self.versionFullCsv, contents)
            contents = re.sub("(?P<pre>PRODUCTVERSION\s+)[0-9]+,[0-9]+,[0-9]+,[0-9]+",
                              '\g<pre>' + self.versionFullCsv, contents)
            contents = re.sub(
                "(?P<pre>FileVersion\",\s*\")[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+",
                '\g<pre>' + self.versionFull, contents)
            contents = re.sub(
                "(?P<pre>ProductVersion\",\s*\")[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+",
                '\g<pre>' + self.versionFull, contents)

        elif ext == ".wxi":
            contents = re.sub("(?P<pre>ProductVersion\s*=\s*\")[0-9]+\.[0-9]+",
                              '\g<pre>' + self.versionMajorMinor, contents)
            contents = re.sub("(?P<pre>ProductBuild\s*=\s*\")[0-9]+\.[0-9]|[1-9][0-9]",
                              '\g<pre>' + self.versionBuildAndRevision, contents)

        elif ext in [".wixproj", ".proj"]:
            contents = re.sub(
                "(?P<pre>\<OutputName\>" + self.projectName + "_)[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+",
                '\g<pre>' + self.version, contents)

        elif ext == ".vsixmanifest":
            contents = re.sub("(?P<pre>\<Version\>)[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+]",
                              '\g<pre>' + self.versionFull)

        elif ext == ".svg":
            contents = re.sub("(?P<pre>VERSION\s+)[0-9]+\.[0-9]+\.[0-9]+]",
                              '\g<pre>' + self.versionMajorMinorAndBuild, contents)

        elif ext == ".xml":
            if os.path.split(fn)[1] == "WMAppManifest":
                contents = re.sub("(?P<pre>Version\s*=\s*)[0-9]+\.[0-9]+",
                                  '\g<pre>' + self.versionMajorAndMinor, contents)

        elif ext == ".py":
            contents = re.sub(
                '(?P<pre>__version__\s*=\s*("|\'))[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+',
                '\g<pre>' + self.versionFull, contents)

        else:
            return

        with open(fileName, "w") as file:
            file.write(contents)

if __name__ == '__main__':
    t = ChalkTool()
    if t.ProcessCommandLine():
        t.Execute()
