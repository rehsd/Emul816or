MyCode.65816.asm
.setting "HandleLongBranch", true
.setting "RegA16", true
.setting "RegXY16", true

;VIA Registers
VIA_PORTB = $00
VIA_PORTA = $01
VIA_DDRB  = $02
VIA_DDRA  = $03
VIA_T1C_L = $04
VIA_T1C_H = $05
VIA_T1L_L = $06
VIA_T1L_H = $07
VIA_T2C_L = $08
VIA_T2C_H = $09
VIA_SR    = $0A
VIA_ACR   = $0B
VIA_PCR   = $0C
VIA_IFR   = $0D
VIA_IER   = $0E

;VIA1 Address Line A15 - %00010000:10000000:00000000 - $10:8000
VIA1_ADDR  = $108000
VIA1_PORTB = VIA1_ADDR + VIA_PORTB
VIA1_PORTA = VIA1_ADDR + VIA_PORTA
VIA1_DDRB  = VIA1_ADDR + VIA_DDRB
VIA1_DDRA  = VIA1_ADDR + VIA_DDRA
VIA1_IFR   = VIA1_ADDR + VIA_IFR
VIA1_IER   = VIA1_ADDR + VIA_IER

E   = %01000000
RW  = %00100000
RS  = %00010000

;for kb_flags
RELEASE                 = %0000000000000001
SHIFT                   = %0000000000000010
ARROW_LEFT              = %0000000000000100
ARROW_RIGHT             = %0000000000001000
ARROW_UP                = %0000000000010000
ARROW_DOWN              = %0000000000100000
NKP5                    = %0000000001000000
NKP_PLUS                = %0000000010000000

;for kb_flags2
NKP_INSERT              = %0000000000000001
NKP_DELETE              = %0000000000000010
NKP_MINUS               = %0000000000000100
NKP_ASTERISK            = %0000000000001000
PRINTSCREEN             = %0000000000010000
;room for four more

kb_wptr                 = $0020
kb_rptr                 = $0030
kb_flags                = $0040
kb_flags2               = $0050
kb_buffer               = $1000  ; 256-byte kb buffer 0200-02ff

delayDuration   = $001A     ;Count from this number (high byte) to FF - higher number results in shorter delay

.org $0000
    .word $ABCD     ;need something to write at $00 -- wasting first 32K of ROM and using addresses for RAM
                    ;if starting with .org $8000, assembler ends up writing it to $0000, which won't work
.org $8000

reset:
    clc
    xce
    rep #$30            ;set 16-bit mode

    ; ******* KEYBOARD *******    init keyboard handling memory **************
    lda #$00
    sta kb_flags
    sta kb_flags2
    sta kb_wptr
    sta kb_rptr
    ; ************************************************************************

    ;VIA config
    ;Set(1)/Clear(0)|Timer1|Timer2|CB1|CB2|ShiftReg|CA1|CA2
    lda #%0000000001111111	        ; Disable all interrupts
    sta VIA1_IER

    lda #%0000000011111111 
    sta VIA1_DDRB           ; Set all pins on port B to output
    sta VIA1_DDRA           ; Set all pins on port A to output

    lda #$FFFE  ;start counting up at this value; higher # = shorter delay
    sta delayDuration

    ; ******* LCD *******
    ;see page 42 of https://eater.net/datasheets/HD44780.pdf
    ;when running 6502 at ~5.0 MHz (versus 1.0 MHz), sometimes init needs additional call or delay
    jsr lcd_init
    lda #%00101000 ; Set 4-bit mode; 2-line display; 5x8 font     ;See page 24 of HD44780.pdf
    jsr lcd_instruction
    ;call again for higher clock speed setup
    lda #%00101000 ; Set 4-bit mode; 2-line display; 5x8 font     ;See page 24 of HD44780.pdf
    jsr lcd_instruction
    lda #%00001110 ; Display on; cursor on; blink off
    jsr lcd_instruction
    lda #%00000110 ; Increment and shift cursor; don't shift display
    jsr lcd_instruction
    lda #%00000001 ; Clear display
    jsr lcd_instruction
    lda #%00001110 ; Display on; cursor on; blink off
    jsr lcd_instruction
    lda #$3E    ;'>'
    jsr print_char_lcd

    jmp loop_label
