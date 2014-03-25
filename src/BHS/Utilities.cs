#region Release Information
//
// =====================================================================================
// Copyright 2013, SC Leong, All Rights Reserved.
// =====================================================================================
// FileName      Utilities.cs
// Revision:      1.0 -   23 September 2013 
// =====================================================================================
// 
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PALS.Utilities;
using System.Data.SqlClient;
using System.Data;

namespace BHS
{
    public class Utilities
    {
        // The name of current class 
        private static readonly string _className =
                    System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString();
        // Create a logger for use in this class
        private static readonly log4net.ILog _logger =
                    log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Convert Hex Values into Byte Array
        /// </summary>
        /// <param name="hexVal"></param>
        /// <param name="fieldLen"></param>
        /// <param name="reverse"></param>
        /// <returns></returns>
        public static byte[] ToByteArray(String hexVal, int fieldLen, bool reverse)
        {
            hexVal = hexVal.PadLeft(fieldLen * 2, '0');

            int NumberChars = hexVal.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hexVal.Substring(i, 2), 16);
            }
            return bytes;
        }

        /// <summary>
        /// Convert Hex into Decimal 
        /// </summary>
        /// <param name="fieldActualValue"></param>
        /// <param name="strIntType"></param>
        /// <returns></returns>
        public static string ConvertVal2Decimal(byte[] fieldActualValue, string strIntType)
        {
            string strRetVal = string.Empty ;
            string strOrgVal = string.Empty;

            strOrgVal = Functions.ConvertByteArrayToString(fieldActualValue, -1, HexToStrMode.ToPaddedHexString).Trim().Replace(" ", string.Empty);

            if (strIntType == "16")
            {
                Int16 intVal = Int16.Parse(strOrgVal, System.Globalization.NumberStyles.HexNumber);
                strRetVal = intVal.ToString();
            }
            else if (strIntType == "32")
            {
                Int32 intVal = Int32.Parse(strOrgVal, System.Globalization.NumberStyles.HexNumber);
                strRetVal = intVal.ToString();
            }
            else if (strIntType == "64")
            {
                Int64 intVal = Int64.Parse(strOrgVal, System.Globalization.NumberStyles.HexNumber);
                strRetVal = intVal.ToString();
            }

            return strRetVal;
        }

        /// <summary>
        /// Allocation property : Early, Too Early, Open, Late, Too Late
        /// </summary>
        /// <param name="strLicensePlate"></param>
        /// <param name="DBPersistorConnStr"></param>
        /// <param name="DBPersistor_STP"></param>
        /// <returns></returns>
        public static string AllocationProperty(string strLicensePlate, string strCarrier, string strFlightNo, string strSDO,string DBPersistorConnStr, string DBPersistor_STP)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            SqlCommand sqlCmd = null;

            System.TimeSpan timeSpan_allocOpenOffset, timeSpan_allocCloseOffset, timeSpan_allocEarlyOpenOffset, timeSpan_allocRushDuration;
            string alloc_open_offset, alloc_open_related, alloc_close_offset, alloc_close_related, alloc_early_open_offset, alloc_rush_duration;
            string sdo, edo, ido, sto, eto, ito;
            DateTime std, etd, itd, s_do, e_do, i_do;

            alloc_open_offset = string.Empty;
            alloc_open_related = string.Empty;
            alloc_close_offset = string.Empty;
            alloc_close_related = string.Empty;
            alloc_early_open_offset = string.Empty;
            alloc_rush_duration = string.Empty;
            sdo = string.Empty;
            edo = string.Empty;
            ido = string.Empty;
            sto = string.Empty;
            eto = string.Empty;
            ito = string.Empty;

            string Allocation_Property = "UNKNOWN";
            try
            {
                alloc_open_related = "STD";

                sqlConn = new SqlConnection(DBPersistorConnStr);
                sqlCmd = new SqlCommand(DBPersistor_STP, sqlConn);
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.Parameters.AddWithValue("@LICENSE_PLATE", strLicensePlate);
                sqlCmd.Parameters.AddWithValue("@CARRIER", strCarrier);
                sqlCmd.Parameters.AddWithValue("@FLIGHT_NO", strFlightNo);
                sqlCmd.Parameters.AddWithValue("@S_DO", strSDO);

                sqlConn.Open();
                SqlDataReader sqlReader = sqlCmd.ExecuteReader();
                while (sqlReader.Read())
                {
                    alloc_open_offset = sqlReader["ALLOC_OPEN_OFFSET"].ToString();
                    alloc_open_related = sqlReader["ALLOC_OPEN_RELATED"].ToString();
                    alloc_close_offset = sqlReader["ALLOC_CLOSE_OFFSET"].ToString();
                    alloc_close_related = sqlReader["ALLOC_CLOSE_RELATED"].ToString();
                    alloc_early_open_offset = sqlReader["ALLOC_EARLY_OPEN_OFFSET"].ToString();
                    alloc_rush_duration = sqlReader["ALLOC_RUSH_DURATION"].ToString();

                    sdo = sqlReader["SDO"].ToString();
                    edo = sqlReader["EDO"].ToString();
                    ido = sqlReader["IDO"].ToString();

                    sto = sqlReader["STO"].ToString();
                    eto = sqlReader["ETO"].ToString();
                    ito = sqlReader["ITO"].ToString();

                    s_do = Convert.ToDateTime(sdo == string.Empty ? null : sdo);
                    e_do = Convert.ToDateTime(edo == string.Empty ? null : edo);
                    i_do = Convert.ToDateTime(ido == string.Empty ? null : ido);

                    // Need to imporve on getting the correct Date Time based on current Regional & Language setting
                    std = sdo == string.Empty ? Convert.ToDateTime(null) : Convert.ToDateTime(s_do.Year.ToString() + "-" + s_do.Month.ToString() + "-" + s_do.Day.ToString() + " " + sto.Substring(0, 2) + ":" + sto.Substring(2, 2));
                    etd = edo == string.Empty ? Convert.ToDateTime(null) : Convert.ToDateTime(e_do.Year.ToString() + "-" + e_do.Month.ToString() + "-" + e_do.Day.ToString() + " " + eto.Substring(0, 2) + ":" + eto.Substring(2, 2));
                    itd = ido == string.Empty ? Convert.ToDateTime(null) : Convert.ToDateTime(i_do.Year.ToString() + "-" + i_do.Month.ToString() + "-" + i_do.Day.ToString() + " " + ito.Substring(0, 2) + ":" + ito.Substring(2, 2));

                    timeSpan_allocOpenOffset = timeSpan(alloc_open_offset);
                    timeSpan_allocCloseOffset = timeSpan(alloc_close_offset);
                    timeSpan_allocEarlyOpenOffset = timeSpan(alloc_early_open_offset);
                    timeSpan_allocRushDuration = timeSpan(alloc_rush_duration);

                    DateTime alloc_open = std;
                    switch (alloc_open_related)
                    {
                        case "STD":
                            alloc_open = std.Add(timeSpan_allocOpenOffset);
                            break;
                        case "ETD":
                            alloc_open = etd.Add(timeSpan_allocOpenOffset);
                            break;
                        case "ITD":
                            alloc_open = itd.Add(timeSpan_allocOpenOffset);
                            break;
                    }

                    DateTime alloc_close = std;
                    switch (alloc_close_related)
                    {
                        case "STD":
                            alloc_close = std.Add(timeSpan_allocCloseOffset);
                            break;
                        case "ETD":
                            alloc_close = etd.Add(timeSpan_allocCloseOffset);
                            break;
                        case "ITD":
                            alloc_close = itd.Add(timeSpan_allocCloseOffset);
                            break;
                    }

                    DateTime alloc_early_open = alloc_open.Add(timeSpan_allocEarlyOpenOffset);
                    DateTime alloc_rush = alloc_close.Add(timeSpan_allocRushDuration);

                    if (DateTime.Now < alloc_early_open)
                    {
                        // Too early allocation 
                        Allocation_Property = "2EARLY";
                    }
                    else if (alloc_early_open < DateTime.Now && DateTime.Now < alloc_open)
                    {
                        // Early allocation
                        Allocation_Property = "EARLY";
                    }
                    else if (alloc_open < DateTime.Now && DateTime.Now < alloc_close)
                    {
                        // Open allocation
                        Allocation_Property = "OPEN";
                    }
                    else if (alloc_close < DateTime.Now && DateTime.Now < alloc_rush)
                    {
                        // Rush allocation 
                        Allocation_Property = "RUSH";
                    }
                    else if (alloc_rush < DateTime.Now)
                    {
                        // Too late allocation
                        Allocation_Property = "2LATE";
                    }
                }

                if (_logger.IsInfoEnabled)
                    _logger.Info("Allocation property for License Plate " + strLicensePlate + " is "+  Allocation_Property +" <" + thisMethod + ">");
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Checking of Allocation property failure !<" + thisMethod + ">", ex); 
            }
            finally
            {
                if (sqlConn != null) sqlConn.Close();
            }

            return Allocation_Property;

        }

        /// <summary>
        /// Convert string into Time Span
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static TimeSpan timeSpan(string offset)
        {
            TimeSpan timeSpan;

            if (offset == string.Empty || offset.ToUpper() == "NULL")
            {
                timeSpan = new System.TimeSpan(0, 0, 0);
            }
            else
            {
                if (offset.Contains("-"))
                {
                    timeSpan = new System.TimeSpan(-int.Parse(offset.Substring(1, 2).ToString()), -int.Parse(offset.Substring(3, 2).ToString()), 0);
                }
                else
                {
                    timeSpan = new System.TimeSpan(int.Parse(offset.Substring(0, 2).ToString()), int.Parse(offset.Substring(2, 2).ToString()), 0);
                }
            }

            return timeSpan;
        }

    }

}
