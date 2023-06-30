using System;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;


namespace NetworkService
{
    public class SmartFarmSerialPort
    {
        private const string TAG = "SmartFarmSerialPort";
        private const int BUF_SIZE = 0x4000;


        //modbus 추가
        public J2ModbusRTUMaster m_modbusrtu;

        // SerialPort 관련 변수들
        public SerialPort mSerialPort;
        private byte[] mSerialPortReadBuffer;


        private ByteCircularBuffer mRecvBytesQueue_modbus;

        // Read Thread 관련 변수들

        private object mReadNotifyObject;


        private object mSyncObjsformodbus;




        public SmartFarmSerialPort()
        {
            mSerialPort = new SerialPort();
            mSerialPort.BaudRate = 115200;
            mSerialPort.Parity = Parity.None;
            mSerialPort.DataBits = 8;
            mSerialPort.StopBits = StopBits.One;
            mSerialPort.Handshake = Handshake.None;
            mSerialPort.Encoding = Encoding.ASCII;
            mSerialPort.WriteTimeout = 100;
            mSerialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived);




            mSerialPortReadBuffer = new byte[mSerialPort.ReadBufferSize * 0x10];

#if DEBUG


            mRecvBytesQueue_modbus = new ByteCircularBuffer(mSerialPort.ReadBufferSize * 100, true);


#else
			//mRecvBytesQueue = new ByteCircularBuffer(mSerialPort.ReadBufferSize, true);
#endif



            mReadNotifyObject = new object();








            mSyncObjsformodbus = new object();
            m_modbusrtu = new J2ModbusRTUMaster(mSerialPort, mReadNotifyObject, mSyncObjsformodbus);



        }

        ~SmartFarmSerialPort()
        {
            Close();
        }

        public bool linuxOpen(string tty, int mboudrate = 38400)
        {

            if (mSerialPort.IsOpen)
            {
                Debug.WriteLine("Serial::Open // 이미 open 되었습니다.");
                return true;
            }

            try
            {
                mSerialPort.PortName = tty;
                //mSerialPort.PortName = "COM3";
                mSerialPort.Parity = Parity.None;
                mSerialPort.DataBits = 8;
                mSerialPort.StopBits = StopBits.One;
                mSerialPort.Handshake = Handshake.None;
                mSerialPort.Encoding = Encoding.ASCII;
                mSerialPort.WriteTimeout = 100;
                mSerialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived);
                mSerialPort.Open();

            }
            catch (IOException e)
            {
                Console.WriteLine("Serial::Open // Fail!! // Device is not reponse. COM" + tty);
                Console.WriteLine(e.ToString());
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("Serial::Open // Fail!! COM" + tty);
                Console.WriteLine(e.ToString());

                return false;
            }


            //			mReadThread = new Thread(new ThreadStart(ReadThreadProc));
            //		mReadThreadRun = true;
            //	mReadThread.Start();

