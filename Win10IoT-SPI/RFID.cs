using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;

namespace Win10IoT_SPI
{
    /* RFID - Library to use ARDUINO RFID MODULE KIT 13.56 MHZ WITH TAGS SPI W AND R BY COOQROBOT.
     * Based on code Dr.Leong   ( WWW.B2CQSHOP.COM )
     * Created by Inoki (veyx.shaw@gmail.com)
     */
    class RFID
    {
        private SpiDevice rfidMF522;

        public const byte MAX_LEN = 16;   // 数组最大长度

        //MF522命令字
        public const byte PCD_IDLE = 0x00;               //无动作，取消当前命令
        public const byte PCD_AUTHENT = 0x0E;               //验证密钥
        public const byte PCD_RECEIVE = 0x08;               //接收数据
        public const byte PCD_TRANSMIT = 0x04;               //发送数据
        public const byte PCD_TRANSCEIVE = 0x0C;         //发送并接收数据
        public const byte PCD_RESETPHASE = 0x0F;               //复位
        public const byte PCD_CALCCRC = 0x03;               //CRC计算

        //Mifare_One卡片命令字
        public const byte PICC_REQIDL = 0x26;               //寻天线区内未进入休眠状态
        public const byte PICC_REQALL = 0x52;               //寻天线区内全部卡
        public const byte PICC_ANTICOLL = 0x93;               //防冲撞
        public const byte PICC_SElECTTAG = 0x93;               //选卡
        public const byte PICC_AUTHENT1A = 0x60;               //验证A密钥
        public const byte PICC_AUTHENT1B = 0x61;               //验证B密钥
        public const byte PICC_READ = 0x30;               //读块
        public const byte PICC_WRITE = 0xA0;               //写块
        public const byte PICC_DECREMENT = 0xC0;
        public const byte PICC_INCREMENT = 0xC1;
        public const byte PICC_RESTORE = 0xC2;               //调块数据到缓冲区
        public const byte PICC_TRANSFER = 0xB0;               //保存缓冲区中数据
        public const byte PICC_HALT = 0x50;               //休眠

        //和MF522通讯时返回的错误代码
        public const byte MI_OK        = 0;
        public const byte MI_NOTAGERR  = 1;
        public const byte MI_ERR       = 2;

        //------------------MFRC522寄存器---------------
        //Page 0:Command and Status
        public const byte Reserved00 = 0x00;
        public const byte CommandReg = 0x01;
        public const byte CommIEnReg = 0x02;
        public const byte DivlEnReg = 0x03;
        public const byte CommIrqReg = 0x04;
        public const byte DivIrqReg = 0x05;
        public const byte ErrorReg = 0x06;
        public const byte Status1Reg = 0x07;
        public const byte Status2Reg = 0x08;
        public const byte FIFODataReg = 0x09;
        public const byte FIFOLevelReg = 0x0A;
        public const byte WaterLevelReg = 0x0B;
        public const byte ControlReg = 0x0C;
        public const byte BitFramingReg = 0x0D;
        public const byte CollReg = 0x0E;
        public const byte Reserved01 = 0x0F;
        //Page 1:Command     
        public const byte Reserved10 = 0x10;
        public const byte ModeReg = 0x11;
        public const byte TxModeReg = 0x12;
        public const byte RxModeReg = 0x13;
        public const byte TxControlReg = 0x14;
        public const byte TxAutoReg = 0x15;
        public const byte TxSelReg = 0x16;
        public const byte RxSelReg = 0x17;
        public const byte RxThresholdReg = 0x18;
        public const byte DemodReg = 0x19;
        public const byte Reserved11 = 0x1A;
        public const byte Reserved12 = 0x1B;
        public const byte MifareReg = 0x1C;
        public const byte Reserved13 = 0x1D;
        public const byte Reserved14 = 0x1E;
        public const byte SerialSpeedReg = 0x1F;
        //Page 2:CFG    
        public const byte Reserved20 = 0x20;
        public const byte CRCResultRegM = 0x21;
        public const byte CRCResultRegL = 0x22;
        public const byte Reserved21 = 0x23;
        public const byte ModWidthReg = 0x24;
        public const byte Reserved22 = 0x25;
        public const byte RFCfgReg = 0x26;
        public const byte GsNReg = 0x27;
        public const byte CWGsPReg = 0x28;
        public const byte ModGsPReg = 0x29;
        public const byte TModeReg = 0x2A;
        public const byte TPrescalerReg = 0x2B;
        public const byte TReloadRegH = 0x2C;
        public const byte TReloadRegL = 0x2D;
        public const byte TCounterValueRegH = 0x2E;
        public const byte TCounterValueRegL = 0x2F;
        //Page 3:TestRegister     
        public const byte Reserved30 = 0x30;
        public const byte TestSel1Reg = 0x31;
        public const byte TestSel2Reg = 0x32;
        public const byte TestPinEnReg = 0x33;
        public const byte TestPinValueReg = 0x34;
        public const byte TestBusReg = 0x35;
        public const byte AutoTestReg = 0x36;
        public const byte VersionReg = 0x37;
        public const byte AnalogTestReg = 0x38;
        public const byte TestDAC1Reg = 0x39;
        public const byte TestDAC2Reg = 0x3A;
        public const byte TestADCReg = 0x3B;
        public const byte Reserved31 = 0x3C;
        public const byte Reserved32 = 0x3D;
        public const byte Reserved33 = 0x3E;
        public const byte Reserved34 = 0x3F;
        //-----------------------------------------------

