using System;
using System.IO;
using System.Xml;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Globalization;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using Encryption;
using UtilityReferences.KCCA;
using System.Collections;
using System.Messaging;
using System.Xml.Serialization;
//using UtilityRefeerences.KCCA;

/// <summary>
/// Summary description for BusinessLogic
/// </summary>
public class BusinessLogic
{
    private string path = "E://URACerts//";
    public string SmsQueuePath = @".\private$\smsQueue";
    DatabaseHandler dh = new DatabaseHandler();
	public BusinessLogic()
	{
		//
		// TODO: Add constructor logic here
		//
	}
    public string EncryptString(string ClearText)
    {
        string ret = "";
        ret = Encryption.encrypt.EncryptString(ClearText, "Umeme2501PegPay");
        return ret;
    }
    public string DecryptString(string Encrypted)
    {
        string ret = "";
        string pword = EncryptString("stan_counter");
        ret = Encryption.encrypt.DecryptString(Encrypted, "Umeme2501PegPay");
        return ret;
    }
    internal bool IsTestNumber(string testNumber)
    {
        try
        {
            DataTable dt = dh.CheckIfNumberIsTestNumber(testNumber);
            if (dt.Rows.Count > 0)
            {
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            return false;

        }
    }

    public string GetSha1(string value)
    {

        using (SHA1 sha256Hash = SHA1.Create())
        {
            // ComputeHash - returns byte array  
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(value));

            // Convert byte array to a string   
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }

    }

    internal string GetTelecomNetwork(string customerTel)
    {
        string Network = "";
        string NetworkCode = "";
        try
        {
            string[] AirtelNetworkCodes = { "070", "075", "25670", "25675" };
            string[] MTNNetworkCodes = { "077", "078", "25677", "25678" };
            string[] AFRICELNetworkCodes = { "079", "25679" };
            string[] UTLNetworkCodes = { "071", "25671" };
            foreach (string Code in AirtelNetworkCodes)
            {
                if (customerTel.StartsWith(Code))
                {
                    Network = "AIRTEL";
                }

            }
            foreach (string Code in MTNNetworkCodes)
            {
                if (customerTel.StartsWith(Code))
                {
                    Network = "MTN";
                }

            }
            foreach (string Code in AFRICELNetworkCodes)
            {
                if (customerTel.StartsWith(Code))
                {
                    Network = "AFRICEL";
                }

            }
            foreach (string Code in UTLNetworkCodes)
            {
                if (customerTel.StartsWith(Code))
                {
                    Network = "UTL";
                }
            }

        }

        catch (Exception ex)
        {
        }
        return Network;
    }


    public bool IsNumeric(string amount)
    {

        if (amount.Equals("0"))
        {
            return false;
        }
        else
        {
            double amt = double.Parse(amount);
            amount = amt.ToString();
            float Result;
            return float.TryParse(amount, out Result);
        }
    }

    public void SendToSmsMSQ(SMS sms)
    {
        try
        {
            MessageQueue smsqueue;
            if (MessageQueue.Exists(SmsQueuePath))
            {
                smsqueue = new MessageQueue(SmsQueuePath);
            }
            else
            {
                smsqueue = MessageQueue.Create(SmsQueuePath);
            }
            Message smsmsg = new Message(sms);
            smsmsg.Label = sms.VendorTranId;
            smsmsg.Recoverable = true;
            smsqueue.Send(smsmsg);
        }
        catch (Exception ex)
        {
            //donothing
        }
    }

