using System;
using System.Threading;
using System.IO;
using System.IO.Ports;
using System.Text;

namespace Koro_Robot_driver
{
    public class RobotClass : RobotDefine
    {
        private static Thread drvThread;
        private static bool _continue;
        private static bool _bSet_flag;

        private static SerialPort _serialPort;        

        public static ROBOT_STATUS[] robotStatus = new ROBOT_STATUS[ROBOT_MAX];
        public static Error_List error_List;

        public static void Koro_robot_Init()
        {
            drvThread = null;
            bool bRtn;

            robotStatus[0].sR_ErrorSts = new string[2];
            robotStatus[0].sR_StageNum = new string[2];

            for (int i = 0; i < ROBOT_MAX; i++)
            {
                robotStatus[i].sR_Initial = string.Empty;
                robotStatus[i].sR_Busy = string.Empty;
                robotStatus[i].sR_VacSol1 = string.Empty;
                robotStatus[i].sR_VacSns1 = string.Empty;
                robotStatus[i].sR_VacSol2 = string.Empty;
                robotStatus[i].sR_VacSns2 = string.Empty;
                robotStatus[i].sR_ErrorSts[0] = string.Empty;
                robotStatus[i].sR_ErrorSts[1] = string.Empty;
                robotStatus[i].sR_Arm_A = string.Empty;
                robotStatus[i].sR_Arm_B = string.Empty;
                robotStatus[i].sR_Axis_Z = string.Empty;
                robotStatus[i].sR_Pause = string.Empty;
                robotStatus[i].sR_SideDoor = string.Empty;
                robotStatus[i].sR_StageNum[0] = string.Empty;
                robotStatus[i].sR_StageNum[1] = string.Empty;
            }

            bRtn = _DRV_INIT();
            if (bRtn)
            {
                _continue = true;
                _bSet_flag = false;

                drvThread = new Thread(_Koro_robot_thread);
                drvThread.Start();
            }
            else
            {
                Global.EventLog("Koro robot driver initialization fail");
                _DRV_CLOSE();
            }
        }

        private static bool _DRV_INIT()
        {
            if (_InitPortInfo())
            {
                Global.EventLog("Acquisition of serial communication port information is completed");
            }
            else
            {
                return false;
            }

            if (_PortOpen())
            {
                Global.EventLog("Serial port opened successfully");                
            }
            else
            {
                return false;
            }

            return true;
        }

