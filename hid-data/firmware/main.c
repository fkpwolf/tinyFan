#define LED_PORT_DDR        DDRB
#define LED_PORT_OUTPUT     PORTB
#define LED_BIT             4
#define T0_CLK  16113
#define PIN_STEADY_THRESHOLD 6 

#include <avr/io.h>
#include <avr/wdt.h>
#include <avr/interrupt.h>  /* for sei() */
#include <util/delay.h>     /* for _delay_ms() */
#include <avr/eeprom.h>

#include <avr/pgmspace.h>   /* required by usbdrv.h */
#include "usbdrv.h"
/* ------------------------------------------------------------------------ */
volatile int timecount = 0;
volatile int counterRPS = 0;
volatile int lastButtonState = 0;
volatile long lastChangedTime = 0;
volatile long t = 0;
volatile int lastButtonValue =0;

volatile long interval;

ISR(TIM0_OVF_vect) { //one round is 128um(1:8), 1000um(1:64)
  sei();//otherwise usb not stable. see http://forums.obdev.at/viewtopic.php?f=8&t=2827

	TCNT0 =0;
	//read pin data. see http://www.avrfreaks.net/index.php?name=PNphpBB2&file=printview&t=93058&start=0
	int reading = PINB & _BV(LED_BIT);
	if( reading != lastButtonState)
		timecount = 0;
	else
		timecount++;
	if(timecount == PIN_STEADY_THRESHOLD){ 
	 if( lastButtonValue != reading) { //value changed
			interval = t - lastChangedTime;
			lastChangedTime = t;
			lastButtonValue = reading;
		}
	}
	lastButtonState = reading;
	t++;
	if (t > 10000){ //re-init
		t = t - lastChangedTime;
		lastChangedTime = 0;	
	}
}

/* ----------------------------- USB interface ----------------------------- */

PROGMEM char usbHidReportDescriptor[22] = {    /* USB report descriptor */
    0x06, 0x00, 0xff,              // USAGE_PAGE (Generic Desktop)
    0x09, 0x01,                    // USAGE (Vendor Usage 1)
    0xa1, 0x01,                    // COLLECTION (Application)
    0x15, 0x00,                    //   LOGICAL_MINIMUM (0)
    0x26, 0xff, 0x00,              //   LOGICAL_MAXIMUM (255)
    0x75, 0x08,                    //   REPORT_SIZE (8)
    0x95, 0x80,                    //   REPORT_COUNT (128)
    0x09, 0x00,                    //   USAGE (Undefined)
    0xb2, 0x02, 0x01,              //   FEATURE (Data,Var,Abs,Buf)
    0xc0                           // END_COLLECTION
};
/* Since we define only one feature report, we don't use report-IDs (which
 * would be the first byte of the report). The entire report consists of 128
 * opaque data bytes.
 */

/* The following variables store the status of the current data transfer */
static uchar    currentAddress;
static uchar    bytesRemaining;

/* ------------------------------------------------------------------------- */

/* usbFunctionRead() is called when the host requests a chunk of data from
 * the device. For more information see the documentation in usbdrv/usbdrv.h.
 */
uchar   usbFunctionRead(uchar *data, uchar len)
{
		//speedometer
		//unsigned char currentRPS = (T0_CLK/4) / counterRPS;
		//unsigned int currentRPS = 0x1234;
		//unsigned int currentRPS = counterRPS;
		data[0] = interval;
		//data[1] = currentRPS >> 8;
		return 2;
		
    /*if(len > bytesRemaining)
        len = bytesRemaining;
    eeprom_read_block(data, (uchar *)0 + currentAddress, len);
    currentAddress += len;
    bytesRemaining -= len;
    return len;*/
}

/* usbFunctionWrite() is called when the host sends a chunk of data to the
 * device. For more information see the documentation in usbdrv/usbdrv.h.
 */
uchar   usbFunctionWrite(uchar *data, uchar len)
{
    if(bytesRemaining == 0)
        return 1;               /* end of transfer */
    if(len > bytesRemaining)
        len = bytesRemaining;
    eeprom_write_block(data, (uchar *)0 + currentAddress, len);
    currentAddress += len;
    bytesRemaining -= len;
    return bytesRemaining == 0; /* return 1 if this was the last chunk */
}

/* ------------------------------------------------------------------------- */

usbMsgLen_t usbFunctionSetup(uchar data[8])
{
usbRequest_t    *rq = (void *)data;

    if((rq->bmRequestType & USBRQ_TYPE_MASK) == USBRQ_TYPE_CLASS){    /* HID class request */
        if(rq->bRequest == USBRQ_HID_GET_REPORT){  /* wValue: ReportType (highbyte), ReportID (lowbyte) */
            /* since we have only one report type, we can ignore the report-ID */
            bytesRemaining = 128;
            currentAddress = 0;
            return USB_NO_MSG;  /* use usbFunctionRead() to obtain data */
        }else if(rq->bRequest == USBRQ_HID_SET_REPORT){
            /* since we have only one report type, we can ignore the report-ID */
            bytesRemaining = 128;
            currentAddress = 0;
            return USB_NO_MSG;  /* use usbFunctionWrite() to receive data from host */
        }
    }else{
        /* ignore vendor type requests, we don't use any */
    }
    return 0;
}

/* ------------------------------------------------------------------------- */

int main(void)
{
		uchar   i;

    wdt_enable(WDTO_1S);
    /* Even if you don't use the watchdog, turn it off here. On newer devices,
     * the status of the watchdog (on/off, period) is PRESERVED OVER RESET!
     */
    /* RESET status: all port bits are inputs without pull-up.
     * That's the way we need D+ and D-. Therefore we don't need any
     * additional hardware initialization.
     */
    usbInit();
    usbDeviceDisconnect();  /* enforce re-enumeration, do this while interrupts are disabled! */
    i = 0;
    while(--i){             /* fake USB disconnect for > 250 ms */
        wdt_reset();
        _delay_ms(1);
    }
    usbDeviceConnect();
		
		LED_PORT_DDR &= ~_BV(LED_BIT);   // make the LED bit an input 
    TCCR0B = 0x03;// 1:8 presc. 
		TCNT0 =0;
		TIMSK= 1 << TOIE0; //unmark Timer 0 overflow interrupt
   
    sei();
    for(;;){                /* main event loop */
        wdt_reset();
        usbPoll();
    }
    return 0;
}
