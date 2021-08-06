using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using Sterling.MSSQL;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;
using System.Security.Cryptography;

namespace ConsoleJobs
{
    public class SterlingSecureCodeRecs
    {
        //Retrieve the data to be uploaded
        public static string[] GetMastercardUploadData(DateTime dt, string[] initialRecs)
        {
            DataSet ds = new DataSet();
            string[] filenames = null; int j = 1;
            string product = ConfigurationManager.AppSettings["product"];
            string stat = ConfigurationManager.AppSettings["status"];
            string pth = ConfigurationManager.AppSettings["GenPath"];
            string dte = dt.ToString("yyyy-MM-dd");

            string sql = "SELECT TOP 500 PAN, CUSTOMERNUMBER, CELLPHONE, EMAIL, STATUSFLAG, c.ID FROM CardRequest c INNER JOIN Product p ON c.PRODUCT = p.ID WHERE p.Misc in ('Visa Credit','Mastercard Products') " +
            " AND P.IsDeleted = '0' AND STATUSFLAG IN('4', '5', '11') " +
            "AND ((DATEADDED > '2021-07-01 00:00:00.000' AND DateDiff(DAY, DATEADDED, DATEAPPROVED) >= 1) or " +
            "(DATEADDED > '2021-07-01 00:00:00.000' AND DateDiff(DAY, DATEADDED, DATEAPPROVED) = 0)) " +
            "AND Securecode is NULL " +
            "AND PAN IS NOT NULL";

            Connect cn = new Connect("CardApp")
            {
                Persist = true
            };
            cn.SetSQL(sql);
            ds = cn.Select();
            cn.CloseAll();

            bool hasRows = ds.Tables.Cast<DataTable>().Any(table => table.Rows.Count != 0);
            if (hasRows)
            {
                int rows = ds.Tables[0].Rows.Count;
                if (rows > 0)
                {
                    string[] sc = new string[5];
                    string filename = initialRecs[0].Remove(initialRecs[0].Length - 4, 4);
                    new ErrorLog(rows + " records records to process!!!");
                    filenames = new string[2];
                    int k = 1;

                    string recIds = string.Empty;

                    for (int i = 0; i < rows; i++)
                    {
                        try
                        {
                            new ErrorLog("Processing record " + i);
                            DataRow dr = ds.Tables[0].Rows[i];
                            string encpan = dr[0].ToString();
                            string pan = Program.IBS_Decrypt(encpan);

                            recIds += i == 0 ? dr["ID"].ToString() : "," + dr["ID"].ToString();

                            sc[0] = "ADD";
                            sc[1] = pan.Trim();
                            sc[2] = "";
                            sc[3] = "";
                            sc[4] = "1";
                            string cusnum = dr[1].ToString();
                            string email = dr[3].ToString();
                            string mobile = dr[2].ToString();

                            if (mobile.Length == 11) { mobile = "234" + mobile.Remove(0, 1); }

                            if ((email == "info@gmail.com") || (email == "na@yahoo.com") || (email == "none@gmail.com") || (email == "info@sterlingbankng.com") || (email == "none@yahoo.com") || (email == "none@sterlingbankng.com") || (email == "info@yahoo.com") || (email == "info@gmail.com") || (email == "customer@info.com") || (email == "email@customer.com"))
                            {
                                email = "";
                            }
                            else if ((email == "info@gmail.com".ToUpper()) || (email == "na@yahoo.com".ToUpper()) || (email == "none@gmail.com".ToUpper()) || (email == "info@sterlingbankng.com".ToUpper()) || (email == "none@yahoo.com".ToUpper()) || (email == "none@sterlingbankng.com".ToUpper()) || (email == "info@yahoo.com".ToUpper()) || (email == "info@gmail.com".ToUpper()) || (email == "customer@info.com".ToUpper()) || (email == "email@customer.com".ToUpper()))
                            {
                                email = "";
                            }
                            if ((!string.IsNullOrEmpty(email)) && (!string.IsNullOrEmpty(mobile)))
                            {
                                if (mobile == "N/A")
                                {
                                    mobile = "";
                                }
                                sc[3] = email.Trim();
                                sc[2] = mobile.Trim();
                                Program.GenGenericDelimCsv(pth, filename, sc, ",");
                                new ErrorLog("Processed record " + k + " in batch" + filename + " for: " + encpan);
                            }
                            else if (!string.IsNullOrEmpty(email))
                            {
                                sc[3] = email.Trim();
                                Program.GenGenericDelimCsv(pth, filename, sc, ",");
                                new ErrorLog("Processed record " + k + " in batch" + filename + " for: " + encpan);
                            }
                            else if (!string.IsNullOrEmpty(mobile))
                            {
                                if (mobile == "N/A")
                                {
                                    mobile = "";
                                }
                                sc[2] = mobile.Trim();
                                Program.GenGenericDelimCsv(pth, filename, sc, ","); 
                                new ErrorLog("Processed record " + k + " in batch" + filename + " for: " + encpan);
                            }
                            else
                            {
                                sc[3] = "";
                                sc[2] = "";
                                Program.GenGenericDelimCsv(pth, filename + "ErrorFile", sc, ",");
                            }
                        }
                        catch (Exception ex)
                        {
                            new ErrorLog(ex);
                        }
                    }
                    if (!string.IsNullOrEmpty(initialRecs[2]))
                    {
                        initialRecs[2] += "," + recIds;
                    }
                    else { initialRecs[2] += recIds; }
                }
            }
            return initialRecs;
        }

