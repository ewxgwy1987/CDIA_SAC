#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       Persistor.cs
// Revision:      1.0 -   04 Jun 2009, By Xu Jian
// =====================================================================================
//
#endregion

using System;
using System.Data;
using System.Data.SqlClient;

namespace BHS.Engine.TCPClientChains.DataPersistor.Database
{
    /// <summary>
    /// The single contact with Database
    /// </summary>
    public class Persistor : IDisposable
    {

        #region Class Fields and Properties Declaration

        // The name of current class 
        private static readonly string _className =
                    System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString();
        // Create a logger for use in this class
        private static readonly log4net.ILog _logger =
                    log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        /// <summary>
        /// ID of class object
        /// </summary>
        public string ObjectID { get; set; }
        
        /// <summary>
        /// Property, object of PersistorParameters class.
        /// </summary>
        public Engine.TCPClientChains.DataPersistor.Database.PersistorParameters ClassParameters { get; set; }

        // Indicate whether the database has been ready when SortEngine is starting.
        // When SortEngine is starting, DBConnector class will check the database by means of 
        // open one DB connection to it. It connection is able to be opened, it represents
        // that DB is ready. And this indicater (m_IsDBReady) will be change to True as well.
        private bool _isDBReady;

        #endregion

        #region Class Constructor, Dispose, & Destructor

        /// <summary>
        /// Class constructer.
        /// </summary>
        public Persistor(PALS.Common.IParameters param)
        {
            if (param == null)
                throw new Exception("Constractor parameter can not be null! Creating class object failed! " + 
                    "<BHS.Engine.DataPersistor.Database.PersistorParameters.Constructor()>");

            ClassParameters = (Engine.TCPClientChains.DataPersistor.Database.PersistorParameters)param;

            // Call Init() method to perform class initialization tasks.
            if ( !Init() )
                throw new Exception("Instantiate class object failure! " +
                    "<BHS.Engine.DataPersistor.Database.PersistorParameters.Constructor()>");
        }

        /// <summary>
        /// Class destructer.
        /// </summary>
        ~Persistor()
        {
            Dispose(false);
        }

        /// <summary>
        /// Class method to be called by class wrapper for release resources explicitly.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        // Dispose(bool disposing) executes in two distinct scenarios. If disposing equals true, 
        // the method has been called directly or indirectly by a user's code. Managed and 
        // unmanaged resources can be disposed.
        // If disposing equals false, the method has been called by the runtime from inside the 
        // finalizer and you should not reference other objects. Only unmanaged resources can be disposed.
        private void Dispose(bool disposing)
        {
            // Release managed & unmanaged resources...
            if (disposing)
            {
                if (_logger.IsInfoEnabled)
                    _logger.Info("Class:[" + _className + "] object is being destroyed... <" + _className + ".Dispose()>");
            }

            // Terminate message handling thread.
            if (ClassParameters != null)
            {
                ClassParameters.Dispose();
                ClassParameters = null;
            }

            if (disposing)
            {
                if (_logger.IsInfoEnabled)
                    _logger.Info("Class:[" + _className + "] object has been destroyed. <" + _className + ".Dispose()>");
            }
        }

        #endregion

        #region Class Method Declaration.

        private bool Init()
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

            try
            {
                if (_logger.IsInfoEnabled)
                    _logger.Info("Class:[" + _className + "] object is initializing... <" + thisMethod + ">");

                ObjectID = string.Empty;

                // Check the database connection before further processing
                if (_logger.IsInfoEnabled)
                {
                    _logger.Info("Database connection checking... <" + thisMethod + ">");
                    _logger.Info("Database ConnectionString = (" + ClassParameters.DBConnectionString + ") <" + thisMethod + ">");
                }

                // Perform database initialization tasks:
                // 1. Check database readiness by open DB connection to it. The opened DB connection will be closed immediately after it is opened;
                // 2. Load system parameter settings from database table into global variables.
                _isDBReady = DatabaseInitializing();
                if (!_isDBReady) 
                {
                    if (_logger.IsWarnEnabled)
                        _logger.Warn("Database is not ready for operation!!! <" + thisMethod + ">");
                }
                
                if (_logger.IsInfoEnabled)
                    _logger.Info("Class:[" + _className + "] object has been initialized. <" + thisMethod + ">");

                return true;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Initializing class setting is failed! <" + thisMethod + ">", ex);

                return false;
            }
        }