lcd_wait:
  pha
  lda #%11110000  ; LCD data is input       ;8042
  sta VIA1_DDRB
lcdbusy:
  lda #RW
  sta VIA1_PORTB
  lda #(RW | E)                             ;805A
  sta VIA1_PORTB
  lda VIA1_PORTB       ; Read high nibble
  pha             ; and put on stack since it has the busy flag
  lda #RW
  sta VIA1_PORTB
  lda #(RW | E)
  sta VIA1_PORTB
  lda VIA1_PORTB       ; Read low nibble    ;8074
  pla             ; Get high nibble off stack
  and #%00001000                            
  bne lcdbusy                               ;807C

  lda #RW
  sta VIA1_PORTB
  lda #%11111111  ; LCD data is output
  sta VIA1_DDRB                             ;8088
  pla
  rts
lcd_init:
  ;wait a bit before initializing the screen - helpful at higher 6502 clock speeds
  jsr  Delay

  ;see page 42 of https://eater.net/datasheets/HD44780.pdf
  lda #%00000010 ; Set 4-bit mode
  sta VIA1_PORTB
  jsr  Delay
  ora #E
  sta VIA1_PORTB
  jsr  Delay
  and #%00001111
  sta VIA1_PORTB

  rts
lcd_instruction:
  ;send an instruction to the 2-line LCD
  jsr lcd_wait
  pha
  lsr
  lsr
  lsr
  lsr            ; Send high 4 bits
  sta VIA1_PORTB
  ora #E         ; Set E bit to send instruction
  sta VIA1_PORTB
  eor #E         ; Clear E bit
  sta VIA1_PORTB
  pla
  and #%00001111 ; Send low 4 bits
  sta VIA1_PORTB
  ora #E         ; Set E bit to send instruction
  sta VIA1_PORTB
  eor #E         ; Clear E bit
  sta VIA1_PORTB
  rts
print_char_lcd:
  ;print a character on the 2-line LCD
  jsr lcd_wait
  pha                                       ;80E1
  lsr
  lsr
  lsr
  lsr             ; Send high 4 bits
  ora #RS         ; Set RS
  sta VIA1_PORTB
  ora #E          ; Set E bit to send instruction
  sta VIA1_PORTB
  eor #E          ; Clear E bit
  sta VIA1_PORTB
  pla
  pha
  and #%00001111  ; Send low 4 bits
  ora #RS         ; Set RS
  sta VIA1_PORTB
  ora #E          ; Set E bit to send instruction
  sta VIA1_PORTB
  eor #E          ; Clear E bit
  sta VIA1_PORTB
  pla
  rts
Delay:
    pha       ;save current accumulator
    lda delayDuration	;counter start - increase number to shorten delay
    Delayloop:
        clc
        adc #01
        bne Delayloop
    pla
    rts
key_pressed:
    ;put items on stack, so we can return them
    pha ;a to stack
    phx ;x to stack
    phy ;y to stack

    ldx kb_rptr
    lda kb_buffer, x
    cmp #$0a           ; enter - go to next line
    beq enter_pressed
    cmp #$1b           ; escape - clear display
    beq esc_pressed
    jsr print_char_lcd
    inc kb_rptr
    inc kb_rptr

    ;return items from stack
    ply ;stack to y
    plx ;stack to x
    pla ;stack to a
    bra loop_label
loop_label:
  ;sit here and loop, process key presses via interrupts as they come in
  lda kb_rptr
  cmp kb_wptr
  cli                   ;Clear Interrupt Disable
  bne key_pressed

  ;Handle KB flags
  jmp Handle_KB_flags