        public static string[] GetVisaUploadData(DateTime dt, string[] initialRecs)
        {
            DataSet ds = new DataSet();
            string[] filenames = null; int j = 1;
            string product = ConfigurationManager.AppSettings["product"];
            string stat = ConfigurationManager.AppSettings["status"];
            string pth = ConfigurationManager.AppSettings["GenPath"];
            string dte = dt.ToString("yyyy-MM-dd");

            string sql = "SELECT TOP 500 PAN, CUSTOMERNUMBER, CELLPHONE, EMAIL, STATUSFLAG, c.ID FROM CardRequest c INNER JOIN Product p ON c.PRODUCT = p.ID WHERE p.Misc in ('Local Visa Products') " +
            "AND P.IsDeleted = '0' AND STATUSFLAG IN('4', '5', '11') " +
            "AND((DATEADDED > '2021-07-01 00:00:00.000' AND DateDiff(DAY, DATEADDED, DATEAPPROVED) >= 1) or " +
            "(DATEADDED > '2021-07-01 00:00:00.000' AND DateDiff(DAY, DATEADDED, DATEAPPROVED) = 0)) " +
            "AND Securecode is NULL " +
            "AND PAN IS NOT NULL";
            Connect cn = new Connect("CardApp")
            {
                Persist = true
            };
            cn.SetSQL(sql);
            ds = cn.Select();
            cn.CloseAll();

            bool hasRows = ds.Tables.Cast<DataTable>().Any(table => table.Rows.Count != 0);
            if (hasRows)
            {
                int rows = ds.Tables[0].Rows.Count;
                if (rows > 0)
                {
                    string[] sc = new string[7];
                    string filename = initialRecs[0].Remove(initialRecs[0].Length - 4, 4);
                    new ErrorLog(rows + " records records to process!!!");

                    filenames = new string[2];
                    int k = 1;
                    filenames[0] = filename + j + ".csv";
                    string recIds = string.Empty;
                    for (int i = 0; i < rows; i++)
                    {
                        try
                        {
                            new ErrorLog("Processing record " + i);
                            DataRow dr = ds.Tables[0].Rows[i];
                            string encpan = dr[0].ToString();
                            string pan = Program.IBS_Decrypt(encpan);
                            recIds += i == 0 ? dr["ID"].ToString() : "," + dr["ID"].ToString();

                            sc[0] = pan.Trim(); sc[1] = "ADD";
                            sc[2] = "2"; sc[3] = "EmailAddress";
                            sc[4] = "NULL"; sc[5] = "MobilePhone";
                            sc[6] = "NULL";
                            string cusnum = dr[1].ToString();
                            string email = dr[3].ToString();
                            string mobile = dr[2].ToString();

                            if ((email == "info@gmail.com") || (email == "na@yahoo.com") || (email == "none@gmail.com") || (email == "info@sterlingbankng.com") || (email == "none@yahoo.com") || (email == "none@sterlingbankng.com") || (email == "info@yahoo.com") || (email == "info@gmail.com") || (email == "customer@info.com") || (email == "email@customer.com"))
                            {
                                email = "";
                            }
                            else if ((email == "info@gmail.com".ToUpper()) || (email == "na@yahoo.com".ToUpper()) || (email == "none@gmail.com".ToUpper()) || (email == "info@sterlingbankng.com".ToUpper()) || (email == "none@yahoo.com".ToUpper()) || (email == "none@sterlingbankng.com".ToUpper()) || (email == "info@yahoo.com".ToUpper()) || (email == "info@gmail.com".ToUpper()) || (email == "customer@info.com".ToUpper()) || (email == "email@customer.com".ToUpper()))
                            {
                                email = "";
                            }
                            if ((email != "") && (mobile != ""))
                            {
                                sc[4] = email.Trim();
                                sc[6] = mobile.Trim();
                                Program.GenGenericDelimCsv(pth, filename + j, sc, ",");
                                new ErrorLog("Processed record " + k + " in batch" + filename + j);
                            }
                            else if (email != "")
                            {
                                sc[4] = email.Trim();
                                Program.GenGenericDelimCsv(pth, filename + j, sc, ",");
                                new ErrorLog("Processed record " + k + " in batch" + filename + j);
                            }
                            else if (mobile != "")
                            {
                                sc[6] = mobile.Trim();
                                Program.GenGenericDelimCsv(pth, filename + j, sc, ",");
                                new ErrorLog("Processed record " + k + " in batch" + filename + j);
                            }
                            else
                            {
                                sc[4] = "NO EMAIL";
                                sc[6] = "NO MOBILE";
                                Program.GenGenericDelimCsv(pth, filename + "ErrorFile", sc, ",");
                            }
                            filenames[1] = recIds;
                        }
                        catch (Exception ex)
                        {
                            new ErrorLog(ex);
                        }
                    }
                    if (!string.IsNullOrEmpty(initialRecs[2]))
                    {
                        initialRecs[2] += "," + recIds;
                    }
                    else { initialRecs[2] += recIds; }
                }
            }
            return initialRecs;
        }

