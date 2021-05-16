using System;
using System.Collections.Generic;
using CoreBoy.Core.Utils;
using Microsoft.Extensions.Logging;

namespace CoreBoy.Core.Processors
{
    public partial class Cpu
    {
		private void InitOpcodes()
        {
            opcodeTable = new Dictionary<byte, Action>()
            {
                // No Operation
                { 0x00, () => { } },                                                                    // NOP

                // Stop
                { 0x10, () => { State.Stop = true; } },													// STOP

                // Halt
                { 0x76, () => { State.Halt = true; } },													// HALT

                // Disable Interrupts
                { 0xF3, () => { DisableInterrupts(); } },					                            // DI

                // Enable Interrupts
                { 0xFB, () => { EnableInterrupts(); } },					                            // EI

                // 8-Bit Loads
                { 0x06, () => { State.BC.High = ReadByte(State.PC++); } },                              // LD B, #
                { 0x0E, () => { State.BC.Low = ReadByte(State.PC++); } },                               // LD C, #
                { 0x16, () => { State.DE.High = ReadByte(State.PC++); } },                              // LD D, #
                { 0x1E, () => { State.DE.Low = ReadByte(State.PC++); } },                               // LD E, #
                { 0x26, () => { State.HL.High = ReadByte(State.PC++); } },                              // LD H, #
                { 0x2E, () => { State.HL.Low = ReadByte(State.PC++); } },                               // LD L, #
                { 0x36, () => { WriteByte(State.HL, ReadByte(State.PC++)); } },                         // LD (HL), #
                { 0x3E, () => { State.AF.High = ReadByte(State.PC++); } },                              // LD A, #

                { 0x02, () => { WriteByte(State.BC, State.AF.High); } },                                // LD (BC), A
                { 0x12, () => { WriteByte(State.DE, State.AF.High); } },                                // LD (DE), A
                { 0x0A, () => { State.AF.High = ReadByte(State.BC); } },                                // LD A, (BC)
                { 0x1A, () => { State.AF.High = ReadByte(State.DE); } },                                // LD A, (DE)

                { 0x40, () => { State.BC.High = State.BC.High; } },                                     // LD B, B
                { 0x41, () => { State.BC.High = State.BC.Low; } },                                      // LD B, C
                { 0x42, () => { State.BC.High = State.DE.High; } },                                     // LD B, D
                { 0x43, () => { State.BC.High = State.DE.Low; } },                                      // LD B, E
                { 0x44, () => { State.BC.High = State.HL.High; } },                                     // LD B, H
                { 0x45, () => { State.BC.High = State.HL.Low; } },                                      // LD B, L
                { 0x46, () => { State.BC.High = ReadByte(State.HL); } },                                // LD B, (HL)
                { 0x47, () => { State.BC.High = State.AF.High; } },                                     // LD B, A

                { 0x48, () => { State.BC.Low = State.BC.High; } },                                      // LD C, B
                { 0x49, () => { State.BC.Low = State.BC.Low; } },                                       // LD C, C
                { 0x4A, () => { State.BC.Low = State.DE.High; } },                                      // LD C, D
                { 0x4B, () => { State.BC.Low = State.DE.Low; } },                                       // LD C, E
                { 0x4C, () => { State.BC.Low = State.HL.High; } },                                      // LD C, H
                { 0x4D, () => { State.BC.Low = State.HL.Low; } },                                       // LD C, L
                { 0x4E, () => { State.BC.Low = ReadByte(State.HL); } },                                 // LD C, (HL)
                { 0x4F, () => { State.BC.Low = State.AF.High; } },                                      // LD C, A

                { 0x50, () => { State.DE.High = State.BC.High; } },                                     // LD D, B
                { 0x51, () => { State.DE.High = State.BC.Low; } },                                      // LD D, C
                { 0x52, () => { State.DE.High = State.DE.High; } },                                     // LD D, D
                { 0x53, () => { State.DE.High = State.DE.Low; } },                                      // LD D, E
                { 0x54, () => { State.DE.High = State.HL.High; } },                                     // LD D, H
                { 0x55, () => { State.DE.High = State.HL.Low; } },                                      // LD D, L
                { 0x56, () => { State.DE.High = ReadByte(State.HL); } },                                // LD D, (HL)
                { 0x57, () => { State.DE.High = State.AF.High; } },                                     // LD D, A

                { 0x58, () => { State.DE.Low = State.BC.High; } },                                      // LD E, B
                { 0x59, () => { State.DE.Low = State.BC.Low; } },                                       // LD E, C
                { 0x5A, () => { State.DE.Low = State.DE.High; } },                                      // LD E, D
                { 0x5B, () => { State.DE.Low = State.DE.Low; } },                                       // LD E, E
                { 0x5C, () => { State.DE.Low = State.HL.High; } },                                      // LD E, H
                { 0x5D, () => { State.DE.Low = State.HL.Low; } },                                       // LD E, L
                { 0x5E, () => { State.DE.Low = ReadByte(State.HL); } },                                 // LD E, (HL)
                { 0x5F, () => { State.DE.Low = State.AF.High; } },                                      // LD E, A

                { 0x60, () => { State.HL.High = State.BC.High; } },                                     // LD H, B
                { 0x61, () => { State.HL.High = State.BC.Low; } },                                      // LD H, C
                { 0x62, () => { State.HL.High = State.DE.High; } },                                     // LD H, D
                { 0x63, () => { State.HL.High = State.DE.Low; } },                                      // LD H, E
                { 0x64, () => { State.HL.High = State.HL.High; } },                                     // LD H, H
                { 0x65, () => { State.HL.High = State.HL.Low; } },                                      // LD H, L
                { 0x66, () => { State.HL.High = ReadByte(State.HL); } },                                // LD H, (HL)
                { 0x67, () => { State.HL.High = State.AF.High; } },                                     // LD H, A

                { 0x68, () => { State.HL.Low = State.BC.High; } },                                      // LD L, B
                { 0x69, () => { State.HL.Low = State.BC.Low; } },                                       // LD L, C
                { 0x6A, () => { State.HL.Low = State.DE.High; } },                                      // LD L, D
                { 0x6B, () => { State.HL.Low = State.DE.Low; } },                                       // LD L, E
                { 0x6C, () => { State.HL.Low = State.HL.High; } },                                      // LD L, H
                { 0x6D, () => { State.HL.Low = State.HL.Low; } },                                       // LD L, L
                { 0x6E, () => { State.HL.Low = ReadByte(State.HL); } },                                 // LD L, (HL)
                { 0x6F, () => { State.HL.Low = State.AF.High; } },                                      // LD L, A

                { 0x70, () => { WriteByte(State.HL, State.BC.High); } },                                // LD (HL), B
                { 0x71, () => { WriteByte(State.HL, State.BC.Low); } },                                 // LD (HL), C
                { 0x72, () => { WriteByte(State.HL, State.DE.High); } },                                // LD (HL), D
                { 0x73, () => { WriteByte(State.HL, State.DE.Low); } },                                 // LD (HL), E
                { 0x74, () => { WriteByte(State.HL, State.HL.High); } },                                // LD (HL), H
                { 0x75, () => { WriteByte(State.HL, State.HL.Low); } },                                 // LD (HL), L
                { 0x77, () => { WriteByte(State.HL, State.AF.High); } },                                // LD (HL), A

                { 0x78, () => { State.AF.High = State.BC.High; } },                                     // LD A, B
                { 0x79, () => { State.AF.High = State.BC.Low; } },                                      // LD A, C
                { 0x7A, () => { State.AF.High = State.DE.High; } },                                     // LD A, D
                { 0x7B, () => { State.AF.High = State.DE.Low; } },                                      // LD A, E
                { 0x7C, () => { State.AF.High = State.HL.High; } },                                     // LD A, H
                { 0x7D, () => { State.AF.High = State.HL.Low; } },                                      // LD A, L
                { 0x7E, () => { State.AF.High = ReadByte(State.HL); } },                                // LD A, (HL)
                { 0x7F, () => { State.AF.High = State.AF.High; } },                                     // LD A, A
                
                { 0xE0, () => { WriteByte((ushort)(0xFF00 + ReadByte(State.PC++)), State.AF.High); } }, // LDH #, A
                { 0xF0, () => { State.AF.High = ReadByte((ushort)(0xFF00 + ReadByte(State.PC++))); } }, // LDH A, #
                { 0xE2, () => { WriteByte((ushort)(0xFF00 + State.BC.Low), State.AF.High); } },         // LD (C), A
                { 0xF2, () => { State.AF.High = ReadByte((ushort)(0xFF00 + State.BC.Low)); } },         // LD A, (C)
                { 0xEA, () => { WriteByte(ReadWord(State.PC), State.AF.High); State.PC += 2; } },       // LD (##), A
                { 0xFA, () => { State.AF.High = ReadByte(ReadWord(State.PC)); State.PC += 2; } },       // LD A, (##)

                // 16-Bit Loads
				{ 0x01, () => { State.BC = ReadWord(State.PC); State.PC += 2; } },		                // LD BC, ##
                { 0x11, () => { State.DE = ReadWord(State.PC); State.PC += 2; } },		                // LD DE, ##
                { 0x21, () => { State.HL = ReadWord(State.PC); State.PC += 2; } },		                // LD HL, ##
                { 0x31, () => { State.SP = ReadWord(State.PC); State.PC += 2; } },		                // LD SP, ##

                { 0x08, () => { WriteWord(ReadWord(State.PC), State.SP); State.PC += 2; } },            // LD (##), SP
                { 0xF8, () => { LoadSPIntoHL(); } },                                                    // LD HL, SP + #
                { 0xF9, () => { State.SP = State.HL; Idle(); } },                                       // LD SP, HL

                // Special Loads
                { 0x22, () => { WriteByte(State.HL, State.AF.High); State.HL++; } },			        // LDI (HL), A
                { 0x2A, () => { State.AF.High = ReadByte(State.HL); State.HL++; } },			        // LDI A, (HL)
                { 0x32, () => { WriteByte(State.HL, State.AF.High); State.HL--; } },			        // LDD (HL), A
                { 0x3A, () => { State.AF.High = ReadByte(State.HL); State.HL--; } },			        // LDD A, (HL)            

                // Increments
                { 0x04, () => { State.BC.High = Increment(State.BC.High); } },                          // INC B
                { 0x0C, () => { State.BC.Low = Increment(State.BC.Low); } },                            // INC C
                { 0x14, () => { State.DE.High = Increment(State.DE.High); } },                          // INC D
                { 0x1C, () => { State.DE.Low = Increment(State.DE.Low); } },                            // INC E
                { 0x24, () => { State.HL.High = Increment(State.HL.High); } },                          // INC H
                { 0x2C, () => { State.HL.Low = Increment(State.HL.Low); } },                            // INC L
                { 0x34, () => { WriteByte(State.HL, Increment(ReadByte(State.HL))); } },                // INC (HL)
                { 0x3C, () => { State.AF.High = Increment(State.AF.High); } },                          // INC A

                { 0x03, () => { State.BC++; Idle(); } },                                                // INC BC
                { 0x13, () => { State.DE++; Idle(); } },                                                // INC DE
                { 0x23, () => { State.HL++; Idle(); } },                                                // INC HL
                { 0x33, () => { State.SP++; Idle(); } },                                                // INC SP

                // Decrements
                { 0x05, () => { State.BC.High = Decrement(State.BC.High); } },                          // DEC B
                { 0x0D, () => { State.BC.Low = Decrement(State.BC.Low); } },                            // DEC C
                { 0x15, () => { State.DE.High = Decrement(State.DE.High); } },                          // DEC D
                { 0x1D, () => { State.DE.Low = Decrement(State.DE.Low); } },                            // DEC E
                { 0x25, () => { State.HL.High = Decrement(State.HL.High); } },                          // DEC H
                { 0x2D, () => { State.HL.Low = Decrement(State.HL.Low); } },                            // DEC L
                { 0x35, () => { WriteByte(State.HL, Decrement(ReadByte(State.HL))); } },                // DEC (HL)
                { 0x3D, () => { State.AF.High = Decrement(State.AF.High); } },                          // DEC A

                { 0x0B, () => { State.BC--; Idle(); } },                                                // DEC BC
                { 0x1B, () => { State.DE--; Idle(); } },                                                // DEC DE
                { 0x2B, () => { State.HL--; Idle(); } },                                                // DEC HL
                { 0x3B, () => { State.SP--; Idle(); } },                                                // DEC SP

                // Jumps
                { 0xC3, () => { Jump(true); } },                                                        // JP ##
                { 0xC2, () => { Jump(!GetFlag(RegisterFlag.Z)); } },                                    // JP NZ, ##
                { 0xCA, () => { Jump(GetFlag(RegisterFlag.Z)); } },                                     // JP Z, ##
                { 0xD2, () => { Jump(!GetFlag(RegisterFlag.C)); } },                                    // JP NC, ##
                { 0xDA, () => { Jump(GetFlag(RegisterFlag.C)); } },                                     // JP C, ##
                { 0xE9, () => { State.PC = State.HL; } },                                               // JP (HL)

                // Realtive Jumps
                { 0x18, () => { JumpRelative(true); } },                                                // JR #
                { 0x20, () => { JumpRelative(!GetFlag(RegisterFlag.Z)); } },                            // JR NZ, #
                { 0x28, () => { JumpRelative(GetFlag(RegisterFlag.Z)); } },                             // JR Z, #
                { 0x30, () => { JumpRelative(!GetFlag(RegisterFlag.C)); } },                            // JR NC, #
                { 0x38, () => { JumpRelative(GetFlag(RegisterFlag.C)); } },                             // JR C, #

                // Pop
                { 0xC1, () => { Pop(ref State.BC); } },                                                 // POP BC
                { 0xD1, () => { Pop(ref State.DE); } },                                                 // POP DE
                { 0xE1, () => { Pop(ref State.HL); } },                                                 // POP HL
                { 0xF1, () => { Pop(ref State.AF); } },                                                 // POP AF  

                // Push
                { 0xC5, () => { Push(State.BC); } },                                                    // PUSH BC
                { 0xD5, () => { Push(State.DE); } },                                                    // PUSH DE
                { 0xE5, () => { Push(State.HL); } },                                                    // PUSH HL
                { 0xF5, () => { Push(State.AF); } },                                                    // PUSH AF  

                // Call
                { 0xCD, () => { Call(true); } },                                                        // CALL ##
                { 0xC4, () => { Call(!GetFlag(RegisterFlag.Z)); } },                                    // CALL NZ, ##
                { 0xCC, () => { Call(GetFlag(RegisterFlag.Z)); } },                                     // CALL Z, ##
                { 0xD4, () => { Call(!GetFlag(RegisterFlag.C)); } },                                    // CALL NC, ##
                { 0xDC, () => { Call(GetFlag(RegisterFlag.C)); } },                                     // CALL C, ##

                // Return
                { 0xC9, () => { Return(); } },                                                          // RET
                { 0xD9, () => { Return(); EnableInterrupts(); } },                                      // RETI
                { 0xC0, () => { Return(!GetFlag(RegisterFlag.Z)); } },                                  // RET NZ
                { 0xC8, () => { Return(GetFlag(RegisterFlag.Z)); } },                                   // RET Z
                { 0xD0, () => { Return(!GetFlag(RegisterFlag.C)); } },                                  // RET NC
                { 0xD8, () => { Return(GetFlag(RegisterFlag.C)); } },                                   // RET C

                // Adds
                { 0x80, () => { Add(State.BC.High, false); } },                                         // ADD A, B
                { 0x81, () => { Add(State.BC.Low, false); } },                                          // ADD A, C
                { 0x82, () => { Add(State.DE.High, false); } },                                         // ADD A, D
                { 0x83, () => { Add(State.DE.Low, false); } },                                          // ADD A, E
                { 0x84, () => { Add(State.HL.High, false); } },                                         // ADD A, H
                { 0x85, () => { Add(State.HL.Low, false); } },                                          // ADD A, L
                { 0x86, () => { Add(ReadByte(State.HL), false); } },                                    // ADD A, (HL)
                { 0x87, () => { Add(State.AF.High, false); } },                                         // ADD A, A
                { 0xC6, () => { Add(ReadByte(State.PC++), false); } },                                  // ADD A, #

                { 0x88, () => { Add(State.BC.High, true); } },                                          // ADC A, B
                { 0x89, () => { Add(State.BC.Low, true); } },                                           // ADC A, C
                { 0x8A, () => { Add(State.DE.High, true); } },                                          // ADC A, D
                { 0x8B, () => { Add(State.DE.Low, true); } },                                           // ADC A, E
                { 0x8C, () => { Add(State.HL.High, true); } },                                          // ADC A, H
                { 0x8D, () => { Add(State.HL.Low, true); } },                                           // ADC A, L
                { 0x8E, () => { Add(ReadByte(State.HL), true); } },                                     // ADC A, (HL)
                { 0x8F, () => { Add(State.AF.High, true); } },                                          // ADC A, A
                { 0xCE, () => { Add(ReadByte(State.PC++), true); } },                                   // ADC A, #

                { 0x09, () => { Add(ref State.HL, State.BC); } },                                       // ADD HL, BC
                { 0x19, () => { Add(ref State.HL, State.DE); } },                                       // ADD HL, BC
                { 0x29, () => { Add(ref State.HL, State.HL); } },                                       // ADD HL, BC
                { 0x39, () => { Add(ref State.HL, State.SP); } },                                       // ADD HL, BC
                { 0xE8, () => { Add(ref State.SP, ReadByte(State.PC++)); } },                           // ADD SP, #

                // Subtracts
                { 0x90, () => { Subtract(State.BC.High, false); } },                                    // SUB A, B
                { 0x91, () => { Subtract(State.BC.Low, false); } },                                     // SUB A, C
                { 0x92, () => { Subtract(State.DE.High, false); } },                                    // SUB A, D
                { 0x93, () => { Subtract(State.DE.Low, false); } },                                     // SUB A, E
                { 0x94, () => { Subtract(State.HL.High, false); } },                                    // SUB A, H
                { 0x95, () => { Subtract(State.HL.Low, false); } },                                     // SUB A, L
                { 0x96, () => { Subtract(ReadByte(State.HL), false); } },                               // SUB A, (HL)
                { 0x97, () => { Subtract(State.AF.High, false); } },                                    // SUB A, A
                { 0xD6, () => { Subtract(ReadByte(State.PC++), false); } },                             // SUB A, #

                { 0x98, () => { Subtract(State.BC.High, true); } },                                     // SBC A, B
                { 0x99, () => { Subtract(State.BC.Low, true); } },                                      // SBC A, C
                { 0x9A, () => { Subtract(State.DE.High, true); } },                                     // SBC A, D
                { 0x9B, () => { Subtract(State.DE.Low, true); } },                                      // SBC A, E
                { 0x9C, () => { Subtract(State.HL.High, true); } },                                     // SBC A, H
                { 0x9D, () => { Subtract(State.HL.Low, true); } },                                      // SBC A, L
                { 0x9E, () => { Subtract(ReadByte(State.HL), true); } },                                // SBC A, (HL)
                { 0x9F, () => { Subtract(State.AF.High, true); } },                                     // SBC A, A
                { 0xDE, () => { Subtract(ReadByte(State.PC++), true); } },                              // SBC A, #

                // Ands
                { 0xA0, () => { And(State.BC.High); } },                                                // AND B
                { 0xA1, () => { And(State.BC.Low); } },                                                 // AND C
                { 0xA2, () => { And(State.DE.High); } },                                                // AND D
                { 0xA3, () => { And(State.DE.Low); } },                                                 // AND E
                { 0xA4, () => { And(State.HL.High); } },                                                // AND H
                { 0xA5, () => { And(State.HL.Low); } },                                                 // AND L
                { 0xA6, () => { And(ReadByte(State.HL)); } },                                           // AND (HL)
                { 0xA7, () => { And(State.AF.High); } },                                                // AND A
                { 0xE6, () => { And(ReadByte(State.PC++)); } },                                         // AND #

                // Exclusive Ors
                { 0xA8, () => { ExclusiveOr(State.BC.High); } },                                        // XOR B
                { 0xA9, () => { ExclusiveOr(State.BC.Low); } },                                         // XOR C
                { 0xAA, () => { ExclusiveOr(State.DE.High); } },                                        // XOR D
                { 0xAB, () => { ExclusiveOr(State.DE.Low); } },                                         // XOR E
                { 0xAC, () => { ExclusiveOr(State.HL.High); } },                                        // XOR H
                { 0xAD, () => { ExclusiveOr(State.HL.Low); } },                                         // XOR L
                { 0xAE, () => { ExclusiveOr(ReadByte(State.HL)); } },                                   // XOR (HL)
                { 0xAF, () => { ExclusiveOr(State.AF.High); } },                                        // XOR A
                { 0xEE, () => { ExclusiveOr(ReadByte(State.PC++)); } },                                 // XOR #

                // Ors
                { 0xB0, () => { Or(State.BC.High); } },                                                 // OR B
                { 0xB1, () => { Or(State.BC.Low); } },                                                  // OR C
                { 0xB2, () => { Or(State.DE.High); } },                                                 // OR D
                { 0xB3, () => { Or(State.DE.Low); } },                                                  // OR E
                { 0xB4, () => { Or(State.HL.High); } },                                                 // OR H
                { 0xB5, () => { Or(State.HL.Low); } },                                                  // OR L
                { 0xB6, () => { Or(ReadByte(State.HL)); } },                                            // OR (HL)
                { 0xB7, () => { Or(State.AF.High); } },                                                 // OR A
                { 0xF6, () => { Or(ReadByte(State.PC++)); } },                                          // OR #

                // Compares
                { 0xB8, () => { Compare(State.BC.High); } },                                            // CP B
                { 0xB9, () => { Compare(State.BC.Low); } },                                             // CP C
                { 0xBA, () => { Compare(State.DE.High); } },                                            // CP D
                { 0xBB, () => { Compare(State.DE.Low); } },                                             // CP E
                { 0xBC, () => { Compare(State.HL.High); } },                                            // CP H
                { 0xBD, () => { Compare(State.HL.Low); } },                                             // CP L
                { 0xBE, () => { Compare(ReadByte(State.HL)); } },                                       // CP (HL)
                { 0xBF, () => { Compare(State.AF.High); } },                                            // CP A
                { 0xFE, () => { Compare(ReadByte(State.PC++)); } },                                     // CP #

                // Rotates
                { 0x07, () => { RotateLeft(ref State.AF.High, true); } },                               // RLCA
                { 0x17, () => { RotateLeft(ref State.AF.High, false); } },                              // RLA
                { 0x0F, () => { RotateRight(ref State.AF.High, true); } },                              // RRCA
                { 0x1F, () => { RotateRight(ref State.AF.High, false); } },                             // RRA

                // Handle CB opcodes
                { 0xCB, () => 
                    {
                        try
                        {
                            // Fetch
                            var opcode = ReadByte(State.PC++);

                            // Decode
                            var instruction = cbTable[opcode];

                            // Execute
                            instruction();
                        }
                        catch (KeyNotFoundException e)
                        {
                            throw new MissingOpcodeException($"Unimplemented cb opcode encountered: {ReadByte(--State.PC):X2}", e);
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
                { 0x00, () => { RotateLeft(ref State.BC.High, true); } },                               // RLC B
                { 0x01, () => { RotateLeft(ref State.BC.Low, true); } },                                // RLC C
                { 0x02, () => { RotateLeft(ref State.DE.High, true); } },                               // RLC D
                { 0x03, () => { RotateLeft(ref State.DE.Low, true); } },                                // RLC E
                { 0x04, () => { RotateLeft(ref State.HL.High, true); } },                               // RLC H
                { 0x05, () => { RotateLeft(ref State.HL.Low, true); } },                                // RLC L
                { 0x06, () => { RotateLeft(State.HL, true); } },                                        // RLC (HL)
                { 0x07, () => { RotateLeft(ref State.AF.High, true); } },                               // RLC A
                                                                                                        
                { 0x10, () => { RotateLeft(ref State.BC.High, false); } },                              // RL B
                { 0x11, () => { RotateLeft(ref State.BC.Low, false); } },                               // RL C
                { 0x12, () => { RotateLeft(ref State.DE.High, false); } },                              // RL D
                { 0x13, () => { RotateLeft(ref State.DE.Low, false); } },                               // RL E
                { 0x14, () => { RotateLeft(ref State.HL.High, false); } },                              // RL H
                { 0x15, () => { RotateLeft(ref State.HL.Low, false); } },                               // RL L
                { 0x16, () => { RotateLeft(State.HL, false); } },                                       // RL (HL)
                { 0x17, () => { RotateLeft(ref State.AF.High, false); } },                              // RL A

                // Rotate Right
                { 0x08, () => { RotateRight(ref State.BC.High, true); } },                              // RRC B
                { 0x09, () => { RotateRight(ref State.BC.Low, true); } },                               // RRC C
                { 0x0A, () => { RotateRight(ref State.DE.High, true); } },                              // RRC D
                { 0x0B, () => { RotateRight(ref State.DE.Low, true); } },                               // RRC E
                { 0x0C, () => { RotateRight(ref State.HL.High, true); } },                              // RRC H
                { 0x0D, () => { RotateRight(ref State.HL.Low, true); } },                               // RRC L
                { 0x0E, () => { RotateRight(State.HL, true); } },                                       // RRC (HL)
                { 0x0F, () => { RotateRight(ref State.AF.High, true); } },                              // RRC A

                { 0x18, () => { RotateRight(ref State.BC.High, false); } },                             // RR B
                { 0x19, () => { RotateRight(ref State.BC.Low, false); } },                              // RR C
                { 0x1A, () => { RotateRight(ref State.DE.High, false); } },                             // RR D
                { 0x1B, () => { RotateRight(ref State.DE.Low, false); } },                              // RR E
                { 0x1C, () => { RotateRight(ref State.HL.High, false); } },                             // RR H
                { 0x1D, () => { RotateRight(ref State.HL.Low, false); } },                              // RR L
                { 0x1E, () => { RotateRight(State.HL, false); } },                                      // RR (HL)
                { 0x1F, () => { RotateRight(ref State.AF.High, false); } },                             // RR A

                // Bit
                { 0x40, () => { TestBit(State.BC.High, 0); } },											// BIT 0, B
                { 0x41, () => { TestBit(State.BC.Low, 0); } },											// BIT 0, C
                { 0x42, () => { TestBit(State.DE.High, 0); } },											// BIT 0, D
                { 0x43, () => { TestBit(State.DE.Low, 0); } },											// BIT 0, E
                { 0x44, () => { TestBit(State.HL.High, 0); } },											// BIT 0, H
                { 0x45, () => { TestBit(State.HL.Low, 0); } },											// BIT 0, L
                { 0x46, () => { TestBit(ReadByte(State.HL), 0); } },								    // BIT 0, (HL)
                { 0x47, () => { TestBit(State.AF.High, 0); } },											// BIT 0, A

				{ 0x48, () => { TestBit(State.BC.High, 1); } },											// BIT 1, B
                { 0x49, () => { TestBit(State.BC.Low, 1); } },											// BIT 1, C
                { 0x4A, () => { TestBit(State.DE.High, 1); } },											// BIT 1, D
                { 0x4B, () => { TestBit(State.DE.Low, 1); } },											// BIT 1, E
                { 0x4C, () => { TestBit(State.HL.High, 1); } },											// BIT 1, H
                { 0x4D, () => { TestBit(State.HL.Low, 1); } },											// BIT 1, L
                { 0x4E, () => { TestBit(ReadByte(State.HL), 1); } },								    // BIT 1, (HL)
                { 0x4F, () => { TestBit(State.AF.High, 1); } },											// BIT 1, A

				{ 0x50, () => { TestBit(State.BC.High, 2); } },											// BIT 2, B
                { 0x51, () => { TestBit(State.BC.Low, 2); } },											// BIT 2, C
                { 0x52, () => { TestBit(State.DE.High, 2); } },											// BIT 2, D
                { 0x53, () => { TestBit(State.DE.Low, 2); } },											// BIT 2, E
                { 0x54, () => { TestBit(State.HL.High, 2); } },											// BIT 2, H
                { 0x55, () => { TestBit(State.HL.Low, 2); } },											// BIT 2, L
                { 0x56, () => { TestBit(ReadByte(State.HL), 2); } },								    // BIT 2, (HL)
                { 0x57, () => { TestBit(State.AF.High, 2); } },											// BIT 2, A

				{ 0x58, () => { TestBit(State.BC.High, 3); } },											// BIT 3, B
                { 0x59, () => { TestBit(State.BC.Low, 3); } },											// BIT 3, C
                { 0x5A, () => { TestBit(State.DE.High, 3); } },											// BIT 3, D
                { 0x5B, () => { TestBit(State.DE.Low, 3); } },											// BIT 3, E
                { 0x5C, () => { TestBit(State.HL.High, 3); } },											// BIT 3, H
                { 0x5D, () => { TestBit(State.HL.Low, 3); } },											// BIT 3, L
                { 0x5E, () => { TestBit(ReadByte(State.HL), 3); } },								    // BIT 3, (HL)
                { 0x5F, () => { TestBit(State.AF.High, 3); } },											// BIT 3, A

				{ 0x60, () => { TestBit(State.BC.High, 4); } },											// BIT 4, B
                { 0x61, () => { TestBit(State.BC.Low, 4); } },											// BIT 4, C
                { 0x62, () => { TestBit(State.DE.High, 4); } },											// BIT 4, D
                { 0x63, () => { TestBit(State.DE.Low, 4); } },											// BIT 4, E
                { 0x64, () => { TestBit(State.HL.High, 4); } },											// BIT 4, H
                { 0x65, () => { TestBit(State.HL.Low, 4); } },											// BIT 4, L
                { 0x66, () => { TestBit(ReadByte(State.HL), 4); } },								    // BIT 4, (HL)
                { 0x67, () => { TestBit(State.AF.High, 4); } },											// BIT 4, A

				{ 0x68, () => { TestBit(State.BC.High, 5); } },											// BIT 5, B
                { 0x69, () => { TestBit(State.BC.Low, 5); } },											// BIT 5, C
                { 0x6A, () => { TestBit(State.DE.High, 5); } },											// BIT 5, D
                { 0x6B, () => { TestBit(State.DE.Low, 5); } },											// BIT 5, E
                { 0x6C, () => { TestBit(State.HL.High, 5); } },											// BIT 5, H
                { 0x6D, () => { TestBit(State.HL.Low, 5); } },											// BIT 5, L
                { 0x6E, () => { TestBit(ReadByte(State.HL), 5); } },								    // BIT 5, (HL)
                { 0x6F, () => { TestBit(State.AF.High, 5); } },											// BIT 5, A

				{ 0x70, () => { TestBit(State.BC.High, 6); } },											// BIT 6, B
                { 0x71, () => { TestBit(State.BC.Low, 6); } },											// BIT 6, C
                { 0x72, () => { TestBit(State.DE.High, 6); } },											// BIT 6, D
                { 0x73, () => { TestBit(State.DE.Low, 6); } },											// BIT 6, E
                { 0x74, () => { TestBit(State.HL.High, 6); } },											// BIT 6, H
                { 0x75, () => { TestBit(State.HL.Low, 6); } },											// BIT 6, L
                { 0x76, () => { TestBit(ReadByte(State.HL), 6); } },								    // BIT 6, (HL)
                { 0x77, () => { TestBit(State.AF.High, 6); } },											// BIT 6, A

				{ 0x78, () => { TestBit(State.BC.High, 7); } },											// BIT 7, B
                { 0x79, () => { TestBit(State.BC.Low, 7); } },											// BIT 7, C
                { 0x7A, () => { TestBit(State.DE.High, 7); } },											// BIT 7, D
                { 0x7B, () => { TestBit(State.DE.Low, 7); } },											// BIT 7, E
                { 0x7C, () => { TestBit(State.HL.High, 7); } },											// BIT 7, H
                { 0x7D, () => { TestBit(State.HL.Low, 7); } },											// BIT 7, L
                { 0x7E, () => { TestBit(ReadByte(State.HL), 7); } },								    // BIT 7, (HL)
                { 0x7F, () => { TestBit(State.AF.High, 7); } }											// BIT 7, A
            };
        }

        private Dictionary<byte, Action> opcodeTable;
        private Dictionary<byte, Action> cbTable;
    }
}