enter_pressed:
    ;*** lcd ***
    lda #%10101000 ; put cursor at position 40
    jsr lcd_instruction

    ;*** fpga vga ***
    lda #$0a      ;enter
    sta VIA1_PORTB
    lda #%10000001    ;printchar
    sta VIA1_PORTB
    jsr Delay
    lda #%00000001    ;printchar
    sta VIA1_PORTB

    inc kb_rptr
    jmp loop_label 
esc_pressed:
    ;*** lcd ***
    lda #%00000001 ; Clear display
    jsr lcd_instruction
    inc kb_rptr
    inc kb_rptr
    jmp loop_label
Handle_KB_flags:
  ;TOOD :?: pha   ;remember A

  ;process arrow keys (would not have been handled in code above, as not ASCII codes)
  lda kb_flags

  bit #ARROW_UP   
  bne Handle_Arrow_Up
  
  bit #ARROW_LEFT 
  bne Handle_Arrow_Left
  
  bit #ARROW_RIGHT  
  bne Handle_Arrow_Right

  bit #ARROW_DOWN   
  bne Handle_Arrow_Down

  bit #NKP5      
  bne Handle_NKP5

  bit #NKP_PLUS
  bne Handle_NKP_Plus

  jmp Handle_KB_flags2
Handle_Arrow_Up:
    ;put items on stack, so we can return them
    pha ;a to stack
    phx ;x to stack
    ;phy ;y to stack

    lda kb_flags
    eor #ARROW_UP  ; flip the arrow bit
    sta kb_flags

    ;return items from stack
    ;ply ;stack to y
    plx ;stack to x
    pla ;stack to a
    jmp loop_label
Handle_Arrow_Left:

    pha ;a to stack
    phx ;x to stack
    ;phy ;y to stack

    lda kb_flags
    eor #ARROW_LEFT  ; flip the left arrow bit
    sta kb_flags

    ;return items from stack
    ;ply ;stack to y
    plx ;stack to x
    pla ;stack to a

    jmp loop_label
Handle_Arrow_Right:

    ;put items on stack, so we can return them
    pha ;a to stack
    phx ;x to stack
    ;phy ;y to stack

    lda kb_flags
    eor #ARROW_RIGHT  ; flip the arrow bit
    sta kb_flags

    ;return items from stack
    ;ply ;stack to y
    plx ;stack to x
    pla ;stack to a

    jmp loop_label
Handle_Arrow_Down:

    ;put items on stack, so we can return them
    pha ;a to stack
    phx ;x to stack
    ;phy ;y to stack

    lda kb_flags
    eor #ARROW_DOWN  ; flip the arrow bit
    sta kb_flags

    ;return items from stack
    ;ply ;stack to y
    plx ;stack to x
    pla ;stack to a

    jmp loop_label
Handle_NKP5:
    ;put items on stack, so we can return them
    pha ;a to stack

    lda kb_flags
    eor #NKP5  ; flip the arrow bit
    sta kb_flags

    ;return items from stack
    pla ;stack to a

    jmp loop_label
Handle_NKP_Plus:

      lda kb_flags
      eor #NKP_PLUS  ; flip the left arrow bit
      sta kb_flags
      jmp loop_label
Handle_NKP_Insert:
    lda kb_flags2
    eor #NKP_INSERT
    sta kb_flags2
    jmp loop_label
Handle_KB_flags2:
  lda kb_flags2

  bit #NKP_INSERT
  bne Handle_NKP_Insert

  bit #NKP_DELETE
  bne Handle_NKP_Delete

  bit #NKP_MINUS
  bne Handle_NKP_Minus

  bit #NKP_ASTERISK
  bne Handle_NKP_Asterisk

  bit #PRINTSCREEN
  bne Handle_PrintScreen
  
  jmp loop_label
Handle_NKP_Delete:
    lda kb_flags2
    eor #NKP_DELETE
    sta kb_flags2
    jmp loop_label
Handle_NKP_Minus:
    lda kb_flags2
    eor #NKP_MINUS
    sta kb_flags2
    jmp loop_label
