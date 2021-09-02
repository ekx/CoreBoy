using System.Runtime.Serialization;

namespace CoreBoy.Core.Utils.Memory
{
    [DataContract]
    public class InputCell : IMemoryCell
    {
        public byte Value
        {
            get
            {
                byte returnValue = 0xFF;
                
                returnValue = returnValue.SetBit(Player1.ActionButtons, this[Player1.ActionButtons]);
                returnValue = returnValue.SetBit(Player1.DirectionButtons, this[Player1.DirectionButtons]);

                returnValue = returnValue.SetBit(Player1.DownOrStart, this[Player1.DownOrStart]);
                returnValue = returnValue.SetBit(Player1.LeftOrB, this[Player1.LeftOrB]);
                returnValue = returnValue.SetBit(Player1.RightOrA, this[Player1.RightOrA]);
                returnValue = returnValue.SetBit(Player1.UpOrSelect, this[Player1.UpOrSelect]);

                return returnValue;
            }
            
            set
            {
                dPadEnabled = !value.GetBit(Player1.DirectionButtons);
                buttonsEnabled = !value.GetBit(Player1.ActionButtons);
            }
        }

        public bool this[int bitIndex]
        {
            get
            {
                switch (bitIndex)
                {
                    case Player1.ActionButtons:
                        return !buttonsEnabled;
                    case Player1.DirectionButtons:
                        return !dPadEnabled;
                    case Player1.DownOrStart:
                        return !((dPadEnabled && inputState.DownPressed) || (buttonsEnabled && inputState.StartPressed));
                    case Player1.LeftOrB:
                        return !((dPadEnabled && inputState.LeftPressed) || (buttonsEnabled && inputState.BPressed));
                    case Player1.RightOrA:
                        return !((dPadEnabled && inputState.RightPressed) || (buttonsEnabled && inputState.APressed));
                    case Player1.UpOrSelect:
                        return !((dPadEnabled && inputState.UpPressed) || (buttonsEnabled && inputState.SelectPressed));
                    default:
                        return true;
                }
            }
            
            set
            {
                switch (bitIndex)
                {
                    case Player1.DirectionButtons:
                        dPadEnabled = !value;
                        break;
                    case Player1.ActionButtons:
                        buttonsEnabled = !value;
                        break;
                }
            }
        }

        public bool UpdateInputState(InputState inputStateIn)
        {
            var prevRightOrA = this[Player1.RightOrA];
            var prevLeftOrB = this[Player1.LeftOrB];
            var prevUpOrSelect = this[Player1.UpOrSelect];
            var prevDownOrStart = this[Player1.DownOrStart];

            inputState = inputStateIn;

            return (prevRightOrA && !this[Player1.RightOrA])
                   || (prevLeftOrB && !this[Player1.LeftOrB])
                   || (prevUpOrSelect && !this[Player1.UpOrSelect])
                   || (prevDownOrStart && !this[Player1.DownOrStart]);
        }

        public void LockBit(int index, bool valueIn) { }

        public void LockBits(int startIndex, int numberOfBits, bool value) { }

        [DataMember]
        private bool dPadEnabled;
        [DataMember]
        private bool buttonsEnabled;
        
        private InputState inputState = new(false, false, false ,false, false, false, false, false);
    }
}