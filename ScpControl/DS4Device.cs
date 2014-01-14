﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HidLibrary;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
namespace ScpControl
{
    public struct ledColor
    {
        public byte red;
        public byte green;
        public byte blue;
    }

    public class DS4Device
    {
        private short charge = 0;
        private bool isUSB = true;
        private int deviceNum = 0;
        private bool m_isTouchEnabled = false;
        private bool lastIsActive = false;
        private Point lastPoint = new Point(0, 0);
        private byte lastL1Value = 0;
        private byte lastL2Value = 0;
        private byte rumbleBoost = 100;
        private byte smallRumble = 0;
        private byte bigRumble = 0;
        private byte ledFlash = 0;
        private HidDevice hid_device;
        private ledColor m_LedColor;

        public HidDevice Device
        {
            get { return hid_device; }
        }

        public byte SmallRumble
        {
            get {
                uint boosted = ((uint)smallRumble * (uint)rumbleBoost)/100;
                if (boosted > 255)
                    boosted = 255;
                return (byte)boosted; 
            }
            set { smallRumble = value; }
        }

        public byte BigRumble
        {
            get
            {
                uint boosted = ((uint)bigRumble * (uint)rumbleBoost) / 100;
                if (boosted > 255)
                    boosted = 255;
                return (byte)boosted;
            }
            set { bigRumble = value; }
        }

        public byte RumbleBoost
        {
            get { return rumbleBoost; }
            set { rumbleBoost = value; }
        }

        public bool isTouchEnabled
        {
            get { return m_isTouchEnabled; }
            set { m_isTouchEnabled = value; }
        }

        public void setLedColor(byte red, byte green, byte blue)
        {
            m_LedColor = new ledColor();
            m_LedColor.red = red;
            m_LedColor.green = green;
            m_LedColor.blue = blue;
        }

        public ledColor LedColor
        {
            get { return m_LedColor; }
            set { m_LedColor = value; }
        }

        public byte FlashLed
        {
            get { return ledFlash; }
            set { ledFlash = value; }
        }

        public DS4Device(HidDevice device, int controllerID)
        {
            hid_device = device;
            deviceNum = controllerID;
            isUSB = Device.Capabilities.InputReportByteLength == 64;
            isTouchEnabled = Global.getTouchEnabled(deviceNum);
        }

        public byte[] retrieveData()
        {
            if (!isUSB)
            {
                byte[] data64 = null;
                byte[] data = new byte[Device.Capabilities.InputReportByteLength];
                if ( Device.ReadWithFileStream(data,16) == HidDevice.ReadStatus.Success)
                {
                    data64 = new byte[Device.Capabilities.InputReportByteLength];
                    Array.Copy(data, 2, data64, 0, 64);
                    bool touchPressed = (data64[7] & (1 << 2 - 1)) != 0 ? true : false;
                    toggleTouchpad(data64[8], data64[9], touchPressed);
                    updateBatteryStatus(data64[30], isUSB);

                    byte[] touchData = new byte[4];
                    Array.Copy(data64, 35, touchData, 0, 4);
                    if (isTouchEnabled)
                    {
                        HandleTouchpad(touchData);
                        permormMouseClick(data64[6]);
                    }
                }
                Device.flush_Queue();
                return mapButtons(data64);
               

            }
            else
            {
                byte[] data = new byte[Device.Capabilities.InputReportByteLength];
                if ( Device.ReadWithFileStream(data, 8) == HidDevice.ReadStatus.Success)
                {
                    bool touchPressed = (data[7] & (1 << 2 - 1)) != 0 ? true : false;
                    toggleTouchpad(data[8], data[9], touchPressed);
                    updateBatteryStatus(data[30], isUSB);
                    byte[] touchData = new byte[4];
                    Array.Copy(data, 35, touchData, 0, 4);
                    if (isTouchEnabled)
                    {
                        HandleTouchpad(touchData);
                        permormMouseClick(data[6]);
                    }
                }
                else
                {
                    return null;
                }
                Device.flush_Queue();
                return mapButtons(data);
            }
        }

