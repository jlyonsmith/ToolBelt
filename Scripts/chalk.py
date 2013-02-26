#!python

__version__ = "Feb. 2013"

import fnmatch
import os
import datetime
import xml.etree.ElementTree as xmlDoc


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
            print "Chalk Version Number Maintainer. Version",__version__
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
        projectName = projectFileName[:projectFileName.index(".")]
        self.versionFile = projectName + ".version"

        print "Version file is", self.versionFile

        self.major = 1;
        self.minor = 0;
        self.build = 0;
        self.revision = 0;
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

        versionBuildAndRevision = "%d.%d" % (self.build, self.revision)
        versionMajorAndMinor = "%d.%d" % (self.major, self.minor)
        versionMajorMinorAndBuild = "%d.%d.%d" % (self.major, self.minor, self.build)
        versionFull = "%d.%d.%d.%d" % (self.major, self.minor, self.build, self.revision)
        versionFullCsv = versionFull.replace('.', ',')

        print "New version is", versionFull
        print "Updating version information in files:"

        for f in self.fileList:
            path = os.path.join(os.path.split(projectSln)[0],f)
            if (not (os.path.exists(path))):
                print "File '%s' does not exist" % path
            else:
                fn,ext = os.path.splitext(path)
                if (ext == ".cs"):
                    self.UpdateCSVersion(path, versionMajorAndMinor, versionFull)
                elif (ext == ".rc"):
                    self.UpdateRCVersion(path, versionFull, versionFullCsv)
                elif (ext == ".wxi"):
                    self.UpdateWxiVersion(path, versionMajorAndMinor, versionBuildAndRevision);
                elif (ext in [".wixproj",".proj"]):
                    self.UpdateProjVersion(path, versionFull, projectName);
                elif (ext == ".vsixmanifest"):
                    self.UpdateVsixManifestVersion(path, versionFull);
                elif (ext == ".config"):
                    self.UpdateConfigVersion(path, versionMajorAndMinor);
                elif (ext == ".svg"):
                    self.UpdateSvgContentVersion(path, versionMajorMinorAndBuild);
                elif (ext == ".xml"):
                    if (os.path.split(fn)[1] == "WMAppManifest"):
                        self.UpdateWMAppManifestContentVersion(path, versionMajorAndMinor)
                print path

        self.WriteVersionFile()

    def GetProjectSolution(self,dire=os.curdir):
        for filename in os.listdir(dire):
            d = os.path.join(dire,filename)
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

    def JDate(self,startYear):
        today = datetime.date.today()
        return (((today.year - startYear + 1) * 10000) + (today.month * 100) + today.day)


if __name__ == '__main__':
    t = ChalkTool()
    if (t.ProcessCommandLine()):
        t.Execute()