        int _chipSelectPin;
        int _NRSTPD;

        /******************************************************************************
         * 构造 RFID
         * int chipSelectPin RFID /ENABLE pin
         ******************************************************************************/
        RFID(int chipSelectPin, int NRSTPD)
        {
            _chipSelectPin = chipSelectPin;
            _NRSTPD = NRSTPD;

            startScenarioAsync();
        }

        async Task startScenarioAsync()
        {
            String spiDeviceSelector = SpiDevice.GetDeviceSelector();
            IReadOnlyList<DeviceInformation> devices = await DeviceInformation.FindAllAsync(spiDeviceSelector);

            // 0 = Chip select line to use.
            var rfidMF522_Settings = new SpiConnectionSettings(0);

            // 5MHz is the rated speed of the ADXL345 accelerometer.
            rfidMF522_Settings.ClockFrequency = 5000000;

            // The accelerometer expects an idle-high clock polarity.
            // We use Mode3 to set the clock polarity and phase to: CPOL = 1, CPHA = 1.
            rfidMF522_Settings.Mode = SpiMode.Mode3;

            // If this next line crashes with an ArgumentOutOfRangeException,
            // then the problem is that no SPI devices were found.
            //
            // If the next line crashes with Access Denied, then the problem is
            // that access to the SPI device (ADXL345) is denied.
            //
            // The call to FromIdAsync will also crash if the settings are invalid.
            //
            // FromIdAsync produces null if there is a sharing violation on the device.
            // This will result in a NullReferenceException a few lines later.
            rfidMF522 = await SpiDevice.FromIdAsync(devices[0].Id, rfidMF522_Settings);

            GpioController gpio = GpioController.GetDefault();

            using (GpioPin pin = gpio.OpenPin(_chipSelectPin))
            {
                // Latch HIGH value first. This ensures a default value when the pin is set as output
                pin.Write(GpioPinValue.Low);

                // Set the IO direction as output
                pin.SetDriveMode(GpioPinDriveMode.Output);
            }

            using (GpioPin pin = gpio.OpenPin(_NRSTPD))
            {
                // Latch HIGH value first. This ensures a default value when the pin is set as output
                pin.Write(GpioPinValue.High);

                // Set the IO direction as output
                pin.SetDriveMode(GpioPinDriveMode.Output);
            }

            //pinMode(_chipSelectPin, OUTPUT);     // 设置管脚_chipSelectPin为输出并连接到模块使能口
            //digitalWrite(_chipSelectPin, LOW);

            //pinMode(_NRSTPD, OUTPUT);            // 设置管脚NRSTPD为输出，非重置或掉电
            //digitalWrite(_NRSTPD, HIGH);
        }

        /******************************************************************************
         * 用户 API
         ******************************************************************************/

