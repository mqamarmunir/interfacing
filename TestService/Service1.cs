using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Timers;
using System.Configuration;
using MySql.Data.MySqlClient;
using BusinessLayer;
using System.Data.OleDb;

namespace TestService
{
    public partial class Service1 : ServiceBase
    {
        delegate void _remoteinserter(DataTable dt);
        private StringBuilder sb_port1 = new StringBuilder();
        private StringBuilder sb_port2 = new StringBuilder();
        private Timer timer;
        private Timer timer_deleteolddata;
        string rmserver = ConfigurationSettings.AppSettings["rmserver"].ToString();
        static MySqlConnection Conn = null;
        static string maxid = "";
        public Service1()
        {
            InitializeComponent();
            //eventLog1.LogDisplayName = "TestService";
            if (!System.Diagnostics.EventLog.SourceExists("WindowCopyServiceSource"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "WindowCopyServiceSource", "WindowCopyServiceSourcelog");
            }
            eventLog1.Source = "WindowCopyServiceSource";
            eventLog1.Log = "WindowCopyServiceSourcelog";

        }

        protected override void OnStart(string[] args)
        {
            eventLog1.WriteEntry("In Onstart");
            eventLog1.ModifyOverflowPolicy(OverflowAction.OverwriteAsNeeded, 1);

            serialPort1.Open();

            this.timer = new System.Timers.Timer(180000D);  // 30000 milliseconds = 30 seconds
            this.timer.AutoReset = true;
            this.timer.Elapsed += new System.Timers.ElapsedEventHandler(this.Main);
            this.timer.Start();

            this.timer_deleteolddata = new System.Timers.Timer(24 * 60 * 60 * 1000);//Run this timer method after one day
            this.timer_deleteolddata.AutoReset = true;
            this.timer_deleteolddata.Elapsed += new System.Timers.ElapsedEventHandler(this.Deleteolddate);
        }

        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {

            string data = "";
            try
            {
                data = serialPort1.ReadExisting();
                if (data.Length > 0)
                {

                    serialPort1.Write(new byte[] { 0x06 }, 0, 1);//send Ack to machine
                    sb_port1.Append(data);
                    if (sb_port1.ToString().Contains("L|1"))//L|1 is a terminator record according to astm standards
                    {
                        ///if the recieved string contains the terminator
                        ///then parse the record and Clear the string Builder for next 
                        ///Record.
                        // eventLog1.WriteEntry(sb.ToString());
                       // eventLog1.WriteEntry(System.Configuration.ConfigurationSettings.AppSettings["parsingalgorithm_port1"].ToString().Trim());
                        Parsethisandinsert(sb_port1.ToString(), Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["parsingalgorithm_port1"].ToString().Trim()));
                        sb_port1.Clear();


                    }
                }


            }
            catch (Exception ee)
            {
                eventLog1.WriteEntry("Following Exception occured in serialport datarecieved method please check." + ee.ToString());
            }

        }



