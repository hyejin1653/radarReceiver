using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Pixoneer.NXDL;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Runtime.InteropServices;

namespace RadarReceiver
{
    internal class Program
    {
        public class location
        {
            public string lon;
            public string lat;
        }

        StreamWriter writer_;
        static string gnStr = "";
        static string radarStr = "";
        static bool gnExiFlag = true;

        static void Main(string[] args)
        {
            Program pg = new Program();
            try
            {
                string strRecvMsg = "";
                
                TcpClient sockClient = new TcpClient("127.0.0.1", 8080);
                NetworkStream ns = sockClient.GetStream();
                StreamReader sr = new StreamReader(ns);
                StreamWriter sw = new StreamWriter(ns);

                
                while (true)
                {
                    strRecvMsg = sr.ReadLine();
                    
                    if(strRecvMsg.Length > 0)
                    {
                        string[] msg = strRecvMsg.Split(',');
                        if (msg[0] == "$GNRMC")
                        {
                            gnStr = strRecvMsg;

                            if (!gnExiFlag)
                            {
                                pg.saveRadar(radarStr, gnStr, true);
                            }
                        }
                        else if (msg[0] == "$RATTM")
                        {
                            radarStr = strRecvMsg;
                            if (gnStr == "")
                            {
                                gnExiFlag = false;
                                continue;
                            }
                            pg.saveRadar(radarStr, gnStr, gnExiFlag);
                        }
                    }
                }

                sr.Close();
                sw.Close();
                ns.Close();
                sockClient.Close();

                //Console.WriteLine("접속 종료");
                Console.ReadLine();
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        public void saveRadar(string radarDt, string gnDt, bool flg)
        {
            string[] radar = radarDt.Split(','); //Target데이터
            string[] gn = gnDt.Split(','); //기준데이터

            StringBuilder dataBase = new StringBuilder();
            StringBuilder ip = new StringBuilder();
            StringBuilder port = new StringBuilder();
            StringBuilder id = new StringBuilder();
            StringBuilder pw = new StringBuilder();

            GetPrivateProfileString("SETTING", "DATABASE", "(NONE)", dataBase, 32, System.Environment.CurrentDirectory + @"\config.ini");
            GetPrivateProfileString("SETTING", "IP", "(NONE)", ip, 32, System.Environment.CurrentDirectory + @"\config.ini");
            GetPrivateProfileString("SETTING", "PORT", "(NONE)", port, 32, System.Environment.CurrentDirectory + @"\config.ini");
            GetPrivateProfileString("SETTING", "ID", "(NONE)", id, 32, System.Environment.CurrentDirectory + @"\config.ini");
            GetPrivateProfileString("SETTING", "PW", "(NONE)", pw, 32, System.Environment.CurrentDirectory + @"\config.ini");

            string strDataBase = dataBase.ToString();
            string strIP = ip.ToString();
            string strPort = port.ToString();
            string strID = id.ToString();
            string strPW = pw.ToString();

            //서버연결
            string constring = "server=" + strIP + "," + strPort + ";database=" + strDataBase + ";uid=" + strID + ";pwd=" + strPW;
            try
            {
                using(MySqlConnection playCommand = new MySqlConnection(constring))
                {
                    playCommand.Open();

                    MySqlCommand cmd = playCommand.CreateCommand();
                    cmd.CommandText = @"INSERT INTO RADAR_SIGN_INFO
	                                    (   GN_TIME,	 		GN_STATUS, 		GN_LAT, 		GN_NS, 			GN_LON, 		GN_EW, 
	                                        GN_SPEED,			GN_TRACK, 		GN_DATE,		GN_MAGNETIC, 	GN_EW2, 		RA_TAR_NUM, 
	                                        RA_TAR_DISTANCE,	RA_BEARING, 	RA_STATUS, 	    RA_TAR_SPEED, 	RA_TAR_COURCE, 	RA_STATUS2,
	                                        RA_DISTANCE, 		RA_APPROACH,	RA_KN, 		    RA_TAR_NM, 		RA_TAR_STATUS,	RA_REF_TAR, 
	                                        RA_DATA, 			RA_TYPE, 		RA_LON, 		RA_LAT,         INSERT_DT
                                        )VALUES(
	                                        @GN_TIME,	 		@GN_STATUS, 	@GN_LAT, 	    @GN_NS, 		@GN_LON, 		@GN_EW, 
	                                        @GN_SPEED,			@GN_TRACK, 		@GN_DATE,	    @GN_MAGNETIC, 	@GN_EW2, 		@RA_TAR_NUM, 
	                                        @RA_TAR_DISTANCE,	@RA_BEARING, 	@RA_STATUS,     @RA_TAR_SPEED,  @RA_TAR_COURCE,	@RA_STATUS2,
	                                        @RA_DISTANCE, 		@RA_APPROACH,	@RA_KN, 		@RA_TAR_NM, 	@RA_TAR_STATUS,	@RA_REF_TAR, 
	                                        @RA_DATA, 			@RA_TYPE, 		@RA_LON, 	    @RA_LAT,        NOW()
                                        )";

                    string  @GN_TIME = "", @GN_STATUS = "", @GN_LAT = "", @GN_NS = "", @GN_LON = "", @GN_EW = "",
                            @GN_SPEED = "", @GN_TRACK = "", @GN_DATE = "", @GN_MAGNETIC, @GN_EW2 = "", @RA_TAR_NUM = "",
                            @RA_TAR_DISTANCE = "", @RA_BEARING = "", @RA_STATUS = "", @RA_TAR_SPEED = "", @RA_TAR_COURCE = "", @RA_STATUS2 = "",
                            @RA_DISTANCE = "", @RA_APPROACH = "", @RA_KN = "", @RA_TAR_NM = "", @RA_TAR_STATUS = "", @RA_REF_TAR = "",
                            @RA_DATA = "", @RA_TYPE = "", @RA_LON = "", @RA_LAT = "";

                    

                    @GN_TIME = gn[1];
                    @GN_STATUS = gn[2];
                    string lat = gn[3];

                    int latDo = Convert.ToInt32(lat.Substring(0, 2));
                    double latbun = Convert.ToDouble(lat.Substring(2, lat.Length - 2));
                    double resultLat = latDo + (latbun / 60);

                    @GN_LAT = resultLat.ToString();
                    @GN_NS = gn[4];
                    string lon = gn[5];

                    int lonDo = Convert.ToInt32(lon.Substring(0, 3));
                    double lonbun = Convert.ToDouble(lon.Substring(3, lon.Length - 3));
                    double resultLon = lonDo + (lonbun / 60);

                    @GN_LON = resultLon.ToString();
                    @GN_EW = gn[6];
                    @GN_SPEED = gn[7];
                    @GN_TRACK = gn[8];
                    @GN_DATE = gn[9];
                    @GN_MAGNETIC = gn[10];
                    @GN_EW2 = gn[11];

                    @RA_TAR_NUM = radar[1];
                    @RA_TAR_DISTANCE = radar[2];
                    @RA_BEARING = radar[3];
                    @RA_STATUS = radar[4];
                    @RA_TAR_SPEED = radar[5];
                    @RA_TAR_COURCE = radar[6];
                    @RA_STATUS2 = radar[7];
                    @RA_DISTANCE = radar[8];
                    @RA_APPROACH = radar[9];
                    @RA_KN = radar[10];
                    @RA_TAR_NM = radar[11];
                    @RA_TAR_STATUS = radar[12];
                    @RA_REF_TAR = radar[13];
                    @RA_DATA = radar[14];
                    @RA_TYPE = radar[15];

                    location lonLat = getLonLat(resultLon.ToString(), resultLat.ToString(), Convert.ToInt32(radar[3]), Convert.ToInt32(radar[2]));
                    @RA_LON = lonLat.lon;
                    @RA_LAT = lonLat.lat;

                    cmd.Parameters.AddWithValue("@GN_TIME", @GN_TIME);
                    cmd.Parameters.AddWithValue("@GN_STATUS", @GN_STATUS);
                    cmd.Parameters.AddWithValue("@GN_LAT", @GN_LAT);
                    cmd.Parameters.AddWithValue("@GN_NS", @GN_NS);
                    cmd.Parameters.AddWithValue("@GN_LON", @GN_LON);
                    cmd.Parameters.AddWithValue("@GN_EW", @GN_EW);
                    cmd.Parameters.AddWithValue("@GN_SPEED", @GN_SPEED);
                    cmd.Parameters.AddWithValue("@GN_TRACK", @GN_TRACK);
                    cmd.Parameters.AddWithValue("@GN_DATE", @GN_DATE);
                    cmd.Parameters.AddWithValue("@GN_MAGNETIC", @GN_MAGNETIC);
                    cmd.Parameters.AddWithValue("@GN_EW2", @GN_EW2);

                    cmd.Parameters.AddWithValue("@RA_TAR_NUM", @RA_TAR_NUM);
                    cmd.Parameters.AddWithValue("@RA_TAR_DISTANCE", @RA_TAR_DISTANCE);
                    cmd.Parameters.AddWithValue("@RA_BEARING", @RA_BEARING);
                    cmd.Parameters.AddWithValue("@RA_STATUS", @RA_STATUS);
                    cmd.Parameters.AddWithValue("@RA_TAR_SPEED", @RA_TAR_SPEED);
                    cmd.Parameters.AddWithValue("@RA_TAR_COURCE", @RA_TAR_COURCE);
                    cmd.Parameters.AddWithValue("@RA_STATUS2", @RA_STATUS2);
                    cmd.Parameters.AddWithValue("@RA_DISTANCE", @RA_DISTANCE);
                    cmd.Parameters.AddWithValue("@RA_APPROACH", @RA_APPROACH);
                    cmd.Parameters.AddWithValue("@RA_KN", @RA_KN);
                    cmd.Parameters.AddWithValue("@RA_TAR_NM", @RA_TAR_NM);
                    cmd.Parameters.AddWithValue("@RA_TAR_STATUS", @RA_TAR_STATUS);
                    cmd.Parameters.AddWithValue("@RA_REF_TAR", @RA_REF_TAR);
                    cmd.Parameters.AddWithValue("@RA_DATA", @RA_DATA);
                    cmd.Parameters.AddWithValue("@RA_TYPE", @RA_TYPE);
                    cmd.Parameters.AddWithValue("@RA_LON", @RA_LON);
                    cmd.Parameters.AddWithValue("@RA_LAT", @RA_LAT);

                    try
                    {
                        if (cmd.ExecuteNonQuery() == 1)
                        {
                            Console.WriteLine(radarStr);
                            //LOG파일 생성;
                            createLogFile(radarStr);

                            radarStr = "";
                            gnStr = "";
                            gnExiFlag = flg;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("==============DB Error ==============");
                        Console.WriteLine(ex.ToString());
                    }
                    playCommand.Close();
                }
            }catch (Exception e)
            {
                Console.WriteLine (e.ToString());
            }

        }

        private void createLogFile(string message)
        {
            string path = System.Environment.CurrentDirectory + "\\LOG";
            string nowDate = System.DateTime.Now.ToString("yyyy-MM-dd");
            string folderPath = path + "\\" + nowDate;
            //string folderPath = path + "\\" + "2021-10-23";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            //로그파일 작성
            String strFilePath = folderPath + ".txt"; //반환후 저장되야하는 txt파일
            FileInfo fileInfo = new FileInfo(strFilePath);

            if (fileInfo.Exists)
            {
                writer_ = File.AppendText(strFilePath);
            }
            else
            {
                writer_ = File.CreateText(strFilePath);
            }

            writer_.WriteLine(message);
            writer_.Close();
        }

        private location getLonLat(string lon, string lat, int bear, int dis)
        {
            XAngle lon1 = new XAngle();
            XAngle lat1 = new XAngle();

            lon1 = XAngle.FromDegree(Convert.ToDouble(lon));
            lat1 = XAngle.FromDegree(Convert.ToDouble(lat));

            XAngle bearing = XAngle.FromDegree(bear);
            double dist = dis;

            XAngle lon2 = new XAngle();
            XAngle lat2 = new XAngle();

            XAngle ang = Xfn.CalcPosByBearingAndDist(lon1, lat1, bearing, dist, ref lon2, ref lat2);

            location loc = new location
            {
                lon = lon2.deg.ToString(),
                lat = lat2.deg.ToString(),
            };

            return loc;
        }
    }
}