        /******************************************************************************
         * 函 数 名：init
         * 功能描述：初始化RC522
         * 输入参数：无
         * 返 回 值：无
         ******************************************************************************/
        void init()
        {
            GpioController gpio = GpioController.GetDefault();
            if (gpio == null) return; // GPIO not available on this system

            using (GpioPin pin = gpio.OpenPin(_NRSTPD))
            {
                // Latch HIGH value first. This ensures a default value when the pin is set as output
                pin.Write(GpioPinValue.High);

                // Set the IO direction as output
                pin.SetDriveMode(GpioPinDriveMode.Output);
            }

            reset();

            //Timer: TPrescaler*TreloadVal/6.78MHz = 24ms
            writeMFRC522(TModeReg, 0x8D);   //Tauto=1; f(Timer) = 6.78MHz/TPreScaler
            writeMFRC522(TPrescalerReg, 0x3E);  //TModeReg[3..0] + TPrescalerReg
            writeMFRC522(TReloadRegL, 30);
            writeMFRC522(TReloadRegH, 0);
            writeMFRC522(TxAutoReg, 0x40);    //100%ASK
            writeMFRC522(ModeReg, 0x3D);    // CRC valor inicial de 0x6363

            //ClearBitMask(Status2Reg, 0x08); //MFCrypto1On=0
            //writeMFRC522(RxSelReg, 0x86);   //RxWait = RxSelReg[5..0]
            //writeMFRC522(RFCfgReg, 0x7F);     //RxGain = 48dB

            antennaOn();    //打开天线
        }

        /******************************************************************************
         * 函 数 名：reset
         * 功能描述：复位RC522
         * 输入参数：无
         * 返 回 值：无
         ******************************************************************************/
        void reset()
        {
            writeMFRC522(CommandReg, PCD_RESETPHASE);
        }

        /******************************************************************************
         * 函 数 名：writeMFRC522
         * 功能描述：向MFRC522的某一寄存器写一个字节数据
         * 输入参数：addr--寄存器地址；val--要写入的值
         * 返 回 值：无
         ******************************************************************************/
        void writeMFRC522(byte addr, byte val)
        {
            GpioController gpio = GpioController.GetDefault();
            if (gpio == null) return; // GPIO not available on this system

            using (GpioPin pin = gpio.OpenPin(_chipSelectPin))
            {
                // Latch HIGH value first. This ensures a default value when the pin is set as output
                pin.Write(GpioPinValue.Low);

                // Set the IO direction as output
                pin.SetDriveMode(GpioPinDriveMode.Output);
            }

            byte[] data = new byte[2];

            //地址格式：0XXXXXX0
            data[0] = (byte)((addr << 1) & 0x7E);
            data[1] = val;

            this.rfidMF522.Write(data);

            using (GpioPin pin = gpio.OpenPin(_chipSelectPin))
            {
                // Latch HIGH value first. This ensures a default value when the pin is set as output
                pin.Write(GpioPinValue.High);

                // Set the IO direction as output
                pin.SetDriveMode(GpioPinDriveMode.Output);
            }
        }

        /******************************************************************************
         * 函 数 名：readMFRC522
         * 功能描述：从MFRC522的某一寄存器读一个字节数据
         * 输入参数：addr--寄存器地址
         * 返 回 值：返回读取到的一个字节数据
         ******************************************************************************/
        byte readMFRC522(byte addr)
        {
            byte[] val = new byte[1];

            GpioController gpio = GpioController.GetDefault();

            using (GpioPin pin = gpio.OpenPin(_chipSelectPin))
            {
                // Latch HIGH value first. This ensures a default value when the pin is set as output
                pin.Write(GpioPinValue.Low);

                // Set the IO direction as output
                pin.SetDriveMode(GpioPinDriveMode.Output);
            }

            byte[] data = new byte[2];

            //地址格式：0XXXXXX0
            data[0] = (byte)(((addr << 1) & 0x7E) | 0x80);
            data[1] = 0x00;

            this.rfidMF522.Write(data);

            this.rfidMF522.Read(val);

            using (GpioPin pin = gpio.OpenPin(_chipSelectPin))
            {
                // Latch HIGH value first. This ensures a default value when the pin is set as output
                pin.Write(GpioPinValue.High);

                // Set the IO direction as output
                pin.SetDriveMode(GpioPinDriveMode.Output);
            }
            return val[0];
        }

        /******************************************************************************
         * 函 数 名：setBitMask
         * 功能描述：置RC522寄存器位
         * 输入参数：reg--寄存器地址;mask--置位值
         * 返 回 值：无
         ******************************************************************************/
        void setBitMask(byte reg, byte mask)
        {
            byte tmp;
            tmp = readMFRC522(reg);
            writeMFRC522(reg, (byte)(tmp | mask));  // set bit mask
        }

