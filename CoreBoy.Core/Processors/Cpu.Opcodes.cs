using System;
using System.Collections.Generic;
using CoreBoy.Core.Utils;
using Microsoft.Extensions.Logging;

namespace CoreBoy.Core.Processors
{
    public sealed partial class Cpu
    {
		private void InitOpcodes()
        {
            opcodeTable = new Dictionary<byte, Action>()
            {
                // No Operation
                { 0x00, () => { } },                                                                        // NOP

                // Stop
                { 0x10, () => { State.Stop = true; } },													    // STOP

                // Halt
                { 0x76, () => { State.Halt = true; } },													    // HALT

                // Disable Interrupts
                { 0xF3, DisableInterrupts },					                                            // DI

                // Enable Interrupts
                { 0xFB, EnableInterrupts },					                                                // EI

                // 8-Bit Loads
                { 0x06, () => { State.Bc.High = ReadByte(State.Pc++); } },                              // LD B, #
                { 0x0E, () => { State.Bc.Low = ReadByte(State.Pc++); } },                               // LD C, #
                { 0x16, () => { State.De.High = ReadByte(State.Pc++); } },                              // LD D, #
                { 0x1E, () => { State.De.Low = ReadByte(State.Pc++); } },                               // LD E, #
                { 0x26, () => { State.Hl.High = ReadByte(State.Pc++); } },                              // LD H, #
                { 0x2E, () => { State.Hl.Low = ReadByte(State.Pc++); } },                               // LD L, #
                { 0x36, () => { WriteByte(State.Hl, ReadByte(State.Pc++)); } },                    // LD (HL), #
                { 0x3E, () => { State.Af.High = ReadByte(State.Pc++); } },                              // LD A, #

                { 0x02, () => { WriteByte(State.Bc, State.Af.High); } },                                // LD (BC), A
                { 0x12, () => { WriteByte(State.De, State.Af.High); } },                                // LD (DE), A
                { 0x0A, () => { State.Af.High = ReadByte(State.Bc); } },                                // LD A, (BC)
                { 0x1A, () => { State.Af.High = ReadByte(State.De); } },                                // LD A, (DE)

                { 0x40, () => { State.Bc.High = State.Bc.High; } },                                         // LD B, B
                { 0x41, () => { State.Bc.High = State.Bc.Low; } },                                          // LD B, C
                { 0x42, () => { State.Bc.High = State.De.High; } },                                         // LD B, D
                { 0x43, () => { State.Bc.High = State.De.Low; } },                                          // LD B, E
                { 0x44, () => { State.Bc.High = State.Hl.High; } },                                         // LD B, H
                { 0x45, () => { State.Bc.High = State.Hl.Low; } },                                          // LD B, L
                { 0x46, () => { State.Bc.High = ReadByte(State.Hl); } },                               // LD B, (HL)
                { 0x47, () => { State.Bc.High = State.Af.High; } },                                         // LD B, A

                { 0x48, () => { State.Bc.Low = State.Bc.High; } },                                          // LD C, B
                { 0x49, () => { State.Bc.Low = State.Bc.Low; } },                                           // LD C, C
                { 0x4A, () => { State.Bc.Low = State.De.High; } },                                          // LD C, D
                { 0x4B, () => { State.Bc.Low = State.De.Low; } },                                           // LD C, E
                { 0x4C, () => { State.Bc.Low = State.Hl.High; } },                                          // LD C, H
                { 0x4D, () => { State.Bc.Low = State.Hl.Low; } },                                           // LD C, L
                { 0x4E, () => { State.Bc.Low = ReadByte(State.Hl); } },                                // LD C, (HL)
                { 0x4F, () => { State.Bc.Low = State.Af.High; } },                                          // LD C, A

                { 0x50, () => { State.De.High = State.Bc.High; } },                                         // LD D, B
                { 0x51, () => { State.De.High = State.Bc.Low; } },                                          // LD D, C
                { 0x52, () => { State.De.High = State.De.High; } },                                         // LD D, D
                { 0x53, () => { State.De.High = State.De.Low; } },                                          // LD D, E
                { 0x54, () => { State.De.High = State.Hl.High; } },                                         // LD D, H
                { 0x55, () => { State.De.High = State.Hl.Low; } },                                          // LD D, L
                { 0x56, () => { State.De.High = ReadByte(State.Hl); } },                               // LD D, (HL)
                { 0x57, () => { State.De.High = State.Af.High; } },                                         // LD D, A

                { 0x58, () => { State.De.Low = State.Bc.High; } },                                          // LD E, B
                { 0x59, () => { State.De.Low = State.Bc.Low; } },                                           // LD E, C
                { 0x5A, () => { State.De.Low = State.De.High; } },                                          // LD E, D
                { 0x5B, () => { State.De.Low = State.De.Low; } },                                           // LD E, E
                { 0x5C, () => { State.De.Low = State.Hl.High; } },                                          // LD E, H
                { 0x5D, () => { State.De.Low = State.Hl.Low; } },                                           // LD E, L
                { 0x5E, () => { State.De.Low = ReadByte(State.Hl); } },                                // LD E, (HL)
                { 0x5F, () => { State.De.Low = State.Af.High; } },                                          // LD E, A

                { 0x60, () => { State.Hl.High = State.Bc.High; } },                                         // LD H, B
                { 0x61, () => { State.Hl.High = State.Bc.Low; } },                                          // LD H, C
                { 0x62, () => { State.Hl.High = State.De.High; } },                                         // LD H, D
                { 0x63, () => { State.Hl.High = State.De.Low; } },                                          // LD H, E
                { 0x64, () => { State.Hl.High = State.Hl.High; } },                                         // LD H, H
                { 0x65, () => { State.Hl.High = State.Hl.Low; } },                                          // LD H, L
                { 0x66, () => { State.Hl.High = ReadByte(State.Hl); } },                               // LD H, (HL)
                { 0x67, () => { State.Hl.High = State.Af.High; } },                                         // LD H, A

                { 0x68, () => { State.Hl.Low = State.Bc.High; } },                                          // LD L, B
                { 0x69, () => { State.Hl.Low = State.Bc.Low; } },                                           // LD L, C
                { 0x6A, () => { State.Hl.Low = State.De.High; } },                                          // LD L, D
                { 0x6B, () => { State.Hl.Low = State.De.Low; } },                                           // LD L, E
                { 0x6C, () => { State.Hl.Low = State.Hl.High; } },                                          // LD L, H
                { 0x6D, () => { State.Hl.Low = State.Hl.Low; } },                                           // LD L, L
                { 0x6E, () => { State.Hl.Low = ReadByte(State.Hl); } },                                // LD L, (HL)
                { 0x6F, () => { State.Hl.Low = State.Af.High; } },                                          // LD L, A

                { 0x70, () => { WriteByte(State.Hl, State.Bc.High); } },                                // LD (HL), B
                { 0x71, () => { WriteByte(State.Hl, State.Bc.Low); } },                                 // LD (HL), C
                { 0x72, () => { WriteByte(State.Hl, State.De.High); } },                                // LD (HL), D
                { 0x73, () => { WriteByte(State.Hl, State.De.Low); } },                                 // LD (HL), E
                { 0x74, () => { WriteByte(State.Hl, State.Hl.High); } },                                // LD (HL), H
                { 0x75, () => { WriteByte(State.Hl, State.Hl.Low); } },                                 // LD (HL), L
                { 0x77, () => { WriteByte(State.Hl, State.Af.High); } },                                // LD (HL), A

                { 0x78, () => { State.Af.High = State.Bc.High; } },                                         // LD A, B
                { 0x79, () => { State.Af.High = State.Bc.Low; } },                                          // LD A, C
                { 0x7A, () => { State.Af.High = State.De.High; } },                                         // LD A, D
                { 0x7B, () => { State.Af.High = State.De.Low; } },                                          // LD A, E
                { 0x7C, () => { State.Af.High = State.Hl.High; } },                                         // LD A, H
                { 0x7D, () => { State.Af.High = State.Hl.Low; } },                                          // LD A, L
                { 0x7E, () => { State.Af.High = ReadByte(State.Hl); } },                                // LD A, (HL)
                { 0x7F, () => { State.Af.High = State.Af.High; } },                                         // LD A, A
                
                { 0xE0, () => { WriteByte((ushort)(0xFF00 + ReadByte(State.Pc++)), State.Af.High); } },     // LDH #, A
                { 0xF0, () => { State.Af.High = ReadByte((ushort)(0xFF00 + ReadByte(State.Pc++))); } },     // LDH A, #
                { 0xE2, () => { WriteByte((ushort)(0xFF00 + State.Bc.Low), State.Af.High); } },           // LD (C), A
                { 0xF2, () => { State.Af.High = ReadByte((ushort)(0xFF00 + State.Bc.Low)); } },           // LD A, (C)
                { 0xEA, () => { WriteByte(ReadWord(State.Pc), State.Af.High); State.Pc += 2; } },       // LD (##), A
                { 0xFA, () => { State.Af.High = ReadByte(ReadWord(State.Pc)); State.Pc += 2; } },       // LD A, (##)

                // 16-Bit Loads
				{ 0x01, () => { State.Bc = ReadWord(State.Pc); State.Pc += 2; } },		                // LD BC, ##
                { 0x11, () => { State.De = ReadWord(State.Pc); State.Pc += 2; } },		                // LD DE, ##
                { 0x21, () => { State.Hl = ReadWord(State.Pc); State.Pc += 2; } },		                // LD HL, ##
                { 0x31, () => { State.Sp = ReadWord(State.Pc); State.Pc += 2; } },		                // LD SP, ##

                { 0x08, () => { WriteWord(ReadWord(State.Pc), State.Sp); State.Pc += 2; } },   // LD (##), SP
                { 0xF8, LoadSpIntoHl},                                                                          // LD HL, SP + #
                { 0xF9, () => { State.Sp = State.Hl; Idle(); } },                                               // LD SP, HL

                // Special Loads
                { 0x22, () => { WriteByte(State.Hl, State.Af.High); State.Hl++; } },			        // LDI (HL), A
                { 0x2A, () => { State.Af.High = ReadByte(State.Hl); State.Hl++; } },			        // LDI A, (HL)
                { 0x32, () => { WriteByte(State.Hl, State.Af.High); State.Hl--; } },			        // LDD (HL), A
                { 0x3A, () => { State.Af.High = ReadByte(State.Hl); State.Hl--; } },			        // LDD A, (HL)            

                // Increments
                { 0x04, () => { State.Bc.High = Increment(State.Bc.High); } },                          // INC B
                { 0x0C, () => { State.Bc.Low = Increment(State.Bc.Low); } },                            // INC C
                { 0x14, () => { State.De.High = Increment(State.De.High); } },                          // INC D
                { 0x1C, () => { State.De.Low = Increment(State.De.Low); } },                            // INC E
                { 0x24, () => { State.Hl.High = Increment(State.Hl.High); } },                          // INC H
                { 0x2C, () => { State.Hl.Low = Increment(State.Hl.Low); } },                            // INC L
                { 0x34, () => { WriteByte(State.Hl, Increment(ReadByte(State.Hl))); } },           // INC (HL)
                { 0x3C, () => { State.Af.High = Increment(State.Af.High); } },                          // INC A

                { 0x03, () => { State.Bc++; Idle(); } },                                                // INC BC
                { 0x13, () => { State.De++; Idle(); } },                                                // INC DE
                { 0x23, () => { State.Hl++; Idle(); } },                                                // INC HL
                { 0x33, () => { State.Sp++; Idle(); } },                                                // INC SP

                // Decrements
                { 0x05, () => { State.Bc.High = Decrement(State.Bc.High); } },                          // DEC B
                { 0x0D, () => { State.Bc.Low = Decrement(State.Bc.Low); } },                            // DEC C
                { 0x15, () => { State.De.High = Decrement(State.De.High); } },                          // DEC D
                { 0x1D, () => { State.De.Low = Decrement(State.De.Low); } },                            // DEC E
                { 0x25, () => { State.Hl.High = Decrement(State.Hl.High); } },                          // DEC H
                { 0x2D, () => { State.Hl.Low = Decrement(State.Hl.Low); } },                            // DEC L
                { 0x35, () => { WriteByte(State.Hl, Decrement(ReadByte(State.Hl))); } },           // DEC (HL)
                { 0x3D, () => { State.Af.High = Decrement(State.Af.High); } },                          // DEC A

                { 0x0B, () => { State.Bc--; Idle(); } },                                                // DEC BC
                { 0x1B, () => { State.De--; Idle(); } },                                                // DEC DE
                { 0x2B, () => { State.Hl--; Idle(); } },                                                // DEC HL
                { 0x3B, () => { State.Sp--; Idle(); } },                                                // DEC SP

                // Jumps
                { 0xC3, () => { Jump(true); } },                                                        // JP ##
                { 0xC2, () => { Jump(!GetFlag(RegisterFlag.Z)); } },                                    // JP NZ, ##
                { 0xCA, () => { Jump(GetFlag(RegisterFlag.Z)); } },                                     // JP Z, ##
                { 0xD2, () => { Jump(!GetFlag(RegisterFlag.C)); } },                                    // JP NC, ##
                { 0xDA, () => { Jump(GetFlag(RegisterFlag.C)); } },                                     // JP C, ##
                { 0xE9, () => { State.Pc = State.Hl; } },                                               // JP (HL)

                // Relative Jumps
                { 0x18, () => { JumpRelative(true); } },                                                // JR #
                { 0x20, () => { JumpRelative(!GetFlag(RegisterFlag.Z)); } },                            // JR NZ, #
                { 0x28, () => { JumpRelative(GetFlag(RegisterFlag.Z)); } },                             // JR Z, #
                { 0x30, () => { JumpRelative(!GetFlag(RegisterFlag.C)); } },                            // JR NC, #
                { 0x38, () => { JumpRelative(GetFlag(RegisterFlag.C)); } },                             // JR C, #

                // Pop
                { 0xC1, () => { Pop(ref State.Bc); } },                                                 // POP BC
                { 0xD1, () => { Pop(ref State.De); } },                                                 // POP DE
                { 0xE1, () => { Pop(ref State.Hl); } },                                                 // POP HL
                { 0xF1, () => { Pop(ref State.Af); } },                                                 // POP AF  

                // Push
                { 0xC5, () => { Push(State.Bc); } },                                                    // PUSH BC
                { 0xD5, () => { Push(State.De); } },                                                    // PUSH DE
                { 0xE5, () => { Push(State.Hl); } },                                                    // PUSH HL
                { 0xF5, () => { Push(State.Af); } },                                                    // PUSH AF  

                // Call
                { 0xCD, () => { Call(true); } },                                                        // CALL ##
                { 0xC4, () => { Call(!GetFlag(RegisterFlag.Z)); } },                                    // CALL NZ, ##
                { 0xCC, () => { Call(GetFlag(RegisterFlag.Z)); } },                                     // CALL Z, ##
                { 0xD4, () => { Call(!GetFlag(RegisterFlag.C)); } },                                    // CALL NC, ##
                { 0xDC, () => { Call(GetFlag(RegisterFlag.C)); } },                                     // CALL C, ##

                // Return
                { 0xC9, Return },                                                                       // RET
                { 0xD9, () => { Return(); EnableInterrupts(); } },                                      // RETI
                { 0xC0, () => { Return(!GetFlag(RegisterFlag.Z)); } },                                  // RET NZ
                { 0xC8, () => { Return(GetFlag(RegisterFlag.Z)); } },                                   // RET Z
                { 0xD0, () => { Return(!GetFlag(RegisterFlag.C)); } },                                  // RET NC
                { 0xD8, () => { Return(GetFlag(RegisterFlag.C)); } },                                   // RET C

                // Reset
                { 0xC7, () => { Reset(0x0000); } },                                               // RST 00
                { 0xCF, () => { Reset(0x0008); } },                                               // RST 08
                { 0xD7, () => { Reset(0x0010); } },                                               // RST 10
                { 0xDF, () => { Reset(0x0018); } },                                               // RST 18
                { 0xE7, () => { Reset(0x0020); } },                                               // RST 20
                { 0xEF, () => { Reset(0x0028); } },                                               // RST 28
                { 0xF7, () => { Reset(0x0030); } },                                               // RST 30
                { 0xFF, () => { Reset(0x0038); } },                                               // RST 38
                
                // Adds
                { 0x80, () => { Add(State.Bc.High, false); } },                                         // ADD A, B
                { 0x81, () => { Add(State.Bc.Low, false); } },                                          // ADD A, C
                { 0x82, () => { Add(State.De.High, false); } },                                         // ADD A, D
                { 0x83, () => { Add(State.De.Low, false); } },                                          // ADD A, E
                { 0x84, () => { Add(State.Hl.High, false); } },                                         // ADD A, H
                { 0x85, () => { Add(State.Hl.Low, false); } },                                          // ADD A, L
                { 0x86, () => { Add(ReadByte(State.Hl), false); } },                               // ADD A, (HL)
                { 0x87, () => { Add(State.Af.High, false); } },                                         // ADD A, A
                { 0xC6, () => { Add(ReadByte(State.Pc++), false); } },                             // ADD A, #

                { 0x88, () => { Add(State.Bc.High, true); } },                                          // ADC A, B
                { 0x89, () => { Add(State.Bc.Low, true); } },                                           // ADC A, C
                { 0x8A, () => { Add(State.De.High, true); } },                                          // ADC A, D
                { 0x8B, () => { Add(State.De.Low, true); } },                                           // ADC A, E
                { 0x8C, () => { Add(State.Hl.High, true); } },                                          // ADC A, H
                { 0x8D, () => { Add(State.Hl.Low, true); } },                                           // ADC A, L
                { 0x8E, () => { Add(ReadByte(State.Hl), true); } },                                // ADC A, (HL)
                { 0x8F, () => { Add(State.Af.High, true); } },                                          // ADC A, A
                { 0xCE, () => { Add(ReadByte(State.Pc++), true); } },                              // ADC A, #

                { 0x09, () => { Add(ref State.Hl, State.Bc); } },                                       // ADD HL, BC
                { 0x19, () => { Add(ref State.Hl, State.De); } },                                       // ADD HL, BC
                { 0x29, () => { Add(ref State.Hl, State.Hl); } },                                       // ADD HL, BC
                { 0x39, () => { Add(ref State.Hl, State.Sp); } },                                       // ADD HL, BC
                { 0xE8, () => { Add(ref State.Sp, ReadByte(State.Pc++)); } },                      // ADD SP, #

                // Subtracts
                { 0x90, () => { Subtract(State.Bc.High, false); } },                                    // SUB A, B
                { 0x91, () => { Subtract(State.Bc.Low, false); } },                                     // SUB A, C
                { 0x92, () => { Subtract(State.De.High, false); } },                                    // SUB A, D
                { 0x93, () => { Subtract(State.De.Low, false); } },                                     // SUB A, E
                { 0x94, () => { Subtract(State.Hl.High, false); } },                                    // SUB A, H
                { 0x95, () => { Subtract(State.Hl.Low, false); } },                                     // SUB A, L
                { 0x96, () => { Subtract(ReadByte(State.Hl), false); } },                          // SUB A, (HL)
                { 0x97, () => { Subtract(State.Af.High, false); } },                                    // SUB A, A
                { 0xD6, () => { Subtract(ReadByte(State.Pc++), false); } },                        // SUB A, #

                { 0x98, () => { Subtract(State.Bc.High, true); } },                                     // SBC A, B
                { 0x99, () => { Subtract(State.Bc.Low, true); } },                                      // SBC A, C
                { 0x9A, () => { Subtract(State.De.High, true); } },                                     // SBC A, D
                { 0x9B, () => { Subtract(State.De.Low, true); } },                                      // SBC A, E
                { 0x9C, () => { Subtract(State.Hl.High, true); } },                                     // SBC A, H
                { 0x9D, () => { Subtract(State.Hl.Low, true); } },                                      // SBC A, L
                { 0x9E, () => { Subtract(ReadByte(State.Hl), true); } },                           // SBC A, (HL)
                { 0x9F, () => { Subtract(State.Af.High, true); } },                                     // SBC A, A
                { 0xDE, () => { Subtract(ReadByte(State.Pc++), true); } },                         // SBC A, #

                // Ands
                { 0xA0, () => { And(State.Bc.High); } },                                                // AND B
                { 0xA1, () => { And(State.Bc.Low); } },                                                 // AND C
                { 0xA2, () => { And(State.De.High); } },                                                // AND D
                { 0xA3, () => { And(State.De.Low); } },                                                 // AND E
                { 0xA4, () => { And(State.Hl.High); } },                                                // AND H
                { 0xA5, () => { And(State.Hl.Low); } },                                                 // AND L
                { 0xA6, () => { And(ReadByte(State.Hl)); } },                                           // AND (HL)
                { 0xA7, () => { And(State.Af.High); } },                                                // AND A
                { 0xE6, () => { And(ReadByte(State.Pc++)); } },                                         // AND #

                // Exclusive Ors
                { 0xA8, () => { ExclusiveOr(State.Bc.High); } },                                        // XOR B
                { 0xA9, () => { ExclusiveOr(State.Bc.Low); } },                                         // XOR C
                { 0xAA, () => { ExclusiveOr(State.De.High); } },                                        // XOR D
                { 0xAB, () => { ExclusiveOr(State.De.Low); } },                                         // XOR E
                { 0xAC, () => { ExclusiveOr(State.Hl.High); } },                                        // XOR H
                { 0xAD, () => { ExclusiveOr(State.Hl.Low); } },                                         // XOR L
                { 0xAE, () => { ExclusiveOr(ReadByte(State.Hl)); } },                                   // XOR (HL)
                { 0xAF, () => { ExclusiveOr(State.Af.High); } },                                        // XOR A
                { 0xEE, () => { ExclusiveOr(ReadByte(State.Pc++)); } },                                 // XOR #

                // Ors
                { 0xB0, () => { Or(State.Bc.High); } },                                                 // OR B
                { 0xB1, () => { Or(State.Bc.Low); } },                                                  // OR C
                { 0xB2, () => { Or(State.De.High); } },                                                 // OR D
                { 0xB3, () => { Or(State.De.Low); } },                                                  // OR E
                { 0xB4, () => { Or(State.Hl.High); } },                                                 // OR H
                { 0xB5, () => { Or(State.Hl.Low); } },                                                  // OR L
                { 0xB6, () => { Or(ReadByte(State.Hl)); } },                                            // OR (HL)
                { 0xB7, () => { Or(State.Af.High); } },                                                 // OR A
                { 0xF6, () => { Or(ReadByte(State.Pc++)); } },                                          // OR #

                // Compares
                { 0xB8, () => { Compare(State.Bc.High); } },                                            // CP B
                { 0xB9, () => { Compare(State.Bc.Low); } },                                             // CP C
                { 0xBA, () => { Compare(State.De.High); } },                                            // CP D
                { 0xBB, () => { Compare(State.De.Low); } },                                             // CP E
                { 0xBC, () => { Compare(State.Hl.High); } },                                            // CP H
                { 0xBD, () => { Compare(State.Hl.Low); } },                                             // CP L
                { 0xBE, () => { Compare(ReadByte(State.Hl)); } },                                       // CP (HL)
                { 0xBF, () => { Compare(State.Af.High); } },                                            // CP A
                { 0xFE, () => { Compare(ReadByte(State.Pc++)); } },                                     // CP #

                // Misc
                { 0x27, DecimalAdjustA },                                                                   // DAA
                { 0x2F, ComplementA },                                                                      // CPL
                { 0x3F, ComplementCarryFlag },                                                              // CCF
                { 0x37, SetCarryFlag },                                                                     // SCF
                
                // Rotates
                { 0x07, () => { RotateLeft(ref State.Af.High, true); } },                               // RLCA
                { 0x17, () => { RotateLeft(ref State.Af.High, false); } },                              // RLA
                { 0x0F, () => { RotateRight(ref State.Af.High, true); } },                              // RRCA
                { 0x1F, () => { RotateRight(ref State.Af.High, false); } },                             // RRA

                // Handle CB opcodes
                { 0xCB, () => 
                    {
                        try
                        {
                            // Fetch
                            var opcode = ReadByte(State.Pc++);

                            // Decode
                            var instruction = cbTable[opcode];

                            // Execute
                            instruction();
                        }
                        catch (KeyNotFoundException e)
                        {
                            throw new MissingOpcodeException($"Unimplemented cb opcode encountered: {ReadByte(--State.Pc):X2}", e);
                        }
                    }
                },

                // Unused opcodes
                { 0xD3, () => { log.LogError("Unused opcode encountered: D3"); } },
                { 0xDB, () => { log.LogError("Unused opcode encountered: DB"); } },
                { 0xDD, () => { log.LogError("Unused opcode encountered: DD"); } },
                { 0xE3, () => { log.LogError("Unused opcode encountered: E3"); } },
                { 0xE4, () => { log.LogError("Unused opcode encountered: E4"); } },
                { 0xEB, () => { log.LogError("Unused opcode encountered: EB"); } },
                { 0xEC, () => { log.LogError("Unused opcode encountered: EC"); } },
                { 0xED, () => { log.LogError("Unused opcode encountered: ED"); } },
                { 0xF4, () => { log.LogError("Unused opcode encountered: F4"); } },
                { 0xFC, () => { log.LogError("Unused opcode encountered: FC"); } },
                { 0xFD, () => { log.LogError("Unused opcode encountered: FD"); } }
            };

            cbTable = new Dictionary<byte, Action>()
            {
                // Rotate Left
                { 0x00, () => { RotateLeft(ref State.Bc.High, true); } },                               // RLC B
                { 0x01, () => { RotateLeft(ref State.Bc.Low, true); } },                                // RLC C
                { 0x02, () => { RotateLeft(ref State.De.High, true); } },                               // RLC D
                { 0x03, () => { RotateLeft(ref State.De.Low, true); } },                                // RLC E
                { 0x04, () => { RotateLeft(ref State.Hl.High, true); } },                               // RLC H
                { 0x05, () => { RotateLeft(ref State.Hl.Low, true); } },                                // RLC L
                { 0x06, () => { RotateLeft(State.Hl, true); } },                                      // RLC (HL)
                { 0x07, () => { RotateLeft(ref State.Af.High, true); } },                               // RLC A
                                                                                                        
                { 0x10, () => { RotateLeft(ref State.Bc.High, false); } },                              // RL B
                { 0x11, () => { RotateLeft(ref State.Bc.Low, false); } },                               // RL C
                { 0x12, () => { RotateLeft(ref State.De.High, false); } },                              // RL D
                { 0x13, () => { RotateLeft(ref State.De.Low, false); } },                               // RL E
                { 0x14, () => { RotateLeft(ref State.Hl.High, false); } },                              // RL H
                { 0x15, () => { RotateLeft(ref State.Hl.Low, false); } },                               // RL L
                { 0x16, () => { RotateLeft(State.Hl, false); } },                                     // RL (HL)
                { 0x17, () => { RotateLeft(ref State.Af.High, false); } },                              // RL A

                // Rotate Right
                { 0x08, () => { RotateRight(ref State.Bc.High, true); } },                              // RRC B
                { 0x09, () => { RotateRight(ref State.Bc.Low, true); } },                               // RRC C
                { 0x0A, () => { RotateRight(ref State.De.High, true); } },                              // RRC D
                { 0x0B, () => { RotateRight(ref State.De.Low, true); } },                               // RRC E
                { 0x0C, () => { RotateRight(ref State.Hl.High, true); } },                              // RRC H
                { 0x0D, () => { RotateRight(ref State.Hl.Low, true); } },                               // RRC L
                { 0x0E, () => { RotateRight(State.Hl, true); } },                                     // RRC (HL)
                { 0x0F, () => { RotateRight(ref State.Af.High, true); } },                              // RRC A

                { 0x18, () => { RotateRight(ref State.Bc.High, false); } },                             // RR B
                { 0x19, () => { RotateRight(ref State.Bc.Low, false); } },                              // RR C
                { 0x1A, () => { RotateRight(ref State.De.High, false); } },                             // RR D
                { 0x1B, () => { RotateRight(ref State.De.Low, false); } },                              // RR E
                { 0x1C, () => { RotateRight(ref State.Hl.High, false); } },                             // RR H
                { 0x1D, () => { RotateRight(ref State.Hl.Low, false); } },                              // RR L
                { 0x1E, () => { RotateRight(State.Hl, false); } },                                    // RR (HL)
                { 0x1F, () => { RotateRight(ref State.Af.High, false); } },                             // RR A

                // Shift Left
                { 0x20, () => { ShiftLeft(ref State.Bc.High); } },                                          // SLA B
                { 0x21, () => { ShiftLeft(ref State.Bc.Low); } },                                           // SLA C
                { 0x22, () => { ShiftLeft(ref State.De.High); } },                                          // SLA D
                { 0x23, () => { ShiftLeft(ref State.De.Low); } },                                           // SLA E
                { 0x24, () => { ShiftLeft(ref State.Hl.High); } },                                          // SLA H
                { 0x25, () => { ShiftLeft(ref State.Hl.Low); } },                                           // SLA L
                { 0x26, () => { ShiftLeft(State.Hl); } },                                                 // SLA (HL)
                { 0x27, () => { ShiftLeft(ref State.Af.High); } },                                          // SLA A
                
                // Shift Right
                { 0x28, () => { ShiftRight(ref State.Bc.High, true); } },                            // SRA B
                { 0x29, () => { ShiftRight(ref State.Bc.Low, true); } },                             // SRA C
                { 0x2A, () => { ShiftRight(ref State.De.High, true); } },                            // SRA D
                { 0x2B, () => { ShiftRight(ref State.De.Low, true); } },                             // SRA E
                { 0x2C, () => { ShiftRight(ref State.Hl.High, true); } },                            // SRA H
                { 0x2D, () => { ShiftRight(ref State.Hl.Low, true); } },                             // SRA L
                { 0x2E, () => { ShiftRight(State.Hl, true); } },                                   // SRA (HL)
                { 0x2F, () => { ShiftRight(ref State.Af.High, true); } },                            // SRA A
                
                { 0x38, () => { ShiftRight(ref State.Bc.High, false); } },                           // SRL B
                { 0x39, () => { ShiftRight(ref State.Bc.Low, false); } },                            // SRL C
                { 0x3A, () => { ShiftRight(ref State.De.High, false); } },                           // SRL D
                { 0x3B, () => { ShiftRight(ref State.De.Low, false); } },                            // SRL E
                { 0x3C, () => { ShiftRight(ref State.Hl.High, false); } },                           // SRL H
                { 0x3D, () => { ShiftRight(ref State.Hl.Low, false); } },                            // SRL L
                { 0x3E, () => { ShiftRight(State.Hl, false); } },                                  // SRL (HL)
                { 0x3F, () => { ShiftRight(ref State.Af.High, false); } },                           // SRL A
                
                // Swap
                { 0x30, () => { Swap(ref State.Bc.High); } },                                               // SWAP B
                { 0x31, () => { Swap(ref State.Bc.Low); } },                                                // SWAP C
                { 0x32, () => { Swap(ref State.De.High); } },                                               // SWAP D
                { 0x33, () => { Swap(ref State.De.Low); } },                                                // SWAP E
                { 0x34, () => { Swap(ref State.Hl.High); } },                                               // SWAP H
                { 0x35, () => { Swap(ref State.Hl.Low); } },                                                // SWAP L
                { 0x36, () => { Swap(State.Hl); } },                                                      // SWAP (HL)
                { 0x37, () => { Swap(ref State.Af.High); } },                                               // SWAP A
                
                // Bit
                { 0x40, () => { TestBit(State.Bc.High, 0); } },									    // BIT 0, B
                { 0x41, () => { TestBit(State.Bc.Low, 0); } },											// BIT 0, C
                { 0x42, () => { TestBit(State.De.High, 0); } },										// BIT 0, D
                { 0x43, () => { TestBit(State.De.Low, 0); } },											// BIT 0, E
                { 0x44, () => { TestBit(State.Hl.High, 0); } },										// BIT 0, H
                { 0x45, () => { TestBit(State.Hl.Low, 0); } },											// BIT 0, L
                { 0x46, () => { TestBit(ReadByte(State.Hl), 0); } },								// BIT 0, (HL)
                { 0x47, () => { TestBit(State.Af.High, 0); } },										// BIT 0, A

				{ 0x48, () => { TestBit(State.Bc.High, 1); } },										// BIT 1, B
                { 0x49, () => { TestBit(State.Bc.Low, 1); } },											// BIT 1, C
                { 0x4A, () => { TestBit(State.De.High, 1); } },										// BIT 1, D
                { 0x4B, () => { TestBit(State.De.Low, 1); } },											// BIT 1, E
                { 0x4C, () => { TestBit(State.Hl.High, 1); } },										// BIT 1, H
                { 0x4D, () => { TestBit(State.Hl.Low, 1); } },											// BIT 1, L
                { 0x4E, () => { TestBit(ReadByte(State.Hl), 1); } },								// BIT 1, (HL)
                { 0x4F, () => { TestBit(State.Af.High, 1); } },										// BIT 1, A

				{ 0x50, () => { TestBit(State.Bc.High, 2); } },										// BIT 2, B
                { 0x51, () => { TestBit(State.Bc.Low, 2); } },											// BIT 2, C
                { 0x52, () => { TestBit(State.De.High, 2); } },										// BIT 2, D
                { 0x53, () => { TestBit(State.De.Low, 2); } },											// BIT 2, E
                { 0x54, () => { TestBit(State.Hl.High, 2); } },										// BIT 2, H
                { 0x55, () => { TestBit(State.Hl.Low, 2); } },											// BIT 2, L
                { 0x56, () => { TestBit(ReadByte(State.Hl), 2); } },								// BIT 2, (HL)
                { 0x57, () => { TestBit(State.Af.High, 2); } },										// BIT 2, A

				{ 0x58, () => { TestBit(State.Bc.High, 3); } },										// BIT 3, B
                { 0x59, () => { TestBit(State.Bc.Low, 3); } },											// BIT 3, C
                { 0x5A, () => { TestBit(State.De.High, 3); } },										// BIT 3, D
                { 0x5B, () => { TestBit(State.De.Low, 3); } },											// BIT 3, E
                { 0x5C, () => { TestBit(State.Hl.High, 3); } },										// BIT 3, H
                { 0x5D, () => { TestBit(State.Hl.Low, 3); } },											// BIT 3, L
                { 0x5E, () => { TestBit(ReadByte(State.Hl), 3); } },								// BIT 3, (HL)
                { 0x5F, () => { TestBit(State.Af.High, 3); } },										// BIT 3, A

				{ 0x60, () => { TestBit(State.Bc.High, 4); } },										// BIT 4, B
                { 0x61, () => { TestBit(State.Bc.Low, 4); } },											// BIT 4, C
                { 0x62, () => { TestBit(State.De.High, 4); } },										// BIT 4, D
                { 0x63, () => { TestBit(State.De.Low, 4); } },											// BIT 4, E
                { 0x64, () => { TestBit(State.Hl.High, 4); } },										// BIT 4, H
                { 0x65, () => { TestBit(State.Hl.Low, 4); } },											// BIT 4, L
                { 0x66, () => { TestBit(ReadByte(State.Hl), 4); } },								// BIT 4, (HL)
                { 0x67, () => { TestBit(State.Af.High, 4); } },										// BIT 4, A

				{ 0x68, () => { TestBit(State.Bc.High, 5); } },										// BIT 5, B
                { 0x69, () => { TestBit(State.Bc.Low, 5); } },											// BIT 5, C
                { 0x6A, () => { TestBit(State.De.High, 5); } },										// BIT 5, D
                { 0x6B, () => { TestBit(State.De.Low, 5); } },											// BIT 5, E
                { 0x6C, () => { TestBit(State.Hl.High, 5); } },										// BIT 5, H
                { 0x6D, () => { TestBit(State.Hl.Low, 5); } },											// BIT 5, L
                { 0x6E, () => { TestBit(ReadByte(State.Hl), 5); } },								// BIT 5, (HL)
                { 0x6F, () => { TestBit(State.Af.High, 5); } },										// BIT 5, A

				{ 0x70, () => { TestBit(State.Bc.High, 6); } },										// BIT 6, B
                { 0x71, () => { TestBit(State.Bc.Low, 6); } },											// BIT 6, C
                { 0x72, () => { TestBit(State.De.High, 6); } },										// BIT 6, D
                { 0x73, () => { TestBit(State.De.Low, 6); } },											// BIT 6, E
                { 0x74, () => { TestBit(State.Hl.High, 6); } },										// BIT 6, H
                { 0x75, () => { TestBit(State.Hl.Low, 6); } },											// BIT 6, L
                { 0x76, () => { TestBit(ReadByte(State.Hl), 6); } },								// BIT 6, (HL)
                { 0x77, () => { TestBit(State.Af.High, 6); } },										// BIT 6, A

				{ 0x78, () => { TestBit(State.Bc.High, 7); } },										// BIT 7, B
                { 0x79, () => { TestBit(State.Bc.Low, 7); } },											// BIT 7, C
                { 0x7A, () => { TestBit(State.De.High, 7); } },										// BIT 7, D
                { 0x7B, () => { TestBit(State.De.Low, 7); } },											// BIT 7, E
                { 0x7C, () => { TestBit(State.Hl.High, 7); } },										// BIT 7, H
                { 0x7D, () => { TestBit(State.Hl.Low, 7); } },											// BIT 7, L
                { 0x7E, () => { TestBit(ReadByte(State.Hl), 7); } },								// BIT 7, (HL)
                { 0x7F, () => { TestBit(State.Af.High, 7); } },										// BIT 7, A
                
                { 0x80, () => { ResetBit(ref State.Bc.High, 0); } },                                     // RES 0, B
                { 0x81, () => { ResetBit(ref State.Bc.Low, 0); } },                                      // RES 0, C
                { 0x82, () => { ResetBit(ref State.De.High, 0); } },                                     // RES 0, D
                { 0x83, () => { ResetBit(ref State.De.Low, 0); } },                                      // RES 0, E
                { 0x84, () => { ResetBit(ref State.Hl.High, 0); } },                                     // RES 0, H
                { 0x85, () => { ResetBit(ref State.Hl.Low, 0); } },                                      // RES 0, L
                { 0x86, () => { ResetBit(State.Hl, 0); } },                                            // RES 0, (HL)
                { 0x87, () => { ResetBit(ref State.Af.High, 0); } },                                     // RES 0, A
                
                { 0x88, () => { ResetBit(ref State.Bc.High, 1); } },                                     // RES 1, B
                { 0x89, () => { ResetBit(ref State.Bc.Low, 1); } },                                      // RES 1, C
                { 0x8A, () => { ResetBit(ref State.De.High, 1); } },                                     // RES 1, D
                { 0x8B, () => { ResetBit(ref State.De.Low, 1); } },                                      // RES 1, E
                { 0x8C, () => { ResetBit(ref State.Hl.High, 1); } },                                     // RES 1, H
                { 0x8D, () => { ResetBit(ref State.Hl.Low, 1); } },                                      // RES 1, L
                { 0x8E, () => { ResetBit(State.Hl, 1); } },                                            // RES 1, (HL)
                { 0x8F, () => { ResetBit(ref State.Af.High, 1); } },                                     // RES 1, A
                
                { 0x90, () => { ResetBit(ref State.Bc.High, 2); } },                                     // RES 2, B
                { 0x91, () => { ResetBit(ref State.Bc.Low, 2); } },                                      // RES 2, C
                { 0x92, () => { ResetBit(ref State.De.High, 2); } },                                     // RES 2, D
                { 0x93, () => { ResetBit(ref State.De.Low, 2); } },                                      // RES 2, E
                { 0x94, () => { ResetBit(ref State.Hl.High, 2); } },                                     // RES 2, H
                { 0x95, () => { ResetBit(ref State.Hl.Low, 2); } },                                      // RES 2, L
                { 0x96, () => { ResetBit(State.Hl, 2); } },                                            // RES 2, (HL)
                { 0x97, () => { ResetBit(ref State.Af.High, 2); } },                                     // RES 2, A
                
                { 0x98, () => { ResetBit(ref State.Bc.High, 3); } },                                     // RES 3, B
                { 0x99, () => { ResetBit(ref State.Bc.Low, 3); } },                                      // RES 3, C
                { 0x9A, () => { ResetBit(ref State.De.High, 3); } },                                     // RES 3, D
                { 0x9B, () => { ResetBit(ref State.De.Low, 3); } },                                      // RES 3, E
                { 0x9C, () => { ResetBit(ref State.Hl.High, 3); } },                                     // RES 3, H
                { 0x9D, () => { ResetBit(ref State.Hl.Low, 3); } },                                      // RES 3, L
                { 0x9E, () => { ResetBit(State.Hl, 3); } },                                            // RES 3, (HL)
                { 0x9F, () => { ResetBit(ref State.Af.High, 3); } },                                     // RES 3, A
                
                { 0xA0, () => { ResetBit(ref State.Bc.High, 4); } },                                     // RES 4, B
                { 0xA1, () => { ResetBit(ref State.Bc.Low, 4); } },                                      // RES 4, C
                { 0xA2, () => { ResetBit(ref State.De.High, 4); } },                                     // RES 4, D
                { 0xA3, () => { ResetBit(ref State.De.Low, 4); } },                                      // RES 4, E
                { 0xA4, () => { ResetBit(ref State.Hl.High, 4); } },                                     // RES 4, H
                { 0xA5, () => { ResetBit(ref State.Hl.Low, 4); } },                                      // RES 4, L
                { 0xA6, () => { ResetBit(State.Hl, 4); } },                                            // RES 4, (HL)
                { 0xA7, () => { ResetBit(ref State.Af.High, 4); } },                                     // RES 4, A
                
                { 0xA8, () => { ResetBit(ref State.Bc.High, 5); } },                                     // RES 5, B
                { 0xA9, () => { ResetBit(ref State.Bc.Low, 5); } },                                      // RES 5, C
                { 0xAA, () => { ResetBit(ref State.De.High, 5); } },                                     // RES 5, D
                { 0xAB, () => { ResetBit(ref State.De.Low, 5); } },                                      // RES 5, E
                { 0xAC, () => { ResetBit(ref State.Hl.High, 5); } },                                     // RES 5, H
                { 0xAD, () => { ResetBit(ref State.Hl.Low, 5); } },                                      // RES 5, L
                { 0xAE, () => { ResetBit(State.Hl, 5); } },                                            // RES 5, (HL)
                { 0xAF, () => { ResetBit(ref State.Af.High, 5); } },                                     // RES 5, A
                
                { 0xB0, () => { ResetBit(ref State.Bc.High, 6); } },                                     // RES 6, B
                { 0xB1, () => { ResetBit(ref State.Bc.Low, 6); } },                                      // RES 6, C
                { 0xB2, () => { ResetBit(ref State.De.High, 6); } },                                     // RES 6, D
                { 0xB3, () => { ResetBit(ref State.De.Low, 6); } },                                      // RES 6, E
                { 0xB4, () => { ResetBit(ref State.Hl.High, 6); } },                                     // RES 6, H
                { 0xB5, () => { ResetBit(ref State.Hl.Low, 6); } },                                      // RES 6, L
                { 0xB6, () => { ResetBit(State.Hl, 6); } },                                            // RES 6, (HL)
                { 0xB7, () => { ResetBit(ref State.Af.High, 6); } },                                     // RES 6, A
                
                { 0xB8, () => { ResetBit(ref State.Bc.High, 7); } },                                     // RES 7, B
                { 0xB9, () => { ResetBit(ref State.Bc.Low, 7); } },                                      // RES 7, C
                { 0xBA, () => { ResetBit(ref State.De.High, 7); } },                                     // RES 7, D
                { 0xBB, () => { ResetBit(ref State.De.Low, 7); } },                                      // RES 7, E
                { 0xBC, () => { ResetBit(ref State.Hl.High, 7); } },                                     // RES 7, H
                { 0xBD, () => { ResetBit(ref State.Hl.Low, 7); } },                                      // RES 7, L
                { 0xBE, () => { ResetBit(State.Hl, 7); } },                                            // RES 7, (HL)
                { 0xBF, () => { ResetBit(ref State.Af.High, 7); } },                                     // RES 7, A
                
                { 0xC0, () => { SetBit(ref State.Bc.High, 0); } },                                       // SET 0, B
                { 0xC1, () => { SetBit(ref State.Bc.Low, 0); } },                                        // SET 0, C
                { 0xC2, () => { SetBit(ref State.De.High, 0); } },                                       // SET 0, D
                { 0xC3, () => { SetBit(ref State.De.Low, 0); } },                                        // SET 0, E
                { 0xC4, () => { SetBit(ref State.Hl.High, 0); } },                                       // SET 0, H
                { 0xC5, () => { SetBit(ref State.Hl.Low, 0); } },                                        // SET 0, L
                { 0xC6, () => { SetBit(State.Hl, 0); } },                                              // SET 0, (HL)
                { 0xC7, () => { SetBit(ref State.Af.High, 0); } },                                       // SET 0, A
                
                { 0xC8, () => { SetBit(ref State.Bc.High, 1); } },                                       // SET 1, B
                { 0xC9, () => { SetBit(ref State.Bc.Low, 1); } },                                        // SET 1, C
                { 0xCA, () => { SetBit(ref State.De.High, 1); } },                                       // SET 1, D
                { 0xCB, () => { SetBit(ref State.De.Low, 1); } },                                        // SET 1, E
                { 0xCC, () => { SetBit(ref State.Hl.High, 1); } },                                       // SET 1, H
                { 0xCD, () => { SetBit(ref State.Hl.Low, 1); } },                                        // SET 1, L
                { 0xCE, () => { SetBit(State.Hl, 1); } },                                              // SET 1, (HL)
                { 0xCF, () => { SetBit(ref State.Af.High, 1); } },                                       // SET 1, A
                
                { 0xD0, () => { SetBit(ref State.Bc.High, 2); } },                                       // SET 2, B
                { 0xD1, () => { SetBit(ref State.Bc.Low, 2); } },                                        // SET 2, C
                { 0xD2, () => { SetBit(ref State.De.High, 2); } },                                       // SET 2, D
                { 0xD3, () => { SetBit(ref State.De.Low, 2); } },                                        // SET 2, E
                { 0xD4, () => { SetBit(ref State.Hl.High, 2); } },                                       // SET 2, H
                { 0xD5, () => { SetBit(ref State.Hl.Low, 2); } },                                        // SET 2, L
                { 0xD6, () => { SetBit(State.Hl, 2); } },                                              // SET 2, (HL)
                { 0xD7, () => { SetBit(ref State.Af.High, 2); } },                                       // SET 2, A
                
                { 0xD8, () => { SetBit(ref State.Bc.High, 3); } },                                       // SET 3, B
                { 0xD9, () => { SetBit(ref State.Bc.Low, 3); } },                                        // SET 3, C
                { 0xDA, () => { SetBit(ref State.De.High, 3); } },                                       // SET 3, D
                { 0xDB, () => { SetBit(ref State.De.Low, 3); } },                                        // SET 3, E
                { 0xDC, () => { SetBit(ref State.Hl.High, 3); } },                                       // SET 3, H
                { 0xDD, () => { SetBit(ref State.Hl.Low, 3); } },                                        // SET 3, L
                { 0xDE, () => { SetBit(State.Hl, 3); } },                                              // SET 3, (HL)
                { 0xDF, () => { SetBit(ref State.Af.High, 3); } },                                       // SET 3, A
                
                { 0xE0, () => { SetBit(ref State.Bc.High, 4); } },                                       // SET 4, B
                { 0xE1, () => { SetBit(ref State.Bc.Low, 4); } },                                        // SET 4, C
                { 0xE2, () => { SetBit(ref State.De.High, 4); } },                                       // SET 4, D
                { 0xE3, () => { SetBit(ref State.De.Low, 4); } },                                        // SET 4, E
                { 0xE4, () => { SetBit(ref State.Hl.High, 4); } },                                       // SET 4, H
                { 0xE5, () => { SetBit(ref State.Hl.Low, 4); } },                                        // SET 4, L
                { 0xE6, () => { SetBit(State.Hl, 4); } },                                              // SET 4, (HL)
                { 0xE7, () => { SetBit(ref State.Af.High, 4); } },                                       // SET 4, A
                
                { 0xE8, () => { SetBit(ref State.Bc.High, 5); } },                                       // SET 5, B
                { 0xE9, () => { SetBit(ref State.Bc.Low, 5); } },                                        // SET 5, C
                { 0xEA, () => { SetBit(ref State.De.High, 5); } },                                       // SET 5, D
                { 0xEB, () => { SetBit(ref State.De.Low, 5); } },                                        // SET 5, E
                { 0xEC, () => { SetBit(ref State.Hl.High, 5); } },                                       // SET 5, H
                { 0xED, () => { SetBit(ref State.Hl.Low, 5); } },                                        // SET 5, L
                { 0xEE, () => { SetBit(State.Hl, 5); } },                                              // SET 5, (HL)
                { 0xEF, () => { SetBit(ref State.Af.High, 5); } },                                       // SET 5, A
                
                { 0xF0, () => { SetBit(ref State.Bc.High, 6); } },                                       // SET 6, B
                { 0xF1, () => { SetBit(ref State.Bc.Low, 6); } },                                        // SET 6, C
                { 0xF2, () => { SetBit(ref State.De.High, 6); } },                                       // SET 6, D
                { 0xF3, () => { SetBit(ref State.De.Low, 6); } },                                        // SET 6, E
                { 0xF4, () => { SetBit(ref State.Hl.High, 6); } },                                       // SET 6, H
                { 0xF5, () => { SetBit(ref State.Hl.Low, 6); } },                                        // SET 6, L
                { 0xF6, () => { SetBit(State.Hl, 6); } },                                              // SET 6, (HL)
                { 0xF7, () => { SetBit(ref State.Af.High, 6); } },                                       // SET 6, A
                
                { 0xF8, () => { SetBit(ref State.Bc.High, 7); } },                                       // SET 7, B
                { 0xF9, () => { SetBit(ref State.Bc.Low, 7); } },                                        // SET 7, C
                { 0xFA, () => { SetBit(ref State.De.High, 7); } },                                       // SET 7, D
                { 0xFB, () => { SetBit(ref State.De.Low, 7); } },                                        // SET 7, E
                { 0xFC, () => { SetBit(ref State.Hl.High, 7); } },                                       // SET 7, H
                { 0xFD, () => { SetBit(ref State.Hl.Low, 7); } },                                        // SET 7, L
                { 0xFE, () => { SetBit(State.Hl, 7); } },                                              // SET 7, (HL)
                { 0xFF, () => { SetBit(ref State.Af.High, 7); } },                                       // SET 7, A
            };
        }

        private Dictionary<byte, Action> opcodeTable;
        private Dictionary<byte, Action> cbTable;
    }
}
