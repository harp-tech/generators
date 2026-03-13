#ifndef _APP_IOS_AND_REGS_H_
#define _APP_IOS_AND_REGS_H_
#include "cpu.h"

void init_ios(void);
/************************************************************************/
/* Definition of input pins                                             */
/************************************************************************/
// POKE0_IR                Description: Poke 0 infrared
// POKE0_IO                Description: Poke 0 DIO
// POKE1_IR                Description: Poke 1 infrared
// POKE1_IO                Description: Poke 1 DIO
// POKE2_IR                Description: Poke 2 infrared
// POKE2_IO                Description: Poke 2 DIO
// ADC1_AVAILABLE          Description: ADC1 is available on hardware
// DI3                     Description: Input DI3

#define read_POKE0_IR read_io(PORTD, 4)          // POKE0_IR
#define read_POKE0_IO read_io(PORTD, 5)          // POKE0_IO
#define read_POKE1_IR read_io(PORTE, 4)          // POKE1_IR
#define read_POKE1_IO read_io(PORTE, 5)          // POKE1_IO
#define read_POKE2_IR read_io(PORTF, 4)          // POKE2_IR
#define read_POKE2_IO read_io(PORTF, 5)          // POKE2_IO
#define read_ADC1_AVAILABLE read_io(PORTJ, 0)    // ADC1_AVAILABLE
#define read_DI3 read_io(PORTH, 0)               // DI3

/************************************************************************/
/* Definition of output pins                                            */
/************************************************************************/
// DO3                     Description: Output DO0
// DO2                     Description: Output DO1
// DO1                     Description: Output DO2
// DO0                     Description: Output DO3
// LED0                    Description: Output LED0
// LED1                    Description: Output LED1
// RGBS                    Description: One wire LEDs
// POKE0_LED               Description: Poke 0 digital output
// POKE0_VALVE             Description: Poke 0 Valve
// POKE1_LED               Description: Poke 1 digital output
// POKE1_VALVE             Description: Poke 1 Valve
// POKE2_LED               Description: Poke 2 digital output
// POKE2_VALVE             Description: Poke 2 Valve
// CS0_1                   Description: CS0_1 / CS_ADC0

/* DO3 */
#define set_DO3 set_io(PORTC, 0)
#define clr_DO3 clear_io(PORTC, 0)
#define tgl_DO3 toggle_io(PORTC, 0)
#define read_DO3 read_io(PORTC, 0)

/* DO2 */
#define set_DO2 set_io(PORTD, 0)
#define clr_DO2 clear_io(PORTD, 0)
#define tgl_DO2 toggle_io(PORTD, 0)
#define read_DO2 read_io(PORTD, 0)

/* DO1 */
#define set_DO1 set_io(PORTE, 0)
#define clr_DO1 clear_io(PORTE, 0)
#define tgl_DO1 toggle_io(PORTE, 0)
#define read_DO1 read_io(PORTE, 0)

/* DO0 */
#define set_DO0 set_io(PORTF, 0)
#define clr_DO0 clear_io(PORTF, 0)
#define tgl_DO0 toggle_io(PORTF, 0)
#define read_DO0 read_io(PORTF, 0)

/* LED0 */
#define set_LED0 clear_io(PORTB, 6)
#define clr_LED0 set_io(PORTB, 6)
#define tgl_LED0 toggle_io(PORTB, 6)
#define read_LED0 read_io(PORTB, 6)

/* LED1 */
#define set_LED1 clear_io(PORTB, 5)
#define clr_LED1 set_io(PORTB, 5)
#define tgl_LED1 toggle_io(PORTB, 5)
#define read_LED1 read_io(PORTB, 5)

/* RGBS */
#define set_RGBS set_io(PORTC, 5)
#define clr_RGBS clear_io(PORTC, 5)
#define tgl_RGBS toggle_io(PORTC, 5)
#define read_RGBS read_io(PORTC, 5)

/* POKE0_LED */
#define set_POKE0_LED set_io(PORTD, 6)
#define clr_POKE0_LED clear_io(PORTD, 6)
#define tgl_POKE0_LED toggle_io(PORTD, 6)
#define read_POKE0_LED read_io(PORTD, 6)

/* POKE0_VALVE */
#define set_POKE0_VALVE set_io(PORTD, 7)
#define clr_POKE0_VALVE clear_io(PORTD, 7)
#define tgl_POKE0_VALVE toggle_io(PORTD, 7)
#define read_POKE0_VALVE read_io(PORTD, 7)

/* POKE1_LED */
#define set_POKE1_LED set_io(PORTE, 6)
#define clr_POKE1_LED clear_io(PORTE, 6)
#define tgl_POKE1_LED toggle_io(PORTE, 6)
#define read_POKE1_LED read_io(PORTE, 6)

