#include <avr/io.h>
#include "hwbp_core_types.h"
#include "app_ios_and_regs.h"

extern AppRegs app_regs;

/************************************************************************/
/* Configure and initialize IOs                                         */
/************************************************************************/
void init_ios(void)
{
    /* Configure input pins */
    io_pin2in(&PORTD, 4, PULL_IO_UP, SENSE_IO_EDGES_BOTH);               // POKE0_IR
    io_pin2in(&PORTD, 5, PULL_IO_UP, SENSE_IO_EDGES_BOTH);               // POKE0_IO
    io_pin2in(&PORTE, 4, PULL_IO_UP, SENSE_IO_EDGES_BOTH);               // POKE1_IR
    io_pin2in(&PORTE, 5, PULL_IO_UP, SENSE_IO_EDGES_BOTH);               // POKE1_IO
    io_pin2in(&PORTF, 4, PULL_IO_UP, SENSE_IO_EDGES_BOTH);               // POKE2_IR
    io_pin2in(&PORTF, 5, PULL_IO_UP, SENSE_IO_EDGES_BOTH);               // POKE2_IO
    io_pin2in(&PORTJ, 0, PULL_IO_DOWN, SENSE_IO_EDGES_BOTH);             // ADC1_AVAILABLE
    io_pin2in(&PORTH, 0, PULL_IO_TRISTATE, SENSE_IO_EDGES_BOTH);         // DI3

    /* Configure input interrupts */
    io_set_int(&PORTD, INT_LEVEL_LOW, 0, (1<<4), false);                 // POKE0_IR
    io_set_int(&PORTE, INT_LEVEL_LOW, 0, (1<<4), false);                 // POKE1_IR
    io_set_int(&PORTF, INT_LEVEL_LOW, 0, (1<<4), false);                 // POKE2_IR
    io_set_int(&PORTH, INT_LEVEL_LOW, 0, (1<<0), false);                 // DI3

    /* Configure output pins */
    io_pin2out(&PORTC, 0, OUT_IO_DIGITAL, IN_EN_IO_EN);                  // DO3
    io_pin2out(&PORTD, 0, OUT_IO_DIGITAL, IN_EN_IO_EN);                  // DO2
    io_pin2out(&PORTE, 0, OUT_IO_DIGITAL, IN_EN_IO_EN);                  // DO1
    io_pin2out(&PORTF, 0, OUT_IO_DIGITAL, IN_EN_IO_EN);                  // DO0
    io_pin2out(&PORTB, 6, OUT_IO_DIGITAL, IN_EN_IO_EN);                  // LED0
    io_pin2out(&PORTB, 5, OUT_IO_DIGITAL, IN_EN_IO_EN);                  // LED1
    io_pin2out(&PORTC, 5, OUT_IO_DIGITAL, IN_EN_IO_DIS);                 // RGBS
    io_pin2out(&PORTD, 6, OUT_IO_DIGITAL, IN_EN_IO_EN);                  // POKE0_LED
    io_pin2out(&PORTD, 7, OUT_IO_DIGITAL, IN_EN_IO_EN);                  // POKE0_VALVE
    io_pin2out(&PORTE, 6, OUT_IO_DIGITAL, IN_EN_IO_EN);                  // POKE1_LED
    io_pin2out(&PORTE, 7, OUT_IO_DIGITAL, IN_EN_IO_EN);                  // POKE1_VALVE
    io_pin2out(&PORTF, 6, OUT_IO_DIGITAL, IN_EN_IO_EN);                  // POKE2_LED
    io_pin2out(&PORTF, 7, OUT_IO_DIGITAL, IN_EN_IO_EN);                  // POKE2_VALVE
    io_pin2out(&PORTC, 0, OUT_IO_WIREDAND, IN_EN_IO_EN);                 // CS0_1

    /* Initialize output pins */
    clr_DO3;
    clr_DO2;
    clr_DO1;
    clr_DO0;
    clr_LED0;
    clr_LED1;
    clr_RGBS;
    clr_POKE0_LED;
    clr_POKE0_VALVE;
    clr_POKE1_LED;
    clr_POKE1_VALVE;
    clr_POKE2_LED;
    clr_POKE2_VALVE;
    set_CS0_1;
}

/************************************************************************/
/* Registers' stuff                                                     */
/************************************************************************/
AppRegs app_regs;

uint8_t app_regs_type[] = {
    TYPE_U8,
    TYPE_FLOAT,
    TYPE_U8,
    TYPE_U8,
    TYPE_U32,
    TYPE_U32,
    TYPE_U8,
    TYPE_U8,
    TYPE_I32,
    TYPE_U8,
    TYPE_U16,
    TYPE_U16,
    TYPE_U16,
    TYPE_U16
};

uint16_t app_regs_n_elements[] = {
    1,
    6,
    17,
    32,
    3,
    3,
    3,
    1,
    1,
    1,
    1,
    1,
    1,
    2
};

uint8_t *app_regs_pointer[] = {
    (uint8_t*)(&app_regs.REG_DIGITAL_INPUTS),
    (uint8_t*)(&app_regs.REG_ANALOG_DATA),
    (uint8_t*)(&app_regs.REG_COMPLEX_CONFIGURATION),
    (uint8_t*)(&app_regs.REG_VERSION),
    (uint8_t*)(&app_regs.REG_CUSTOM_PAYLOAD),
    (uint8_t*)(&app_regs.REG_CUSTOM_RAW_PAYLOAD),
    (uint8_t*)(&app_regs.REG_CUSTOM_MEMBER_CONVERTER),
    (uint8_t*)(&app_regs.REG_BITMASK_SPLITTER),
    (uint8_t*)(&app_regs.REG_COUNTER0),
    (uint8_t*)(&app_regs.REG_PORT_DIO_SET),
    (uint8_t*)(&app_regs.REG_PULSE_DO_PORT0),
    (uint8_t*)(&app_regs.REG_PULSE_DO0),
    (uint8_t*)(&app_regs.REG_START_PULSE),
    (uint8_t*)(&app_regs.REG_START_PULSE_TRAIN)
};