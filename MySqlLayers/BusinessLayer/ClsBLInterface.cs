using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataLayer;
using System.Data;
namespace BusinessLayer
{
   public class ClsBLInterface
    {
        #region "Class Variables"
             static clsoperation objTrans = new clsoperation();
             static clsdbhims objdbhims = new clsdbhims();
             static QueryBuilder objQB = new QueryBuilder();
            private const string Default = "~!@";
            private const string TableName = "ls_tinterface";

            private string StrErrorMessage = "";
            private string StrAttributeID = Default;
            private string StrMMserialno = Default;
            private string StrValue = Default;
            private string StrEnteredon = Default;
            private string StrClientID = Default;
            private string StrMachinecode = Default;
            #endregion

        #region "Properties"

            public string Error
            {
                get { return StrErrorMessage; }
                set { StrErrorMessage = value; }
            }
            public string AttributeID
            {
                get { return StrAttributeID; }
                set { StrAttributeID = value; }
            }
            public string Mserialno
            {
                get { return StrMMserialno; }
                set { StrMMserialno = value; }
            }
            public string Value
            {
                get { return StrValue; }
                set { StrValue = value; }
            }
            public string Enteredon
            {
                get { return StrEnteredon; }
                set { StrEnteredon = value; }
            }


            public string ClientID
            {
               get { return StrClientID; }

               set {  StrClientID= value; }
            }
            public string EquipmentCode
            {
                get
                {
                    return StrMachinecode;
                }
                set { StrMachinecode = value; }
            }
            //public string Password
            //{
            //    get { return StrPasword; }
            //    set { StrPasword = value; }
            //}
            //public string RSerialNO
            //{
            //    get { return Str_RSerialNo; }
            //    set { Str_RSerialNo = value; }
            //}
            //public string MSerialNO
            //{
            //    get { return Str_MSerialNo; }
            //    set { Str_MSerialNo = value; }
            //}

            #endregion


        #region "Methods"

       
      
            public DataView GetAll(int flag)
        {
            switch (flag)
            {
                case 1:
                    objdbhims.Query = "select * from ls_tinterface where MserialNo ='"+StrMMserialno+"' and AttributeID="+ StrAttributeID +"";
                    break;

                case 2:
                    objdbhims.Query = "select * from mi_tresult where ResultID > '" + StrClientID+"'";
                    //objdbhims.Query = objdbhims.Query + " Where Upper(t.GroupName) = '" + this._GroupName.ToUpper() + "'";
                    break;

                case 3:
                    objdbhims.Query = "select max(ResultID) AS maxid from mi_tresult where ResultID>'" + StrClientID + "'";
                    break;

                case 4:
                    objdbhims.Query = "select max(maxresultid) as maxresultid from mi_taudit  ";
                    break;
            }

            return objTrans.DataTrigger_Get_All(objdbhims);
        }
            public bool Update()
        {
           
               // clsoperation objTrans = new clsoperation();
               // QueryBuilder objQB = new QueryBuilder();

                objTrans.Start_Transaction();
                objdbhims.Query = objQB.QBUpdate(MakeArray(), TableName);
                this.StrErrorMessage = objTrans.DataTrigger_Update(objdbhims);
                objTrans.End_Transaction();

                if (this.StrErrorMessage.Equals("True"))
                {
                    this.StrErrorMessage = objTrans.OperationError;
                    return false;
                }
                else
                {
                    return true;
                }
            }
           
        
             public bool Insert()
        {
           
                try
                {
                    //clsoperation objTrans = new clsoperation();
                   // QueryBuilder objQB = new QueryBuilder();
                    objTrans.Start_Transaction();

                   // objdbhims.Query = objQB.QBGetMax("GroupId", TableName);
                   // this._GroupId = objTrans.DataTrigger_Get_Max(objdbhims);

                    //if (!this._GroupId.Equals("True"))
                    //{
                        objdbhims.Query = objQB.QBInsert(MakeArray(), TableName);
                        this.StrErrorMessage = objTrans.DataTrigger_Insert(objdbhims);

                        objTrans.End_Transaction();

                        if (this.StrErrorMessage.Equals("True"))
                        {
                            this.StrErrorMessage = objTrans.OperationError;
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                   // }
                    
                }
                catch (Exception e)
                {
                    this.StrErrorMessage = e.Message;
                    return false;
                }
            }
            
        


            public string[,] MakeArray()
            {
                string[,] strArr = new string[6, 3];

                if (!this.StrAttributeID.ToString().Equals(Default) && !this.StrAttributeID.ToString().Equals(""))
                {
                    strArr[0, 0] = "AttributeID";
                    strArr[0, 1] = StrAttributeID;
                    strArr[0, 2] = "string";
                }
                if (!this.StrMMserialno.Equals(Default))
                {
                    strArr[1, 0] = "MSerialNo";
                    strArr[1, 1] = this.StrMMserialno;
                    strArr[1, 2] = "string";
                }
                if (!this.StrValue.Equals(Default))
                {
                    strArr[2, 0] = "Value";
                    strArr[2, 1] = this.StrValue;
                    strArr[2, 2] = "string";
                }
                if (!this.StrEnteredon.Equals(Default))
                {
                    strArr[3, 0] = "Enteredon";
                    strArr[3, 1] = this.StrEnteredon;
                    strArr[3, 2] = "datetime";
                }
                if (!this.StrClientID.Equals(Default))
                {
                    strArr[4, 0] = "clientid";
                    strArr[4, 1] = this.StrClientID;
                    strArr[4, 2] = "string";
                }
                if (!this.StrMachinecode.Equals(Default))
                {
                    strArr[5, 0] = "analyzercode";
                    strArr[5, 1] = this.StrMachinecode;
                    strArr[5, 2] = "string";
                }
                
                

                return strArr;
            }

        }
            #endregion
    }

