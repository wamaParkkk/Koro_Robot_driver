using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koro_Robot_driver
{
    public enum DigitalValue
    {
        Off = 0,
        On = 1
    };
    
    public class Error_List
    {
        private string _error_Name = "";

        public string error_code
        {
            get
            {
                if (string.IsNullOrEmpty(_error_Name))
                {
                    _error_Name = "Error name is missing";
                }

                return _error_Name;
            }
            set
            {
                if (value == "00")      _error_Name = "NO ERROR";                
                else if (value == "01") _error_Name = "NOT HOME";
                else if (value == "02") _error_Name = "UNAVALUE COMMAND";
                else if (value == "03") _error_Name = "NOT GET WAFER";
                else if (value == "04") _error_Name = "NOT PUT WAFER";
                else if (value == "05") _error_Name = "OVER SLOT NUMBER";
                else if (value == "06") _error_Name = "ECH WAFER";
                else if (value == "10") _error_Name = "SIDE DOOR OPEN";
                else if (value == "11") _error_Name = "PAUSE ON";
                else if (value == "12") _error_Name = "STOP ON";
                else if (value == "20") _error_Name = "DLP1 CLOSE";
                else if (value == "21") _error_Name = "DLP2 CLOSE";
                else if (value == "22") _error_Name = "DLP3 CLOSE";
                else if (value == "23") _error_Name = "DLP4 CLOSE";
                else if (value == "24") _error_Name = "DLP5 CLOSE";
                else if (value == "30") _error_Name = "A(R)-AXIS DRIVE ALARM";
                else if (value == "31") _error_Name = "B-AXIS DRIVE ALARM";
                else if (value == "32") _error_Name = "T-AXIS DRIVE ALARM";
                else if (value == "33") _error_Name = "Z-AXIS DRIVE ALARM";
                else if (value == "34") _error_Name = "X-AXIS DRIVE ALARM";
                else if (value == "35") _error_Name = "MOTION CONTROLLER ALARM";
                else if (value == "36") _error_Name = "MOTION CONTROLLER COMM ALARM";
                else if (value == "37") _error_Name = "WAFER DROP ALARM";
                else if (value == "38") _error_Name = "MOTOR TORQUE ALARM";
                else if (value == "39") _error_Name = "FX DRIVE ALARM";
                else if (value == "40") _error_Name = "MAPPING NOT ALARM";
                else if (value == "41") _error_Name = "MAP R COMM ALARM";
                else if (value == "42") _error_Name = "STOP ERROR";
                else if (value == "44") _error_Name = "EMG ALARM";
                else if (value == "45") _error_Name = "EMG SENSOR ON ALARM";
                else if (value == "46") _error_Name = "LMT SENSOR ERROR";
                else if (value == "47") _error_Name = "GRIP ALARM";
                else if (value == "50") _error_Name = "A(R)-AXIS NOT BUSY ALARM";
                else if (value == "51") _error_Name = "B-AXIS NOT BUSY ALARM";
                else if (value == "52") _error_Name = "T-AXIS NOT BUSY ALARM";
                else if (value == "53") _error_Name = "Z-AXIS NOT BUSY ALARM";
                else if (value == "54") _error_Name = "X-AXIS NOT BUSY ALARM";
                else if (value == "55") _error_Name = "F-AXIS NOT BUSY ALARM";
                else if (value == "56") _error_Name = "AB-AXIS NOT BUSY ALARM";
                else if (value == "57") _error_Name = "ZX-AXIS NOT BUSY ALARM";
                else if (value == "58") _error_Name = "TZ-AXIS NOT BUSY ALARM";
                else if (value == "59") _error_Name = "TZX-AXIS NOT BUSY ALARM";
                else if (value == "60") _error_Name = "ABTZ-AXIS NOT BUSY ALARM";
                else if (value == "70") _error_Name = "MA(R) END ALARM"; 
                else if (value == "71") _error_Name = "MB END ALARM"; 
                else if (value == "72") _error_Name = "MT END ALARM";
                else if (value == "73") _error_Name = "MZ END ALARM";
                else if (value == "74") _error_Name = "MX END ALARM";
                else if (value == "75") _error_Name = "MF END ALARM";
                else if (value == "81") _error_Name = "MA(R) ESC ALARM"; 
                else if (value == "82") _error_Name = "MB ESC ALARM";
                else if (value == "83") _error_Name = "MT ESC ALARM";
                else if (value == "84") _error_Name = "MZ ESC ALARM";
                else if (value == "85") _error_Name = "MX ESC ALARM";
                else if (value == "86") _error_Name = "MF ESC ALARM";
            }
        }
    }    

    public struct ROBOT_STATUS
    {
        // Robot status
        public string sR_Initial;       // Initial
        public string sR_Busy;          // Busy
        public string sR_VacSol1;       // Vacuum sol v/v #1
        public string sR_VacSns1;       // Vacuum sensor #1
        public string sR_VacSol2;       // Vacuum sol v/v #2
        public string sR_VacSns2;       // Vacuum sensor #2
        public string[] sR_ErrorSts;    // Error status
        public string sR_Arm_A;         // Arm A extend/retract
        public string sR_Arm_B;         // Arm B extend/retract
        public string sR_Axis_Z;        // Axis Z up/down
        public string sR_Pause;         // Robot pause status
        public string sR_SideDoor;      // Side door status
        public string[] sR_StageNum;    // Stage number        
    }

    public class RobotDefine
    {
        public const int RS_NUL = 0x00;
        public const int RS_SOH = 0x01;
        public const int RS_STX = 0x02;
        public const int RS_ETX = 0x03;
        public const int RS_LF = 0x0A;
        public const int RS_CR = 0x0D;
        public const int RS_NAK = 0x15;
        public const int RS_SP = 0x20;

        public const int ROBOT_MAX = 1;

        public const int r_Id1 = 0;
        public const int r_Id2 = 1;

        // Stage number
        public const int _Stage_LP = 0;
        public const int _Stage_Aligner = 1;
        public const int _Stage_PM1 = 2;

        // Error name
        public static string sErrorName;
    }
}
