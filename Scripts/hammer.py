#!python

__version__ = "Feb. 2013"


class HammerTool:

    def ProcessCommandLine(self):
        import optparse
        parser = optparse.OptionParser()
        parser.set_description("Reports on and fixes line endings for text files.")
        parser.add_option("-o", dest="OutputFilename",
                          help="Specify different name for output file.")
        parser.add_option("-f", dest="lineEndings",
                          help="Fix line endings to be cr, lf, crlf or auto.")
        parser.add_option("-q", dest="NoLogo", action="store_true",
                          help="Suppress logo.")
        parser.remove_option("-h")
        parser.add_option("-h", dest="help", action="store_true",
                          help="Show help.")

        parser.set_usage("%prog [options] <text-file>")
        options, args = parser.parse_args()

        if (not options.NoLogo):
            print "Hammer text line ending fixer. Version",__version__
            print "Copyright (c) 2012, John Lyon-Smith."
            
        if (options.help):
            parser.print_help()
            return False

        elif (len(args) == 0):
            print "Error: A text file must be specified"
            return False

        else:
            self.InputFilename = args[0]
            if (options.OutputFilename is None):
                self.OutputFilename = self.InputFilename
            else:
                self.OutputFilename = options.OutputFilename

            self.FixedEndings = options.lineEndings
            return True
    
    def Execute(self):
        ifile = open(self.InputFilename)
        fileContents = ifile.read()
        ifile.close()

        numCr = 0;
        numLf = 0;
        numCrLf = 0;
        numLines = 1
        
        for i in range(len(fileContents)):

            if (fileContents[i] == '\r'):
                if (i < len(fileContents) - 1):
                    c1 = fileContents[i + 1]
                else:
                    c1 = '\0'
                if (c1 == '\n'):
                    numCrLf += 1
                    i += 1
                else:
                    numCr += 1
                numLines += 1

            elif (fileContents[i] == '\n'):
                numLf += 1
                numLines += 1
        
        sb = self.InputFilename 
        sb += " lines=%d, cr=%d, lf=%d, crlf=%d" % (numLines, numCr, numLf, numCrLf)
        if (self.FixedEndings is None):
            print sb
            return

        autoLineEnding = "Lf"
        n = numLf;

        if (numCrLf > n):
            autoLineEnding = "CrLf"
            n = numCrLf

        if (numCr > n):
            autoLineEnding = "Cr"

        if (self.FixedEndings.lower() == 'auto'):
            self.FixedEndings = autoLineEnding

        if (self.FixedEndings.lower() == 'cr'):
            newLineChars = "\r"
        elif (self.FixedEndings.lower() == 'lf'):
            newLineChars = "\n"
        else:
            newLineChars = "\r\n"
            
        ofile = open(self.OutputFilename,"w")
        
        n = 0
        for i in range(len(fileContents)):
            c = fileContents[i]
            if (c == '\r'):
                if (i < len(fileContents) - 1):
                    c1 = fileContents[i + 1]
                else:
                    c1 = '\0'
                if (c1 == '\n'):
                    i += 1
                n += 1
                ofile.write(newLineChars)

            elif (c == '\n'):
                n += 1
                ofile.write(newLineChars)
            
            else:
                ofile.write(c)
        ofile.close()
        sb += ' -> '+self.OutputFilename
        sb += ", lines=%d, %s=%d" % (n + 1, self.FixedEndings, n)
        print sb

if __name__ == '__main__':
    t = HammerTool()
    if (t.ProcessCommandLine()):
        t.Execute()