        /******************************************************************************
         * 函 数 名：clearBitMask
         * 功能描述：清RC522寄存器位
         * 输入参数：reg--寄存器地址;mask--清位值
         * 返 回 值：无
         ******************************************************************************/
        void clearBitMask(byte reg, byte mask)
        {
            byte tmp;
            tmp = readMFRC522(reg);
            writeMFRC522(reg, (byte)(tmp & (~mask)));  // clear bit mask
        }

        /******************************************************************************
         * 函 数 名：antennaOn
         * 功能描述：开启天线,每次启动或关闭天险发射之间应至少有1ms的间隔
         * 输入参数：无
         * 返 回 值：无
         ******************************************************************************/
        void antennaOn()
        {
            byte temp;

            temp = readMFRC522(TxControlReg);
            if ((temp & 0x03) == 0)
            {
                setBitMask(TxControlReg, 0x03);
            }
        }

        /******************************************************************************
         * 函 数 名：antennaOff
         * 功能描述：关闭天线,每次启动或关闭天险发射之间应至少有1ms的间隔
         * 输入参数：无
         * 返 回 值：无
         ******************************************************************************/
        void antennaOff()
        {
            byte temp;

            temp = readMFRC522(TxControlReg);
            if ((temp & 0x03) == 0)
            {
                clearBitMask(TxControlReg, 0x03);
            }
        }

        /******************************************************************************
         * 函 数 名：calculateCRC
         * 功能描述：用MF522计算CRC
         * 输入参数：pIndata--要读数CRC的数据，len--数据长度，pOutData--计算的CRC结果
         * 返 回 值：无
         ******************************************************************************/
        byte[] calculateCRC(byte[] pIndata, byte len, byte[] pOutData)
        {
            byte i, n;

            clearBitMask(DivIrqReg, 0x04);      //CRCIrq = 0
            setBitMask(FIFOLevelReg, 0x80);     //清FIFO指针
                                                //Write_MFRC522(CommandReg, PCD_IDLE);

            //向FIFO中写入数据
            for (i = 0; i < len; i++)
                writeMFRC522(FIFODataReg, pIndata[i]);
            writeMFRC522(CommandReg, PCD_CALCCRC);

            //等待CRC计算完成
            i = 0xFF;
            do
            {
                n = readMFRC522(DivIrqReg);
                i--;
            }
            while ((i != 0) && ((n & 0x04) == 0));      //CRCIrq = 1

            //读取CRC计算结果
            pOutData[0] = readMFRC522(CRCResultRegL);
            pOutData[1] = readMFRC522(CRCResultRegM);
            return pOutData;
        }

        /******************************************************************************
         * 函 数 名：MFRC522ToCard
         * 功能描述：RC522和ISO14443卡通讯
         * 输入参数：command--MF522命令字，
         *           sendData--通过RC522发送到卡片的数据,
         *                     sendLen--发送的数据长度
         *                     backData--接收到的卡片返回数据，
         *                     backLen--返回数据的位长度
         * 返 回 值：成功返回MI_OK
         ******************************************************************************/
        byte MFRC522ToCard(byte command, byte[] sendData, byte sendLen, out byte[] backData, out int backLen)
        {
            byte status = RFID.MI_ERR;
            byte irqEn = 0x00;
            byte waitIRq = 0x00;
            byte lastBits;
            byte n;
            int i;

            backLen = 0;

            switch (command)
            {
                case RFID.PCD_AUTHENT:   //认证卡密
                    {
                        irqEn = 0x12;
                        waitIRq = 0x10;
                        break;
                    }
                case PCD_TRANSCEIVE:  //发送FIFO中数据
                    {
                        irqEn = 0x77;
                        waitIRq = 0x30;
                        break;
                    }
                default:
                    break;
            }

            writeMFRC522(CommIEnReg, (byte)(irqEn | 0x80)); //允许中断请求
            clearBitMask(CommIrqReg, 0x80);       //清除所有中断请求位
            setBitMask(FIFOLevelReg, 0x80);       //FlushBuffer=1, FIFO初始化

            writeMFRC522(CommandReg, PCD_IDLE);   //无动作，取消当前命令

            //向FIFO中写入数据
            for (i = 0; i < sendLen; i++)
                writeMFRC522(FIFODataReg, sendData[i]);

            //执行命令
            writeMFRC522(CommandReg, command);
            if (command == PCD_TRANSCEIVE)
                setBitMask(BitFramingReg, 0x80);    //StartSend=1,transmission of data starts

            //等待接收数据完成
            i = 2000; //i根据时钟频率调整，操作M1卡最大等待时间25ms
            do
            {
                //CommIrqReg[7..0]
                //Set1 TxIRq RxIRq IdleIRq HiAlerIRq LoAlertIRq ErrIRq TimerIRq
                n = readMFRC522(CommIrqReg);
                i--;
            }
            while ((i != 0) && (n & 0x01) == 0 && (n & waitIRq) == 0);

            clearBitMask(BitFramingReg, 0x80);      //StartSend=0

            if (i != 0)
            {
                if ((readMFRC522(ErrorReg) & 0x1B) == 0) //BufferOvfl Collerr CRCErr ProtecolErr
                {
                    status = MI_OK;
                    if ((n & irqEn & 0x01) != 0)
                        status = MI_NOTAGERR;     //??

                    if (command == PCD_TRANSCEIVE)
                    {
                        n = readMFRC522(FIFOLevelReg);
                        lastBits = (byte)(readMFRC522(ControlReg) & 0x07);
                        if (lastBits != 0)
                            backLen = (n - 1) * 8 + lastBits;
                        else
                            backLen = n * 8;

                        if (n == 0)
                            n = 1;
                        if (n > MAX_LEN)
                            n = MAX_LEN;

                        //读取FIFO中接收到的数据
                        for (i = 0; i < n; i++)
                            backData[i] = readMFRC522(FIFODataReg);
                    }
                }
                else
                    status = MI_ERR;
            }

            //SetBitMask(ControlReg,0x80);           //timer stops
            //Write_MFRC522(CommandReg, PCD_IDLE);

            return status;
        }


