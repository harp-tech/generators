#ifndef _APP_FUNCTIONS_H_
#define _APP_FUNCTIONS_H_
#include <avr/io.h>


/************************************************************************/
/* Define if not defined                                                */
/************************************************************************/
#ifndef bool
	#define bool uint8_t
#endif
#ifndef true
	#define true 1
#endif
#ifndef false
	#define false 0
#endif


/************************************************************************/
/* Prototypes                                                           */
/************************************************************************/
void app_read_REG_DIGITAL_INPUTS(void);
void app_read_REG_ANALOG_DATA(void);
void app_read_REG_COMPLEX_CONFIGURATION(void);
void app_read_REG_VERSION(void);
void app_read_REG_CUSTOM_PAYLOAD(void);
void app_read_REG_CUSTOM_RAW_PAYLOAD(void);
void app_read_REG_CUSTOM_MEMBER_CONVERTER(void);
void app_read_REG_BITMASK_SPLITTER(void);
void app_read_REG_COUNTER0(void);
void app_read_REG_PORT_DIO_SET(void);
void app_read_REG_PULSE_DO_PORT0(void);
void app_read_REG_PULSE_DO0(void);
void app_read_REG_START_PULSE(void);
void app_read_REG_START_PULSE_TRAIN(void);

bool app_write_REG_DIGITAL_INPUTS(void *a);
bool app_write_REG_ANALOG_DATA(void *a);
bool app_write_REG_COMPLEX_CONFIGURATION(void *a);
bool app_write_REG_VERSION(void *a);
bool app_write_REG_CUSTOM_PAYLOAD(void *a);
bool app_write_REG_CUSTOM_RAW_PAYLOAD(void *a);
bool app_write_REG_CUSTOM_MEMBER_CONVERTER(void *a);
bool app_write_REG_BITMASK_SPLITTER(void *a);
bool app_write_REG_COUNTER0(void *a);
bool app_write_REG_PORT_DIO_SET(void *a);
bool app_write_REG_PULSE_DO_PORT0(void *a);
bool app_write_REG_PULSE_DO0(void *a);
bool app_write_REG_START_PULSE(void *a);
bool app_write_REG_START_PULSE_TRAIN(void *a);


#endif /* _APP_FUNCTIONS_H_ */