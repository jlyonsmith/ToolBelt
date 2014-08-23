all: \
	$(patsubst %.resx,%.resources,$(wildcard *.resx)) \
	$(patsubst %.strings,%.resources,$(wildcard *.strings))

%.resources: %.resx
	/usr/local/bin/strapper $< -i -n:ToolBelt.Tests -w:ToolBelt.Message

%.strings: %.resx
	/usr/local/bin/strapper $< -i -n:ToolBelt.Tests -w:ToolBelt.Message
