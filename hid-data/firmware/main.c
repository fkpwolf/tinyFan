#define TACH_BIT            0 //PB0, wire 5
#define PWM_BIT             4 //PB4, wire 3
#define T0_CLK  16113
#define PIN_STEADY_THRESHOLD 6 
#define Plus1Sec 1000 // 1m/1000um

#include <avr/io.h>
#include <avr/wdt.h>
#include <avr/interrupt.h>  /* for sei() */
#include <util/delay.h>     /* for _delay_ms() */
#include <avr/eeprom.h>
#include <stdbool.h>
#include <string.h>

#include <avr/pgmspace.h>   /* required by usbdrv.h */
#include "usbdrv.h"
/* ------------------------------------------------------------------------ */
volatile unsigned long SecCnt = Plus1Sec;
volatile bool	  NewSecond;
volatile unsigned long TachoDebounce = 0;
volatile unsigned int TachoCapture = 0;
volatile unsigned int TachoCaptureOut = 0;
volatile unsigned int PWMDuty = 0;
/*
ISR(TIM0_OVF_vect) { //one round is 128um(1:8), 1000um(1:64)
	TCNT0 =0;
	if(PINB & _BV(TACH_BIT)) {
		if(TachoDebounce != 255) TachoDebounce++;			// count up if the input is high
	} else
		TachoDebounce = 0;									// reset the count if the input is low
	if(TachoDebounce == 4) TachoCapture++;	//why TachoDebounce'++' here?
	if(--SecCnt == 0) {
		NewSecond = true;
		SecCnt = Plus1Sec;
	}
}
*/

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

static uchar 		command[128];
/* ------------------------------------------------------------------------- */

/* usbFunctionRead() is called when the host requests a chunk of data from
 * the device. For more information see the documentation in usbdrv/usbdrv.h.
 */
uchar   usbFunctionRead(uchar *data, uchar len)
{
		//data[0] = TachoCaptureOut;
		//return 2;
  	if(len > bytesRemaining)
        len = bytesRemaining;
		strncpy(data, command + currentAddress, len);
    currentAddress += len;
    bytesRemaining -= len;
    return len;

}

/* usbFunctionWrite() is called when the host sends a chunk of data to the
 * device. For more information see the documentation in usbdrv/usbdrv.h.
 */
uchar   usbFunctionWrite(uchar *data, uchar len)
{
    if(bytesRemaining == 0){
			  return 1;               /* end of transfer */
		}
    if(len > bytesRemaining)
        len = bytesRemaining;
		strncpy(command + currentAddress, data, len);
		//PWMDuty = data;
		OCR1A = command[0];
		OCR0B = command[1];
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
		
		memset(command, 1, 100); //for debug
/*		
		//tach
		DDRB &= ~_BV(TACH_BIT); 

		//set timer 0
    TCCR0B = 0x03;//0x02, 1:8. 0x03, 1:64 presc. 
		TCNT0 =0;
		TIMSK= 1 << TOIE0; //unmark Timer 0 overflow interrupt
*/
		//set PWM. see http://aquaticus.info/pwm
		DDRD |= _BV(PD5); //set as output. FAN1 PWM
		TCCR0A |= _BV(WGM01) | _BV(WGM00); 	
		TCCR0B |= _BV(CS01);
		TCCR0A |= _BV(COM0B1);
		OCR0B=20; 	
		
		//FAN3 pwm
		DDRB |= _BV(PB1); 
		TCCR1A |= _BV(WGM10);
		TCCR1B |= _BV(CS11) | _BV(CS10); //1:64
		TCCR1A |= _BV(COM1A1);
		OCR1A = 125;
   
    sei();
    for(;;){                /* main event loop */
        wdt_reset();
        usbPoll();
		/*		if(NewSecond) {
					NewSecond = false;
					TachoCaptureOut = TachoCapture;
					TachoCapture = 0;
				}
				OCR1B = PWMDuty;*/
    }
    return 0;
}