            return true;

        }


        public bool Open(int portNumber, int mboudrate = 38400)
        {
            if (mSerialPort.IsOpen)
            {
                Debug.WriteLine("Serial::Open // 이미 open 되었습니다.");
                return true;
            }

            try
            {
                mSerialPort.PortName = "COM" + portNumber;
                mSerialPort.BaudRate = mboudrate;
                mSerialPort.Open();


            }
            catch (IOException e)
            {
                Debug.WriteLine("Serial::Open // Fail!! // Device is not reponse. COM" + portNumber);
                Debug.WriteLine(e.ToString());
                return false;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Serial::Open // Fail!! COM" + portNumber);
                Debug.WriteLine(e.ToString());

                return false;
            }

            Debug.WriteLine("Serial::Open // Success!! COM" + portNumber);

            //			mReadThread = new Thread(new ThreadStart(ReadThreadProc));
            //		mReadThreadRun = true;
            //	mReadThread.Start();

            return true;
        }

        public bool IsOpen()
        {
            return mSerialPort.IsOpen;
        }

        public void Close()
        {
            mSerialPort.Close();



        }



        public bool Modbus_readonlyRegister(int Slave, int StartAddr, int Length, int waitMillisecond)
        {



            try
            {

                m_modbusrtu.ManualWordRead_F4(Slave, StartAddr, Length, false);
                lock (mSyncObjsformodbus)
                {

                    bool rv = Monitor.Wait(mSyncObjsformodbus, waitMillisecond);

                    if (rv == true && m_modbusrtu.wordDatas != null)
                    {

                        return true;
                    }
                    return false;
                }


            }
            catch (TimeoutException)
            {

                return false;
            }

        }



        public bool StandardModbus_readRegister(int Slave, int StartAddr, int Length, int waitMillisecond)
        {



            try
            {

                m_modbusrtu.StandardManualWordRead_F3(Slave, StartAddr, Length, waitMillisecond);




                if (m_modbusrtu.wordDatas != null)
                {

                    return true;
                }

                return false;
            }
            catch (TimeoutException)
            {
                Debug.WriteLine(" Modbus_readRegister TimeoutException");
                return false;
            }

        }

        public bool Modbus_readRegister(int Slave, int StartAddr, int Length, int waitMillisecond)
        {



            try
            {

                m_modbusrtu.ManualWordRead_F3(Slave, StartAddr, Length, false);


                for (int i = 0; i < waitMillisecond / 10; i++)
                {
                    if (m_modbusrtu.wordDatas != null)
                    {
                        break;
                    }

                    Thread.Sleep(10);


                }
                DateTime nPrev = DateTime.Now;

                lock (mSyncObjsformodbus)
                {
                    for (int i = 0; i < waitMillisecond / 10; i++)
                    {

                        int gap = Convert.ToInt32((DateTime.Now - nPrev).TotalMilliseconds);


                        // bool rv = Monitor.Wait(mSyncObjsformodbus, waitMillisecond);

                        if (m_modbusrtu.wordDatas != null && gap > 10)
                        {

                            return true;
                        }
                        Thread.Sleep(10);
                    }

                    Debug.WriteLine(" Modbus_readRegister no data");
                    return false;
                }


            }
            catch (TimeoutException)
            {
                Debug.WriteLine(" Modbus_readRegister TimeoutException");
                return false;
            }

        }

        public bool StandardModbus_readRegisterF4(int Slave, int StartAddr, int Length, int waitMillisecond)
        {



            try
            {

                m_modbusrtu.StandardManualWordRead_F4(Slave, StartAddr, Length, waitMillisecond);




                if (m_modbusrtu.wordDatas != null)
                {

                    return true;
                }

                return false;
            }
            catch (TimeoutException)
            {
                Debug.WriteLine(" Modbus_readRegister TimeoutException");
                return false;
            }

        }



        public bool Modbus_writeRegister_word(int Slave, int StartAddr, int value, int waitMillisecond)
        {



            try
            {

                m_modbusrtu.StandardManualWordWrite(Slave, StartAddr, value, false);
                if (m_modbusrtu.wordDatas != null)
                {

                    return true;
                }

                return false;


            }
            catch (TimeoutException)
            {

                return false;
            }

        }

        public bool Modbus_writeRegister_words(int Slave, int StartAddr, int[] values, int waitMillisecond)
        {

            try
            {
                m_modbusrtu.StandardManualMultiWordWrite(Slave, StartAddr, values, false);
                if (m_modbusrtu.wordDatas != null)
                {

                    return true;
                }

                return false;


            }
            catch (TimeoutException)
            {

                return false;
            }

        }

        public bool Modbus_writeRegister_float(int Slave, int StartAddr, float fvalue, int waitMillisecond)
        {

            try
            {
                byte[] byteArray = BitConverter.GetBytes(fvalue);
                int[] shortarry = new int[2];

                shortarry[0] = Convert.ToUInt16(byteArray[1] << 8 | byteArray[0]);
                shortarry[1] = Convert.ToUInt16(byteArray[3] << 8 | byteArray[2]);
                m_modbusrtu.ManualMultiWordWrite(Slave, StartAddr, shortarry, false);
                lock (mSyncObjsformodbus)
                {

                    bool rv = Monitor.Wait(mSyncObjsformodbus, waitMillisecond);

                    return rv;
                }


            }
            catch (TimeoutException)
            {

                return false;
            }

        }



        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {



                int readCount = mSerialPort.Read(mSerialPortReadBuffer, 0, mSerialPort.BytesToRead);

                if (readCount > 0)
                {
                    Debug.WriteLine("mSerialPort.BytesToRead =" + mSerialPort.BytesToRead + ",readCount =" + readCount);
                    Debug.WriteLine(Encoding.ASCII.GetString(mSerialPortReadBuffer, 0, readCount));
                    Debug.WriteLine("\r\nrrrrr=");
                    for (int i = 0; i < readCount; i++)
                    {

                        Debug.Write("," + mSerialPortReadBuffer[i].ToString("X"));
                    }
                }


                lock (mReadNotifyObject)
                {
                    m_modbusrtu.mRecvBytesformodbus.pushBytes(mSerialPortReadBuffer, 0, readCount);

                    Monitor.Pulse(mReadNotifyObject);
                }
            }
            catch (Exception e1)
            {
                Debug.WriteLine(e1.ToString());
            }
        }




    }
}
