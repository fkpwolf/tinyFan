#define Plus1Sec 1000 // 1m/1000um 
#define COMMAND_SET_FAN_DUTY  123
#define COMMAND_SET_FAN_MODE  124
#define COMMAND_SET_SN  125
#define FAN_MODE_3PIN 33
#define FAN_MODE_4PIN 44 

#define SERIAL_NUMBER_LENGTH 6 // the number of characters required for your serial number

static int  serialNumberDescriptor[SERIAL_NUMBER_LENGTH + 1];

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
volatile unsigned long TachoDebounce[4];
volatile unsigned int TachoCapture[4];
volatile unsigned int TachoCaptureOut[4];

ISR(TIMER2_OVF_vect) { //one tick is 1000um(1:64). Even 100um will break usb!
	TCNT2 = 6; //count 250 times

	if(PIND & _BV(PD1)) {  //fan1      //this approach take more stack than 'for'???
		if(TachoDebounce[0] != 255) TachoDebounce[0]++;			
	} else
		TachoDebounce[0] = 0;									
	if(TachoDebounce[0] == 4) TachoCapture[0]++;	

	if(PIND & _BV(PD3)) {  //fan2
		if(TachoDebounce[1] != 255) TachoDebounce[1]++;			
	} else
		TachoDebounce[1] = 0;									
	if(TachoDebounce[1] == 4) TachoCapture[1]++;	


	if(PINB & _BV(PB0)) { //fan3
		if(TachoDebounce[2] != 255) TachoDebounce[2]++;			// count up if the input is high
	} else
		TachoDebounce[2] = 0;									// reset the count if the input is low
	if(TachoDebounce[2] == 4) TachoCapture[2]++;	//why TachoDebounce'++' here?

	if(PINB & _BV(PB3)) {  //fan4
		if(TachoDebounce[3] != 255) TachoDebounce[3]++;			
	} else
		TachoDebounce[3] = 0;									
	if(TachoDebounce[3] == 4) TachoCapture[3]++;	
	

	if(--SecCnt == 0) {
		NewSecond = true;
		SecCnt = Plus1Sec;
	}
}


/* ----------------------------- USB interface ----------------------------- */

const PROGMEM char usbHidReportDescriptor[22] = {    /* USB report descriptor */
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

static uchar 		command[12];

/* ------------------------------------------------------------------------- */

/* usbFunctionRead() is called when the host requests a chunk of data from
 * the device. For more information see the documentation in usbdrv/usbdrv.h.
 */

uchar   usbFunctionRead_debug(uchar *data, uchar len)
{
    if(len > bytesRemaining)
        len = bytesRemaining;
    eeprom_read_block(data, (uchar *)0 + currentAddress, len);
    currentAddress += len;
    bytesRemaining -= len;
    return len;
}


uchar   usbFunctionRead(uchar *data, uchar len)
{
		data[0] = TachoCaptureOut[0]; //biggest is 256. so Biggest Fan speed is 7000RPM
		data[1] = TachoCaptureOut[1];
		data[2] = TachoCaptureOut[2];
		data[3] = TachoCaptureOut[3];
		//fan pin mode
		//why read and write is different?
		//data[4] =	(PINC & _BV(PC4)) ? 1:0;
		//data[5] = (PINC & _BV(PC5)) ? 1:0;
		//data[6] = (PINC & _BV(PC3)) ? 1:0;
		//data[7] = 1;
		//duty
		//readBuffer[3] = TachoCaptureOut[3]; //this lead to all ZERO value

		//readBuffer[8] = OCR1B;
		//readBuffer[9] = OCR1A;
		//readBuffer[10] = OCR0B;
		//readBuffer[11] = OCR0A;

		return 4;
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
		if(command[0] == COMMAND_SET_FAN_DUTY ) {
			OCR1B = command[1];
			OCR1A = command[2];
			OCR0B = command[3];
			OCR0A = command[4];
		} else if( command[0] == COMMAND_SET_FAN_MODE) {
			if(command[1] == FAN_MODE_3PIN)
					PORTC &= ~_BV(PC4); //Fan1. set to 0
			else if(command[1] == FAN_MODE_4PIN)
					PORTC |= _BV(PC4); //set to 1

			if(command[2] == FAN_MODE_3PIN)
					PORTC &= ~_BV(PC5); // Fan2. set to 0
			else if(command[2] == FAN_MODE_4PIN)
					PORTC |= _BV(PC5); //set to 1

			if(command[3] == FAN_MODE_3PIN)
					PORTC &= ~_BV(PC3); //Fan3. set to 0
			else if(command[3] == FAN_MODE_4PIN)
					PORTC |= _BV(PC3); //set to 1

		} else if( command[0] == COMMAND_SET_SN){
			     eeprom_write_block(&command[1], 0, 6);
		}

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
            bytesRemaining = 12;
            currentAddress = 0;
            return USB_NO_MSG;  /* use usbFunctionRead() to obtain data */
        }else if(rq->bRequest == USBRQ_HID_SET_REPORT){
            /* since we have only one report type, we can ignore the report-ID */
            bytesRemaining = 12;
            currentAddress = 0;
            return USB_NO_MSG;  /* use usbFunctionWrite() to receive data from host */
        }
    }else{
        /* ignore vendor type requests, we don't use any */
    }
    return 0;
}

