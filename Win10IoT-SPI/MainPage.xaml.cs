using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Spi;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace Win10IoT_SPI
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private RFIDRC522.Driver driver = null;

        private string uid = "";

        private long times = 0;
        private bool cracked = false;
        private int secNum = 0;
        private byte status = 0;

        bool[] sectorKeyACracked = {
            false, false, false, false,
            false, false, false, false,
            false, false, false, false,
            false, false, false, false
        };

        //扇区A密码，16个扇区，每个扇区密码6Byte
        private byte[] sectorKeyA = {
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,  //1
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,  //2
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,  //3
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,  //4
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,  //5
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,  //6
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,  //7
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,  //8
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,  //9
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,  //10
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,  //11
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,  //12
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,  //13
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,  //14
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,  //15
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF   //16
        };

        private TextBlock[] sectors = new TextBlock[16];

        void printPasswords()
        {
            int i;
            for (i = 0; i < 16; i++)
            {
                Debug.Write(i + " sector : ");
                printPassword(i);
            }
        }

        void printPassword(int num)
        {
            Debug.Write(sectorKeyA[num * 6 + 0].ToString("X").PadLeft(2, '0'));
            Debug.Write(sectorKeyA[num * 6 + 1].ToString("X").PadLeft(2, '0'));
            Debug.Write(sectorKeyA[num * 6 + 2].ToString("X").PadLeft(2, '0'));
            Debug.Write(sectorKeyA[num * 6 + 3].ToString("X").PadLeft(2, '0'));
            Debug.Write(sectorKeyA[num * 6 + 4].ToString("X").PadLeft(2, '0'));
            Debug.Write(sectorKeyA[num * 6 + 5].ToString("X").PadLeft(2, '0'));
            Debug.WriteLine("");
        }

        void crackSector()
        {
            if (secNum < 0 || secNum > 15)
            {
                Debug.Write("Cannot crack Sector ");
                Debug.Write(secNum);
                Debug.WriteLine("");
                return;
            }

            byte[] serNum = Enumerable.Range(0, this.uid.Length)
                     .Where(x => x % 2 == 0)
                     .Select(x => Convert.ToByte(this.uid.Substring(x, 2), 16))
                     .ToArray();
            byte[] keyA = new byte[6];
            for (int i = 0; i < 6; i++)
                keyA[i] = sectorKeyA[secNum * 6 + i];
            Debug.WriteLine("Attemp with " + BitConverter.ToString(keyA));
            Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                this.uidLabel.Text = this.uid.ToString();
                this.password.Text = BitConverter.ToString(keyA);
                this.sector.Text = "" + secNum + " " + times;
            });
            status = this.driver.Auth(RFIDRC522.PiccCommands.AuthenticateKeyA, (byte)(secNum * 4), keyA, serNum);        //认证
            if (status == 0)
            {
                Debug.WriteLine("crack success!");

                Debug.Write(secNum);
                Debug.Write(" sector : ");
                printPassword(secNum);

                cracked = true;
                sectorKeyACracked[secNum] = true;
                return;
            }
            else
            {
                if (times == 0)
                {
                    Debug.WriteLine("Attemp with A0B0C0D0E0F0");
                    sectorKeyA[secNum * 6 + 0] = 0xA0;
                    sectorKeyA[secNum * 6 + 1] = 0xB0;
                    sectorKeyA[secNum * 6 + 2] = 0xC0;
                    sectorKeyA[secNum * 6 + 3] = 0xD0;
                    sectorKeyA[secNum * 6 + 4] = 0xE0;
                    sectorKeyA[secNum * 6 + 5] = 0xF0;
                }
                else if (times == 1)
                {
                    Debug.WriteLine("Attemp with 000000000000");
                    sectorKeyA[secNum * 6 + 0] = 0x00;
                    sectorKeyA[secNum * 6 + 1] = 0x00;
                    sectorKeyA[secNum * 6 + 2] = 0x00;
                    sectorKeyA[secNum * 6 + 3] = 0x00;
                    sectorKeyA[secNum * 6 + 4] = 0x00;
                    sectorKeyA[secNum * 6 + 5] = 0x01;
                }
                else
                {
                    sectorKeyA[secNum * 6 + 5] += 1;
                    if (sectorKeyA[secNum * 6 + 5] == 0xFF)
                    {
                        sectorKeyA[secNum * 6 + 5] = 0x00;
                        sectorKeyA[secNum * 6 + 4] += 1;
                        if (sectorKeyA[secNum * 6 + 4] == 0xFF)
                        {
                            sectorKeyA[secNum * 6 + 4] = 0x00;
                            sectorKeyA[secNum * 6 + 3] += 1;
                            if (sectorKeyA[secNum * 6 + 3] == 0xFF)
                            {
                                sectorKeyA[secNum * 6 + 3] = 0x00;
                                sectorKeyA[secNum * 6 + 2] += 1;
                                if (sectorKeyA[secNum * 6 + 2] == 0xFF)
                                {
                                    sectorKeyA[secNum * 6 + 2] = 0x00;
                                    sectorKeyA[secNum * 6 + 1] += 1;
                                    if (sectorKeyA[secNum * 6 + 1] == 0xFF)
                                    {
                                        sectorKeyA[secNum * 6 + 1] = 0x00;
                                        sectorKeyA[secNum * 6 + 0] += 1;
                                        if (sectorKeyA[secNum * 6 + 0] == 0xFF)
                                        {
                                            Debug.WriteLine("All password tried, not found");
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                Debug.Write("Attemp with ");
                printPassword(secNum);
            }
        }

        public MainPage()
        {
            this.InitializeComponent();

            this.sectors[0] = this.s0;
            this.sectors[1] = this.s1;
            this.sectors[2] = this.s2;
            this.sectors[3] = this.s3;
            this.sectors[4] = this.s4;
            this.sectors[5] = this.s5;
            this.sectors[6] = this.s6;
            this.sectors[7] = this.s7;
            this.sectors[8] = this.s8;
            this.sectors[9] = this.s9;
            this.sectors[10] = this.s10;
            this.sectors[11] = this.s11;
            this.sectors[12] = this.s12;
            this.sectors[13] = this.s13;
            this.sectors[14] = this.s14;
            this.sectors[15] = this.s15;
            //this.initRFIDRC522Async();
            Debug.WriteLine("Reach here");
        }

        private void startCrackTask()
        {
            Task task = new Task(() =>
            {
                while (true)
                {
                    if (this.driver.IsTagPresent())
                    {
                        var uid = this.driver.ReadUid();
                        if (uid != null)
                        {
                            if (this.uid != uid.ToString())
                            {
                                this.uid = uid.ToString();
                                Debug.WriteLine("Tag presented " + this.uid);
                                this.driver.SelectTag(uid);
                                Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                                {
                                    this.uidLabel.Text = this.uid;
                                });
                            }
                            else
                            {
                                Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                                {
                                    for (int i = 0; i < 16; i++)
                                    {
                                        this.sectors[i].Text =
                                            sectorKeyA[i * 6 + 0].ToString("X").PadLeft(2, '0') +
                                            sectorKeyA[i * 6 + 1].ToString("X").PadLeft(2, '0') +
                                            sectorKeyA[i * 6 + 2].ToString("X").PadLeft(2, '0') +
                                            sectorKeyA[i * 6 + 3].ToString("X").PadLeft(2, '0') +
                                            sectorKeyA[i * 6 + 4].ToString("X").PadLeft(2, '0') +
                                            sectorKeyA[i * 6 + 5].ToString("X").PadLeft(2, '0');
                                    }
                                });
                                if (cracked == true && secNum <= 15)
                                {
                                    secNum++;
                                    times = 0;
                                    cracked = false;
                                }
                                else if (secNum >= 16)
                                {
                                    Debug.WriteLine("All password found");
                                    printPasswords();
                                    this.driver.HaltTag();
                                }
                                else if (cracked == false)
                                {
                                    Debug.Write("Crack the card: ...");
                                    Debug.Write(secNum + 1);
                                    Debug.Write(" ");
                                    Debug.WriteLine(times);
                                    crackSector();
                                    times += 1;
                                }
                                else
                                {
                                    Debug.WriteLine("Nothing to do");
                                    this.driver.HaltTag();
                                }
                            }
                        } else
                        {
                            Debug.WriteLine("Invalid UID");
                        }
                    }

                }
            });
            task.Start();
        }

        private async void initRFIDRC522Async()
        {
            if (this.driver == null)
            {
                this.driver = new RFIDRC522.Driver();
                await this.driver.InitIO();
            }
            Debug.WriteLine("Init ok");
            this.startCrackTask();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.driver == null)
            {
                this.initRFIDRC522Async();
            } 
        }
    }
}