    public bool IsValidDate(string paymentDate)
    {
        DateTime date;
        //if (DateTime.TryParse(paymentDate, out date))
        //{
            //return true;
            string format = "dd/MM/yyyy";
            return DateTime.TryParseExact(paymentDate, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        //}
        //else
        //{
        //    return false;
        //}
    }

    internal string EncryptUraParameter(string parameter)
    {
        try
        {
            X509Certificate2 CertV = GetURACert(); //GetBankCert();
            byte[] cipherbytes = ASCIIEncoding.ASCII.GetBytes(parameter);

            RSACryptoServiceProvider rsa = (RSACryptoServiceProvider)CertV.PublicKey.Key;// .PrivateKey;//to verify/confirm
            byte[] ciph = rsa.Encrypt(cipherbytes, false);
            string CryptPass = Convert.ToBase64String(ciph);
            return CryptPass;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    private X509Certificate2 GetURACert()
    {
        try
        {
            //string fileName = System.Windows.Forms.Application.StartupPath + "\\certs\\" + "URAPayment.cer";
            string fileName = path + "URAioPmtCert1.cer";

            if (fileName.Trim().Length > 0)
            {
                X509Certificate2 cert = new X509Certificate2(fileName);
                return cert;
            }
            else
                return null;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    internal string GetKccaAuthentication(string ap_key, string ap_username, string ap_passord, string hash, string backref)
    {
        try
        {
            // KCCA.BankPaymentService payment = new KCCA.BankPaymentService();
            UtilityReferences.KCCA.TelecomPaymentService payment = new UtilityReferences.KCCA.TelecomPaymentService();
            System.Net.ServicePointManager.ServerCertificateValidationCallback = RemoteCertValidation;
            string resp = payment.authenticate(ap_key, ap_username, ap_passord, hash, backref);
            return resp;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    private bool RemoteCertValidation(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        internal KCCAQueryResponse ParseXmlResponse(string resp, string Action)
        {
            try
            {
                KCCAQueryResponse authresp = new KCCAQueryResponse();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(resp);

                if (Action.Trim().ToUpper().Equals("AUTH"))
                {
                    XmlNode message = doc.DocumentElement.SelectSingleNode("/authenticateResponse/message");
                    authresp.Success = message.Attributes["success"].Value;
                    if (authresp.Success.Equals("1"))
                    {
                        authresp.SessionKey = message.Attributes["session_key"].Value;
                        authresp.RefCheck = message.Attributes["refcheck"].Value;
                    }
                    else
                    {
                        authresp.ErrorCode = message.Attributes["error_code"].Value;
                        authresp.RefCheck = message.Attributes["refcheck"].Value;
                        authresp.ErrorDescription = message.InnerText.ToString();
                    }
                }
                else if (Action.Trim().ToUpper().Equals("VERIFICATION"))
                {
                    XmlNode message = doc.DocumentElement.SelectSingleNode("/verifyReferenceResponse/message");
                    authresp.Success = message.Attributes["success"].Value;
                    if (authresp.Success.Equals("1"))
                    {
                        authresp.SessionKey = message.Attributes["session_key"].Value;
                        authresp.RefCheck = message.Attributes["refcheck"].Value;
                        authresp.PaymentReference = message.Attributes["PRN"].Value;
                        authresp.CustomerName = message.Attributes["customerName"].Value;
                        authresp.CustomerPhone = message.Attributes["customerPhone"].Value;
                        authresp.PaymentAmount = message.Attributes["paymentAmount"].Value;
                        authresp.PaymentCurrency = message.Attributes["paymentCurrency"].Value;
                        authresp.Status = message.Attributes["status"].Value;
                        authresp.Coin = message.Attributes["COIN"].Value;
                        authresp.PrnDate = message.Attributes["prnDate"].Value;
                        authresp.ExpiryDate = message.Attributes["expiryDate"].Value;
                       // authresp.PaymentReference = message.Attributes["paymentReference"].Value;
                        //authresp.PaymentType = message.Attributes["paymentType"].Value;
                    }
                    else
                    {
                        authresp.ErrorCode = message.Attributes["error_code"].Value;
                        authresp.RefCheck = message.Attributes["refcheck"].Value;
                        authresp.ErrorDescription = message.InnerText.ToString();
                    }
                }
                else if (Action.Trim().ToUpper().Equals("TRANSACT"))
                {
                    XmlNode message = doc.DocumentElement.SelectSingleNode("/transactResponse/message");
                    authresp.Success = message.Attributes["success"].Value;
                    if (authresp.Success.Equals("1"))
                    {
                        authresp.SessionKey = message.Attributes["session_key"].Value;
                        authresp.RefCheck = message.Attributes["refcheck"].Value;
                        authresp.TransactionID = message.Attributes["transactionID"].Value;
                        authresp.PaymentReference = message.Attributes["PRN"].Value;
                        //authresp.PaymentReference = message.Attributes["paymentReference"].Value;
                        //authresp.PaymentDate = message.Attributes["paymentDate"].Value;
                        //authresp.TrackingID = message.Attributes["trackingID"].Value;
                    }
                    else
                    {
                        authresp.ErrorCode = message.Attributes["error_code"].Value;
                        authresp.RefCheck = message.Attributes["refcheck"].Value;
                        authresp.ErrorDescription = message.InnerText.ToString();
                    }
                }
                else if (Action.Trim().ToUpper().Equals("CLOSETRANSACT"))
                {
                    XmlNode message = doc.DocumentElement.SelectSingleNode("/closeTransactionResponse/message");
                    authresp.Success = message.Attributes["success"].Value;
                    if (authresp.Success.Equals("1"))
                    {
                        authresp.RefCheck = message.Attributes["refcheck"].Value;
                        authresp.TransactionID = message.Attributes["transactionID"].Value;
                        authresp.PaymentReference = message.Attributes["paymentReference"].Value;
                    }
                    else
                    {
                        authresp.ErrorCode = message.Attributes["error_code"].Value;
                        authresp.RefCheck = message.Attributes["refcheck"].Value;
                        authresp.ErrorDescription = message.InnerText.ToString();
                    }
                }

                return authresp;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        internal string VerifyKccaPRN(string SessionKey, string custRef, string Hash, string ap_backCheck)
        {
            try
            {
                //KCCA.BankPaymentService payment = new KCCA.BankPaymentService();
                UtilityReferences.KCCA.TelecomPaymentService payment = new UtilityReferences.KCCA.TelecomPaymentService();
                System.Net.ServicePointManager.ServerCertificateValidationCallback = RemoteCertValidation;
                string resp = payment.verifyReference(SessionKey,custRef,Hash,ap_backCheck);
                return resp;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        
    //    internal string MakeKccaPayment(Transaction trans)
    //    {
    //        //KCCAResponse authResp=new KCCAResponse();
    //        try
    //        {
    //           // DataTable kcca_access2 = dac.GetGlobalValues("9", "10"); // not goin thru
    //           // string ap_secretKey = kcca_access2.Rows[0]["Valuename"].ToString();
    //           // string ap_secretKey = "F8FF335747EE7AB31F7911C233AE5616";
    //           // string ap_Sessionkey = trans.Session_Key;
    //            //string ap_secretKey = trans.secret_Key;
    //            //KCCA.BankPaymentService svc = new KCCA.BankPaymentService();
    //            UtilityReferences.KCCA.TelecomPaymentService svc = new UtilityReferences.KCCA.TelecomPaymentService();

    //            //Transact
    //            string ap_backCheck = "";
    //            string sessionkey = trans.Session_Key;
    //            string prn = trans.PaymentReference;
    //            string tranId = "1"; ;
    //            string coin = trans.Coin;
    //            string bankBranchCode = "2344";
    //            string status = "C";
    //            string chequeNo = "";
    //           // string PaymentType = trans.PaymentType;
    //            //string payGroup = trans.PaymentGroup;
    //            string payAmount = trans.BillAmount.ToString();
    //            string valueDate = DateTime.Now.ToString("dd'/'MM'/'yyyy");
    //            string payDate = DateTime.Now.ToString("dd'/'MM'/'yyyy");
    //           // string payDate = String.Format("{0:dd-MM-yyyy HH:mm}", DateTime.Now);//2001-03-10 17:16
               
    //            string Trans = "<?xml version=\"1.0\"?>" +
    //"<transactionRecord>" +
    //"<PRN>" + prn + "</PRN>" +
    //"<COIN>" + coin + "</COIN>" +
    //"<amountPaid>" + payAmount + "</amountPaid>" +
    //"<paymentDate>" + payDate + "</paymentDate>" +
    //"<valueDate>" + valueDate + "</valueDate>" +
    //"<status>" + status + "</status>" +
    //"<bankBranchCode>" + bankBranchCode + "</bankBranchCode>"+
    //"<transactionID>" + tranId + "</transactionID>" +
    // "<chequeNumber>" + chequeNo + "</chequeNumber>" +
    
    //"</transactionRecord>";
    //            //string ap_hash = calculateMD5(trans.Session_Key + trans.CustRef + ap_secretKey);
    //            string ap_hash = "";
    //            //string ap_hash = dac.calculateMD5(ap_key + ap_username + ap_passord + ap_secretKey);
    //            string resp = svc.transact(sessionkey, prn, Trans, ap_hash, ap_backCheck);
    //            return resp;
                
               
    //        }
    //        catch(Exception ex)
    //        {
    //            throw ex;
    //        }
    //    }
    //    internal string CloseTransaction(string SessionKey, string CustomerID, string TransactionID, string Hash, string RefCheck)
    //    {
    //        try
    //        {
    //            UtilityReferences.KCCA.TelecomPaymentService payment = new UtilityReferences.KCCA.TelecomPaymentService();

    //            System.Net.ServicePointManager.ServerCertificateValidationCallback = RemoteCertValidation;
    //            //string resp = payment.verifyReference(SessionKey, custRef, Hash, ap_backCheck);

    //            //ap_hash = calculateMD5(authResp.SessionKey + authResp.CustomerID + authResp.TransactionID + authResp.TrackingID + trans.SecretKey);
    //            string resp = payment.closeTransaction(SessionKey, CustomerID, TransactionID, Hash, RefCheck);
    //            return resp;
    //        }
    //        catch (Exception ex)
    //        {
    //            throw ex;
    //        }
    //    }
    

    //public KCCAPostResponse closeTrans(string xmlstring)
    //{
    //    try
    //    {

    //        XmlDocument doc = new XmlDocument();
    //        KCCAPostResponse resp = new KCCAPostResponse();
    //        doc.LoadXml(xmlstring);
    //        XmlNode message = doc.DocumentElement.SelectSingleNode("/transactionResponse/message");
    //        resp.status = message.Attributes["success"].Value;

    //        if (resp.status.ToString().Equals("1"))
    //        {
    //            resp.PaymentReference = message.Attributes["paymentReference"].Value;
    //            resp.TransID = message.Attributes["transactionID"].Value;

    //        }
    //        else
    //        {
    //            resp.Error_code = message.Attributes["error_code"].Value;
    //            resp.backRef = message.Attributes["refcheck"].Value;
    //        }
    //        return resp;
    //    }

    //    catch (Exception ex)
    //    {
    //        throw ex;
    //    }


    //}
    //internal string GetKccaAuthentication(string ap_key, string ap_username, string ap_passord, string hash, string backref)
    //{
    //    try
    //    {
    //        UtilityReferences.KCCA.BankPaymentService payment = new UtilityReferences.KCCA.BankPaymentService();
    //        System.Net.ServicePointManager.ServerCertificateValidationCallback = RemoteCertificateValidation;
    //        string resp = payment.authenticate(ap_key, ap_username, ap_passord, hash, backref);
    //        return resp;
    //    }
    //    catch (Exception ex)
    //    {
    //        throw ex;
    //    }
    //}
    public string CalculateMD5Hash(string input)
    {
        // step 1, calculate MD5 hash from input
        MD5 md5 = MD5.Create();
        byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
        byte[] hash = md5.ComputeHash(inputBytes);

        // step 2, convert byte array to hex string
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < hash.Length; i++)
        {
            sb.Append(hash[i].ToString("X2"));
        }
        return sb.ToString();
    }
    //public KCCAQueryResponse ParserXML(string xmlString)
    //{
    //    try
    //    {
    //        KCCAQueryResponse resp = new KCCAQueryResponse();
    //        XmlDocument doc = new XmlDocument();
    //        doc.LoadXml(xmlString);
    //        XmlNode message = doc.DocumentElement.SelectSingleNode("/authenticateResponse/message");
    //        resp.status = message.Attributes["success"].Value;
    //        if (resp.status.Equals("1"))
    //        {
    //            resp.sessionKey = message.Attributes["session_key"].Value;
    //            resp.backRef = message.Attributes["refcheck"].Value;
    //        }
    //        else
    //        {
    //            resp.StatusCode = message.Attributes["error_code"].Value;
    //            resp.backRef = message.Attributes["refcheck"].Value;
    //            resp.StatusDescription = message.InnerText.ToString();
    //        }
    //        return resp;
    //    }
    //    catch (Exception ex)
    //    {
    //        throw ex;
    //    }

    //}
    //public KCCAPostResponse ParserXMLPost(string xmlString)
    //{
    //    try
    //    {
    //        KCCAPostResponse resp = new KCCAPostResponse();
    //        XmlDocument doc = new XmlDocument();
    //        doc.LoadXml(xmlString);
    //        XmlNode message = doc.DocumentElement.SelectSingleNode("/authenticateResponse/message");
    //        resp.status = message.Attributes["success"].Value;
    //        resp.sessionKey = message.Attributes["session_key"].Value;
    //        resp.backRef = message.Attributes["refcheck"].Value;

    //        return resp;
    //    }
    //    catch (Exception ex)
    //    {
    //        throw ex;
    //    }

    //}
    //public KCCAQueryResponse verifyXml(string xmlstring)
    //{
    //    try
    //    {
    //        KCCAQueryResponse resp = new KCCAQueryResponse();
    //        XmlDocument doc = new XmlDocument();
    //        doc.LoadXml(xmlstring);
    //        XmlNode message = doc.DocumentElement.SelectSingleNode("/verifyReferenceResponse/message");
    //        resp.status = message.Attributes["success"].Value;
    //        if (resp.status.ToString().Equals("1"))
    //        {
    //            resp.sessionKey = message.Attributes["session_key"].Value;
    //            resp.backRef = message.Attributes["refcheck"].Value;
    //            resp.CustomerID = message.Attributes["customerID"].Value;
    //            resp.CustomerName = message.Attributes["customerName"].Value;
    //            resp.CustomerPhone = message.Attributes["customerPhone"].Value;
    //            resp.PaymentReference = message.Attributes["paymentReference"].Value;
    //            resp.PaymentType = message.Attributes["paymentType"].Value;
    //            resp.CustomerType = message.Attributes["paymentType"].Value;
    //            resp.PaymentGroup = message.Attributes["paymentGroup"].Value;
    //            resp.PaymentDescription = message.Attributes["paymentDescription"].Value;
    //            resp.PaymentAmount = message.Attributes["paymentAmount"].Value;

    //            resp.OutstandingBalance = message.Attributes["paymentAmount"].Value;
    //            resp.CustomerReference = message.Attributes["paymentReference"].Value;
    //            resp.PaymentCurrency = message.Attributes["paymentCurrency"].Value;
    //        }
    //        else
    //        {
    //            resp.StatusCode = message.Attributes["error_code"].Value;
    //            resp.backRef = message.Attributes["refcheck"].Value;
    //            resp.StatusDescription = message.InnerText.ToString();
    //        }

    //        return resp;

    //    }
    //    catch (Exception ex)
    //    {
    //        throw ex;
    //    }
    //}

    //public KCCAPostResponse verifyPostXml(string xmlstring)
    //{
    //    try
    //    {
    //        KCCAPostResponse resp = new KCCAPostResponse();
    //        XmlDocument doc = new XmlDocument();
    //        doc.LoadXml(xmlstring);
    //        XmlNode message = doc.DocumentElement.SelectSingleNode("/verifyReferenceResponse/message");
    //        resp.status = message.Attributes["success"].Value;
    //        if (resp.status.ToString().Equals("1"))
    //        {
    //            resp.sessionKey = message.Attributes["session_key"].Value;
    //            resp.backRef = message.Attributes["refcheck"].Value;
    //            resp.CustomerID = message.Attributes["customerID"].Value;
    //            resp.CustomerName = message.Attributes["customerName"].Value;
    //            resp.CustomerPhone = message.Attributes["customerPhone"].Value;
    //            resp.PaymentReference = message.Attributes["paymentReference"].Value;
    //            resp.PaymentType = message.Attributes["paymentType"].Value;
    //            resp.PaymentGroup = message.Attributes["paymentGroup"].Value;
    //            resp.PaymentDescription = message.Attributes["paymentDescription"].Value;
    //            resp.PaymentAmount = message.Attributes["paymentAmount"].Value;
    //            //resp.OutStandingBalance=
    //            resp.PaymentCurrency = message.Attributes["paymentCurrency"].Value;
    //        }
    //        else 
    //        {
    //            resp.StatusCode = message.Attributes["error_code"].Value;
    //            resp.backRef = message.Attributes["refcheck"].Value;
    //            resp.StatusDescription = message.InnerText.ToString();
    //        }
    //        return resp;

    //    }
    //    catch (Exception ex)
    //    {
    //        throw ex;
    //    }
    //}
    //public KCCAPostResponse Transaction(string xmlstring)
    //{
    //    try
    //    {
    //        KCCAPostResponse resp = new KCCAPostResponse();
    //        XmlDocument doc = new XmlDocument();
    //        doc.LoadXml(xmlstring);
    //        XmlNode message = doc.DocumentElement.SelectSingleNode("/transactionResponse/message");
    //        resp.status = message.Attributes["success"].Value;

    //        if (resp.status.ToString().Equals("1"))
    //        {
    //            resp.sessionKey = message.Attributes["session_key"].Value;
    //            resp.TransID = message.Attributes["transactionID"].Value;
    //            resp.PaymentReference = message.Attributes["paymentReference"].Value;
    //            resp.PaymentDate = message.Attributes["paymentDate"].Value;
    //            resp.backRef = message.Attributes["refcheck"].Value;
    //            resp.TrackingID = message.Attributes["trackingID"].Value;
    //        }
    //        else
    //        {

    //            resp.StatusCode = message.Attributes["error_code"].Value;
    //            resp.backRef = message.Attributes["refcheck"].Value;
    //            resp.StatusDescription = message.InnerText.ToString();
    //        }
    //        return resp;
    //    }
    //    catch (Exception ex)
    //    {
    //        throw ex;
    //    }
    //}
    public string GetXMLAsString(XmlDocument myxml)
    {

        StringWriter sw = new StringWriter();
        XmlTextWriter tx = new XmlTextWriter(sw);
        myxml.WriteTo(tx);

        string str = sw.ToString();
        return str;
    }
    public void xmlConvert(KCCATransaction trans)
    {
        try
        {

            XmlDocument doc = new XmlDocument();
            XmlTextWriter writer = new XmlTextWriter(@"E:\STANBIC-KCCA\xmlKCCAtransactionRequests\NewXmlFile.xml", Encoding.UTF8);
            XmlDeclaration declaration = doc.CreateXmlDeclaration("1.0", "", null);
            writer.WriteStartDocument();
            writer.WriteStartElement("transactionResponse");
            writer.WriteElementString("transactionID", trans.transactionID);
            writer.WriteElementString("amountPaid", trans.TransactionAmount);
            writer.WriteElementString("paymentDate", trans.PaymentDate);
            writer.WriteElementString("paymentType", trans.PaymentType);
            writer.WriteElementString("paymentGroup", trans.PaymentType);
            writer.WriteEndElement();
            writer.WriteEndDocument();

            writer.Close();
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }


    //internal KCCAQueryResponse ParseXmlResponse(string resp, string Action)
    //{
    //    try
    //    {
    //        KCCAQueryResponse authresp = new KCCAQueryResponse();
    //        XmlDocument doc = new XmlDocument();
    //        doc.LoadXml(resp);

    //        if (Action.Trim().ToUpper().Equals("AUTH"))
    //        {
    //            XmlNode message = doc.DocumentElement.SelectSingleNode("/authenticateResponse/message");
    //            authresp.Success = message.Attributes["success"].Value;
    //            if (authresp.Success.Equals("1"))
    //            {
    //                authresp.sessionKey = message.Attributes["session_key"].Value;
    //                authresp.backRef = message.Attributes["refcheck"].Value;
    //            }
    //            else
    //            {
    //                authresp.StatusCode = message.Attributes["error_code"].Value;
    //                authresp.backRef = message.Attributes["refcheck"].Value;
    //                authresp.StatusDescription = message.InnerText.ToString();
    //            }
    //        }
    //        else if (Action.Trim().ToUpper().Equals("VERIFICATION"))
    //        {
    //            XmlNode message = doc.DocumentElement.SelectSingleNode("/verifyReferenceResponse/message");
    //            authresp.Success = message.Attributes["success"].Value;
    //            if (authresp.Success.Equals("1"))
    //            {
    //                authresp.sessionKey = message.Attributes["session_key"].Value;
    //                authresp.backRef = message.Attributes["refcheck"].Value;
    //                authresp.CustomerID = message.Attributes["PRN"].Value;
    //                authresp.CustomerName = message.Attributes["customerName"].Value;
    //                authresp.CustomerPhone = message.Attributes["customerPhone"].Value;
    //                authresp.PaymentAmount = message.Attributes["paymentAmount"].Value;
    //                authresp.PaymentCurrency = message.Attributes["paymentCurrency"].Value;
    //                authresp.status= message.Attributes["status"].Value;
    //                authresp.Coin= message.Attributes["COIN"].Value;
    //                authresp.PrnDate = message.Attributes["prnDate"].Value;
    //                authresp.ExpiryDate = message.Attributes["expiryDate"].Value;
    //                // authresp.PaymentReference = message.Attributes["paymentReference"].Value;
    //                //authresp.PaymentType = message.Attributes["paymentType"].Value;
    //            }
    //            else
    //            {
    //                authresp.StatusCode = message.Attributes["error_code"].Value;
    //                authresp.backRef = message.Attributes["refcheck"].Value;
    //                authresp.StatusDescription = message.InnerText.ToString();
    //            }
    //        }
    //        else if (Action.Trim().ToUpper().Equals("TRANSACT"))
    //        {
    //            XmlNode message = doc.DocumentElement.SelectSingleNode("/transactResponse/message");
    //            authresp.Success = message.Attributes["success"].Value;
    //            if (authresp.Success.Equals("1"))
    //            {
    //                authresp.sessionKey = message.Attributes["session_key"].Value;
    //                authresp.backRef = message.Attributes["refcheck"].Value;
    //                authresp.VendorTransactionRef = message.Attributes["transactionID"].Value;
    //                authresp.CustomerReference = message.Attributes["PRN"].Value;
    //                //authresp.PaymentReference = message.Attributes["paymentReference"].Value;
    //                //authresp.PaymentDate = message.Attributes["paymentDate"].Value;
    //                //authresp.TrackingID = message.Attributes["trackingID"].Value;
    //            }
    //            else
    //            {
    //                authresp.StatusCode = message.Attributes["error_code"].Value;
    //                authresp.backRef = message.Attributes["refcheck"].Value;
    //                authresp.StatusDescription = message.InnerText.ToString();
    //            }
    //        }
    //        else if (Action.Trim().ToUpper().Equals("CLOSETRANSACT"))
    //        {
    //            XmlNode message = doc.DocumentElement.SelectSingleNode("/closeTransactionResponse/message");
    //            authresp.Success = message.Attributes["success"].Value;
    //            if (authresp.Success.Equals("1"))
    //            {
    //                authresp.backRef = message.Attributes["refcheck"].Value;
    //                authresp.VendorTransactionRef = message.Attributes["transactionID"].Value;
    //                authresp.TransID = message.Attributes["paymentReference"].Value;
    //            }
    //            else
    //            {
    //                authresp.StatusCode = message.Attributes["error_code"].Value;
    //                authresp.backRef = message.Attributes["refcheck"].Value;
    //                authresp.StatusDescription = message.InnerText.ToString();
    //            }
    //        }

    //        return authresp;
    //    }
    //    catch (Exception ex)
    //    {
    //        throw ex;
    //    }
    //}

    //public string Verify1KccaPRN(string SessionKey, string custRef, string Hash, string ap_backCheck)
    //{
    //    try
    //    {
    //        UtilityReferences.KCCA.TelecomPaymentService payment = new UtilityReferences.KCCA.TelecomPaymentService();
    //       System.Net.ServicePointManager.ServerCertificateValidationCallback = RemoteCertificateValidation;
    //        string resp = payment.verifyReference(SessionKey, custRef, Hash, ap_backCheck);
    //        return resp;
    //    }
    //    catch (Exception ex)
    //    {
    //        throw ex;
    //    }
    //}
    //private static bool RemoteCertificateValidation(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    //{
    //    return true;
    //}

    //    internal KCCAQueryResponse MakeKccaPayment(KCCATransaction trans)
    //    {
    //        KCCAQueryResponse authResp = new KCCAQueryResponse();
    //        BusinessLogic bll = new BusinessLogic();
    //        try
    //        {
    //           UtilityReferences.KCCA.BankPaymentService svc = new UtilityReferences.KCCA.BankPaymentService();
    //            //Transact
    //            string ap_backCheck = "";
    //            string prn = trans.CustRef;
    //            string tranId = trans.VendorTransactionRef;
    //            string coin = trans.Coin;
    //            string bankBranchCode = trans.BranchCode;
    //            string status = "C";
    //            string chequeNo = "";
    //            string PaymentType = trans.PaymentType;
    //            //string payGroup = trans.PaymentGroup;
    //            string payAmount = trans.TransactionAmount;
    //            string payDate = trans.PaymentDate;//String.Format("{0:dd-MM-yyyy HH:mm}", DateTime.Now);//2001-03-10 17:16:18
    //            string Trans = "<?xml version=\"1.0\"?>" +
    //"<transactionRecord>" +
    //"<PRN>" + prn + "</PRN>" +
    //"<COIN>" + payAmount + "</COIN>" +
    //"<amountPaid>" + payAmount + "</amountPaid>" +
    //"<paymentDate>" + payDate + "</paymentDate>" +
    //"<valueDate>" + payDate + "</valueDate>" +
    //"<status>" + status + "</status>" +
    //"<bankBranchCode>" + bankBranchCode + "</bankBranchCode>" +
    //"<transactionID>" + tranId + "</transactionID>" +
    // "<chequeNumber>" + chequeNo + "</chequeNumber>" +

    //"</transactionRecord>";
    //            //string ap_hash = calculateMD5(trans.Session_Key + trans.CustRef + ap_secretKey);
    //            //string ap_hash = dac.calculateMD5(ap_key + ap_username + ap_passord + ap_secretKey);
    //            string resp = svc.transact(trans.SessonKey, trans.CustRef, Trans, "", ap_backCheck);

    //            authResp = bll.ParseXmlResponse(resp, "Transact");
    //            if (authResp.Equals("1"))
    //            {
    //                // dh.CommitTransaction(trans.VendorTransactionRef, TranID, authResp.TrackingID, "", "S");
    //                //CloseTransaction authResp.SessionKey, authResp.PaymentReference, authResp.TransactionID, authResp.TrackingID, "", ap_backCheck
    //                //ap_hash = dac.calculateMD5(authResp.SessionKey + authResp.PaymentReference + authResp.TransactionID + authResp.TrackingID + ap_secretKey);
    //                resp = svc.closeTransaction(authResp.sessionKey, authResp.PaymentReference, authResp.VendorTransactionRef, "", authResp.backRef);
    //                KCCAQueryResponse authResp2 = new KCCAQueryResponse();
    //                authResp2 = ParseXmlResponse(resp, "closeTransact");
    //                if (authResp2.Success.Equals("1"))
    //                {
    //                    return authResp;
    //                }
    //                else
    //                {
    //                    authResp.PaymentReference = "";
    //                }
    //            }
    //            else
    //            {
    //                authResp.PaymentReference = "";

    //            }
    //            return authResp;
    //        }
    //        catch (Exception ex)
    //        {
    //            throw ex;
    //        }
    //    }

    // new kcca Implementations below here

    internal string calculateMD5(string input)
    {
        try
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }
        catch (Exception ex)
        {
            throw ex;
        }

    }

    //internal string GetKccaAuthentication(string ap_key, string ap_username, string ap_passord, string hash, string backref)
    //{
    //    try
    //    {
    //        UtilityReferences.KCCA.TelecomPaymentService payment = new UtilityReferences.KCCA.TelecomPaymentService();
    //        System.Net.ServicePointManager.ServerCertificateValidationCallback = RemoteCertValidation;
    //        string resp = payment.authenticate(ap_key, ap_username, ap_passord, hash, backref);
    //        return resp;
    //    }
    //    catch (Exception ex)
    //    {
    //        throw ex;
    //    }
    //}
    

    //internal KCCAQueryResponse ParseXmlResponse(string resp, string Action)
    //{
    //    try
    //    {
    //        KCCAQueryResponse authresp = new KCCAQueryResponse();
    //        XmlDocument doc = new XmlDocument();
    //        doc.LoadXml(resp);

    //        if (Action.Trim().ToUpper().Equals("AUTH"))
    //        {
    //            XmlNode message = doc.DocumentElement.SelectSingleNode("/authenticateResponse/message");
    //            authresp.Success = message.Attributes["success"].Value;
    //            if (authresp.Success.Equals("1"))
    //            {
    //                authresp.sessionKey = message.Attributes["session_key"].Value;
    //                authresp.RefCheck = message.Attributes["refcheck"].Value;
    //            }
    //            else
    //            {
    //                authresp.ErrorCode = message.Attributes["error_code"].Value;
    //                authresp.RefCheck = message.Attributes["refcheck"].Value;
    //                authresp.ErrorDescription = message.InnerText.ToString();
    //            }
    //        }
    //        else if (Action.Trim().ToUpper().Equals("VERIFICATION"))
    //        {
    //            XmlNode message = doc.DocumentElement.SelectSingleNode("/verifyReferenceResponse/message");
    //            authresp.Success = message.Attributes["success"].Value;
    //            if (authresp.Success.Equals("1"))
    //            {
    //                authresp.sessionKey = message.Attributes["session_key"].Value;
    //                authresp.RefCheck = message.Attributes["refcheck"].Value;
    //                authresp.CustomerID = message.Attributes["PRN"].Value;
    //                authresp.CustomerName = message.Attributes["customerName"].Value;
    //                authresp.CustomerPhone = message.Attributes["customerPhone"].Value;
    //                authresp.PaymentAmount = message.Attributes["paymentAmount"].Value;
    //                authresp.PaymentCurrency = message.Attributes["paymentCurrency"].Value;
    //                authresp.status = message.Attributes["status"].Value;
    //                authresp.Coin = message.Attributes["COIN"].Value;
    //                authresp.PrnDate = message.Attributes["prnDate"].Value;
    //                authresp.ExpiryDate = message.Attributes["expiryDate"].Value;
    //                // authresp.PaymentReference = message.Attributes["paymentReference"].Value;
    //                //authresp.PaymentType = message.Attributes["paymentType"].Value;
    //                authresp.ErrorCode = "";//message.Attributes["error_code"].Value;
    //                authresp.RefCheck = message.Attributes["refcheck"].Value;
    //                authresp.ErrorDescription = message.InnerText.ToString();
    //                authresp.StatusCode = "0";
    //                authresp.StatusDescription = "SUCCESS";
    //            }
    //            else
    //            {
    //                authresp.ErrorCode = message.Attributes["error_code"].Value;
    //                authresp.RefCheck = message.Attributes["refcheck"].Value;
    //                authresp.ErrorDescription = message.InnerText.ToString();
    //            }
    //        }
    //        else if (Action.Trim().ToUpper().Equals("TRANSACT"))
    //        {
    //            XmlNode message = doc.DocumentElement.SelectSingleNode("/transactResponse/message");
    //            authresp.Success = message.Attributes["success"].Value;
    //            if (authresp.Success.Equals("1"))
    //            {
    //                authresp.sessionKey = message.Attributes["session_key"].Value;
    //                authresp.RefCheck = message.Attributes["refcheck"].Value;
    //                authresp.TransactionID = message.Attributes["transactionID"].Value;
    //                authresp.CustomerID = message.Attributes["PRN"].Value;
    //                //authresp.PaymentReference = message.Attributes["paymentReference"].Value;
    //                //authresp.PaymentDate = message.Attributes["paymentDate"].Value;
    //                //authresp.TrackingID = message.Attributes["trackingID"].Value;
    //            }
    //            else
    //            {
    //                authresp.ErrorCode = message.Attributes["error_code"].Value;
    //                authresp.RefCheck = message.Attributes["refcheck"].Value;
    //                authresp.ErrorDescription = message.InnerText.ToString();
    //            }
    //        }
    //        else if (Action.Trim().ToUpper().Equals("CLOSETRANSACT"))
    //        {
    //            XmlNode message = doc.DocumentElement.SelectSingleNode("/closeTransactionResponse/message");
    //            authresp.Success = message.Attributes["success"].Value;
    //            if (authresp.Success.Equals("1"))
    //            {
    //                authresp.RefCheck = message.Attributes["refcheck"].Value;
    //                authresp.TransactionID = message.Attributes["transactionID"].Value;
    //                authresp.PaymentReference = message.Attributes["paymentReference"].Value;
    //            }
    //            else
    //            {
    //                authresp.ErrorCode = message.Attributes["error_code"].Value;
    //                authresp.RefCheck = message.Attributes["refcheck"].Value;
    //                authresp.ErrorDescription = message.InnerText.ToString();
    //            }
    //        }

    //        return authresp;
    //    }
    //    catch (Exception ex)
    //    {
    //        throw ex;
    //    }
    //}

    //internal string VerifyKccaPRN(string SessionKey, string custRef, string Hash, string ap_backCheck)
    //{
    //    try
    //    {
    //        UtilityReferences.KCCA.TelecomPaymentService payment = new UtilityReferences.KCCA.TelecomPaymentService();
    //        System.Net.ServicePointManager.ServerCertificateValidationCallback = RemoteCertValidation;
    //        string resp = payment.verifyReference(SessionKey, custRef, Hash, ap_backCheck);
    //        return resp;
    //    }
    //    catch (Exception ex)
    //    {
    //        throw ex;
    //    }
    //}


    //internal KCCAQueryResponse MakeKccaPayment(KCCATransaction trans)
    //{
    //    KCCAQueryResponse authResp = new KCCAQueryResponse();
    //    dataAccess dac = new dataAccess();
    //    DatabaseHandler dh = new DatabaseHandler();
    //    UtilityCredentials creds;
    //    DatabaseHandler dp = new DatabaseHandler();
    //    try
    //    {
    //        // DataTable kcca_access2 = dac.GetGlobalValues("9", "10"); // not goin thru
    //        // string ap_secretKey = kcca_access2.Rows[0]["Valuename"].ToString();
    //        creds = dp.GetUtilityCreds("KCCA", "MTN");
    //        string ap_secretKey = creds.SecretKey;
    //        //string ap_secretKey = "F8FF335747EE7AB31F7911C233AE5616";
    //        string ap_Sessionkey = dh.GetKCCASession(trans.CustRef, "KCCA ");//trans.sessionKey;
    //        trans.SessonKey = ap_Sessionkey;
    //        //string ap_secretKey = trans.secret_Key;

    //        UtilityReferences.KCCA.TelecomPaymentService svc = new UtilityReferences.KCCA.TelecomPaymentService();
    //        //Transact
    //        string ap_backCheck = "";
    //        string prn = trans.CustRef;
    //        string tranId = trans.VendorTransactionRef;
    //        string coin = "";
    //        string bankBranchCode = "2344";
    //        string status = "C";
    //        string chequeNo = "";
    //        string PaymentType = trans.PaymentType;
    //        //string payGroup = trans.PaymentGroup;
    //        string payAmount = trans.TransactionAmount.ToString();
    //        string valueDate = DateTime.Now.ToString("dd'/'MM'/'yyyy");
    //        string payDate = DateTime.Now.ToString("dd'/'MM'/'yyyy");
    //        //string payDate = DateTime.Now.ToString("dd'/'MM'/'yyyy");
    //        //string payDate = String.Format("{0:dd-MM-yyyy HH:mm}", DateTime.Now);//2001-03-10 17:16

    //        string Trans = "<?xml version=\"1.0\"?>" +
    //"<transactionRecord>" +
    //"<PRN>" + prn + "</PRN>" +
    //"<COIN>" + payAmount + "</COIN>" +
    //"<amountPaid>" + payAmount + "</amountPaid>" +
    //"<paymentDate>" + payDate + "</paymentDate>" +
    //"<valueDate>" + valueDate + "</valueDate>" +
    //"<status>" + status + "</status>" +
    //"<bankBranchCode>" + bankBranchCode + "</bankBranchCode>" +
    //"<transactionID>" + tranId + "</transactionID>" +
    // "<chequeNumber>" + chequeNo + "</chequeNumber>" +

    //"</transactionRecord>";
    //        string ap_hash = calculateMD5(trans.SessonKey + trans.CustRef + ap_secretKey);
    //        //string ap_hash = dac.calculateMD5(ap_key + ap_username + ap_passord + ap_secretKey);
    //        string resp = svc.transact(ap_Sessionkey, trans.CustRef, Trans, "", ap_backCheck);

    //        authResp = ParseXmlResponse(resp, "Transact");
    //        if (authResp.Success.Equals("1"))
    //        {
    //            // dh.CommitTransaction(trans.VendorTransactionRef, TranID, authResp.TrackingID, "", "S");
    //            //CloseTransaction authResp.SessionKey, authResp.PaymentReference, authResp.TransactionID, authResp.TrackingID, "", ap_backCheck
    //            ap_hash = dac.calculateMD5(authResp.sessionKey + authResp.PaymentReference + authResp.TransactionID + authResp.TrackingID + ap_secretKey);
    //            resp = svc.closeTransaction(authResp.sessionKey, authResp.PaymentReference, authResp.TransactionID, "", authResp.RefCheck);
    //            KCCAQueryResponse authResp2 = new KCCAQueryResponse();
    //            authResp2 = ParseXmlResponse(resp, "closeTransact");
    //            if (authResp2.Success.Equals("1"))
    //            {
    //                return authResp;
    //            }
    //            else
    //            {
    //                authResp.PaymentReference = "";
    //            }
    //        }
    //        else
    //        {
    //            authResp.PaymentReference = "";

    //        }
    //        return authResp;
    //    }
    //    catch (Exception ex)
    //    {
    //        throw ex;
    //    }
    //}

    public String GetPbuCBSErrorMessage(string actionCode)
    {
        string errorMessage = "";
        Hashtable ActionCodes = new Hashtable();
        ActionCodes.Add("111", "INVALID SCHEME TYPE");
        ActionCodes.Add("114", "INVALID ACCOUNT NUMBER");
        ActionCodes.Add("115", "REQUESTED FUNCTION NOT SUPPORTED");
        ActionCodes.Add("116", "INSUFFICIENT FUNDS");
        ActionCodes.Add("119", "TRANSACTION NOT PERMITED TO CARD HOLDER");
        ActionCodes.Add("121", "WITHDRAWAL AMOUNT LIMIT EXCEEDED");
        ActionCodes.Add("163", "INVALID CHEQUE STATUS");
        ActionCodes.Add("180", "TRANSFER LIMIT ECXEEDED");
        ActionCodes.Add("181", "CHEQUES ARE IN DIFFERENT BOOKS");
        ActionCodes.Add("182", "NOT ALL CHEQUES COULD BE STOPPED");
        ActionCodes.Add("183", "CHEQUE NOT ISSUED TO THIS ACCOUNT");
        ActionCodes.Add("184", "REQUESTED BLOCK OPERATION FAILED SINCE ACCOUNT IS BLOCKED/FROZEN");
        ActionCodes.Add("185", "INVALID CURRENCY/TRANSACTION AMOUNT");
        ActionCodes.Add("186", "BLOCK DOESN'T EXIST");
        ActionCodes.Add("187", "CHEQUE STOPPED");
        ActionCodes.Add("188", "INVALID RATE CURRENCY COMBINATION");
        ActionCodes.Add("189", "CHEQUE BOOK ALREADY ISSUED");
        ActionCodes.Add("190", "DD ALREADY PAID");
        ActionCodes.Add("999", "GENERAL ERROR AT PBU");

        errorMessage = ActionCodes[actionCode].ToString();
        return errorMessage;
    }
    public  string GetRequestSignature(string DataToSign)
    {
        string certificate = @"C:\UtilityCertificates\STANBIC-KYU\StanbicCB.pfx";
        X509Certificate2 cert = new X509Certificate2(certificate, "Tingate710", X509KeyStorageFlags.MachineKeySet);
        RSACryptoServiceProvider rsa = (RSACryptoServiceProvider)cert.PrivateKey;
        //    // Hash the data
        MD5 md5 = MD5.Create();
        ASCIIEncoding encoding = new ASCIIEncoding();
        byte[] data = encoding.GetBytes(DataToSign);
        byte[] hash = md5.ComputeHash(data);

        // Sign the hash
        byte[] digitalCert = rsa.SignHash(hash, CryptoConfig.MapNameToOID("MD5"));
        string s = hash.ToString();
        string strDigCert = Convert.ToBase64String(digitalCert);
        return strDigCert;

    }
    public string GetRequestDigest(string message, string secret)
    {
        secret = secret ?? "";
        System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
        byte[] keyByte = encoding.GetBytes(secret);
        byte[] messageBytes = encoding.GetBytes(message);
        using (HMACSHA256 hmacsha256 = new HMACSHA256(keyByte))
        {
            byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
            return ByteArrayToString(hashmessage);
        }
    }
    public string ByteArrayToString(byte[] ba)
    {
        StringBuilder hex = new StringBuilder(ba.Length * 2);
        foreach (byte b in ba)
            hex.AppendFormat("{0:x2}", b);
        return hex.ToString();

    }
    internal bool IsBalanceOk(NWSCTransaction trans)
    {
        try
        {
            bool status = false;

            //Get The Transaction Pegasus Charge
            if (trans.VendorCode.Equals("INTERSWITCH"))
            {

                double InterSwitchTransactionCharge = GetCustomisedCharge(trans);
            }
            //any other Vendor
            else
            {

                DataTable ChargedataTable = dh.GetPrePaidVendorPegasusCharge(trans.VendorCode);

                if (ChargedataTable.Rows.Count > 0)
                {
                    trans.ChargeType = ChargedataTable.Rows[0]["ChargeType"].ToString().Trim();


                    if (trans.ChargeType.Equals("1"))
                    {
                        trans.PegpayCharge = Convert.ToDouble(ChargedataTable.Rows[0]["PegasusCharge"].ToString());
                    }
                    else if (trans.ChargeType.Equals("2"))
                    {
                        trans.PegpayCharge = (Convert.ToDouble(ChargedataTable.Rows[0]["PegasusCharge"].ToString()) / 100) * (Convert.ToDouble(trans.TransactionAmount));

                    }
                    else
                    {
                        trans.PegpayCharge = Convert.ToDouble(ChargedataTable.Rows[0]["PegasusCharge"].ToString());
                    }


                    //Compute the TransactionCharge and TransactionAmount
                    double total = 0;
                    total = Convert.ToDouble(trans.TransactionAmount) + Convert.ToDouble(trans.PegpayCharge);
                    DataTable dataTable = dh.GetPrepaidVendorAccountBalance(trans.VendorCode);
                    if (dataTable.Rows.Count > 0)
                    {
                        double balance = Convert.ToDouble(dataTable.Rows[0]["AccountBalance"].ToString().Trim());
                        if (total < balance)
                        {
                            status = false;
                        }
                        else
                        {
                            status = true;
                        }
                    }
                    else
                    {

                        status = true;

                    }
                }

                else
                {
                    status = true;

                }
            }
            return status;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    internal double GetCustomisedCharge(NWSCTransaction trans)
    {

        double ChargeOnTransaction = 0.0;

        if (trans.VendorCode.Equals("INTERSWITCH"))
        {

            DataTable dt = dh.GetPrePaidVendorPegasusCharge(trans.VendorCode);


            if (dt.Rows.Count > 0)
            {
                trans.ChargeType = dt.Rows[0]["ChargeType"].ToString().Trim();

                if (trans.ChargeType.Equals("1"))
                {
                    trans.PegpayCharge = Convert.ToDouble(dt.Rows[0]["PegasusCharge"].ToString());
                }
                else if (trans.ChargeType.Equals("2"))
                {

                    trans.PegpayCharge = (Convert.ToDouble(dt.Rows[0]["PegasusCharge"].ToString()) / 100) * (Convert.ToDouble(trans.TransactionAmount));
                    if (trans.PegpayCharge <= 40.0)
                    {

                        trans.PegpayCharge = 40.0;

                    }
                    else if (trans.PegpayCharge > 40.0 && trans.PegpayCharge <= 3000.0)
                    {

                        trans.PegpayCharge = trans.PegpayCharge;
                    }

                    else if (trans.PegpayCharge > 3000.0)
                    {

                        trans.PegpayCharge = trans.PegpayCharge;
                    }
                }
            }
            else
            {

            }
        }
        return trans.PegpayCharge;
    }


    internal bool IsValidReversalDigitalSignature(ReversalRequest trans)
    {
        string text = "";

        //bool valid = false;
        //DatabaseHandler dp = new DatabaseHandler();
        //DataTable dt2 = dp.GetSystemSettings("1", "6");
        //string certPath = dt2.Rows[0]["ValueVarriable"].ToString();
        //string vendorCode = trans.VendorCode;
        //certPath = certPath + "\\" + vendorCode + "\\";
        //string[] fileEntries = Directory.GetFiles(certPath);
        //string filePath = "";
        //if (fileEntries.Length == 1)
        //{
        //    filePath = fileEntries[0].ToString();
        //    X509Certificate2 cert = new X509Certificate2(filePath);
        //    RSACryptoServiceProvider csp = (RSACryptoServiceProvider)cert.PublicKey.Key;
        //    SHA1Managed sha1 = new SHA1Managed();
        //    //UnicodeEncoding encoding = new UnicodeEncoding();
        //    ASCIIEncoding encoding = new ASCIIEncoding();
        //    byte[] data = encoding.GetBytes(text);
        //    byte[] hash = sha1.ComputeHash(data);
        //    byte[] sig = Convert.FromBase64String(trans.DigitalSignature);
        //    valid = csp.VerifyHash(hash, CryptoConfig.MapNameToOID("SHA1"), sig);
        //    return valid;
        //    //return true;
        //}
        //else
        //{
        //    dp.LogError(" more than 1 certificate in folder", trans.VendorCode, DateTime.Now, "NONE");
        //    //LogGeneralError(" more than 1 certificate in folder", trans.VendorCode);
        //    return false;
        //}
        return true;
    }

    internal bool IsValidVendorCrednetials(string VendorCode, string Password)
    {
        DataTable vendorData = dh.GetVendorDetails(VendorCode);
        if(isValidVendorCredentials(VendorCode,Password,vendorData))
        {
            return true;
        }
        return false;
    }


    private bool isValidVendorCredentials(string vendorCode, string password, DataTable vendorData)
    {
        bool valid = false;
        try
        {
            BusinessLogic bll = new BusinessLogic();
            if (vendorData.Rows.Count != 0)
            {
                string vendor = vendorData.Rows[0]["VendorCode"].ToString();
                string encVendorPassword = vendorData.Rows[0]["VendorPassword"].ToString();
                string vendorPassword = bll.DecryptString(encVendorPassword);
                if (vendor.Trim().Equals(vendorCode.Trim()) && vendorPassword.Trim().Equals(password.Trim()))
                {
                    valid = true;
                }
                else
                {
                    valid = false;
                }
            }
            else
            {
                valid = false;
            }
        }
        catch (Exception ex)
        {
            throw ex;
        }
        return valid;
    }

    internal bool IsDuplicateReversalId(NWSCTransaction tran)
    {
        bool ret = false;
        DatabaseHandler dp = new DatabaseHandler();
        DataTable dt = dp.GetDuplicateReversalRef(tran.VendorCode, tran.VendorTransactionRef);
        if (dt.Rows.Count > 0)
        {
            ret = true;
        }
        else
        {
            ret = false;
        }
        return ret;
    }

    internal bool IsAlreadyReversed(NWSCTransaction tran, out PostResponse resp)
    {
        bool ret = false;
        DatabaseHandler dp = new DatabaseHandler();
        DataTable dt = dp.CheckIfReversed(tran.VendorCode, tran.TranIdToReverse);
        resp = new PostResponse();
        if (dt.Rows.Count > 0)
        {
            ret = true;
            string status = dt.Rows[0]["Status"].ToString();
            string PegPayId = dt.Rows[0]["PegasusReversalRef"].ToString();
            string reversalTranId = dt.Rows[0]["ReversalTransactionId"].ToString();
            if (status == "SUCCESS")
            {
                resp.PegPayPostId = PegPayId;
                resp.StatusCode = "58";
                resp.StatusDescription = dp.GetStatusDescr(resp.StatusCode) + reversalTranId;
            }
            else if (status == "PENDING")
            {
                resp.PegPayPostId = PegPayId;
                resp.StatusCode = "59";
                resp.StatusDescription = dp.GetStatusDescr(resp.StatusCode) + reversalTranId;
            }
        }
        else
        {
            ret = false;
        }
        return ret;
    }

    internal bool IsValidOriginalTransaction(NWSCTransaction tran)
    {
        DataTable dt = dh.GetOriginalPrepaidTransaction(tran.VendorCode, tran.TranIdToReverse);
        if (dt.Rows.Count > 0)
        {
            string UtilityCode = dt.Rows[0]["UtilityCode"].ToString().ToUpper();
            string CustomerType = dt.Rows[0]["CustomerType"].ToString().ToUpper();
            string CustomerRef = dt.Rows[0]["CustomerRef"].ToString().ToUpper();
            if (UtilityCode != tran.utilityCompany || CustomerType != tran.CustomerType || CustomerRef != tran.CustRef)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        else
        {
            return false;
        }
    }

    internal bool OriginalTransactionExistsAndAcceptsReversal(string vendorCode, string OriginalVendorTranId, out PostResponse result)
    {
        DatabaseHandler dh = new DatabaseHandler();
        DataTable dt = dh.GetOriginalPrepaidTransaction(vendorCode, OriginalVendorTranId);
        result = new PostResponse();

        //if original transaction is found
        if (dt.Rows.Count > 0)
        {
            DataRow dr = dt.Rows[0];
            bool acceptsReversal = UtilityAcceptsReversal(dr);
            if (!acceptsReversal)
            {
                result.StatusCode = "54";
                result.StatusDescription = dh.GetStatusDescr(result.StatusCode);
            }
            return acceptsReversal;
        }
        //original transaction not found
        else
        {
            result.StatusCode = "55";
            result.StatusDescription = dh.GetStatusDescr(result.StatusCode);
            return false;
        }
    }

    private bool UtilityAcceptsReversal(DataRow dr)
    {
        string UtilityCode = dr["UtilityCode"].ToString().ToUpper();
        string CustomerType = dr["CustomerType"].ToString().ToUpper();

        //if its a post paid Umeme transaction
        if (UtilityCode == "UMEME" && CustomerType == "POSTPAID")
        {
            return true;
        }
        //if its a Nwsc transaction
        else if (UtilityCode == "NWSC")
        {
            return true;
        }
        //some other utility
        else
        {
            return false;
        }
    }

    internal bool IsValidReversalAmount(NWSCTransaction trans)
    {
        double amt = double.Parse(trans.TransactionAmount);
        if (amt < 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    internal bool IsValidAmountToReverse(NWSCTransaction tran)
    {
        DatabaseHandler dh = new DatabaseHandler();
        DataTable dt = dh.GetOriginalPrepaidTransaction(tran.VendorCode, tran.TranIdToReverse);
        string tranAmount = dt.Rows[0]["TranAmount"].ToString();
        bool valid = false;
        double originalAmt = 0;
        if (Double.TryParse(tranAmount, out originalAmt))
        {
            double reversalAmt = 0;
            Double.TryParse(tran.TransactionAmount, out reversalAmt);
            double sum = originalAmt + reversalAmt;

            if (sum == 0)
            {
                valid = true;
            }
        }
        return valid;
    }


    internal string IsCustomCustomerType(string type)
    {
        string typeCode = "";
        try
        {
            typeCode = dh.GetCustomerType(type);

        }
        catch (Exception ex)
        {
            throw ex;
        }
        return typeCode;
    }
}
