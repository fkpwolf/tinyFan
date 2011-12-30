/* Name: main.c
 * Project: hid-data, example how to use HID for data transfer
 * Author: Christian Starkjohann
 * Creation Date: 2008-04-11
 * Tabsize: 4
 * Copyright: (c) 2008 by OBJECTIVE DEVELOPMENT Software GmbH
 * License: GNU GPL v2 (see License.txt), GNU GPL v3 or proprietary (CommercialLicense.txt)
 * This Revision: $Id: main.c 777 2010-01-15 18:34:48Z cs $
 */

/*
This example should run on most AVRs with only little changes. No special
hardware resources except INT0 are used. You may have to change usbconfig.h for
different I/O pins for USB. Please note that USB D+ must be the INT0 pin, or
at least be connected to INT0 as well.
*/
#define LED_PORT_DDR        DDRB
#define LED_PORT_OUTPUT     PORTB
#define LED_BIT             0
#define T0_CLK  16113

#include <avr/io.h>
#include <avr/wdt.h>
#include <avr/interrupt.h>  /* for sei() */


ISR(SIG_PIN_CHANGE) {
				LED_PORT_OUTPUT ^= _BV(LED_BIT); //toggle LED
}

/* ------------------------------------------------------------------------- */

int main(void)
{

    /* Even if you don't use the watchdog, turn it off here. On newer devices,
     * the status of the watchdog (on/off, period) is PRESERVED OVER RESET!
     */
    /* RESET status: all port bits are inputs without pull-up.
     * That's the way we need D+ and D-. Therefore we don't need any
     * additional hardware initialization.
     */
		
		PCMSK = (1 << PB4); //enable interrupt on PB3
		GIMSK |= (1 << PCIE); //Pin change interrupt enable
		LED_PORT_DDR |= _BV(LED_BIT);   // make the LED bit an output 
   
    sei();
    for(;;){                /* main event loop */
    }
    return 0;
}

/* ------------------------------------------------------------------------- */