/* ------------------------------------------------------------------------- */
uchar usbFunctionDescriptor(usbRequest_t *rq)
{
   uchar len = 0;
   usbMsgPtr = 0;
   if (rq->wValue.bytes[1] == USBDESCR_STRING && rq->wValue.bytes[0] == 3) // 3 is the type of string descriptor, in this case the device serial number
   {
      usbMsgPtr = (uchar*)serialNumberDescriptor;
      len = sizeof(serialNumberDescriptor);
   }
   return len;
}
/* ------------------------------------------------------------------------- */
static void SetSerial(void)
{
   serialNumberDescriptor[0] = USB_STRING_DESCRIPTOR_HEADER(SERIAL_NUMBER_LENGTH);
	 //TODO only work one by one. byte <-> int
	 eeprom_read_block(&serialNumberDescriptor[1], 0, 1);
	 eeprom_read_block(&serialNumberDescriptor[2], 1, 1);
	 eeprom_read_block(&serialNumberDescriptor[3], 2, 1);
	 eeprom_read_block(&serialNumberDescriptor[4], 3, 1);
	 eeprom_read_block(&serialNumberDescriptor[5], 4, 1);
	 eeprom_read_block(&serialNumberDescriptor[6], 5, 1);
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
		SetSerial();
    usbInit();
    usbDeviceDisconnect();  /* enforce re-enumeration, do this while interrupts are disabled! */
    i = 0;
    while(--i){             /* fake USB disconnect for > 250 ms */
        wdt_reset();
        _delay_ms(1);
    }
    usbDeviceConnect();
		
		memset(command, 1, 12); //for debug
	
		
		/******PWM setting. ref  see http://aquaticus.info/pwm*/
		//Time0 pwm(PD5 & PD6)
		DDRD |= _BV(PD5);//fan3
		DDRD |= _BV(PD6);//fan4
		TCCR0A |= _BV(WGM00); 	
		TCCR0B |= _BV(CS00); //1:1
		TCCR0A |= _BV(COM0B1) | _BV(COM0A1);
		OCR0B=125; 	//fan3
		OCR0A=125; 	//fan4

		//Time1 pwm(PB1 & PB2)
		DDRB |= _BV(PB1); //fan2
		DDRB |= _BV(PB2); //fan1
		TCCR1A |= _BV(WGM10);
		TCCR1B |= _BV(CS10) ; //1:1
		TCCR1A |= _BV(COM1A1) | _BV(COM1B1);
		OCR1A = 125; //fan2
		OCR1B = 125; //fan1

		//fan mode. default is 4pin
		DDRC |= _BV(PC3); 
		DDRC |= _BV(PC4);
		DDRC |= _BV(PC5);
		PORTC |= _BV(PC3); //set to 1
		PORTC |= _BV(PC4); //set to 1
		PORTC |= _BV(PC5); //set to 1

		//tach reading pin init
		DDRD &= ~_BV(PD1); //Fan1
		PORTD |= _BV(PD1); //pull-up
		DDRD &= ~_BV(PD3); //Fan2
		PORTD |= _BV(PD3);
		DDRB &= ~_BV(PB0); //Fan3 
		PORTB |= _BV(PB0); 
		DDRB &= ~_BV(PB3); //Fan4
		PORTB |= _BV(PB3);

	
		//use time2 to tick. can reuse this timer for PWM?
		//timer2's overflow is special. ref http://www.avrfreaks.net/index.php?name=PNphpBB2&file=printview&t=84197
 		ASSR |= (1<<EXCLK); 
   	ASSR |= (1<<AS2); 
		TCCR2B |= _BV(CS22);//1:64
		while (ASSR & (1<<TCR2BUB)); 
		TCNT2 = 6;
		TIMSK2= 1 << TOIE2;
		
		//turn on led
		DDRC |= _BV(PC2); //output
		DDRC |= _BV(PC1); //output
		PORTC |= _BV(PC2); //set to 1
		PORTC |= _BV(PC1); //set to 1
   
    sei();
    for(;;){                /* main event loop */
        wdt_reset();
        usbPoll();
				if(NewSecond) {
					NewSecond = false;
					unsigned int;
					for( i = 0; i < 4; i++){
						TachoCaptureOut[i] = TachoCapture[i];
						TachoCapture[i] = 0;
					}
					
					//PORTC ^= _BV(PC2); //toggle LED
					//PORTC ^= _BV(PC1); //toggle LED
				}
    }
    return 0;
}