        private byte[] mapButtons(byte[] data)
        {
            if (data == null)
            {
                return null;
            }
            byte[] Report = new byte[64];

            Report[1] = 0x02;
            Report[2] = 0x05;
            Report[3] = 0x12;

            Report[14] = data[1]; //Left Stick X


            Report[15] = data[2]; //Left Stick Y


            Report[16] = data[3]; //Right Stick X


            Report[17] = data[4]; //Right Stick Y


            Report[26] = data[8]; //Left Trrigger


            Report[27] = data[9]; //Right Trigger

            var bitY = ((byte)data[5] & (1 << 8 - 1)) != 0;
            var bitB = ((byte)data[5] & (1 << 7 - 1)) != 0;
            var bitA = ((byte)data[5] & (1 << 6 - 1)) != 0;
            var bitX = ((byte)data[5] & (1 << 5 - 1)) != 0;
            var dpadUpBit = ((byte)data[5] & (1 << 4 - 1)) != 0;
            var dpadDownBit = ((byte)data[5] & (1 << 3 - 1)) != 0;
            var dpadLeftBit = ((byte)data[5] & (1 << 2 - 1)) != 0;
            var dpadRightBit = ((byte)data[5] & (1 << 1 - 1)) != 0;



            bool[] dpad = { dpadRightBit, dpadLeftBit, dpadDownBit, dpadUpBit };
            byte c = 0;
            for (int i = 0; i < 4; ++i)
            {
                if (dpad[i])
                {
                    c |= (byte)(1 << i);
                }
            }

            bool dpadUp = false;
            bool dpadLeft = false;
            bool dpadDown = false;
            bool dpadRight = false;

            int dpad_state = c;
            switch (dpad_state)
            {
                case 0:
                    dpadUp = true;
                    dpadDown = false;
                    dpadLeft = false;
                    dpadRight = false;
                    break;
                case 1:
                    dpadUp = true;
                    dpadDown = false;
                    dpadLeft = false;
                    dpadRight = true;
                    break;
                case 2:
                    dpadUp = false;
                    dpadDown = false;
                    dpadLeft = false;
                    dpadRight = true;
                    break;
                case 3:
                    dpadUp = false;
                    dpadDown = true;
                    dpadLeft = false;
                    dpadRight = true;
                    break;
                case 4:
                    dpadUp = false;
                    dpadDown = true;
                    dpadLeft = false;
                    dpadRight = false;
                    break;
                case 5:
                    dpadUp = false;
                    dpadDown = true;
                    dpadLeft = true;
                    dpadRight = false;
                    break;
                case 6:
                    dpadUp = false;
                    dpadDown = false;
                    dpadLeft = true;
                    dpadRight = false;
                    break;
                case 7:
                    dpadUp = true;
                    dpadDown = false;
                    dpadLeft = true;
                    dpadRight = false;
                    break;
                case 8:
                    dpadUp = false;
                    dpadDown = false;
                    dpadLeft = false;
                    dpadRight = false;
                    break;

            }

            var thumbRight = ((byte)data[6] & (1 << 8 - 1)) != 0;
            var thumbLeft = ((byte)data[6] & (1 << 7 - 1)) != 0;
            var start = ((byte)data[6] & (1 << 6 - 1)) != 0;
            var options = ((byte)data[6] & (1 << 5 - 1)) != 0;
            var abit5 = ((byte)data[6] & (1 << 4 - 1)) != 0;
            var abit6 = ((byte)data[6] & (1 << 3 - 1)) != 0;
            var rb = ((byte)data[6] & (1 << 2 - 1)) != 0;
            var lb = ((byte)data[6] & (1 << 1 - 1)) != 0;

            bool[] r11 = { false, false, lb, rb, bitY, bitB, bitA, bitX };

            byte b11 = 0;
            for (int i = 0; i < 8; ++i)
            {
                if (r11[i])
                {
                    b11 |= (byte)(1 << i);
                }
            }
            Report[11] = b11;

            bool[] r10 = { options, thumbLeft, thumbRight, start, dpadUp, dpadRight, dpadDown, dpadLeft };
            byte b10 = 0;
            for (int i = 0; i < 8; ++i)
            {
                if (r10[i])
                {
                    b10 |= (byte)(1 << i);
                }
            }

            Report[10] = b10;

            //Guite
            var Guide = data[7] & (1 << 1 - 1);
            Report[12] = (byte)(Guide != 0 ? 0xFF : 0x00);

            return Report;

        }