/* POKE1_VALVE */
#define set_POKE1_VALVE set_io(PORTE, 7)
#define clr_POKE1_VALVE clear_io(PORTE, 7)
#define tgl_POKE1_VALVE toggle_io(PORTE, 7)
#define read_POKE1_VALVE read_io(PORTE, 7)

/* POKE2_LED */
#define set_POKE2_LED set_io(PORTF, 6)
#define clr_POKE2_LED clear_io(PORTF, 6)
#define tgl_POKE2_LED toggle_io(PORTF, 6)
#define read_POKE2_LED read_io(PORTF, 6)

/* POKE2_VALVE */
#define set_POKE2_VALVE set_io(PORTF, 7)
#define clr_POKE2_VALVE clear_io(PORTF, 7)
#define tgl_POKE2_VALVE toggle_io(PORTF, 7)
#define read_POKE2_VALVE read_io(PORTF, 7)

/* CS0_1 */
#define set_CS0_1 set_io(PORTC, 0)
#define clr_CS0_1 clear_io(PORTC, 0)
#define tgl_CS0_1 toggle_io(PORTC, 0)
#define read_CS0_1 read_io(PORTC, 0)

/************************************************************************/
/* Registers' structure                                                 */
/************************************************************************/
typedef struct
{
    uint8_t REG_DIGITAL_INPUTS;
    float_t REG_ANALOG_DATA[6];
    uint8_t REG_COMPLEX_CONFIGURATION[17];
    uint8_t REG_VERSION[32];
    uint32_t REG_CUSTOM_PAYLOAD[3];
    uint32_t REG_CUSTOM_RAW_PAYLOAD[3];
    uint8_t REG_CUSTOM_MEMBER_CONVERTER[3];
    uint8_t REG_BITMASK_SPLITTER;
    int32_t REG_COUNTER0;
    uint8_t REG_PORT_DIO_SET;
    uint16_t REG_PULSE_DO_PORT0;
    uint16_t REG_PULSE_DO0;
    uint16_t REG_START_PULSE;
    uint16_t REG_START_PULSE_TRAIN[2];
} AppRegs;

/************************************************************************/
/* Registers' address                                                   */
/************************************************************************/
/* Registers */
#define ADD_REG_DIGITAL_INPUTS             32 // U8     
#define ADD_REG_ANALOG_DATA                33 // FLOAT  
#define ADD_REG_COMPLEX_CONFIGURATION      34 // U8     
#define ADD_REG_VERSION                    35 // U8     
#define ADD_REG_CUSTOM_PAYLOAD             36 // U32    
#define ADD_REG_CUSTOM_RAW_PAYLOAD         37 // U32    
#define ADD_REG_CUSTOM_MEMBER_CONVERTER    38 // U8     
#define ADD_REG_BITMASK_SPLITTER           39 // U8     
#define ADD_REG_COUNTER0                   40 // I32    
#define ADD_REG_PORT_DIO_SET               41 // U8     
#define ADD_REG_PULSE_DO_PORT0             42 // U16    
#define ADD_REG_PULSE_DO0                  43 // U16    
#define ADD_REG_START_PULSE               100 // U16    Starts a PWM pulse.
#define ADD_REG_START_PULSE_TRAIN         101 // U16    Starts a PWM pulse train.

/************************************************************************/
/* Tests registers' memory limits                                       */
/*                                                                      */
/* DON'T change the APP_REGS_ADD_MIN value !!!                          */
/* DON'T change these names !!!                                         */
/************************************************************************/
/* Memory limits */
#define APP_REGS_ADD_MIN                    0x20
#define APP_REGS_ADD_MAX                    0x65
#define APP_NBYTES_OF_REG_BANK              117

/************************************************************************/
/* Registers' bits                                                      */
/************************************************************************/
#define B_DIO0               (1<<0)       // 
#define B_DIO1               (1<<1)       // 
#define B_DIO2               (1<<2)       // 
#define B_DIO3               (1<<3)       // 
#define B_DI_PORT0           (1<<8)       // 
#define B_TEST_DI_PORT1      (1<<9)       // 
#define B_SUPPLY_PORT0       (1<<10)      // 
#define B_PORT_DIO1          (1<<11)      // 
#define MSK_PWM_PORT_SEL     0x0F         // 
#define GM_PWM_PORT_PWM0     0x01         // 
#define GM_PWM_PORT_PWM1     0x02         // 
#define GM_PWM_PORT_PWM2     0x04         // 
#define GM_PWM_PORT_PWM3     0x0A         // 

#endif /* _APP_REGS_H_ */