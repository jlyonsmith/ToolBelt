#!python

__version__ = "Feb. 2013"


class ChiselTool:

    def ProcessCommandLine(self):
        import optparse
        parser = optparse.OptionParser()
        parser.set_description("Convert tab/spaces at the start of lines.")
        parser.add_option("-o", dest="OutputFilename",
                          help="Specify different name for output file.")
        parser.add_option("-m", dest="convertMode", default="t2s",
                          help="tab to spaces 't2s' or spaces to tab 's2t' (default = %default)")
        parser.add_option("-s", dest="size", default=4, type = int,
                          help="number of spaces corresponding to 1 tab (default = %default)")
        parser.add_option("-q", dest="NoLogo", action="store_true",
                          help="Suppress logo.")
        parser.remove_option("-h")
        parser.add_option("-h", dest="help", action="store_true",
                          help="Show help.")

        parser.set_usage("%prog [options] <text-file>")
        options, args = parser.parse_args()

        if (not options.NoLogo):
            print "Chisel tab/spaces converter. Version", __version__
            print "Copyright (c) 2013, John Lyon-Smith."

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

            self.convertMode = options.convertMode
            self.size = options.size
            return True

    def Execute(self):
        inputFile = open(self.InputFilename)
        fileContents = inputFile.readlines()
        inputFile.close()

        tabs2space = " " * self.size
        stringConstant = None
        ofile = open(self.OutputFilename, "w")

        for line in fileContents:
            if (stringConstant is None):
                if (self.convertMode == "s2t"):
                    i = 0
                    while (line[i:].startswith(tabs2space)):
                        ofile.write("\t")
                        i += self.size
                    ofile.write(line[i:])
                else:
                    for i in range(len(line)):
                        if (line[i] != "\t"):
                            ofile.write(line[i:])
                            break
                        else:
                            ofile.write(tabs2space)

                if ((line.find('@"'))>=0):
                    cstr = line.replace('""','').replace('@"','')
                    if (cstr.find('"')<0):
                        stringConstant = "C#"
            else:
                ofile.write(line)
                cstr = line.replace('""','')
                if ((cstr.find('"'))>=0):
                    stringConstant = None

        ofile.close()

if __name__ == '__main__':
    t = ChiselTool()
    if (t.ProcessCommandLine()):
        t.Execute()