        public static string[] GetOnlineUploadData(DateTime dt, string[] initialRecs)
        {
            DataSet ds = new DataSet();
            string[] filenames = null; int j = 1;
            string products = ConfigurationManager.AppSettings["OnlineProducts"];
            string stat = ConfigurationManager.AppSettings["status"];
            string pth = ConfigurationManager.AppSettings["GenPath"];
            string dte = dt.ToString("yyyy-MM-dd");

            string _products = string.Empty;
            List<string> productList = products.Split('|').ToList();
            foreach (string item in productList)
            {
                _products += "'" + item + "',";
            }
            _products = _products.Remove(_products.Length - 1, 1);

            string sql = "SELECT TOP 500 PAN, CUSTOMERNUMBER, CELLPHONE, EMAIL, STATUSFLAG, c.ID FROM CardRequest c INNER JOIN Product p ON c.PRODUCT = p.ID WHERE PRODUCT in (" + _products + ") AND P.IsDeleted = '0' AND STATUSFLAG IN ('66','5') AND DATEADDED > '2021-07-01 00:00:00.000' AND Securecode is NULL AND PAN IS NOT NULL";

            Connect cn = new Connect("CardApp")
            {
                Persist = true
            };
            cn.SetSQL(sql);
            ds = cn.Select();
            cn.CloseAll();

            bool hasRows = ds.Tables.Cast<DataTable>().Any(table => table.Rows.Count != 0);
            if (hasRows)
            {
                int rows = ds.Tables[0].Rows.Count;
                if (rows > 0)
                {
                    string[] sc = new string[5];
                    string filename = initialRecs[0].Remove(initialRecs[0].Length - 4, 4);
                    new ErrorLog(rows + " records records to process!!!");
                    filenames = new string[2];
                    int k = 1;

                    string recIds = string.Empty;

                    for (int i = 0; i < rows; i++)
                    {
                        try
                        {
                            new ErrorLog("Processing record " + i);
                            DataRow dr = ds.Tables[0].Rows[i];
                            string encpan = dr[0].ToString();
                            string pan = Program.IBS_Decrypt(encpan);
                            recIds += i == 0 ? dr["ID"].ToString() : "," + dr["ID"].ToString();


                            sc[0] = "ADD";
                            sc[1] = pan.Trim();
                            sc[2] = "";
                            sc[3] = "";
                            sc[4] = "1";
                            string cusnum = dr[1].ToString();
                            string email = dr[3].ToString();
                            string mobile = dr[2].ToString();

                            if (mobile.Length == 11) { mobile = "234" + mobile.Remove(0, 1); }

                            if ((email == "info@gmail.com") || (email == "na@yahoo.com") || (email == "none@gmail.com") || (email == "info@sterlingbankng.com") || (email == "none@yahoo.com") || (email == "none@sterlingbankng.com") || (email == "info@yahoo.com") || (email == "info@gmail.com") || (email == "customer@info.com") || (email == "email@customer.com"))
                            {
                                email = "";
                            }
                            else if ((email == "info@gmail.com".ToUpper()) || (email == "na@yahoo.com".ToUpper()) || (email == "none@gmail.com".ToUpper()) || (email == "info@sterlingbankng.com".ToUpper()) || (email == "none@yahoo.com".ToUpper()) || (email == "none@sterlingbankng.com".ToUpper()) || (email == "info@yahoo.com".ToUpper()) || (email == "info@gmail.com".ToUpper()) || (email == "customer@info.com".ToUpper()) || (email == "email@customer.com".ToUpper()))
                            {
                                email = "";
                            }
                            if ((!string.IsNullOrEmpty(email)) && (!string.IsNullOrEmpty(mobile)))
                            {
                                if (mobile == "N/A")
                                {
                                    mobile = "";
                                }
                                sc[3] = email.Trim();
                                sc[2] = mobile.Trim();
                                Program.GenGenericDelimCsv(pth, filename, sc, ",");
                                new ErrorLog("Processed record " + k + " in batch" + filename + " for: " + encpan);
                            }
                            else if (!string.IsNullOrEmpty(email))
                            {
                                sc[3] = email.Trim();
                                Program.GenGenericDelimCsv(pth, filename, sc, ",");
                                new ErrorLog("Processed record " + k + " in batch" + filename + " for: " + encpan);
                            }
                            else if (!string.IsNullOrEmpty(mobile))
                            {
                                if (mobile == "N/A")
                                {
                                    mobile = "";
                                }
                                sc[2] = mobile.Trim();
                                Program.GenGenericDelimCsv(pth, filename, sc, ",");
                                new ErrorLog("Processed record " + k + " in batch" + filename + " for: " + encpan);
                            }
                            else
                            {
                                sc[3] = "";
                                sc[2] = "";
                                Program.GenGenericDelimCsv(pth, filename + "ErrorFile", sc, ",");
                            }
                        }
                        catch (Exception ex)
                        {
                            new ErrorLog(ex);
                        }
                    }
                    if (!string.IsNullOrEmpty(initialRecs[2]))
                    {
                        initialRecs[2] += "," + recIds;
                    }
                    else { initialRecs[2] += recIds; }
                }
            }
            return initialRecs;
        }
    }
}