Handle_NKP_Asterisk:
    ;draw region   

    lda kb_flags2
    eor #NKP_ASTERISK
    sta kb_flags2
    jmp loop_label
Handle_PrintScreen:
    ;sei     //turn off interrupts
    lda kb_flags2
    eor #PRINTSCREEN
    sta kb_flags2
    ;cli
    jmp loop_label
irq_label:
  pha ;a to stack
  phx ;x to stack
  phy ;y to stack

  //to do put scan code into buffer
  lda kb_flags
  and #RELEASE   ; check if we're releasing a key
  beq read_key   ; otherwise, read the key

  lda kb_flags
  eor #RELEASE   ; flip the releasing bit
  sta kb_flags

  lda VIA1_PORTA      ; read key value that is being released
  
  cmp #$12       ; left shift
  beq shift_up
  cmp #$59       ; right shift
  beq shift_up 

  jmp irq_done
irq_done:
  ;return items from stack
  ply ;stack to y
  plx ;stack to x
  pla ;stack to a
  rti
shift_up:
  lda kb_flags
  eor #SHIFT  ; flip the shift bit
  sta kb_flags
  jmp irq_done
key_release:
  lda kb_flags
  ora #RELEASE
  sta kb_flags
  jmp irq_done
shift_down:
  lda kb_flags
  ora #SHIFT
  sta kb_flags
  jmp irq_done
arrow_left_down:
  lda kb_flags
  ora #ARROW_LEFT
  sta kb_flags
  jmp irq_done
arrow_up_down:
  lda kb_flags
  ora #ARROW_UP
  sta kb_flags
  jmp irq_done
nkp5_down:
  lda kb_flags
  ora #NKP5
  sta kb_flags
  jmp irq_done
nkpinsert_down:
  lda kb_flags2
  ora #NKP_INSERT
  sta kb_flags2
  jmp irq_done
nkpdelete_down:
  lda kb_flags2
  ora #NKP_DELETE
  sta kb_flags2
  jmp irq_done
nkpminus_down:
  lda kb_flags2
  ora #NKP_MINUS
  sta kb_flags2
  jmp irq_done
nkpasterisk_down:
  lda kb_flags2
  ora #NKP_ASTERISK
  sta kb_flags2
  jmp irq_done
printscreen_down:
  lda kb_flags2
  ora #PRINTSCREEN
  sta kb_flags2
  jmp irq_done
keyscan_ignore:
  jmp irq_done
shifted_key:
  lda keymap_shifted, x   ; map to character code
  ;fall into push_key
push_key:
  ldx kb_wptr
  sta kb_buffer, x
  inc kb_wptr
  inc kb_wptr
  jmp irq_done
read_key:
  lda VIA1_PORTA
  
  ;jsr print_dec_lcd ;***

  ;cmp #$f0        ; if releasing a key
  ;beq key_release ; set the releasing bit
  ;cmp #$12        ; left shift
  ;beq shift_down
  ;cmp #$59        ; right shift
  ;beq shift_down
  ;cmp #$6b           ; left arrow
  ;beq arrow_left_down
  ;cmp #$74           ; right arrow
  ;beq arrow_right_down
  ;cmp #$75           ; up arrow
  ;beq arrow_up_down
  ;cmp #$72           ; down arrow
  ;beq arrow_down_down
  ;cmp #$73           ; numberic keypad '5'
  ;beq nkp5_down
  ;cmp #$79           ; numeric keypad '+'
  ;beq nkpplus_down
  ;cmp #$70           ; numeric keypad insert
  ;beq nkpinsert_down
  ;cmp #$71           ; numeric keypad delete
  ;beq nkpdelete_down
  ;cmp #$7b           ; numeric keypay minus
  ;beq nkpminus_down
  ;cmp #$7c           ; numeric keypad asterisk
  ;beq nkpasterisk_down
  ;cmp #$07           ; F12
  ;beq printscreen_down
  ;cmp #$e0           ;trying to filter out '?' 0xe0 from printscreen key
  ;beq keyscan_ignore

  tax
  lda kb_flags
  and #SHIFT
  bne shifted_key

  lda keymap, x   ; map to character code ;******
  AND #$00FF

  jmp push_key