        /// <summary>
        /// Perform database initialization tasks:
        /// 1. Check database readiness by open DB connection to it. The opened DB connection will be closed immediately after it is opened;
        /// 2. Load system parameter settings from database table into global variables.
        /// </summary>
        /// <returns></returns>
        private bool DatabaseInitializing()
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;

            try
            {
                //====================================================
                // 1. Check database readiness by open DB connection to it. The opened DB connection 
                // will be closed immediately after it is opened;
                sqlConn = new SqlConnection(ClassParameters.DBConnectionString);
                sqlConn.Open();

                if (_logger.IsInfoEnabled)
                    _logger.Info("Database connection checking is successed. <" + thisMethod + ">");
                //====================================================

                //====================================================
                // 2. Load system parameter settings from database table into global variables.
                // ...
                //        'Retrieve Routing Table from DB for future shortest path calculation.
                //        m_RoutingTableHashTable = New Hashtable
                //        m_RoutingTableSyncdHash = Hashtable.Synchronized(m_RoutingTableHashTable)
                //        If Not GetRoutingTableFromDB(SqlConn) Then
                //            Return False
                //        End If
                //====================================================

                return true;
            }
            catch (SqlException ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Database connection checking is failed! " +
                                    "Please check DB ConnectionString setting, or DB server status. <" + thisMethod + ">", ex);

                return false;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Initializing database is failed! <" + thisMethod + ">", ex);