        private void toggleTouchpad(byte lt, byte rt, bool touchPressed)
        {
            if (lt == 255 && rt == 255 && touchPressed)
            {
                isTouchEnabled = true;
            }
            else if (lt == 255 && touchPressed && rt == 0)
            {
                isTouchEnabled = false;
            }
        }

        private void updateBatteryStatus(int status, bool isUsb)
        {
            int battery = 0;
            if (isUsb)
            {
                battery = (status - 16) * 10;
                if (battery > 100)
                    battery = 100;
            }
            else
            {
                battery = (status + 1) * 10;
                if (battery > 100)
                    battery = 100;
            }

            this.charge = (short) battery;
            if (Global.getLedAsBatteryIndicator(deviceNum))
            {
                byte[] green = { 0, 255, 0 };
                byte[] red = { 255, 0, 0 };

                uint ratio = (uint)battery;
                ledColor color = Global.getTransitionedColor(red, green, ratio);
                LedColor = color;


            }
            else
            {
                ledColor color = Global.loadColor(deviceNum);
                LedColor = color;
            }

            if (Global.getFlashWhenLowBattery(deviceNum))
            {
                if (battery < 20)
                {
                    FlashLed = 0x80;
                }
                else
                {
                    FlashLed = 0;
                }
            }
            else
            {
                FlashLed = 0;
            }
        }

        public void sendOutputReport()
        {
            if (!isUSB)
            {

                byte[] data2 = new byte[78];

                for (int i = 0; i < data2.Length; i++)
                {
                    data2[i] = 0x0;
                }

                data2[0] = 0x11;
                data2[1] = 128;
                data2[3] = 0xff;

                data2[6] = BigRumble; //motor
                data2[7] = SmallRumble; //motor

                data2[8] = LedColor.red; //red
                data2[9] = LedColor.green; //green
                data2[10] = LedColor.blue; //blue
                data2[11] = FlashLed; //flash;
                data2[12] = FlashLed; //flash;

                if (Device.WriteOutputReportViaControl(data2))
                {
                    //success
                }
            }
            else
            {
                byte[] data = new byte[Device.Capabilities.OutputReportByteLength];


                data[0] = 0x5;
                data[1] = 0xFF;
                data[4] = BigRumble; //motor
                data[5] = SmallRumble; //motor

                data[6] = LedColor.red; //red
                data[7] = LedColor.green; //green
                data[8] = LedColor.blue; //blue
                data[9] = FlashLed; //flash;
                data[10] = FlashLed; //flash;


                if (Device.WriteOutputReportViaInterrupt(data,8))
                {
                    //success
                }
            }
        }

        private void HandleTouchpad(byte[] data)
        {
            Cursor c = new Cursor(Cursor.Current.Handle);

            bool _isActive = (data[0] >> 7) != 0 ? false : true;
            if (_isActive)
            {

                int currentX = data[1] + ((data[2] & 0xF) * 255);
                int currentY = ((data[2] & 0xF0) >> 4) + (data[3] * 16);

                if (lastIsActive != _isActive)
                {
                    lastPoint = new Point(currentX, currentY);
                }

                int deltax = currentX - lastPoint.X;
                int deltay = currentY - lastPoint.Y;
                float sensitivity = Global.getTouchSensitivity(deviceNum)/(float)100;
                deltax =(int)( (float)deltax * sensitivity);
                deltay = (int)((float)deltay * sensitivity);
                Cursor.Position = new Point(Cursor.Position.X + deltax, Cursor.Position.Y + deltay);
                lastPoint = new Point(currentX, currentY);

            }
            lastIsActive = _isActive;

        }

        private void permormMouseClick(byte data)
        {

            if (data == 1 && lastL1Value != data)
            {
                mouse_event(0x8000 | 0x0002 | 0x0004, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);

            }
            if (data == 2 && lastL2Value != data)
            {
                mouse_event(0x8000 | 0x0008 | 0x0010, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);

            }
            lastL1Value = data;
            lastL2Value = data;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        public String toString()
        {
            return "Controller " + (deviceNum + 1) + ": " + "Battery = "+charge+"%," +" Touchpad Enabled = " + isTouchEnabled + (isUSB ? " (USB)" : " (BT)");
        }

    }
    

 

}