        private void Parsethisandinsert(string data, int Parsingalgorithm)
        {
            string datetime = "";
            string labid = "";
            string attribresult = "";
            string attribcode = "";
            string patid = "";
            switch (Parsingalgorithm)
            {

                #region 1st parser
                ///According to ASTM standard 
                ///tested on 
                ///sysmex xs800i,cobase411,cobasu411(urine analyzer)
                case 1:
                    string[] sep1 = { "L|1" };

                    char[] sep2 = { Convert.ToChar(13) };
                    string[] abc = data.Split(sep1, StringSplitOptions.RemoveEmptyEntries);
                    char[] sep3 = { '|' };
                    char[] sep4 = { '^' };
                    for (int i = 0; i <= abc.GetUpperBound(0); i++)
                    {
                        string[] def = abc[i].Split(sep2);
                        for (int j = 0; j < def.GetUpperBound(0); j++)
                        {
                            if (def[j].Contains("H|") && !def[j].Contains("O|") && !def[j].Contains("R|"))
                            {
                                // Get date time
                                // def[j] = def[j].Replace("||", "");

                                string[] header = def[j].Split(sep3);
                                try
                                {
                                    datetime = header[13].ToString();
                                }
                                catch { }
                            }
                            else if (def[j].Contains("P|1") && (!def[j].Contains("O|") || def[j].IndexOf("P|") < def[j].IndexOf("O|")) && (!def[j].Contains("R|") || def[j].IndexOf("P|") < def[j].IndexOf("R|")))
                            {
                                string[] patinfo = def[j].Split(sep3);
                                try
                                {
                                    patid = patinfo[4].ToString();
                                }
                                catch (Exception ee)
                                {
                                    eventLog1.WriteEntry("Exception on getting Patientid: " + ee.ToString());
                                }
                            }
                            else if (def[j].Contains("O|") && def[j].Contains("R|") && def[j].IndexOf("O|") < def[j].IndexOf("R|"))
                            {
                                ///Get lab ID
                                string[] order = def[j].Split(sep3);
                                labid = order[2].ToString();
                                if (labid.Contains("^"))
                                {
                                    string[] splitlabid = labid.Split(sep4);
                                    labid = splitlabid[1].ToString().Trim();
                                }
                            }
                            else if (def[j].Contains("R|"))
                            {
                                //Get Result
                                string[] result = def[j].Split(sep3);
                                attribresult = result[3].ToString();
                                string[] attcode = result[2].Split(sep4);
                                //writeLog("Result[2]: " + result[2]);
                                if (attcode[3] != "")
                                {
                                    attribcode = attcode[3].ToString();
                                }
                                else
                                {
                                    attribcode = attcode[4].ToString();
                                }
                                if (attribcode.Contains(@"/"))
                                {
                                    attribcode = attribcode.Replace(@"/", "");
                                }
                                if (attribcode.ToLower() == "wbc" || attribcode.ToLower() == "plt")
                                {
                              
                                    try
                                    {
                                        attribresult = ((Convert.ToDecimal(attribresult)) * 1000).ToString();
                                        if (attribresult.Contains("."))
                                        {
                                            attribresult = attribresult.Substring(0, attribresult.IndexOf('.'));
                                        }
                                    }
                                    catch (Exception ee)
                                    {
                                        eventLog1.WriteEntry("Error Converting Result: " + attribresult);
                                    }


                                }
                                else if (attribcode.ToLower().Equals("900") || attribcode.ToLower().Equals("999") || attribcode.ToLower().Equals("102"))
                                {
                                    if (attribresult.Contains("-1^"))
                                    {
                                        attribresult = attribresult.Replace("-1^", "Negative  \r\n");

                                    }
                                    else if (attribresult.Contains("1^"))
                                    {
                                        attribresult = attribresult.Replace("1^", "Positive  \r\n");

                                    }

                                }
                                else if (attribcode.ToLower().Equals("eo%") || attribcode.ToLower().Equals("mono%") || attribcode.ToLower().Equals("neut%") || attribcode.ToLower().Equals("lymph%"))
                                {
                                    try
                                    {
                                        attribresult = Math.Round(Convert.ToDecimal(attribresult)).ToString().Trim();
                                        if (attribresult.Contains("."))
                                        {
                                            attribresult = attribresult.Substring(0, attribresult.IndexOf('.'));
                                        }
                                    }
                                    catch
                                    { }
                                }
                                if (labid == "")
                                {
                                    labid = patid;
                                }

                                string pars = labid + "," + attribcode + "," + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm") + "," + attribresult;
                                //writeLog("parsed data: " + pars);
                                //eventLog1.WriteEntry("parsed string:" + pars);
                                InsertBooking(pars);
                            }


                        }
                    }
                    break;//case 1 ends here
                #endregion
            }

        }