        /******************************************************************************
         * 函 数 名：findCard
         * 功能描述：寻卡，读取卡类型号
         * 输入参数：reqMode--寻卡方式，
         *           TagType--返回卡片类型
         *                    0x4400 = Mifare_UltraLight
         *                    0x0400 = Mifare_One(S50)
         *                    0x0200 = Mifare_One(S70)
         *                    0x0800 = Mifare_Pro(X)
         *                    0x4403 = Mifare_DESFire
         * 返 回 值：成功返回MI_OK
         ******************************************************************************/
        byte findCard(byte reqMode, byte[] TagType)
        {
            byte status;
            int backBits;      //接收到的数据位数

            writeMFRC522(BitFramingReg, 0x07);    //TxLastBists = BitFramingReg[2..0] ???

            TagType[0] = reqMode;
            status = MFRC522ToCard(PCD_TRANSCEIVE, TagType, 1, TagType, out backBits);

            if ((status != MI_OK) || (backBits != 0x10))
                status = MI_ERR;

            return status;
        }

        /******************************************************************************
         * 函 数 名：anticoll
         * 功能描述：防冲突检测，读取选中卡片的卡序列号
         * 输入参数：serNum--返回4字节卡序列号,第5字节为校验字节
         * 返 回 值：成功返回MI_OK
         ******************************************************************************/
        byte anticoll(byte[] serNum, byte[] serNumOut)
        {
            byte status;
            byte i;
            byte serNumCheck = 0;
            int unLen;

            clearBitMask(Status2Reg, 0x08);   //TempSensclear
            clearBitMask(CollReg, 0x80);     //ValuesAfterColl
            writeMFRC522(BitFramingReg, 0x00);    //TxLastBists = BitFramingReg[2..0]

            serNum[0] = PICC_ANTICOLL;
            serNum[1] = 0x20;

            status = MFRC522ToCard(PCD_TRANSCEIVE, serNum, 2, out serNumOut, out unLen);

            if (status == MI_OK)
            {
                //校验卡序列号
                for (i = 0; i < 4; i++)
                {
                    *(serNum + i) = serNum[i];
                    serNumCheck ^= serNum[i];
                }
                if (serNumCheck != serNum[i])
                {
                    status = MI_ERR;
                }
            }

            setBitMask(CollReg, 0x80);    //ValuesAfterColl=1

            return status;
        }

