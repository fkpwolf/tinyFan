/* Name: hidtool.c
 * Project: hid-data example
 * Author: Christian Starkjohann
 * Creation Date: 2008-04-11
 * Tabsize: 4
 * Copyright: (c) 2008 by OBJECTIVE DEVELOPMENT Software GmbH
 * License: GNU GPL v2 (see License.txt), GNU GPL v3 or proprietary (CommercialLicense.txt)
 * This Revision: $Id: hidtool.c 723 2009-03-16 19:04:32Z cs $
 */

#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include "hiddata.h"
#include "usbconfig.h"  /* for device VID, PID, vendor name and product name */

/* ------------------------------------------------------------------------- */

static char *usbErrorMessage(int errCode)
{
static char buffer[80];

    switch(errCode){
        case USBOPEN_ERR_ACCESS:      return "Access to device denied";
        case USBOPEN_ERR_NOTFOUND:    return "The specified device was not found";
        case USBOPEN_ERR_IO:          return "Communication error with device";
        default:
            sprintf(buffer, "Unknown USB error %d", errCode);
            return buffer;
    }
    return NULL;    /* not reached */
}

static usbDevice_t  *openDevice(void)
{
usbDevice_t     *dev = NULL;
unsigned char   rawVid[2] = {USB_CFG_VENDOR_ID}, rawPid[2] = {USB_CFG_DEVICE_ID};
char            vendorName[] = {USB_CFG_VENDOR_NAME, 0}, productName[] = {USB_CFG_DEVICE_NAME, 0};
int             vid = rawVid[0] + 256 * rawVid[1];
int             pid = rawPid[0] + 256 * rawPid[1];
int             err;

    if((err = usbhidOpenDevice(&dev, vid, vendorName, pid, productName, 0)) != 0){
        fprintf(stderr, "error finding %s: %s\n", productName, usbErrorMessage(err));
        return NULL;
    }
    return dev;
}

/* ------------------------------------------------------------------------- */

void get_tach(char* ret, char* resp){
	usbDevice_t *dev;
	char  buffer[129]; 
	int  err;
	if((dev = openDevice()) == NULL)
			strncpy( resp, "can't open device", 128 );
	 int len = sizeof(buffer);
	 if((err = usbhidGetReport(dev, 0, buffer, &len)) != 0){
			strncpy( resp, "error reading data", 128 );
    }else{
			int i;
    	for(i = 0; i < 4; i++){
					//int rpm =  (buffer[i + 1] & 0xff) * 60 / 2; //would bigger than 255
					int rpm = buffer[i + 1] & 0xff;
					ret[i] = rpm;
    	}
   }
	 //usbhidCloseDevice(dev);
	 strncpy(resp, "finished read", 128 );
}

/* ------------------------------------------------------------------------- */
void set_duty(char* duty, char* resp){
	strncpy(resp, "init", 128 );
	usbDevice_t *dev;
	char  buffer[129]; 
	int  err;
	if((dev = openDevice()) == NULL){
			strncpy(resp, "can't open device", 128 );
			return;
	}
	int i;
  memset(buffer, 0, sizeof(buffer));
	buffer[1] =  123;
  for(i = 0; i < 4; i++){
		buffer[i + 2] = duty[i];
  }
  if((err = usbhidSetReport(dev, buffer, sizeof(buffer))) != 0) {   /* add a dummy report ID */
		strncpy(resp, "error writing data", 128 );
  	return;
	}
  //usbhidCloseDevice(dev);
	strncpy(resp, "finished write", 128 );
}


/* ------------------------------------------------------------------------- */
void set_fan_mode(char* duty, char* resp){
	strncpy(resp, "init", 128 );
	usbDevice_t *dev;
	char  buffer[129]; 
	int  err;
	if((dev = openDevice()) == NULL){
			strncpy(resp, "can't open device", 128 );
			return;
	}
	int i;
  memset(buffer, 0, sizeof(buffer));
	buffer[1] =  124;
  for(i = 0; i < 4; i++){
		buffer[i + 2] = duty[i];
  }
  if((err = usbhidSetReport(dev, buffer, sizeof(buffer))) != 0) {   /* add a dummy report ID */
		strncpy(resp, "error writing data", 128 );
  	return;
	}
  //usbhidCloseDevice(dev);
	strncpy(resp, "finished write", 128 );
}
