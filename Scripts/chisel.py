#!python
__version__ = "1.7.20401.3"

import os.path

class ChiselTool:
    def ProcessCommandLine(self):
        import argparse
        parser = argparse.ArgumentParser(description = "Convert tab/spaces at the start of lines.")
        parser.add_argument(
            "inputFileName", metavar = "INPUTFILENAME")
        parser.add_argument(
            "-o", dest="outputFileName",
            help="Specify different name for output file.")
        parser.add_argument(
            "-m", dest="convertMode", choices=['2s', '2t', None], default = None,
            help="The conversion mode if conversion is required")
        parser.add_argument(
            "-s", dest="tabSize", default = 4, type = int,
            help="The tab size (default is %default)")
        parser.add_argument(
            "-q", dest="noLogo", action="store_true",
            help="Suppress logo.")
        parser.parse_args(namespace = self)

        if not self.noLogo:
            print("Chisel tab/spaces converter. Version " + __version__)
            print("Copyright (c) 2013, John Lyon-Smith.")

        if self.outputFileName is not None and self.convertMode is None:
            print("Error: Output file specified with no conversion mode")
            return False

        if self.outputFileName is None:
            self.outputFileName = self.inputFileName

        if not os.path.exists(self.inputFileName):
            print("Error: File '%s' does not exist" % self.inputFileName)
            return False

        return True

    def Execute(self):
        with open(self.inputFileName) as file:
            fileLines = file.readlines()

        inStringConst = False

        totalTabs = 0
        totalSpaces = 0
        newTotalTabs = 0
        newTotalSpaces = 0

        try:
            if self.convertMode is not None:
                file = open(self.outputFileName, "w")
            else:
                file = None

            for line in fileLines:
                if not inStringConst:
                    # Count the number of spaces, converting tabs to spaces
                    n = 0
                    i = 0
                    while True:
                        c = line[i]
                        if c == ' ':
                            n += 1
                            totalSpaces += 1
                        elif c == '\t':
                            n += self.tabSize
                            totalTabs += 1
                        else:
                            break
                        i += 1

                    if self.convertMode == '2t':
                        m = (n // self.tabSize)
                        file.write(m * '\t')
                        newTotalTabs += m
                        m = (n % self.tabSize)
                        file.write(m * ' ')
                        newTotalSpaces += m
                    elif self.convertMode == '2s':
                        file.write(' ' * n)
                        newTotalSpaces += n

                    if (self.convertMode is not None):
                        file.write(line[i:])

                    if line.find('@"') >= 0:
                        if line.replace('@"', '').replace('""', '').find('"') < 0:
                            inStringConst = True
                else:
                    if file is not None:
                        file.write(line)

                    if (line.replace('""', '').find('"')) >= 0:
                        inStringConst = False
        finally:
            if file is not None:
                file.close()

        print("tabs = %d, spaces = %d" % (totalTabs, totalSpaces), end="")

        if self.convertMode is not None:
            print(" -> tabs = %d, spaces = %d" % (newTotalTabs, newTotalSpaces))
        else:
            print()

if __name__ == '__main__':
    t = ChiselTool()
    if t.ProcessCommandLine():
        t.Execute()