        private void InsertBooking(string pars)
        {

            AppSettingsReader con = new AppSettingsReader();
            string StrConnection = Rot13.Transform(System.Configuration.ConfigurationSettings.AppSettings["localdbConn"].ToString().Trim());
            // StrConnection = "User Id=" + con.GetValue("susername", "".GetType()).ToString() + "; PWD =" + con.GetValue("spassword", "".GetType()).ToString() + "; Server=" + con.GetValue("sserver", "".GetType()).ToString() + ";Port=3306;Database=" + con.GetValue("sdb", "".GetType()).ToString() + ";respect binary flags = false";
            //MySqlConnection objConn = new MySqlConnection(StrConnection);
            //MySqlConnection objConn = new MySqlConnection("Server = localhost; Port = 3306; Database = mi; Uid = " + clsSharedVariable.dbUserName + "; Pwd = " + clsSharedVariable.dbPWD + "");
            MySqlConnection objConn = new MySqlConnection(StrConnection);
            MySqlCommand objCmd = new MySqlCommand();
            MySqlDataAdapter da = new MySqlDataAdapter(objCmd);
            DataSet DS = new DataSet();
            string myquerystring = "";
            string EnteredOn = DateTime.Now.ToString("dd/MM/yyyy hh:mm tt");

            string[] str = { "", "", "", "", "", "", "" };//BookingID,LABID,SendOn,ReceivedOn,Result,AttributeCode,AttributeID

            str[3] = DateTime.Now.ToString("dd/MM/yyyy hh:mm tt");
            try
            {
                objConn.Open();
            }
            catch
            {
                eventLog1.WriteEntry("unable to open databaseconnection");
            }
            //Conn.Open();
            //objCmd.Connection = Conn;
            try
            {
                string[] Delimeter = { "," };
                //writeLog(Msg);
                string[] Tmp = pars.Split(Delimeter, StringSplitOptions.None);//, StringSplitOptions.RemoveEmptyEntries);

                str[1] = Tmp[0].Trim();//Labid

                str[4] = Tmp[3].Trim();//Result
                // writeLog(str[5]);
                str[5] = Tmp[1].Trim();//AttributeCode
                //writeLog(str[5]);
                str[2] = Tmp[2];//timestamp
                //writeLog(str[2]);
            }
            catch (Exception ee)
            {
                eventLog1.WriteEntry(ee.ToString() + " The Exception occured while converting parsed string to array.");
            }


            try
            {
                myquerystring = @"INSERT INTO mi_tresult(BookingID,AttributeID, Result, EnteredBy, EnteredOn, ClientID, Status,MachineName)
values('" + System.DateTime.Now.ToString("yy") + "-" + str[1].Replace("'", "''").ToString().Substring(0, str[1].Replace("'", "''").ToString().IndexOf("-")).PadLeft(3, '0') + "-" + str[1].Replace("'", "''").Substring(str[1].Replace("'", "''").IndexOf("-") + 1).PadLeft(8, '0') + "','" + str[5].Replace("'", "''") + "','" + str[4].Replace("'", "''") + "',1,str_to_date('" + System.DateTime.Now.ToString("dd/MM/yyyy hh:mm ") + "','%d/%m/%Y %h:%i %p') ,'" + System.Configuration.ConfigurationSettings.AppSettings["clientid"].ToString() + "','0','" + System.Configuration.ConfigurationSettings.AppSettings["machinecodecliquePort1"].ToString() + "')";
                //  myquerystring = "INSERT INTO mi_tresult(BookingID,AttributeID, Result, EnteredBy, EnteredOn, ClientID, Status ) values('" + str[1].Replace("'", "''") + "','" + str[5].Replace("'", "''") + "','" + str[4].Replace("'", "''") + "'," + clsSharedVariable.PersonID + ",str_to_date('" + System.DateTime.Now.ToString("dd/MM/yyyy hh:mm ") + "','%d/%m/%Y %h:%i %p')  ,'" + clsSharedVariable.ClientID + "','0' )";
                // eventLog1.WriteEntry(myquerystring);
                objCmd.CommandText = myquerystring;
                objCmd.Connection = objConn;
                objCmd.ExecuteNonQuery();
            }
            catch (Exception ee)
            {
                eventLog1.WriteEntry(ee.ToString() + " The Exception occured while inserting records into the local database");
            }
            finally
            {
                objConn.Close();
                //lblToday.Tag = "";
            }
            // throw new NotImplementedException();
        }


