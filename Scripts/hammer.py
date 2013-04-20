#!python
__version__ = "1.7.20401.3"

class HammerTool:

    def ProcessCommandLine(self):
        import argparse
        parser = argparse.ArgumentParser(
            description = "Reports on and fixes line endings for text files.")
        parser.add_argument(
            "inputFileName", metavar = "INPUTFILE")
        parser.add_argument(
            "-o", metavar="OUTPUTFILENAME", dest = "outputFileName",
            help = "Specify different name for output file.")
        parser.add_argument(
            "-f", dest = "fixedLineEndings",
            help = "Fix line endings to be cr, lf, crlf or auto.")
        parser.add_argument(
            "-q", dest = "noLogo", action = "store_true",
            help = "Suppress logo.")
        parser.parse_args(namespace = self)

        if not self.noLogo:
            print("Hammer text line ending fixer. Version" + __version__)
            print("Copyright (c) 2012, John Lyon-Smith.")

        if len(self.inputFileName) == 0:
            print("error: A text file must be specified")
            return False

        if self.outputFileName is None:
            self.outputFileName = self.inputFileName

        return True

    def Execute(self):
        inputFile = open(self.inputFileName)
        fileContents = inputFile.read()
        inputFile.close()

        numCr = 0
        numLf = 0
        numCrLf = 0
        numLines = 1
        i = 0

        # Count lf, cr, crlf
        while i < len(fileContents):
            c = fileContents[i]
            if c == '\r':
                if i < len(fileContents) - 1:
                    c1 = fileContents[i + 1]
                else:
                    c1 = '\0'
                if next == '\n':
                    numCrLf += 1
                    i += 1
                else:
                    numCr += 1
                numLines += 1
            elif c == '\n':
                numLf += 1
                numLines += 1
            i += 1

        sb = self.inputFileName
        sb += " lines = %d, cr = %d, lf = %d, crlf = %d" % (
            numLines, numCr, numLf, numCrLf)

        if self.fixedLineEndings == None:
            print(sb)
            return

        autoLineEnding = "lf"
        n = numLf

        if numCrLf > n:
            autoLineEnding = "crlf"
            n = numCrLf

        if numCr > n:
            autoLineEnding = "cr"

        if self.fixedLineEndings.lower() == 'auto':
            self.fixedLineEndings = autoLineEnding

        if self.fixedLineEndings.lower() == 'cr':
            newLineChars = "\r"
        elif self.fixedLineEndings.lower() == 'lf':
            newLineChars = "\n"
        else:
            newLineChars = "\r\n"

        outputFile = open(self.outputFileName, "w")
        n = 0
        i = 0

        while i < len(fileContents):
            c = fileContents[i]
            if c == '\r':
                if i < len(fileContents) - 1:
                    c1 = fileContents[i + 1]
                else:
                    c1 = '\0'
                if c1 == '\n':
                    i += 1
                n += 1
                outputFile.write(newLineChars)
            elif c == '\n':
                n += 1
                outputFile.write(newLineChars)
            else:
                outputFile.write(c)
            i += 1

        outputFile.close()
        sb += ' -> ' + self.outputFileName
        sb += ", lines = %d, %s = %d" % (n + 1, self.fixedLineEndings, n)
        print(sb)

if __name__ == '__main__':
    t = HammerTool()
    if t.ProcessCommandLine():
        t.Execute()