                return false;
            }
            finally
            {
                if (sqlConn != null)
                    sqlConn.Close();
            }
        }

        /// <summary>
        /// Log data for AFAI:
        /// </summary>
        /// <returns></returns>
        public void SentAFAILogging(string sender, string receiver, string airportCode,
                        string conflictsDest, string noAllocDest, string noCarrierdest, string noRead)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            SqlCommand sqlCmd = null;

            try
            {
                sqlConn = new SqlConnection(ClassParameters.DBConnectionString);
                sqlCmd = new SqlCommand(ClassParameters.STP_SAC_AIRPORTCODEFUNCALLOCINFORMATION, sqlConn);
                sqlCmd.CommandType = CommandType.StoredProcedure;

                sqlCmd.Parameters.Add("@AirportCode", SqlDbType.VarChar, 4);
                sqlCmd.Parameters["@AirportCode"].Value = airportCode;

                sqlCmd.Parameters.Add("@NoCarrierDest", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@NoCarrierDest"].Value = conflictsDest;

                sqlCmd.Parameters.Add("@NoAllocDest", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@NoAllocDest"].Value = noAllocDest;

                sqlCmd.Parameters.Add("@DumpDest", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@DumpDest"].Value = noCarrierdest;

                sqlCmd.Parameters.Add("@NoReadDest", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@NoReadDest"].Value = noRead;

                sqlConn.Open();
                sqlCmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Logging sent AFAI data failure! <" + thisMethod + ">", ex);

                return;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Logging sent AFAI data failure! <" + thisMethod + ">", ex);

                return;
            }
            finally
            {
                if (sqlConn != null) sqlConn.Close();
            }
        }

        /// <summary>
        /// Log data for CRAI:
        /// </summary>
        /// <returns></returns>
        public void SentCRAILogging(string sender, string receiver, int noOfCarrier, CRAIList[] data)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            SqlCommand sqlCmd = null;
            string noOfTag;

            try
            {
                sqlConn = new SqlConnection(ClassParameters.DBConnectionString);
                sqlCmd = new SqlCommand(ClassParameters.STP_SAC_CARRIERALLOCINFORMATION, sqlConn);
                sqlCmd.CommandType = CommandType.StoredProcedure;

                if (noOfCarrier < 10)
                {
                    noOfTag = "0" + noOfCarrier.ToString();
                }
                else
                {
                    noOfTag = noOfCarrier.ToString();
                }

                sqlCmd.Parameters.Add("@NoOfCarrier", SqlDbType.VarChar, 2);
                sqlCmd.Parameters["@NoOfCarrier"].Value = noOfTag;

                string carrierCode, sortDevice;

                for (int i = 0; i < 10; i++)
                {
                    if (i >= data.Length)
                    {
                        carrierCode = string.Empty;
                        sortDevice = string.Empty;
                    }
                    else
                    {
                        carrierCode = data[i].CarrierCode.ToString();
                        sortDevice = data[i].SortDeviceDestination.ToString();
                    }

                    sqlCmd.Parameters.Add("@CarrierCode" + (i + 1).ToString(), SqlDbType.VarChar, 3);
                    sqlCmd.Parameters["@CarrierCode" + (i + 1).ToString()].Value = carrierCode;

                    sqlCmd.Parameters.Add("@SortDevice" + (i + 1).ToString(), SqlDbType.VarChar, 10);
                    sqlCmd.Parameters["@SortDevice" + (i + 1).ToString()].Value = sortDevice;
                }

                sqlConn.Open();
                sqlCmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Logging sent CRAI data failure! <" + thisMethod + ">", ex);

                return;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Logging sent CRAI data failure! <" + thisMethod + ">", ex);

                return;
            }
            finally
            {
                if (sqlConn != null) sqlConn.Close();
            }
        }

        /// <summary>
        /// Log data for FBTI:
        /// </summary>
        /// <returns></returns>
        public void SentFBTILogging(string sender, string receiver, int noOfTag, Tag[] data)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            SqlCommand sqlCmd = null;
            string stringNoTag;
            try
            {
                sqlConn = new SqlConnection(ClassParameters.DBConnectionString);
                sqlCmd = new SqlCommand(ClassParameters.STP_SAC_FALLBACKTAGINFORMATION, sqlConn);
                sqlCmd.CommandType = CommandType.StoredProcedure;

                if (noOfTag < 10)
                {
                    stringNoTag = "0" + noOfTag.ToString();
                }
                else
                {
                    stringNoTag = noOfTag.ToString();
                }

                sqlCmd.Parameters.Add("@NoOfTag", SqlDbType.VarChar, 2);
                sqlCmd.Parameters["@NoOfTag"].Value = stringNoTag;

                string code, @dest;

                for (int i = 0; i < 10; i++)
                {
                    if (i >= data.Length)
                    {
                        code = string.Empty;
                        dest = string.Empty;
                    }
                    else
                    {
                        code = data[i].Code.ToString();
                        dest = data[i].Destination.ToString();
                    }

                    sqlCmd.Parameters.Add("@Code" + (i + 1).ToString(), SqlDbType.VarChar, 2);
                    sqlCmd.Parameters["@Code" + (i + 1).ToString()].Value = code;

                    sqlCmd.Parameters.Add("@Destination" + (i + 1).ToString(), SqlDbType.VarChar, 10);
                    sqlCmd.Parameters["@Destination" + (i + 1).ToString()].Value = dest;
                }

                sqlConn.Open();
                sqlCmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Logging sent FBTI data failure! <" + thisMethod + ">", ex);

                return;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Logging sent FBTI data failure! <" + thisMethod + ">", ex);

                return;
            }
            finally
            {
                if (sqlConn != null) sqlConn.Close();
            }
        }

        /// <summary>
        /// Log data for FPTI:
        /// </summary>
        /// <returns></returns>
        public void SentFPTILogging(string sender, string receiver, int noOfTag, Tag[] data)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            SqlCommand sqlCmd = null;
            string stringNoTag;

            try
            {
                sqlConn = new SqlConnection(ClassParameters.DBConnectionString);
                sqlCmd = new SqlCommand(ClassParameters.STP_SAC_FOURPIERTAGINFORMATION, sqlConn);
                sqlCmd.CommandType = CommandType.StoredProcedure;
                
                if (noOfTag < 10)
                {
                    stringNoTag = "0" + noOfTag.ToString();
                }
                else
                {
                    stringNoTag = noOfTag.ToString();
                }

                sqlCmd.Parameters.Add("@NoOfTag", SqlDbType.VarChar, 2);
                sqlCmd.Parameters["@NoOfTag"].Value = stringNoTag;

                string code, @dest;

                for (int i = 0; i < 10; i++)
                {
                    if (i >= data.Length)
                    {
                        code = string.Empty;
                        dest = string.Empty;
                    }
                    else
                    {
                        code = data[i].Code.ToString();
                        dest = data[i].Destination.ToString();
                    }

                    sqlCmd.Parameters.Add("@Code" + (i + 1).ToString(), SqlDbType.VarChar, 4);
                    sqlCmd.Parameters["@Code" + (i + 1).ToString()].Value = code.PadLeft(4, '0');

                    sqlCmd.Parameters.Add("@Destination" + (i + 1).ToString(), SqlDbType.VarChar, 10);
                    sqlCmd.Parameters["@Destination" + (i + 1).ToString()].Value = dest;
                }

                sqlConn.Open();
                sqlCmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Logging sent FPTI data failure! <" + thisMethod + ">", ex);

                return;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Logging sent FPTI data failure! <" + thisMethod + ">", ex);

                return;
            }
            finally
            {
                if (sqlConn != null) sqlConn.Close();
            }
        }

        /// <summary>
        /// Log data for TPTI:
        /// </summary>
        /// <returns></returns>
        //public void SentTPTILogging(string sender, string receiver, int noOfTag, Tag[] data)
        //{
        //    string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
        //    SqlConnection sqlConn = null;
        //    SqlCommand sqlCmd = null;
        //    string stringNoTag;

        //    try
        //    {
        //        sqlConn = new SqlConnection(ClassParameters.DBConnectionString);
        //        sqlCmd = new SqlCommand(ClassParameters.STP_SAC_TWOPIERTAGINFORMATION, sqlConn);
        //        sqlCmd.CommandType = CommandType.StoredProcedure;

        //        //sqlCmd.Parameters.Add("@Sender", SqlDbType.VarChar, 8);
        //        //sqlCmd.Parameters["@Sender"].Value = sender;

        //        //sqlCmd.Parameters.Add("@Receiver", SqlDbType.VarChar, 8);
        //        //sqlCmd.Parameters["@Receiver"].Value = receiver;

        //        if (noOfTag < 10)
        //        {
        //            stringNoTag = "0" + noOfTag.ToString();
        //        }
        //        else
        //        {
        //            stringNoTag = noOfTag.ToString();
        //        }

        //        sqlCmd.Parameters.Add("@NoOfTag", SqlDbType.VarChar, 2);
        //        sqlCmd.Parameters["@NoOfTag"].Value = stringNoTag;

        //        string code, @dest;

        //        for (int i = 0; i < 10; i++)
        //        {
        //            if (i >= data.Length)
        //            {
        //                code = string.Empty;
        //                dest = string.Empty;
        //            }
        //            else
        //            {
        //                code = data[i].Code;
        //                dest = data[i].Destination;
        //            }

        //            sqlCmd.Parameters.Add("@Code" + (i + 1).ToString(), SqlDbType.VarChar, 4);
        //            sqlCmd.Parameters["@Code" + (i + 1).ToString()].Value = code;

        //            sqlCmd.Parameters.Add("@Destination" + (i + 1).ToString(), SqlDbType.VarChar, 10);
        //            sqlCmd.Parameters["@Destination" + (i + 1).ToString()].Value = dest;
        //        }

        //        sqlConn.Open();
        //        sqlCmd.ExecuteNonQuery();
        //    }
        //    catch (SqlException ex)
        //    {
        //        if (_logger.IsErrorEnabled)
        //            _logger.Error("Logging sent TPTI data failure! <" + thisMethod + ">", ex);

        //        return;
        //    }
        //    catch (Exception ex)
        //    {
        //        if (_logger.IsErrorEnabled)
        //            _logger.Error("Logging sent TPTI data failure! <" + thisMethod + ">", ex);

        //        return;
        //    }
        //    finally
        //    {
        //        if (sqlConn != null) sqlConn.Close();
        //    }
        //}

        /// <summary>
        /// DLPS Tag Duplication Check
        /// </summary>
        /// <param name="gid"></param>
        /// <param name="licensePlate"></param>
        /// <param name="maxRecords"></param>
        /// <param name="duplicateCount"></param>
        public void DLPSTagDuplicationCheck(string gid, string licensePlate, int maxRecords, out int duplicateCount)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            SqlCommand sqlCmd = null;
            duplicateCount = 0;

            try
            {
                sqlConn = new SqlConnection(ClassParameters.DBConnectionString);
                sqlCmd = new SqlCommand(ClassParameters.STP_SAC_TAGDUPLICATIONCHECK, sqlConn);
                sqlCmd.CommandType = CommandType.StoredProcedure;

                sqlCmd.Parameters.Add("@GID", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@GID"].Value = gid;

                sqlCmd.Parameters.Add("@LicensePlate", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@LicensePlate"].Value = licensePlate;
                
                sqlCmd.Parameters.Add("@MaxRecord", SqlDbType.Int);
                sqlCmd.Parameters["@MaxRecord"].Value = maxRecords;
                
                sqlCmd.Parameters.Add("@DuplicateCount", SqlDbType.Int);
                sqlCmd.Parameters["@DuplicateCount"].Direction = ParameterDirection.Output;

                sqlConn.Open();
                sqlCmd.ExecuteNonQuery();

                if (sqlCmd.Parameters["@DuplicateCount"].Value != DBNull.Value)
                {
                    duplicateCount = Convert.ToInt32(sqlCmd.Parameters["@DuplicateCount"].Value.ToString());
                }

            }
            catch (SqlException ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("DLPS Tag Duplication Check failure! <" + thisMethod + ">", ex);

                return;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("DLPS Tag Duplication Check failure! <" + thisMethod + ">", ex);

                return;
            }
            finally
            {
                if (sqlConn != null) sqlConn.Close();
            }
        }

        /// <summary>
        /// Sent DLPS Logging
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="receiver"></param>
        /// <param name="gid"></param>
        /// <param name="licensePlate"></param>
        /// <param name="status"></param>
        public void SentDLPSLogging(string sender, string receiver, string gid, string licensePlate, string status)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            SqlCommand sqlCmd = null;

            try
            {
                sqlConn = new SqlConnection(ClassParameters.DBConnectionString);
                sqlCmd = new SqlCommand(ClassParameters.STP_SAC_DUPLICATELICENSEPLATESTATUS, sqlConn);
                sqlCmd.CommandType = CommandType.StoredProcedure;

                sqlCmd.Parameters.Add("@GID", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@GID"].Value = gid;

                sqlCmd.Parameters.Add("@LicensePlate", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@LicensePlate"].Value = licensePlate;

                sqlCmd.Parameters.Add("@Status", SqlDbType.VarChar, 2);
                sqlCmd.Parameters["@Status"].Value = status;

                sqlConn.Open();
                sqlCmd.ExecuteNonQuery();

            }
            catch (SqlException ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Sent DLPS Logging failure! <" + thisMethod + ">", ex);

                return;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Sent DLPS Logging failure! <" + thisMethod + ">", ex);

                return;
            }
            finally
            {
                if (sqlConn != null) sqlConn.Close();
            }
        }

        /// <summary>
        /// Collect Airport Location Code
        /// </summary>
        public void CollectAiportLocationCode()
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            string strNoCarrierDest = string.Empty;
            string strNoAllocationDest = string.Empty;
            string strDumpDischargeDest = string.Empty;
            string strNoReadDest = string.Empty;

            SqlConnection sqlConn = null;
            sqlConn = new SqlConnection(ClassParameters.DBConnectionString);
            SqlCommand sqlCmd = new SqlCommand(ClassParameters.STP_SAC_COLLECTENTIREAFAI, sqlConn);
            sqlCmd.CommandType = CommandType.StoredProcedure;
            try
            {
                sqlCmd.Parameters.Add("@AirportDesc", SqlDbType.VarChar, 30);
                sqlCmd.Parameters["@AirportDesc"].Value = ClassParameters.AIRPORT_CODE_DESC;

                sqlCmd.Parameters.Add("@NoCarrierDesc", SqlDbType.VarChar, 4);
                sqlCmd.Parameters["@NoCarrierDesc"].Value = ClassParameters.FUNC_ALLOCATION_NOCR;

                sqlCmd.Parameters.Add("@NoAllocDesc", SqlDbType.VarChar, 4);
                sqlCmd.Parameters["@NoAllocDesc"].Value = ClassParameters.FUNC_ALLOCATION_NOAL;

                sqlCmd.Parameters.Add("@DumpDischargeDesc", SqlDbType.VarChar, 4);
                sqlCmd.Parameters["@DumpDischargeDesc"].Value = ClassParameters.FUNC_ALLOCATION_DUMP;

                sqlCmd.Parameters.Add("@NoReadDesc", SqlDbType.VarChar, 4);
                sqlCmd.Parameters["@NoReadDesc"].Value = ClassParameters.FUNC_ALLOCATION_NORD;

                sqlCmd.Parameters.Add("@Airport", SqlDbType.VarChar, 4);
                sqlCmd.Parameters["@Airport"].Direction = ParameterDirection.Output;

                sqlCmd.Parameters.Add("@NoCarrier", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@NoCarrier"].Direction = ParameterDirection.Output;

                sqlCmd.Parameters.Add("@NoAlloc", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@NoAlloc"].Direction = ParameterDirection.Output;

                sqlCmd.Parameters.Add("@DumpDischarge", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@DumpDischarge"].Direction = ParameterDirection.Output;

                sqlCmd.Parameters.Add("@NoRead", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@NoRead"].Direction = ParameterDirection.Output;

                sqlCmd.Parameters.Add("@DumpDischargeDest", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@DumpDischargeDest"].Direction = ParameterDirection.Output;

                sqlCmd.Parameters.Add("@NoAllocDest", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@NoAllocDest"].Direction = ParameterDirection.Output;

                sqlCmd.Parameters.Add("@NoCarrierDest", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@NoCarrierDest"].Direction = ParameterDirection.Output;

                sqlCmd.Parameters.Add("@NoReadDest", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@NoReadDest"].Direction = ParameterDirection.Output;

                sqlConn.Open();
                sqlCmd.ExecuteNonQuery();

                if (!Convert.IsDBNull(sqlCmd.Parameters["@Airport"].Value))
                {
                    
                    Common.AirportLocationCode = sqlCmd.Parameters["@Airport"].Value.ToString();
                }
                else
                {
                    Common.AirportLocationCode = string.Empty;
                }
            }
            catch (SqlException ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Collecting of Airport Location Code SQL failure! <" + thisMethod + ">", ex);

                return;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Collecting of Airport Location Code failure! <" + thisMethod + ">", ex);

                return;
            }
            finally
            {
                if (sqlConn != null) sqlConn.Close();
            }
        }

        #endregion

    }
}
