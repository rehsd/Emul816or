//represents a PSG: AY-3-8910 or YM2149
//register data: https://f.rdw.se/AY-3-8910-datasheet.pdf
//BC1 and BDIR coming from one VIA port
//Data on the other VIA port
//Starting with basic tones, only writing to PSGs. Reading from PSG I/O not supported.

//Wave playback
//Adaption from https://www.codeguru.com/dotnet/making-sounds-with-waves-using-c/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.VisualBasic.Devices;
using System.IO;
using Microsoft.VisualBasic;
using System.Windows.Media;

namespace Emul816or
{
    public class PSG
    {
        const uint size = 16;
        const uint baseAddress = 0x100000;
        private bool supports16bit = true;
        //private bool bdir;
        //private bool bc1;
        //assuming bc2 is tied high
        private byte currentRegister;   //0x00-0x0F
        //private byte incomingData;      //hold incoming data prior to receiving appropriate bus control signals
        //private byte outgoingData;      //hold outgoing data prior to receiving appropriate bus control signals
        private byte tempData;
        byte[] RegisterValues;
        public static MediaPlayer[] channels;

        public enum REGISTERS
        {
            Reg0_ChA_TonePeriod_Fine,
            Reg1_ChA_TonePeriod_Course,
            Reg2_ChB_TonePeriod_Fine,
            Reg3_ChB_TonePeriod_Course,
            Reg4_ChC_TonePeriod_Fine,
            Reg5_ChC_TonePeriod_Course,
            Reg6_NoisePeriod,
            Reg7_EnableB,
            Reg8_ChA_Amplitude,
            Reg9_ChB_Amplitude,
            RegA_ChC_Amplitude,
            RegB_EnvelopePeriod_Fine,
            RegC_EnvelopePeriod_Course,
            RegD_EnvelopeShapeCycle,
            RegE_IO_PortA_Data,
            RegF_IO_PortB_Data
        }
        public uint Size
        {
            get => size;
        }
        public uint BaseAddress
        {
            get => baseAddress;
        }
        public void SetBusControl(bool BDIR, bool BC1)
        {
            //this.bdir = BDIR;
            //this.bc1 = BC1;

            //setreg        = NACT -> writeValue -> INTAK -> NACT
            //readdata      = NACT -> changeDDRtoRead(00) -> writeZero -> DTB -> changeDDRtoWrite(FF) -> NACT
            //writedata     = NACT -> writeValue -> DWS -> NACT
            if (!BDIR && !BC1)       //inactive  (NACT)
            {
                tempData = 0;
            }
            else if (!BDIR && BC1)   //read      (DTB)
            {
                //readdata
                //not supported yet
                tempData=  RegisterValues[currentRegister];
            }
            else if (BDIR  && !BC1)   //write     (DWS)
            {
                //writedata
                RegisterValues[currentRegister] = tempData;
                Play();
            }
            else if (BDIR && BC1)    //latch     (INTAK)
            {
                //setreg
                if (tempData >= 0 && tempData <= 15)
                {
                    currentRegister = tempData;
                }
            }
        }
        public Byte Value
        {
            get
            {
                return tempData;
            }
            set
            {
                tempData = value;
            }
        }
        public bool Supports16Bit
        {
            get => supports16bit;
        }

        public PSG()
        {
            RegisterValues = new byte[16];
            RegisterValues[(byte)PSG.REGISTERS.Reg0_ChA_TonePeriod_Fine] = 0;
            RegisterValues[(byte)PSG.REGISTERS.Reg1_ChA_TonePeriod_Course] = 0;
            RegisterValues[(byte)PSG.REGISTERS.Reg2_ChB_TonePeriod_Fine] = 0;
            RegisterValues[(byte)PSG.REGISTERS.Reg3_ChB_TonePeriod_Course] = 0;
            RegisterValues[(byte)PSG.REGISTERS.Reg4_ChC_TonePeriod_Fine] = 0;
            RegisterValues[(byte)PSG.REGISTERS.Reg5_ChC_TonePeriod_Course] = 0;
            RegisterValues[(byte)PSG.REGISTERS.Reg6_NoisePeriod] = 0;               //not implementing yet
            RegisterValues[(byte)PSG.REGISTERS.Reg7_EnableB] = 0b11111111;          //turn off noise and tone
            RegisterValues[(byte)PSG.REGISTERS.Reg8_ChA_Amplitude] = 15;            //max volume
            RegisterValues[(byte)PSG.REGISTERS.Reg9_ChB_Amplitude] = 15;            //max volume
            RegisterValues[(byte)PSG.REGISTERS.RegA_ChC_Amplitude] = 15;            //max volume
            RegisterValues[(byte)PSG.REGISTERS.RegB_EnvelopePeriod_Fine] = 0;       //not implementing yet
            RegisterValues[(byte)PSG.REGISTERS.RegC_EnvelopePeriod_Course] = 0;     //not implementing yet
            RegisterValues[(byte)PSG.REGISTERS.RegD_EnvelopeShapeCycle] = 0;        //not implementing yet
            RegisterValues[(byte)PSG.REGISTERS.RegE_IO_PortA_Data] = 0;             //not implementing yet
            RegisterValues[(byte)PSG.REGISTERS.RegF_IO_PortB_Data] = 0;             //not implementing yet

            //Initialize players for channels
            //Supporting six PSGs, each with three channels
            channels = new MediaPlayer[3];
            for(int c = 0; c < 3; c++)
            {
                channels[c] = new MediaPlayer();
            }
        }