        private void Main(object sender, System.Timers.ElapsedEventArgs e)
        {
             eventLog1.WriteEntry("TImer main method");
            // WindowsImpersonationContext impersonationContext = null;
            //// Program g = new Program();
            //getservercredetials();
            //if (!sendingmethod.ToLower().Equals("direct db transfer"))
            //{

            //    SendbyXMLFiles();

            //}
            ////else
            //{

            DataTable dt = getDatatable();
            eventLog1.WriteEntry(dt.Rows.Count.ToString() + " new rows found");
            if (dt != null)
            {
                _remoteinserter ri = null;
                IAsyncResult _result;// = new IAsyncResult;

                ri = insertRemoteData;
                _result = ri.BeginInvoke(dt, new AsyncCallback(callbackmethod), dt.Rows[0]["Bookingid"].ToString().Trim());
                eventLog1.WriteEntry("Now filling remote database");
                // WriteXml(dt);

                //DoWork();
            }

        }
        private DataTable getDatatable()
        {
            // eventLog1.WriteEntry("Now making data table");
            DataTable table = new DataTable();
            try
            {

                clsBLMain main = new clsBLMain();
                DataView d = main.GetAll(4);
                if (d.Table.Rows.Count > 0)
                {
                    main.MAxID = d[0]["maxresultid"].ToString();
                }
                else
                {
                    return null;
                }
                DataView dv = main.GetAll(2);
                if (dv.Table.Rows.Count > 0)
                {
                    table = dv.Table;
                }
                else
                {
                    return null;
                }
                DataView dv1 = main.GetAll(3);
                if (dv1.Table.Rows.Count > 0)
                {
                    maxid = dv1[0]["maxid"].ToString();
                }
            }
            catch (Exception ee)
            {
                eventLog1.WriteEntry("Error while creating DataTable:" + ee.ToString());

            }

            return table;
            // return table;
        }
        public void insertRemoteData(DataTable dt)
        {
            //eventLog1.WriteEntry("In Remote Insertion method");
            try
            {
                eventLog1.WriteEntry("Asynchronous call successful");
                #region "Remote Server DB Login"
                int count = 0;
                string StrConnection = null;
                AppSettingsReader con = new AppSettingsReader();
                //         string connectionString = ConfigurationSettings.AppSettings.Get("ConnectionString");
                StrConnection = Rot13.Transform(con.GetValue("RemoteServerConnectionString", "".GetType()).ToString());
                //  StrConnection = "dsn=saps";

                try
                {
                    Conn = new MySqlConnection(StrConnection);
                    Conn.Open();
                    eventLog1.WriteEntry("Remote DB Connection Established");
                    // return Conn;
                }
                catch (Exception ex)
                {
                    eventLog1.WriteEntry("Remote DB login failed: " + ex.Message);
                    //return;
                }
                #endregion


                #region "Remote Server DB Insertion"
                if (dt.Rows.Count > 0)
                {
                    ClsBLInterface blinterface = new ClsBLInterface();

                    QueryBuilder qb = new QueryBuilder();
                    // StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        //eventLog1.WriteEntry("Reading records ");
                        blinterface.Mserialno = dt.Rows[i]["BookingId"].ToString();
                        //  sb.Append("\t");
                        blinterface.AttributeID = dt.Rows[i]["AttributeID"].ToString();
                        //  sb.Append("\t");
                        blinterface.Value = dt.Rows[i]["Result"].ToString();
                        //  sb.Append("\t");
                        blinterface.Enteredon = System.DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss");//enteredon
                        // sb.Append("\t");
                        blinterface.ClientID = dt.Rows[i]["ClientID"].ToString().Substring(0, 3);
                        // sb.Append("\r\n");
                        blinterface.EquipmentCode = dt.Rows[i]["MachineName"].ToString();
                        DataView dv = null;
                        try
                        {
                            try
                            {
                                //Conn.Open();
                                //eventLog1.WriteEntry("Remote DB login Successful ");
                            }
                            catch (Exception ee)
                            {
                                //eventLog1.WriteEntry("Remote DB login failed: " + ee.Message);
                                return;
                            }
                            DataSet DS = null;
                            try
                            {
                                string query = "select * from ls_tinterface where MserialNo ='" + blinterface.Mserialno + "' and AttributeID='" + blinterface.AttributeID + "'";
                                MySqlCommand ObjCmd = new MySqlCommand(query);
                                // dv = blinterface.GetAll(1);
                                ObjCmd.CommandText = query;

                                ObjCmd.Connection = Conn;
                                MySqlDataAdapter da = new MySqlDataAdapter(ObjCmd);
                                DS = new DataSet();
                                da.Fill(DS);

                                //Conn.Close();
                            }
                            catch (Exception ee)
                            {
                                eventLog1.WriteEntry("Remote DB login off failed: " + ee.Message);
                                //return;
                            }
                            finally
                            {
                                //Conn.Close();
                                //Conn.Dispose();

                            }
                            dv = new DataView(DS.Tables[0]);


                        }
                        catch (Exception ee)
                        {

                            eventLog1.WriteEntry("Check Entry already exist at RDB exp:" + ee.Message);

                        }
                        if (dv.Table.Rows.Count > 0)
                        {

                            //blinterface.Update();
                            try
                            {
                                try
                                {
                                  //  Conn.Open();
                                }
                                catch (Exception ee)
                                {
                                    eventLog1.WriteEntry("Remote DB login failed: " + ee.Message);
                                    return;
                                }
                                int updaterw = 0;
                                try
                                {
                                    string updateq = qb.QBUpdate2(blinterface.MakeArray(), "ls_tinterface");

                                    //string query = "select * from ls_tinterface where MserialNo ='" + blinterface.Mserialno + "' and AttributeID=" + blinterface.AttributeID + "";
                                    MySqlCommand ObjCmd = new MySqlCommand();
                                    // dv = blinterface.GetAll(1);
                                    ObjCmd.CommandText = updateq;

                                    ObjCmd.Connection = Conn;
                                    updaterw = ObjCmd.ExecuteNonQuery();

                                    //Conn.Close();
                                }
                                catch (Exception ee)
                                {
                                    eventLog1.WriteEntry("Remote DB login off failed: " + ee.Message);
                                    //return;
                                }
                                finally
                                {
                                    //Conn.Close();
                                    //Conn.Dispose();

                                }
                                if (updaterw > 0)
                                {
                                    //eventLog1.WriteEntry("Remote DB row update and local log entry ");
                                    clsBLMain main = new clsBLMain();
                                    maxid = dt.Rows[i]["ResultID"].ToString();
                                    main.Senton = DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss");
                                    main.Sentto = rmserver;
                                    main.status = "Y";
                                    main.MAxID = maxid;
                                    try
                                    {
                                        if (main.Insert())
                                        {
                                            eventLog1.WriteEntry("Audit Table insertion successful");
                                        }
                                        else
                                        {
                                            eventLog1.WriteEntry("Following Error Occured while updating Audit Table: " + main.Error);
                                        }
                                    }
                                    catch (Exception ee)
                                    {
                                        eventLog1.WriteEntry("Following Error occured while updating Audit Table: " + ee.ToString());
                                    }

                                }



                            }
                            catch (Exception ee)
                            {

                                eventLog1.WriteEntry("Entry Update at RDB exp:" + ee.Message);

                            }
                        }
                        else
                        {
                            //blinterface.Insert();
                            try
                            {
                                try
                                {
                                    //Conn.Open();
                                }
                                catch (Exception ee)
                                {
                                    eventLog1.WriteEntry("Remote DB login failed: " + ee.Message);
                                    return;
                                }
                                int insertcheck = 0;
                                try
                                {
                                    string insertq = qb.QBInsert(blinterface.MakeArray(), "ls_tinterface");
                                    //string query = "select * from ls_tinterface where MserialNo ='" + blinterface.Mserialno + "' and AttributeID=" + blinterface.AttributeID + "";
                                    MySqlCommand ObjCmd = new MySqlCommand();
                                    // dv = blinterface.GetAll(1);
                                    ObjCmd.CommandText = insertq;

                                    ObjCmd.Connection = Conn;
                                    insertcheck = ObjCmd.ExecuteNonQuery();

                                    //Conn.Close();
                                }
                                catch (Exception ee)
                                {
                                    eventLog1.WriteEntry("Remote DB login off failed: " + ee.Message);
                                    //return;
                                }
                                finally
                                {
                                  //  Conn.Close();
                                   // Conn.Dispose();

                                }
                                if (insertcheck > 0)
                                {
                                    clsBLMain main = new clsBLMain();
                                    maxid = dt.Rows[i]["ResultID"].ToString();
                                    main.Senton = DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss");
                                    main.Sentto = rmserver;
                                    main.status = "Y";
                                    main.MAxID = maxid;
                                    main.Insert();
                                }



                            }
                            catch (Exception ee)
                            {

                                eventLog1.WriteEntry("Entry Update at RDB exp:" + ee.Message);

                            }
                        }
                        count++;
                    }


                    eventLog1.WriteEntry(count.ToString() + " Records Sucessfully inserted ");


                }
                try
                {
                    Conn.Close();
                    Conn.Dispose();
                    eventLog1.WriteEntry("Connection Closed SUccessfully");
                }
                catch (Exception ee)
                {
                    eventLog1.WriteEntry("RDB Connection closing :" + ee.Message);
                }
                //finally
                //{
                //    Conn.Close();
                //    Conn.Dispose();
                //}
                //return count;
                #endregion


            }

            catch (Exception ee)
            {
                eventLog1.WriteEntry("Exception in file operation method. " + ee.Message);
            }
        }
        private void callbackmethod(IAsyncResult result)
        {
            eventLog1.WriteEntry("In callback method");
            // AsyncResult aresult = (AsyncResult)result;
            //_remoteinserter ri = (_remoteinserter)aresult.AsyncDelegate;

            // int _result = ri.EndInvoke(result);
        }
        protected override void OnStop()
        {
            //eventLog1.WriteEntry("In on Stop");
            eventLog1.WriteEntry("In on stop");
            serialPort1.Close();
            //if (serialPort2.IsOpen)
            //{
            //    serialPort2.Close();
            //}
            eventLog1.Clear();
        }

        private void Deleteolddate(object sender, ElapsedEventArgs e)
        {
            clsBLMain _main = new clsBLMain();
            if (_main.Deleteolddata())
            {
                eventLog1.WriteEntry("4 days old data successfully deleted");

            }
            else
            {
                eventLog1.WriteEntry("Error while deleting old data: " +_main.Error);
            }
        }
        
    }
}
