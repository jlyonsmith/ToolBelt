#!python

__version__ = "Feb. 2013"

import fnmatch
import os
import datetime
import xml.etree.ElementTree as xmlDoc
from xml.dom import minidom
import re


class ChalkTool:

    def ProcessCommandLine(self):
        import optparse
        parser = optparse.OptionParser()
        parser.set_description("Creates and increments version information in .version file in .sln directory.")
        parser.add_option("-q", dest="NoLogo", action="store_true",
                          help="Suppress logo.")
        parser.remove_option("-h")
        parser.add_option("-h", dest="help", action="store_true",
                          help="Show help.")

        parser.set_usage("%prog [options]")
        options, args = parser.parse_args()

        if (not options.NoLogo):
            print "Chalk Version Number Maintainer. Version", __version__
            print "Copyright (c) 2012, John Lyon-Smith."

        if (options.help):
            parser.print_help()
            return False

        else:
            return True

    def Execute(self):
        projectSln = self.GetProjectSolution()

        if (projectSln is None):
            print "Cannot find .sln file to determine project root."
            return

        print "Project root is", os.path.split(projectSln)[0]

        projectFileName = os.path.basename(projectSln)
        self.projectName = projectFileName[:projectFileName.index(".")]
        self.versionFile = self.projectName + ".version"

        print "Version file is", self.versionFile

        self.major = 1
        self.minor = 0
        self.build = 0
        self.revision = 0
        self.startYear = datetime.date.today().year
        self.fileList = []
        if (os.path.exists(self.versionFile)):
            self.ReadVersionFile()

        jBuild = self.JDate(self.startYear)
        if (self.build != jBuild):
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

        print "New version is", self.versionFull
        print "Updating version information in files:"

        for f in self.fileList:
            path = os.path.join(os.path.split(projectSln)[0], f)
            if (not (os.path.exists(path))):
                print "File '%s' does not exist" % path
            else:
                self.UpdateFileVersion(path)
                print path

        self.WriteVersionFile()

    def GetProjectSolution(self, dire=os.curdir):
        for filename in os.listdir(dire):
            d = os.path.join(dire, filename)
            if fnmatch.fnmatch(filename, '*.sln'):
                return d
            elif (os.path.isdir(filename)):
                sln = self.GetProjectSolution(dire=d)
                if (sln is not None):
                    return sln
        return None

    def ReadVersionFile(self):
        doc = xmlDoc.parse(self.versionFile)
        version = doc.getroot()
        self.major = int(version.find("Major").text)
        self.minor = int(version.find("Minor").text)
        self.build = int(version.find("Build").text)
        self.revision = int(version.find("Revision").text)
        self.startYear = int(version.find("StartYear").text)
        for elem in version.iter("File"):
            self.fileList.append(elem.text)

    def WriteVersionFile(self):
        root = xmlDoc.Element("Version")
        m = xmlDoc.SubElement(root, "Major")
        m.text = repr(self.major)
        m = xmlDoc.SubElement(root, "Minor")
        m.text = repr(self.minor)
        m = xmlDoc.SubElement(root, "Build")
        m.text = repr(self.build)
        m = xmlDoc.SubElement(root, "Revision")
        m.text = repr(self.revision)
        m = xmlDoc.SubElement(root, "StartYear")
        m.text = repr(self.startYear)
        fl = xmlDoc.SubElement(root, "Files")
        for fn in self.fileList:
            f = xmlDoc.SubElement(fl, "File")
            f.text = fn
        rough_string = xmlDoc.tostring(root, 'utf-8')
        reparsed = minidom.parseString(rough_string)
        xfile = open(self.versionFile, "w")
        xfile.write(reparsed.toprettyxml(indent="  ", encoding="utf-8"))
        xfile.close()

    def JDate(self, startYear):
        today = datetime.date.today()
        return (((today.year - startYear + 1) * 10000) + (today.month * 100) + today.day)

    def UpdateFileVersion(self, filename):
        f = open(filename)
        contents = f.read()
        f.close()

        fn, ext = os.path.splitext(filename)
        if (ext == ".cs"):
            contents = re.sub(
                "(AssemblyVersion\(\"[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)",
                'AssemblyVersion("' + self.versionMajorAndMinor + ".0.0", contents)
            contents = re.sub(
                "(AssemblyFileVersion\(\"[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)",
                'AssemblyFileVersion("' + self.versionFull, contents)

        elif (ext == ".rc"):
            contents = re.sub("(FILEVERSION [0-9]+,[0-9]+,[0-9]+,[0-9]+)",
                              "FILEVERSION " + self.versionFullCsv, contents)
            contents = re.sub("(PRODUCTVERSION [0-9]+,[0-9]+,[0-9]+,[0-9]+)",
                              "PRODUCTVERSION " + self.versionFullCsv, contents)
            contents = re.sub(
                "(FileVersion\",[ \t]*\"[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)",
                'FileVersion", ' + self.versionFull, contents)
            contents = re.sub(
                "(ProductVersion\",[ \t]*\"[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)",
                'ProductVersion", ' + self.versionFull, contents)

        elif (ext == ".wxi"):
            contents = re.sub("(ProductVersion = \"[0-9]+\.[0-9]+)",
                              'ProductVersion = "' + self.versionMajorMinor, contents)
            contents = re.sub("(ProductBuild = \"[0-9]+\.[0-9]|[1-9][0-9])",
                              'ProductBuild = "' + self.versionBuildAndRevision, contents)

        elif (ext in [".wixproj", ".proj"]):
            contents = re.sub(
                "(<OutputName>" + self.projectName +
                "_[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)",
                "<OutputName>" + self.projectName + "_" + self.version, contents)

        elif (ext == ".vsixmanifest"):
            contents = re.sub("(<Version>[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)",
                              "<Version>" + self.versionFull)

        elif (ext == ".config"):
            contents = re.sub("(, +Version=\d+\.\d+",
                              ", Version=" + self.versionMajorMinor, contents)

        elif (ext == ".svg"):
            contents = re.sub("(VERSION [0-9]+\.[0-9]+\.[0-9]+)",
                              "VERSION " + self.versionMajorMinorAndBuild, contents)

        elif (ext == ".xml"):
            if (os.path.split(fn)[1] == "WMAppManifest"):
                contents = re.sub("(Version=\[0-9]+\.[0-9]+)",
                                  "Version=" + self.versionMajorAndMinor, contents)

        f = open(filename + "test", "w")
        f.write(contents)
        f.close()


if __name__ == '__main__':
    t = ChalkTool()
    if (t.ProcessCommandLine()):
        t.Execute()