        /******************************************************************************
         * 函 数 名：auth
         * 功能描述：验证卡片密码
         * 输入参数：authMode--密码验证模式
         *                     0x60 = 验证A密钥
         *                     0x61 = 验证B密钥
         *           BlockAddr--块地址
         *           Sectorkey--扇区密码
         *           serNum--卡片序列号，4字节
         * 返 回 值：成功返回MI_OK
         ******************************************************************************/
        byte auth(byte authMode, byte BlockAddr, byte[] Sectorkey, byte[] serNum)
        {
            byte status;
            int recvBits;
            byte i;
            byte[] buff = new byte[12];

            //验证指令+块地址＋扇区密码＋卡序列号
            buff[0] = authMode;
            buff[1] = BlockAddr;
            for (i = 0; i < 6; i++)
                buff[i + 2] = Sectorkey[i];
            for (i = 0; i < 4; i++)
                buff[i + 8] = serNum[i];

            status = MFRC522ToCard(PCD_AUTHENT, buff, 12, buff, out recvBits);
            if ((status != MI_OK) || ((readMFRC522(Status2Reg) & 0x08) == 0))
                status = MI_ERR;

            return status;
        }

        /******************************************************************************
         * 函 数 名：read
         * 功能描述：读块数据
         * 输入参数：blockAddr--块地址;recvData--读出的块数据
         * 返 回 值：成功返回MI_OK
         ******************************************************************************/
        byte read(byte blockAddr, byte[] recvData)
        {
            byte status;
            int unLen;

            recvData[0] = PICC_READ;
            recvData[1] = blockAddr;
            calculateCRC(recvData, 2, out recvData[2]);
            status = MFRC522ToCard(PCD_TRANSCEIVE, recvData, 4, recvData, out unLen);

            if ((status != MI_OK) || (unLen != 0x90))
                status = MI_ERR;

            return status;
        }

        /******************************************************************************
         * 函 数 名：write
         * 功能描述：写块数据
         * 输入参数：blockAddr--块地址;writeData--向块写16字节数据
         * 返 回 值：成功返回MI_OK
         ******************************************************************************/
        byte write(byte blockAddr, byte[] writeData)
        {
            byte status;
            int recvBits;
            byte i;
            byte[] buff = new byte[18];

            buff[0] = PICC_WRITE;
            buff[1] = blockAddr;
            calculateCRC(buff, 2, out buff[2]);
            status = MFRC522ToCard(PCD_TRANSCEIVE, buff, 4, buff, out recvBits);

            if ((status != MI_OK) || (recvBits != 4) || ((buff[0] & 0x0F) != 0x0A))
                status = MI_ERR;

            if (status == MI_OK)
            {
                for (i = 0; i < 16; i++)    //?FIFO?16Byte?? Datos a la FIFO 16Byte escribir
                    buff[i] = writeData[i];

                calculateCRC(buff, 16, &buff[16]);
                status = MFRC522ToCard(PCD_TRANSCEIVE, buff, 18, buff, out recvBits);

                if ((status != MI_OK) || (recvBits != 4) || ((buff[0] & 0x0F) != 0x0A))
                    status = MI_ERR;
            }

            return status;
        }

        /******************************************************************************
         * 函 数 名：selectTag
         * 功能描述：选卡，读取卡存储器容量
         * 输入参数：serNum--传入卡序列号
         * 返 回 值：成功返回卡容量
         ******************************************************************************/
        byte selectTag(byte[] serNum)
        {
            byte i;
            byte status;
            byte size;
            int recvBits;
            byte[] buffer = new byte[9];

            //ClearBitMask(Status2Reg, 0x08);                        //MFCrypto1On=0

            buffer[0] = PICC_SElECTTAG;
            buffer[1] = 0x70;

            for (i = 0; i < 5; i++)
                buffer[i + 2] = serNum[i];

            calculateCRC(buffer, 7, &buffer[7]);

            status = MFRC522ToCard(PCD_TRANSCEIVE, buffer, 9, buffer, out recvBits);
            if ((status == MI_OK) && (recvBits == 0x18))
                size = buffer[i];
            else
                size = 0;
            return size;
        }

        /******************************************************************************
         * 函 数 名：Halt
         * 功能描述：命令卡片进入休眠状态
         * 输入参数：无
         * 返 回 值：无
         ******************************************************************************/
        void halt()
        {
            byte status;
            int unLen;
            byte[] buff = new byte[4];

            buff[0] = PICC_HALT;
            buff[1] = 0;
            calculateCRC(buff, 2, buff[2]);

            status = MFRC522ToCard(PCD_TRANSCEIVE, buff, 4, buff, out unLen);
        }
    }
}