        private void Play()
        {
            //read all registers and update sound output of PSG
            bool chAenable = !(Convert.ToBoolean(RegisterValues[(int)REGISTERS.Reg7_EnableB] & (byte)0b00000001));
            if (chAenable)
            {
                int frequency = RegisterValues[(int)REGISTERS.Reg1_ChA_TonePeriod_Course];
                //TO DO - Frequency calc based on Course and Fine

                //testing:
                WavePlayer.Play(261.63,5,channels[0]);
            }
            else
            {
                //turn off sound
                channels[0].Stop();
            }

            bool chBenable = !(Convert.ToBoolean(RegisterValues[(int)REGISTERS.Reg7_EnableB] & (byte)0b00000010));
            if (chBenable)
            {
                int frequency = RegisterValues[(int)REGISTERS.Reg3_ChB_TonePeriod_Course];
                //TO DO - Frequency calc based on Course and Fine

                //testing:
                WavePlayer.Play(523.26, 5, channels[1]);
            }
            else
            {
                //turn off sound
                channels[1].Stop();
            }

            bool chCenable = !(Convert.ToBoolean(RegisterValues[(int)REGISTERS.Reg7_EnableB] & (byte)0b00000100));
            if (chCenable)
            {
                int frequency = RegisterValues[(int)REGISTERS.Reg5_ChC_TonePeriod_Course];
                //TO DO - Frequency calc based on Course and Fine

                //testing:
                WavePlayer.Play(784.89, 5, channels[2]);
            }
            else
            {
                //turn off sound
                channels[2].Stop();
            }
        }

    }

    public class WaveHeader
    {
        private const string FILE_TYPE_ID = "RIFF";
        private const string MEDIA_TYPE_ID = "WAVE";

        public string FileTypeId { get; private set; }
        public UInt32 FileLength { get; set; }
        public string MediaTypeId { get; private set; }

        public WaveHeader()
        {
            FileTypeId = FILE_TYPE_ID;
            MediaTypeId = MEDIA_TYPE_ID;
            // Minimum size is always 4 bytes
            FileLength = 4;
        }
        public byte[] GetBytes()
        {
            List<Byte> chunkData = new List<byte>();
            chunkData.AddRange(Encoding.ASCII.GetBytes(FileTypeId));
            chunkData.AddRange(BitConverter.GetBytes(FileLength));
            chunkData.AddRange(Encoding.ASCII.GetBytes(MediaTypeId));
            return chunkData.ToArray();
        }
    }
    public class FormatChunk
    {
        private ushort _bitsPerSample;
        private ushort _channels;
        private uint _frequency;
        private const string CHUNK_ID = "fmt ";

        public string ChunkId { get; private set; }
        public UInt32 ChunkSize { get; private set; }
        public UInt16 FormatTag { get; private set; }

        public UInt16 Channels
        {
            get { return _channels; }
            set { _channels = value; RecalcBlockSizes(); }
        }

        public UInt32 Frequency
        {
            get { return _frequency; }
            set { _frequency = value; RecalcBlockSizes(); }
        }

        public UInt32 AverageBytesPerSec { get; private set; }
        public UInt16 BlockAlign { get; private set; }

        public UInt16 BitsPerSample
        {
            get { return _bitsPerSample; }
            set { _bitsPerSample = value; RecalcBlockSizes(); }
        }

        public FormatChunk()
        {
            ChunkId = CHUNK_ID;
            ChunkSize = 16;
            FormatTag = 1;       // MS PCM (Uncompressed wave file)
            Channels = 2;        // Default to stereo
            Frequency = 44100;   // Default to 44100hz
            BitsPerSample = 16;  // Default to 16bits
            RecalcBlockSizes();
        }

        private void RecalcBlockSizes()
        {
            BlockAlign = (UInt16)(_channels * (_bitsPerSample / 8));
            AverageBytesPerSec = _frequency * BlockAlign;
        }

