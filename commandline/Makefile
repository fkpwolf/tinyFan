# Name: Makefile
# Project: hid-data example
# Author: Christian Starkjohann
# Creation Date: 2008-04-11
# Tabsize: 4
# Copyright: (c) 2008 by OBJECTIVE DEVELOPMENT Software GmbH
# License: GNU GPL v2 (see License.txt), GNU GPL v3 or proprietary (CommercialLicense.txt)
# This Revision: $Id: Makefile 692 2008-11-07 15:07:40Z cs $

# Please read the definitions below and edit them as appropriate for your
# system:

# Use the following 3 lines on Unix and Mac OS X:
#USBFLAGS=   `libusb-config --cflags`
#USBLIBS=    `libusb-config --libs`
#EXE_SUFFIX=

#mac first you should install it
#sudo port install libusb-legacy
USBFLAGS = -I/opt/local/include/libusb-legacy/
USBLIBS = -L/opt/local/lib/libusb-legacy/ -lusb-legacy
EXE_SUFFIX=

#win cygwin
#USBFLAGS = -I/usr/local/include
#USBLIBS = -L/usr/local/lib -lusb
#EXE_SUFFIX = .exe

CC=				gcc
CFLAGS=			-O -Wall $(USBFLAGS)
LIBS=			$(USBLIBS)

OBJ=		hidtool.o hiddata.o
PROGRAM=	hidtool$(EXE_SUFFIX)

all: $(PROGRAM)

$(PROGRAM): $(OBJ)
	$(CC) -o $(PROGRAM) $(OBJ) $(LIBS)

strip: $(PROGRAM)
	strip $(PROGRAM)

clean:
	rm -f $(OBJ) $(PROGRAM)

.c.o:
	$(CC) $(ARCH_COMPILE) $(CFLAGS) -c $*.c -o $*.o
