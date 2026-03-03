#include "app_funcs.h"
#include "app_ios_and_regs.h"
#include "hwbp_core.h"


/************************************************************************/
/* Create pointers to functions                                         */
/************************************************************************/
extern AppRegs app_regs;

void (*app_func_rd_pointer[])(void) = {
	&app_read_REG_DIGITAL_INPUTS,
	&app_read_REG_ANALOG_DATA,
	&app_read_REG_COMPLEX_CONFIGURATION,
	&app_read_REG_VERSION,
	&app_read_REG_CUSTOM_PAYLOAD,
	&app_read_REG_CUSTOM_RAW_PAYLOAD,
	&app_read_REG_CUSTOM_MEMBER_CONVERTER,
	&app_read_REG_BITMASK_SPLITTER,
	&app_read_REG_COUNTER0,
	&app_read_REG_PORT_DIO_SET,
	&app_read_REG_PULSE_DO_PORT0,
	&app_read_REG_PULSE_DO0,
	&app_read_REG_START_PULSE,
	&app_read_REG_START_PULSE_TRAIN
};

bool (*app_func_wr_pointer[])(void*) = {
	&app_write_REG_DIGITAL_INPUTS,
	&app_write_REG_ANALOG_DATA,
	&app_write_REG_COMPLEX_CONFIGURATION,
	&app_write_REG_VERSION,
	&app_write_REG_CUSTOM_PAYLOAD,
	&app_write_REG_CUSTOM_RAW_PAYLOAD,
	&app_write_REG_CUSTOM_MEMBER_CONVERTER,
	&app_write_REG_BITMASK_SPLITTER,
	&app_write_REG_COUNTER0,
	&app_write_REG_PORT_DIO_SET,
	&app_write_REG_PULSE_DO_PORT0,
	&app_write_REG_PULSE_DO0,
	&app_write_REG_START_PULSE,
	&app_write_REG_START_PULSE_TRAIN
};

/************************************************************************/
/* REG_DIGITAL_INPUTS                                                   */
/************************************************************************/
void app_read_REG_DIGITAL_INPUTS(void)
{
	//app_regs.REG_DIGITAL_INPUTS = 0;

}

bool app_write_REG_DIGITAL_INPUTS(void *a) { return false; }

/************************************************************************/
/* REG_ANALOG_DATA                                                      */
/************************************************************************/
// This register is an array with 6 positions
void app_read_REG_ANALOG_DATA(void)
{
	//app_regs.REG_ANALOG_DATA[0] = 0;

}

bool app_write_REG_ANALOG_DATA(void *a) { return false; }

/************************************************************************/
/* REG_COMPLEX_CONFIGURATION                                            */
/************************************************************************/
// This register is an array with 17 positions
void app_read_REG_COMPLEX_CONFIGURATION(void)
{
	//app_regs.REG_COMPLEX_CONFIGURATION[0] = 0;

}

bool app_write_REG_COMPLEX_CONFIGURATION(void *a)
{
	uint8_t *reg = ((uint8_t*)a);

	app_regs.REG_COMPLEX_CONFIGURATION[0] = reg[0];
    return true;
}

/************************************************************************/
/* REG_VERSION                                                          */
/************************************************************************/
// This register is an array with 32 positions
void app_read_REG_VERSION(void)
{
	//app_regs.REG_VERSION[0] = 0;

}

bool app_write_REG_VERSION(void *a) { return false; }

/************************************************************************/
/* REG_CUSTOM_PAYLOAD                                                   */
/************************************************************************/
// This register is an array with 3 positions
void app_read_REG_CUSTOM_PAYLOAD(void)
{
	//app_regs.REG_CUSTOM_PAYLOAD[0] = 0;

}

bool app_write_REG_CUSTOM_PAYLOAD(void *a) { return false; }

/************************************************************************/
/* REG_CUSTOM_RAW_PAYLOAD                                               */
/************************************************************************/
// This register is an array with 3 positions
void app_read_REG_CUSTOM_RAW_PAYLOAD(void)
{
	//app_regs.REG_CUSTOM_RAW_PAYLOAD[0] = 0;

}

bool app_write_REG_CUSTOM_RAW_PAYLOAD(void *a) { return false; }

/************************************************************************/
/* REG_CUSTOM_MEMBER_CONVERTER                                          */
/************************************************************************/
// This register is an array with 3 positions
void app_read_REG_CUSTOM_MEMBER_CONVERTER(void)
{
	//app_regs.REG_CUSTOM_MEMBER_CONVERTER[0] = 0;

}

bool app_write_REG_CUSTOM_MEMBER_CONVERTER(void *a) { return false; }

/************************************************************************/
/* REG_BITMASK_SPLITTER                                                 */
/************************************************************************/
void app_read_REG_BITMASK_SPLITTER(void)
{
	//app_regs.REG_BITMASK_SPLITTER = 0;

}

bool app_write_REG_BITMASK_SPLITTER(void *a)
{
	uint8_t reg = *((uint8_t*)a);

	app_regs.REG_BITMASK_SPLITTER = reg;
    return true;
}

/************************************************************************/
/* REG_COUNTER0                                                         */
/************************************************************************/
void app_read_REG_COUNTER0(void)
{
	//app_regs.REG_COUNTER0 = 0;

}

bool app_write_REG_COUNTER0(void *a) { return false; }

/************************************************************************/
/* REG_PORT_DIO_SET                                                     */
/************************************************************************/
void app_read_REG_PORT_DIO_SET(void)
{
	//app_regs.REG_PORT_DIO_SET = 0;

}

bool app_write_REG_PORT_DIO_SET(void *a)
{
	uint8_t reg = *((uint8_t*)a);

	app_regs.REG_PORT_DIO_SET = reg;
    return true;
}

/************************************************************************/
/* REG_PULSE_DO_PORT0                                                   */
/************************************************************************/
void app_read_REG_PULSE_DO_PORT0(void)
{
	//app_regs.REG_PULSE_DO_PORT0 = 0;

}

bool app_write_REG_PULSE_DO_PORT0(void *a)
{
	uint16_t reg = *((uint16_t*)a);

	app_regs.REG_PULSE_DO_PORT0 = reg;
    return true;
}

/************************************************************************/
/* REG_PULSE_DO0                                                        */
/************************************************************************/
void app_read_REG_PULSE_DO0(void)
{
	//app_regs.REG_PULSE_DO0 = 0;

}

bool app_write_REG_PULSE_DO0(void *a)
{
	uint16_t reg = *((uint16_t*)a);

	app_regs.REG_PULSE_DO0 = reg;
    return true;
}

/************************************************************************/
/* REG_START_PULSE                                                      */
/************************************************************************/
void app_read_REG_START_PULSE(void)
{
	//app_regs.REG_START_PULSE = 0;

}

bool app_write_REG_START_PULSE(void *a)
{
	uint16_t reg = *((uint16_t*)a);

	app_regs.REG_START_PULSE = reg;
    return true;
}

/************************************************************************/
/* REG_START_PULSE_TRAIN                                                */
/************************************************************************/
// This register is an array with 2 positions
void app_read_REG_START_PULSE_TRAIN(void)
{
	//app_regs.REG_START_PULSE_TRAIN[0] = 0;

}

bool app_write_REG_START_PULSE_TRAIN(void *a)
{
	uint16_t *reg = ((uint16_t*)a);

	app_regs.REG_START_PULSE_TRAIN[0] = reg[0];
    return true;
}