        public byte[] GetBytes()
        {
            List<Byte> chunkBytes = new List<byte>();

            chunkBytes.AddRange(Encoding.ASCII.GetBytes(ChunkId));
            chunkBytes.AddRange(BitConverter.GetBytes(ChunkSize));
            chunkBytes.AddRange(BitConverter.GetBytes(FormatTag));
            chunkBytes.AddRange(BitConverter.GetBytes(Channels));
            chunkBytes.AddRange(BitConverter.GetBytes(Frequency));
            chunkBytes.AddRange(BitConverter.GetBytes(AverageBytesPerSec));
            chunkBytes.AddRange(BitConverter.GetBytes(BlockAlign));
            chunkBytes.AddRange(BitConverter.GetBytes(BitsPerSample));

            return chunkBytes.ToArray();
        }

        public UInt32 Length()
        {
            return (UInt32)GetBytes().Length;
        }

    }
    public class DataChunk
    {
        private const string CHUNK_ID = "data";

        public string ChunkId { get; private set; }
        public UInt32 ChunkSize { get; set; }
        public short[] WaveData { get; private set; }

        public DataChunk()
        {
            ChunkId = CHUNK_ID;
            ChunkSize = 0;  // Until we add some data
        }

        public UInt32 Length()
        {
            return (UInt32)GetBytes().Length;
        }

        public byte[] GetBytes()
        {
            List<Byte> chunkBytes = new List<Byte>();

            chunkBytes.AddRange(Encoding.ASCII.GetBytes(ChunkId));
            chunkBytes.AddRange(BitConverter.GetBytes(ChunkSize));
            byte[] bufferBytes = new byte[WaveData.Length * 2];
            Buffer.BlockCopy(WaveData, 0, bufferBytes, 0,
               bufferBytes.Length);
            chunkBytes.AddRange(bufferBytes.ToList());

            return chunkBytes.ToArray();
        }

        public void AddSampleData(short[] leftBuffer,
           short[] rightBuffer)
        {
            WaveData = new short[leftBuffer.Length +
               rightBuffer.Length];
            int bufferOffset = 0;
            for (int index = 0; index < WaveData.Length; index += 2)
            {
                WaveData[index] = leftBuffer[bufferOffset];
                WaveData[index + 1] = rightBuffer[bufferOffset];
                bufferOffset++;
            }
            ChunkSize = (UInt32)WaveData.Length * 2;
        }

    }
    public class SineGenerator
    {
        private readonly double _frequency;
        private readonly UInt32 _sampleRate;
        private readonly UInt16 _secondsInLength;
        private short[] _dataBuffer;

        public short[] Data { get { return _dataBuffer; } }

        public SineGenerator(double frequency,
           UInt32 sampleRate, UInt16 secondsInLength)
        {
            _frequency = frequency;
            _sampleRate = sampleRate;
            _secondsInLength = secondsInLength;
            GenerateData();
        }

        private void GenerateData()
        {
            uint bufferSize = _sampleRate * _secondsInLength;
            _dataBuffer = new short[bufferSize];

            int amplitude = 32760;

            double timePeriod = (Math.PI * 2 * _frequency) /
               (_sampleRate);

            for (uint index = 0; index < bufferSize - 1; index++)
            {
                _dataBuffer[index] = Convert.ToInt16(amplitude *
                   Math.Sin(timePeriod * index));
            }
        }
    }
    public class WavePlayer
    {
        static Audio myAudio = new Audio();
        private static byte[] myWaveData;

        // Sample rate (Or number of samples in one second)
        private const int SAMPLE_RATE = 44100;
        // 60 seconds or 1 minute of audio
        //private const int AUDIO_LENGTH_IN_SECONDS = 1;

        static public void Play(double frequency, ushort duration, MediaPlayer channel)
        {
            List<Byte> tempBytes = new List<byte>();

            WaveHeader header = new WaveHeader();
            FormatChunk format = new FormatChunk();
            DataChunk data = new DataChunk();

            SineGenerator sineData = new SineGenerator(frequency, SAMPLE_RATE, duration);

            data.AddSampleData(sineData.Data, sineData.Data);

            header.FileLength += format.Length() + data.Length();

            tempBytes.AddRange(header.GetBytes());
            tempBytes.AddRange(format.GetBytes());
            tempBytes.AddRange(data.GetBytes());

            myWaveData = tempBytes.ToArray();

            string tempFolder, tempWav;
            tempFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            tempFolder += "\\tempWavs\\";
            if(!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
            }
            tempWav = tempFolder + frequency.ToString() + "_" + duration.ToString() + ".wav";
            if (!File.Exists(tempWav))
            {
                File.WriteAllBytes(tempWav, myWaveData);
            }
            //MediaPlayer p = new();
            channel.Open(new System.Uri(tempWav));
            channel.Play();
        }

    }
}
