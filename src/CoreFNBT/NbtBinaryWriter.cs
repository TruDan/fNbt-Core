using System;
using System.IO;
using System.Text;

namespace fNbt {
    /// <summary> BinaryWriter wrapper that writes NBT primitives to a stream,
    /// while taking care of endianness and string encoding, and counting bytes written. </summary>
    public sealed unsafe class NbtBinaryWriter {
        // Write at most 512 MiB at a time.
        // This works around an overflow in BufferedStream.Write(byte[]) that happens on 1 GiB+ writes.
        public const int MaxWriteChunk = 512*1024*1024;

        // Encoding can be shared among all instances of NbtBinaryWriter, because it is stateless.
        static readonly UTF8Encoding Encoding = new UTF8Encoding(false, true);

        // Each instance has to have its own encoder, because it does maintain state.
        readonly Encoder encoder = Encoding.GetEncoder();

		public bool UseVarInt { get; set; }

		public Stream BaseStream {
            get {
                stream.Flush();
                return stream;
            }
        }

        readonly Stream stream;

        // Buffer used for temporary conversion
        const int BufferSize = 256;

        // UTF8 characters use at most 4 bytes each.
        const int MaxBufferedStringLength = BufferSize/4;

        // Each NbtBinaryWriter needs to have its own instance of the buffer.
        readonly byte[] buffer = new byte[BufferSize];

        // Swap is only needed if endianness of the runtime differs from desired NBT stream
        readonly bool swapNeeded;


        public NbtBinaryWriter([NotNull] Stream input, bool bigEndian) {
            if (input == null) throw new ArgumentNullException("input");
            if (!input.CanWrite) throw new ArgumentException("Given stream must be writable", "input");
            stream = input;
            swapNeeded = (BitConverter.IsLittleEndian == bigEndian);
        }


        public void Write(byte value) {
            stream.WriteByte(value);
        }


        public void Write(NbtTagType value) {
            stream.WriteByte((byte)value);
        }


        public void Write(short value) {
            unchecked {
                if (swapNeeded) {
                    buffer[0] = (byte)(value >> 8);
                    buffer[1] = (byte)value;
                } else {
                    buffer[0] = (byte)value;
                    buffer[1] = (byte)(value >> 8);
                }
            }
            stream.Write(buffer, 0, 2);
        }


        public void Write(ushort value)
        {
            unchecked
            {
                if (swapNeeded)
                {
                    buffer[0] = (byte)(value >> 8);
                    buffer[1] = (byte)value;
                }
                else
                {
                    buffer[0] = (byte)value;
                    buffer[1] = (byte)(value >> 8);
                }
            }
            stream.Write(buffer, 0, 2);
        }


        public void Write(int value) {
		    if (UseVarInt) {
			    WriteVarInt(value);
		    } else {
			    unchecked {
				    if (swapNeeded) {
					    buffer[0] = (byte)(value >> 24);
					    buffer[1] = (byte)(value >> 16);
					    buffer[2] = (byte)(value >> 8);
					    buffer[3] = (byte)value;
				    } else {
					    buffer[0] = (byte)value;
					    buffer[1] = (byte)(value >> 8);
					    buffer[2] = (byte)(value >> 16);
					    buffer[3] = (byte)(value >> 24);
				    }
			    }
			    stream.Write(buffer, 0, 4);
		    }
	    }


	    public void WriteVarInt(int value) {
			// VarInt is LE by default
			VarInt.WriteSInt32(BaseStream, value);
		}

		public static short SwapInt16(short v)
		{
			return (short)(((v & 0xff) << 8) | ((v >> 8) & 0xff));
		}

		public static int SwapInt32(int v)
		{
			return (int)(((SwapInt16((short)v) & 0xffff) << 0x10) |
						  (SwapInt16((short)(v >> 0x10)) & 0xffff));
		}

		public void Write(long value) {
            unchecked {
                if (swapNeeded) {
                    buffer[0] = (byte)(value >> 56);
                    buffer[1] = (byte)(value >> 48);
                    buffer[2] = (byte)(value >> 40);
                    buffer[3] = (byte)(value >> 32);
                    buffer[4] = (byte)(value >> 24);
                    buffer[5] = (byte)(value >> 16);
                    buffer[6] = (byte)(value >> 8);
                    buffer[7] = (byte)value;
                } else {
                    buffer[0] = (byte)value;
                    buffer[1] = (byte)(value >> 8);
                    buffer[2] = (byte)(value >> 16);
                    buffer[3] = (byte)(value >> 24);
                    buffer[4] = (byte)(value >> 32);
                    buffer[5] = (byte)(value >> 40);
                    buffer[6] = (byte)(value >> 48);
                    buffer[7] = (byte)(value >> 56);
                }
            }
            stream.Write(buffer, 0, 8);
        }