nkpplus_down:
  lda kb_flags
  ora #NKP_PLUS
  sta kb_flags
  jmp irq_done
arrow_down_down:
  lda kb_flags
  ora #ARROW_DOWN
  sta kb_flags
  jmp irq_done
arrow_right_down:
  lda kb_flags
  ora #ARROW_RIGHT
  sta kb_flags
  jmp irq_done


.org $D000
;Convert keyboard scan codes to ASCII values
; *** Set 1 *** See http://www.vetra.com/scancodes.html
; This set is incomplete and needs full testing.
keymap:
  .byte "??1234567890-=??qwertyuiop[]??asdfghjkl;'`??zxcvbnm,./????????"
  .byte "????????????????????????????????????????????????????????????????"
  .byte "????????????????????????????????????????????????????????????????"
  .byte "????????????????????????????????????????????????????????????????"
keymap_shifted:
  .byte "?!@#$%^&*()_+??QWERTYUIOP{}??ASDFGHJKL:?~?ZXCVBNM<>?????????"
  .byte "????????????????????????????????????????????????????????????????"
  .byte "????????????????????????????????????????????????????????????????"
  .byte "????????????????????????????????????????????????????????????????"


// PS/2 keyboard scan codes -- Set 2 or 3
// keymap:
//   .byte "????????????? `?"                  ; 00-0F
//   .byte "?????q1???zsaw2?"                  ; 10-1F
//   .byte "?cxde43?? vftr5?"          ; 20-2F
//   .byte "?nbhgy6???mju78?"          ; 30-3F
//   .byte "?,kio09??./l;p-?"          ; 40-4F
//   .byte "??'?[=????",$0a,"]?",$5c,"??"    ; 50-5F     orig:"??'?[=????",$0a,"]?\??"   '\' causes issue with retro assembler - swapped out with hex value 5c
//   .byte "?????????1?47???"          ; 60-6F0
//   .byte "0.2568",$1b,"??+3-*9??"    ; 70-7F
//   .byte "????????????????"          ; 80-8F
//   .byte "????????????????"          ; 90-9F
//   .byte "????????????????"          ; A0-AF
//   .byte "????????????????"          ; B0-BF
//   .byte "????????????????"          ; C0-CF
//   .byte "????????????????"          ; D0-DF
//   .byte "????????????????"          ; E0-EF
//   .byte "????????????????"          ; F0-FF
// keymap_shifted:
//   .byte "????????????? ~?"          ; 00-0F
//   .byte "?????Q!???ZSAW@?"          ; 10-1F
//   .byte "?CXDE#$?? VFTR%?"          ; 20-2F
//   .byte "?NBHGY^???MJU&*?"          ; 30-3F
//   .byte "?<KIO)(??>?L:P_?"          ; 40-4F
//   .byte "??",$22,"?{+?????}?|??"          ; 50-5F      orig:"??"?{+?????}?|??"  ;nested quote - compiler doesn't like - swapped out with hex value 22
//   .byte "?????????1?47???"          ; 60-6F
//   .byte "0.2568???+3-*9??"          ; 70-7F
//   .byte "????????????????"          ; 80-8F
//   .byte "????????????????"          ; 90-9F
//   .byte "????????????????"          ; A0-AF
//   .byte "????????????????"          ; B0-BF
//   .byte "????????????????"          ; C0-CF
//   .byte "????????????????"          ; D0-DF
//   .byte "????????????????"          ; E0-EF
//   .byte "????????????????"          ; F0-FF


.org $FFEE
    .word irq_label   //native 16-bit mode interrupt vector

.org $FFFC
    .word reset
    .word irq_label   //emulation interrupt vector