        private static bool _InitPortInfo()
        {
            _serialPort = new SerialPort();

            string sTmpData;
            string FileName = "RobotPortInfo.txt";

            try
            {
                if (File.Exists(Global.BasePath + FileName))
                {
                    byte[] bytes;
                    using (var fs = File.Open(Global.BasePath + FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        bytes = new byte[fs.Length];
                        fs.Read(bytes, 0, (int)fs.Length);
                        sTmpData = Encoding.Default.GetString(bytes);

                        char sp = ',';
                        string[] spString = sTmpData.Split(sp);
                        for (int i = 0; i < spString.Length; i++)
                        {
                            string sPortName = spString[0];
                            int iBaudRate = int.Parse(spString[1]);
                            int iDataBits = int.Parse(spString[2]);
                            int iStopBits = int.Parse(spString[3]);
                            int iParity = int.Parse(spString[4]);

                            _serialPort.PortName = sPortName;
                            _serialPort.BaudRate = iBaudRate;
                            _serialPort.DataBits = iDataBits;
                            _serialPort.StopBits = (StopBits)iStopBits;
                            _serialPort.Parity = (Parity)iParity;

                            _serialPort.ReadTimeout = 500;
                            _serialPort.WriteTimeout = 500;
                        }
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (FileLoadException)
            {                
                return false;
            }
        }

        private static bool _PortOpen()
        {
            try
            {
                string[] ports = SerialPort.GetPortNames();
                foreach (string port in ports)
                {
                    if (port != "")
                    {
                        _serialPort.Open();
                        if (_serialPort.IsOpen)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                return false;
            }
            catch (IOException)
            {                
                return false;
            }
        }

        public static void _DRV_CLOSE()
        {
            _continue = false;
            
            if (drvThread != null)
            {
                drvThread.Abort();
                Global.EventLog("Koro robot thread abort");
            }            
            
            Global.EventLog("Koro robot driver close");
        }

        #region PARAMETER READ THREAD
        private static void _Koro_robot_thread()
        {
            try
            {
                while (_continue)
                {
                    if (!_bSet_flag)
                    {
                        _Parameter_read();
                        //_Position_read();

                        Thread.Sleep(10);
                    }                    
                }
            }
            catch (ThreadStateException ex)
            {
                Global.EventLog(string.Format("Koro robot thread error : {0}", ex));
            }
        }
        
        private static void _Parameter_read()
        {
            try
            {
                // Robot status request             
                string send_Command = string.Format("{0:D1}RSTS{1}", r_Id1, Convert.ToChar(RS_CR));
                _serialPort.Write(send_Command);
                Global.EventLog("Send : " + send_Command);

                Thread.Sleep(50);

                string readData = _serialPort.ReadLine();
                Global.EventLog("Recv : " + readData);

                if (readData.Length >= 16)
                {
                    int nSize = readData.Length;
                    int bufPos = 0;
                    char[] charArray;
                    charArray = new char[nSize];
                    for (int i = 0; i < nSize; i++)
                    {
                        charArray[bufPos++] = readData[i];
                    }

                    if ((charArray[16] == Convert.ToChar(RS_CR))) //&& (charArray[17] == Convert.ToChar(RS_LF)) )
                    {
                        _ROBOT_STATUS_PARSING(charArray);
                    }

                    Thread.Sleep(50);
                }                
            }
            catch (Exception ex)
            {
                Global.EventLog(ex.Message);
            }            
        }

        private static void _Position_read()
        {
            try
            {
                // 현재 Position Up - Load
                string send_Command = string.Format("{0:D1}RPR{1}", r_Id1, Convert.ToChar(RS_CR));
                _serialPort.Write(send_Command);
                Global.EventLog("Send : " + send_Command);

                Thread.Sleep(50);
            }
            catch (Exception ex)
            {
                Global.EventLog(ex.Message);
            }
        }

        private static void _ROBOT_STATUS_PARSING(char[] cArrData)
        {
            // initial
            if (cArrData[1] == '1')
                robotStatus[r_Id1].sR_Initial = "Initial Complete";
            else
                robotStatus[r_Id1].sR_Initial = "Not Initial";

            // busy
            if (cArrData[2] == '1')
                robotStatus[r_Id1].sR_Busy = "Busy";
            else
                robotStatus[r_Id1].sR_Busy = "StandBy";

            // Vacuum sol v/v #1
            if (cArrData[3] == '1')
                robotStatus[r_Id1].sR_VacSol1 = "On";
            else
                robotStatus[r_Id1].sR_VacSol1 = "Off";

            // Vacuum sensor #1
            if (cArrData[4] == '1')
                robotStatus[r_Id1].sR_VacSns1 = "On";
            else
                robotStatus[r_Id1].sR_VacSns1 = "Off";

            // Vacuum sol v/v #2
            if (cArrData[5] == '1')
                robotStatus[r_Id1].sR_VacSol2 = "On";
            else
                robotStatus[r_Id1].sR_VacSol2 = "Off";

            // Vacuum sensor #2
            if (cArrData[6] == '1')
                robotStatus[r_Id1].sR_VacSns2 = "On";
            else
                robotStatus[r_Id1].sR_VacSns2 = "Off";

            // Error status 10의 자리 (0 ~ 9)
            robotStatus[r_Id1].sR_ErrorSts[0] = cArrData[7].ToString();
            // Error status 1의 자리 (0 ~ 9)
            robotStatus[r_Id1].sR_ErrorSts[1] = cArrData[8].ToString();

            // Arm A extend/retract
            if (cArrData[9] == '1')
                robotStatus[r_Id1].sR_Arm_A = "Extend";
            else
                robotStatus[r_Id1].sR_Arm_A = "Retract";

            // Arm B extend/retract
            if (cArrData[10] == '1')
                robotStatus[r_Id1].sR_Arm_B = "Extend";
            else
                robotStatus[r_Id1].sR_Arm_B = "Retract";

            // Axis Z up/down
            if (cArrData[11] == '1')
                robotStatus[r_Id1].sR_Axis_Z = "Up";
            else
                robotStatus[r_Id1].sR_Axis_Z = "Down";

            // Robot pause status
            if (cArrData[12] == '1')
                robotStatus[r_Id1].sR_Pause = "Pause";
            else
                robotStatus[r_Id1].sR_Pause = "Normal";

            // Side door status
            if (cArrData[13] == '1')
                robotStatus[r_Id1].sR_SideDoor = "Open";
            else
                robotStatus[r_Id1].sR_SideDoor = "Normal";

            // Stage number 10의 자리 (0 ~ 9)
            robotStatus[r_Id1].sR_StageNum[0] = cArrData[14].ToString();
            // Stage number 1의 자리 (0 ~ 9)
            robotStatus[r_Id1].sR_StageNum[1] = cArrData[15].ToString();


            // Error code
            string sE1 = robotStatus[r_Id1].sR_ErrorSts[0];
            string sE2 = robotStatus[r_Id1].sR_ErrorSts[1];
            //error_List.error_code = string.Format("{0}{1}", sE1, sE2);
            //sErrorName = error_List.error_code;
        }
        #endregion

        #region SETTING FUNCTION

        /*
         * ROBOT HOME 동작을 실행
        1. F-AXIS SENSOR 0 or 180 도에 위치일경우에만 다음단계로 이동한다
           그렇치 않을 경우 error 발생 한다
        2. A-AXIS ARM HOM 실행           
        3. B-AXIS ARM HOM 실행
        4. T-AXIS HOM 실행           
        5. Z-AXIS HOM 실행
        6. X-AXIS HOM 실행
        7. 0APP 32B 위치로 이동 후 F-AXIS HOM 실행
        8. 00 STAGE 위치로 이동 후 완료
        */
        public static void SetRobotHome()
        {
            _bSet_flag = true;
            string readData = string.Empty;

            string send_Command = string.Format("{0:D1}HOM{1}", r_Id1, Convert.ToChar(RS_CR));
            _serialPort.Write(send_Command);
            Global.EventLog("Send : " + send_Command);

            Thread.Sleep(100);

            readData = _serialPort.ReadLine();
            Global.EventLog("Recv : " + readData);

            Thread.Sleep(100);

            Global.EventLog(_DataCheck(readData));

            _bSet_flag = false;
        }

        /*
         * 원하는 Stage로 이동하여 선택 한 Slot의 wafer를 Pick-up
        1. Z축과 T축 X축이 Stage 'aa', Slot 'bb' 위치 (Wafer 하단)로 이동
        2. R축 Extend
           ※ Z, T, X축 Extend위치 SPD설정 참고
        3. Vacuum Sol v/v 를 ON
        4. Z축 Up실행
           ※ Z축 Up 실행 위치는 SUD 설정 참고
        5. Wafer 유.무(Vaccum Sensor 사용)를 확인
           a. Wafer 유 -> A or B축 Retract
           b. Wafer 무 -> Vacuum Sol v/v 를 Off후 Z축 Down
                          위의 4, 5 과정을 지정된 회수 반복 시행
           ※ Z축 Down 실행 위치는 SUD 설정 참고
              Wafer Check 회수는 SPM 설정 참고
        */
        public static void Pick(int Stage, int Slot)
        {
            _bSet_flag = true;
            string readData = string.Empty;

            string send_Command = string.Format("{0:D1}GET{1}{2:D2}{3:D2}{4}", r_Id1, Convert.ToChar(RS_SP), Stage, Slot, Convert.ToChar(RS_CR));
            _serialPort.Write(send_Command);
            Global.EventLog("Send : " + send_Command);

            Thread.Sleep(100);

            readData = _serialPort.ReadLine();
            Global.EventLog("Recv : " + readData);

            Thread.Sleep(100);

            Global.EventLog(_DataCheck(readData));

            _bSet_flag = false;
        }

        /*
         * 원하는 Stage로 이동하여 Wafer를 선택 한 Slot에 Place
        1. Z축과 T축 Stage 'aa', Slot 'bb' 위치 (Wafer 상단)로 이동
        2. R축 Extend
           ※ Z, T축 Extend위치 SPD 설정 참고
        3. Vacuum Sol v/v 를 Off
        4. Z축 Down 실행
           ※ Z축 Down 실행 위치는 SUD 설정 참고
        5. R 축 Retract           
        */
        public static void Place(int Stage, int Slot)
        {
            _bSet_flag = true;
            string readData = string.Empty;

            string send_Command = string.Format("{0:D1}PUT{1}{2:D2}{3:D2}{4}", r_Id1, Convert.ToChar(RS_SP), Stage, Slot, Convert.ToChar(RS_CR));
            _serialPort.Write(send_Command);
            Global.EventLog("Send : " + send_Command);

            Thread.Sleep(100);

            readData = _serialPort.ReadLine();
            Global.EventLog("Recv : " + readData);

            Thread.Sleep(100);

            Global.EventLog(_DataCheck(readData));

            _bSet_flag = false;
        }

        /*
         * 원하는 Stage의 선택 한 Slot의 하단 위치로 이동
        1. Z축과 T축 이 Stage 'aa', Slot 'bb' 위치 (Wafer 하단)로 이동        
           ※ Z, T 위치는 SPD 설정참고        
        */
        public static void DownRotation(int Stage, int Slot)
        {
            _bSet_flag = true;
            string readData = string.Empty;

            string send_Command = string.Format("{0:D1}APP{1}{2:D2}{3:D2}{4}", r_Id1, Convert.ToChar(RS_SP), Stage, Slot, Convert.ToChar(RS_CR));
            _serialPort.Write(send_Command);
            Global.EventLog("Send : " + send_Command);

            Thread.Sleep(100);

            readData = _serialPort.ReadLine();
            Global.EventLog("Recv : " + readData);

            Thread.Sleep(100);

            Global.EventLog(_DataCheck(readData));

            _bSet_flag = false;
        }

        /*
         * R축 Retract
        1. R축 설정된 위치로 Retract
        */
        public static void Retract()
        {
            _bSet_flag = true;
            string readData = string.Empty;

            string send_Command = string.Format("{0:D1}RET{1}{2}", r_Id1, Convert.ToChar(RS_SP), Convert.ToChar(RS_CR));
            _serialPort.Write(send_Command);
            Global.EventLog("Send : " + send_Command);

            Thread.Sleep(100);

            readData = _serialPort.ReadLine();
            Global.EventLog("Recv : " + readData);

            Thread.Sleep(100);

            Global.EventLog(_DataCheck(readData));

            _bSet_flag = false;
        }

        /*
         * R축 Extend
        1. 지정한 Stage 'a'와 'b' Arm 설정된 A or B축 위치로 Extend
           ※ R축 Extend 위치는 SPD 설정 참고
        */
        public static void Extend(int Stage)
        {
            _bSet_flag = true;
            string readData = string.Empty;

            string send_Command = string.Format("{0:D1}EXT{1}{2:D2}{3}", r_Id1, Convert.ToChar(RS_SP), Stage, Convert.ToChar(RS_CR));
            _serialPort.Write(send_Command);
            Global.EventLog("Send : " + send_Command);

            Thread.Sleep(100);

            readData = _serialPort.ReadLine();
            Global.EventLog("Recv : " + readData);

            Thread.Sleep(100);

            Global.EventLog(_DataCheck(readData));

            _bSet_flag = false;
        }

        /*
         * Z축 Up
        1. 지정한 Stage 'aa'의 설정된 Z축 위치로 Up 실행
           ※ Z축 Up 실행 위치는 SUD 설정 참고
        */
        public static void Up(int Stage)
        {
            _bSet_flag = true;
            string readData = string.Empty;

            string send_Command = string.Format("{0:D1}ZUP{1}{2:D2}{3}", r_Id1, Convert.ToChar(RS_SP), Stage, Convert.ToChar(RS_CR));
            _serialPort.Write(send_Command);
            Global.EventLog("Send : " + send_Command);

            Thread.Sleep(100);

            readData = _serialPort.ReadLine();
            Global.EventLog("Recv : " + readData);

            Thread.Sleep(100);

            Global.EventLog(_DataCheck(readData));

            _bSet_flag = false;
        }

        /*
         * Z축 Down
        1. 지정한 Stage 'aa'의 설정된 Z축 위치로 Down 실행
           ※ Z축 Down 실행 위치는 SUD 설정 참고
        */
        public static void Down(int Stage)
        {
            _bSet_flag = true;
            string readData = string.Empty;

            string send_Command = string.Format("{0:D1}ZDN{1}{2:D2}{3}", r_Id1, Convert.ToChar(RS_SP), Stage, Convert.ToChar(RS_CR));
            _serialPort.Write(send_Command);
            Global.EventLog("Send : " + send_Command);

            Thread.Sleep(100);

            readData = _serialPort.ReadLine();
            Global.EventLog("Recv : " + readData);

            Thread.Sleep(100);

            Global.EventLog(_DataCheck(readData));

            _bSet_flag = false;
        }

        /*
         * Arm vacuum
        1. Vaccum Sol v/v On/Off 실행
        */
        public static void Vacuum(string OnOff)
        {
            _bSet_flag = true;
            string readData = string.Empty;
            
            string send_Command;
            if (OnOff == "On")
                send_Command = string.Format("{0:D1}VON{1}{2}", r_Id1, Convert.ToChar(RS_SP), Convert.ToChar(RS_CR));
            else
                send_Command = string.Format("{0:D1}VOF{1}{2}", r_Id1, Convert.ToChar(RS_SP), Convert.ToChar(RS_CR));

            _serialPort.Write(send_Command);
            Global.EventLog("Send : " + send_Command);

            Thread.Sleep(100);

            readData = _serialPort.ReadLine();
            Global.EventLog("Recv : " + readData);

            Thread.Sleep(100);

            Global.EventLog(_DataCheck(readData));

            _bSet_flag = false;
        }

        /*
         * Robot Emergency Stop
        1. Robot Quick Stop, Servo Motor Off, Robot Status 초기화
        2. 명령 실행 후 Home이나 Power On -> Off을 실행 후 사용
           ※ Robot Emergency Stop후 반드시 Robot Home을 실행
        */
        public static void EMG_Stop()
        {
            _bSet_flag = true;
            string readData = string.Empty;

            string send_Command = string.Format("{0:D1}EMG{1}", r_Id1, Convert.ToChar(RS_CR));
            _serialPort.Write(send_Command);
            Global.EventLog("Send : " + send_Command);

            Thread.Sleep(100);

            readData = _serialPort.ReadLine();
            Global.EventLog("Recv : " + readData);

            Thread.Sleep(100);

            Global.EventLog(_DataCheck(readData));

            _bSet_flag = false;
        }

        /*
         * Robot 일시정지
        1. ROBOT 의 동작을 일시적으로 멈춤
           ※ Pause 상태에서는 아래 명령어만 사용가능
            - Command : RESUME, RPR, RSTS, EMG, HOM
        */
        public static void Pause()
        {
            _bSet_flag = true;
            string readData = string.Empty;

            string send_Command = string.Format("{0:D1}PAUSE{1}", r_Id1, Convert.ToChar(RS_CR));
            _serialPort.Write(send_Command);
            Global.EventLog("Send : " + send_Command);

            Thread.Sleep(100);

            readData = _serialPort.ReadLine();
            Global.EventLog("Recv : " + readData);

            Thread.Sleep(100);

            Global.EventLog(_DataCheck(readData));

            _bSet_flag = false;
        }

        /*
         * Robot 동작 재개
        1. PAUSE에 의해 정지된 Robot 동작을 재개함           
        */
        public static void Resume()
        {
            _bSet_flag = true;
            string readData = string.Empty;

            string send_Command = string.Format("{0:D1}RESUME{1}", r_Id1, Convert.ToChar(RS_CR));
            _serialPort.Write(send_Command);
            Global.EventLog("Send : " + send_Command);

            Thread.Sleep(100);

            readData = _serialPort.ReadLine();
            Global.EventLog("Recv : " + readData);

            Thread.Sleep(100);

            Global.EventLog(_DataCheck(readData));

            _bSet_flag = false;
        }

        /*
         * Mapping sensor power on/off        
        */
        public static void MappingSensorPower(string OnOff)
        {
            _bSet_flag = true;
            string readData = string.Empty;

            string send_Command;
            if (OnOff == "On")
                send_Command = string.Format("{0:D1}MSON{1}", r_Id1, Convert.ToChar(RS_CR));
            else
                send_Command = string.Format("{0:D1}MSOF{1}", r_Id1, Convert.ToChar(RS_CR));

            _serialPort.Write(send_Command);
            Global.EventLog("Send : " + send_Command);

            Thread.Sleep(100);

            readData = _serialPort.ReadLine();
            Global.EventLog("Recv : " + readData);

            Thread.Sleep(100);

            Global.EventLog(_DataCheck(readData));

            _bSet_flag = false;
        }

        /*
         * Robot 멈춤
        1. ROBOT 의 동작을 멈춤. (동작이 아닌중에 명령어 사용시 ERROR 발생)
           ※ STOP 상태에서는 아래 명령어만 사용가능.
            - Command : RPR, RSTS, EMG, HOM
        */
        public static void Stop()
        {
            _bSet_flag = true;
            string readData = string.Empty;

            string send_Command = string.Format("{0:D1}STOP{1}", r_Id1, Convert.ToChar(RS_CR));
            _serialPort.Write(send_Command);
            Global.EventLog("Send : " + send_Command);

            Thread.Sleep(100);

            readData = _serialPort.ReadLine();
            Global.EventLog("Recv : " + readData);

            Thread.Sleep(100);

            Global.EventLog(_DataCheck(readData));

            _bSet_flag = false;
        }

        /*
         * Wafer mapping
        1. X, T, Z축이 지정된 Stage 'a'의 Slot 01번 위치 (Wafer의 중앙)로 이동
           ※ Z축, T축 위치는 SPD aabccccccddddddeeeeee 설정 참고
        2. Z축 1 slot Down실행
        3. Z축 지정된 Wafer 개수 상승하며 Mapping 실행
        4. R축 Retract
        5. Mapping 결과 Host에 전송
           (WAFER 25매, '0'무, '1'유, '2'엇각, '3'겹침)
           ※ Robot Teaching 후 Set 할 경우 Stage 33~56 에 저장 하여야 한다.
        */
        public static void Mapping(int Stage, int Slot)
        {
            _bSet_flag = true;
            string readData = string.Empty;

            string send_Command = string.Format("{0:D1}MAP{1}{2:D2}{3}", r_Id1, Convert.ToChar(RS_SP), Stage, Slot, Convert.ToChar(RS_CR));
            _serialPort.Write(send_Command);
            Global.EventLog("Send : " + send_Command);

            Thread.Sleep(100);

            readData = _serialPort.ReadLine();
            Global.EventLog("Recv : " + readData);

            Thread.Sleep(100);

            Global.EventLog(_DataCheck(readData));

            _bSet_flag = false;
        }

        /*
         * Error data reset
        */
        public static void Error_Data_Reset()
        {
            _bSet_flag = true;
            string readData = string.Empty;

            string send_Command = string.Format("{0:D1}SLOG{1}", r_Id1, Convert.ToChar(RS_CR));
            _serialPort.Write(send_Command);
            Global.EventLog("Send : " + send_Command);

            Thread.Sleep(100);

            readData = _serialPort.ReadLine();
            Global.EventLog("Recv : " + readData);

            Thread.Sleep(100);

            Global.EventLog(_DataCheck(readData));

            _bSet_flag = false;
        }

        private static string _DataCheck(string sReadData)
        {
            try
            {
                if (sReadData.Length > 1)
                {
                    int nSize = sReadData.Length;
                    int bufPos = 0;
                    char[] charArray;
                    charArray = new char[nSize];
                    for (int i = 0; i < nSize; i++)
                    {
                        charArray[bufPos++] = sReadData[i];
                    }

                    if ((charArray[0] == 'K') &&
                        (charArray[1] == Convert.ToChar(RS_CR))) //&&
                                                                 //(charArray[2] == Convert.ToChar(RS_LF)))
                    {
                        return "Receive data normal";
                    }
                    else
                    {
                        return "Receive data abnormal";
                    }
                }
                else
                {
                    return "Receive data length error";
                }
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }            
        }
        #endregion
    }
}