        public void Write(float value) {
            ulong tmpValue = *(uint*)&value;
            unchecked {
                if (swapNeeded) {
                    buffer[0] = (byte)(tmpValue >> 24);
                    buffer[1] = (byte)(tmpValue >> 16);
                    buffer[2] = (byte)(tmpValue >> 8);
                    buffer[3] = (byte)tmpValue;
                } else {
                    buffer[0] = (byte)tmpValue;
                    buffer[1] = (byte)(tmpValue >> 8);
                    buffer[2] = (byte)(tmpValue >> 16);
                    buffer[3] = (byte)(tmpValue >> 24);
                }
            }
            stream.Write(buffer, 0, 4);
        }


        public void Write(double value) {
            ulong tmpValue = *(ulong*)&value;
            unchecked {
                if (swapNeeded) {
                    buffer[0] = (byte)(tmpValue >> 56);
                    buffer[1] = (byte)(tmpValue >> 48);
                    buffer[2] = (byte)(tmpValue >> 40);
                    buffer[3] = (byte)(tmpValue >> 32);
                    buffer[4] = (byte)(tmpValue >> 24);
                    buffer[5] = (byte)(tmpValue >> 16);
                    buffer[6] = (byte)(tmpValue >> 8);
                    buffer[7] = (byte)tmpValue;
                } else {
                    buffer[0] = (byte)tmpValue;
                    buffer[1] = (byte)(tmpValue >> 8);
                    buffer[2] = (byte)(tmpValue >> 16);
                    buffer[3] = (byte)(tmpValue >> 24);
                    buffer[4] = (byte)(tmpValue >> 32);
                    buffer[5] = (byte)(tmpValue >> 40);
                    buffer[6] = (byte)(tmpValue >> 48);
                    buffer[7] = (byte)(tmpValue >> 56);
                }
            }
            stream.Write(buffer, 0, 8);
        }


        // Based on BinaryWriter.Write(String)
        public void Write([NotNull] string value) {
            if (value == null) {
                throw new ArgumentNullException("value");
            }

            // Write out string length (as number of bytes)
            if(Encoding.GetByteCount(value) > 32767)
            {
                throw new NbtFormatException("Strings longer than 32767 bytes cannot be written.");
            }

            int numBytes = Encoding.GetByteCount(value);
	        if (UseVarInt) {
				Write((byte)numBytes);
			}
			else {
				Write((ushort)numBytes);
			}

			if (numBytes <= BufferSize) {
                // If the string fits entirely in the buffer, encode and write it as one
                Encoding.GetBytes(value, 0, value.Length, buffer, 0);
                stream.Write(buffer, 0, numBytes);
            } else {
                // Aggressively try to not allocate memory in this loop for runtime performance reasons.
                // Use an Encoder to write out the string correctly (handling surrogates crossing buffer
                // boundaries properly).  
                int charStart = 0;
                int numLeft = value.Length;
                while (numLeft > 0) {
                    // Figure out how many chars to process this round.
                    int charCount = (numLeft > MaxBufferedStringLength) ? MaxBufferedStringLength : numLeft;
                    int byteLen;
	                char[] pChars = value.ToCharArray();
	                byte[] pBytes = buffer;
                        {
	                        byteLen = encoder.GetBytes(pChars, charStart, charCount, pBytes, 0, charCount == numLeft);
                            //byteLen = encoder.GetBytes(pChars + charStart, charCount, pBytes, BufferSize,
                            //                           charCount == numLeft);
                        }
                    
                    stream.Write(buffer, 0, byteLen);
                    charStart += charCount;
                    numLeft -= charCount;
                }
            }
        }

        public void Write(byte[] data, int offset, int count) {
            int written = 0;
            while (written < count) {
                int toWrite = Math.Min(MaxWriteChunk, count - written);
                stream.Write(data, offset + written, toWrite);
                written += toWrite;
            }
        }

    }
}
