using System;
using System.Collections;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace HellionCat.DataLink
{
    /// <summary>
    /// Handle the logic to send and received data from the google scripts app
    /// </summary>
    public static class Bridge
    {
        /// <summary>
        /// Create a new spreadsheet on your google drive using the project name.
        /// </summary>
        /// <param name="p_url">The url of the google scripts app</param>
        /// <param name="p_callback">The callback method called once the request is done</param>
        /// <param name="p_timeout">The time in second to wait for a response from the google script before cancelling</param>
        public static void CreateSpreadsheet(string p_url, Action<SpreadsheetInfo> p_callback, int p_timeout = 10)
        {
            Utility.StartBackgroundTask(DoCreateSpreadsheet(p_url.Trim(), p_callback, p_timeout));
        }

        /// <summary>
        /// Change the data in the given sheet
        /// </summary>
        /// <param name="p_url">The url of the google scripts app</param>
        /// <param name="p_callback">The callback method called once the request is done</param>
        /// <param name="p_data">The data of the scriptable object</param>
        /// <param name="p_sheetName">The name of the sheet</param>
        /// <param name="p_timeout">The time in second to wait for a response from the google script before cancelling</param>
        public static void SetSpreadsheetData(string p_url, Action<SendDataInfo> p_callback, string p_data, string p_sheetName, int p_timeout = 20)
        {
            //Will always return success = true, as it create the spreadsheet and sheet if needed
            Utility.StartBackgroundTask(DoSendData(p_url.Trim(), p_callback, p_data, p_sheetName, p_timeout));
        }

        /// <summary>
        /// Change the data in the given sheet
        /// </summary>
        /// <param name="p_url">The url of the google scripts app</param>
        /// <param name="p_callback">The callback method called once the request is done</param>
        /// <param name="p_data">The data of the scriptable object</param>
        /// <param name="p_timeout">The time in second to wait for a response from the google script before cancelling</param>
        public static void SetAllSpreadsheetData(string p_url, Action<SendDataInfo> p_callback, string p_data, int p_timeout = 60)
        {
            //Will always return success = true, as it create the spreadsheet and sheet if needed
            Utility.StartBackgroundTask(DoSendAllData(p_url.Trim(), p_callback, p_data, p_timeout));
        }

        /// <summary>
        /// Get the data in the given sheet
        /// </summary>
        /// <param name="p_url">The url of the google scripts app</param>
        /// <param name="p_callback">The callback method called once the request is done</param>
        /// <param name="p_sheetName">The name of the sheet</param>
        /// <param name="p_timeout">The time in second to wait for a response from the google script before cancelling</param>
        public static void GetSpreadsheetData(string p_url, Action<GetDataInfo> p_callback, string p_sheetName, int p_timeout = 20)
        {
            //Will return success = false if the spreadsheet or the sheet does not exists
            Utility.StartBackgroundTask(DoGetData(p_url.Trim(), p_callback, p_sheetName, p_timeout));
        }

        /// <summary>
        /// Get the data in all the given sheets
        /// </summary>
        /// <param name="p_url">The url of the google scripts app</param>
        /// <param name="p_callback">The callback method called once the request is done</param>
        /// <param name="p_sheetsName">The name of the sheets</param>
        /// <param name="p_timeout">The time in second to wait for a response from the google script before cancelling</param>
        public static void GetAllSpreadsheetData(string p_url, Action<GetDataInfo> p_callback, string p_sheetsName, int p_timeout = 60)
        {
            //Will return success = false if the spreadsheet or the sheet does not exists
            Utility.StartBackgroundTask(DoGetAllData(p_url.Trim(), p_callback, p_sheetsName, p_timeout));
        }

        /// <summary>
        /// Get the last time the spreadsheet has been modified, used to check if updates are available
        /// </summary>
        /// <param name="p_url">The url of the google scripts app</param>
        /// <param name="p_callback">The callback method called once the request is done</param>
        /// <param name="p_timeout">The time in second to wait for a response from the google script before cancelling</param>
        public static void GetLastModificationTime(string p_url, Action<GetTimeInfo> p_callback, int p_timeout = 10)
        {
            //will return success = false if the spreadsheet does not exists
            Utility.StartBackgroundTask(DoGetLastUpdateTime(p_url.Trim(), p_callback, p_timeout));
        }

        /// <summary>
        /// Create a new spreadsheet
        /// </summary>
        /// <param name="p_url">The url of the google scripts app</param>
        /// <param name="p_callback">The callback method called once the request is done</param>
        /// <param name="p_timeout">The time in second to wait for a response from the google script before cancelling</param>
        private static IEnumerator DoCreateSpreadsheet(string p_url, Action<SpreadsheetInfo> p_callback, int p_timeout)
        {
            var l_time = DateTime.Now;
            var l_timedOut = false;

            //Set up a web request to te google scripts app
            using (var l_request = UnityWebRequest.Get(p_url + "?action=CreateSpreadsheet&projectName=" + Application.productName))
            {
                //Wait for the request to be sent
                yield return l_request.SendWebRequest();

                //If there was no errors we continue
                if (!l_request.isNetworkError)
                {
                    //We are waiting for the response to arrive
                    while (!l_request.isDone)
                    {
                        yield return new WaitForSeconds(0.1f);

                        //If the time to received a response is too high, we are canceling the action
                        if ((DateTime.Now - l_time).TotalSeconds > p_timeout)
                        {
                            p_callback(new SpreadsheetInfo { timedOut = true });
                            l_timedOut = true;
                            break;
                        }
                    }

                    if (!l_timedOut)
                    {
                        //Cast the received string to its json equivalent
                        var l_spreadsheetInfo = JsonUtility.FromJson<SpreadsheetInfo>(l_request.downloadHandler.text);
                        l_spreadsheetInfo.lastModificationTime = DateTime.ParseExact(l_spreadsheetInfo.lastModification, "d/M/yyyy H:m:s", CultureInfo.InvariantCulture);
                        //Send the received data to the callback
                        p_callback(l_spreadsheetInfo);
                    }
                }
            }
        }


        /// <summary>
        /// Send the data to the specified sheet
        /// </summary>
        /// <param name="p_url">The url of the google scripts app</param>
        /// <param name="p_callback">The callback method called once the request is done</param>
        /// <param name="p_data">The data of the scriptable object</param>
        /// <param name="p_sheetName">The name of the sheet</param>
        /// <param name="p_timeout">The time in second to wait for a response from the google script before cancelling</param>
        private static IEnumerator DoSendData(string p_url, Action<SendDataInfo> p_callback, string p_data, string p_sheetName, int p_timeout)
        {
            var l_time = DateTime.Now;
            var l_timedOut = false;

            var l_data = new WWWForm();
            l_data.AddField("data", p_data);
            l_data.AddField("sheetName", p_sheetName);
            l_data.AddField("projectName", Application.productName);
            l_data.AddField("action", "SendDataToSpreadsheet");
            //Set up a web request to te google scripts app
            using (var l_request = UnityWebRequest.Post($"{p_url}", l_data))
            {
                //Wait for the request to be sent
                yield return l_request.SendWebRequest();

                //If there was no errors we continue
                if (!l_request.isNetworkError)
                {
                    //We are waiting for the response to arrive
                    while (!l_request.isDone)
                    {
                        yield return new WaitForSeconds(0.1f);

                        //If the time to received a response is too high, we are canceling the action
                        if ((DateTime.Now - l_time).TotalSeconds > p_timeout)
                        {
                            p_callback(new SendDataInfo { timedOut = true });
                            l_timedOut = true;
                            break;
                        }
                    }

                    if (!l_timedOut)
                    {
                        //Cast the received string to its json equivalent
                        var l_sendInfo = JsonUtility.FromJson<SendDataInfo>(l_request.downloadHandler.text);
                        l_sendInfo.lastModificationTime = DateTime.ParseExact(l_sendInfo.lastModification,
                            "d/M/yyyy H:m:s", CultureInfo.InvariantCulture);
                        //Send the received data to the callback
                        p_callback(l_sendInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Send the data to the specified sheet
        /// </summary>
        /// <param name="p_url">The url of the google scripts app</param>
        /// <param name="p_callback">The callback method called once the request is done</param>
        /// <param name="p_data">The data of the scriptable object</param>
        /// <param name="p_timeout">The time in second to wait for a response from the google script before cancelling</param>
        private static IEnumerator DoSendAllData(string p_url, Action<SendDataInfo> p_callback, string p_data, int p_timeout)
        {
            var l_time = DateTime.Now;
            var l_timedOut = false;

            var l_data = new WWWForm();
            l_data.AddField("data", p_data);
            l_data.AddField("projectName", Application.productName);
            l_data.AddField("action", "SendMultipleDataToSpreadsheet");
            //Set up a web request to te google scripts app
            using (var l_request = UnityWebRequest.Post($"{p_url}", l_data))
            {
                //Wait for the request to be sent
                yield return l_request.SendWebRequest();

                //If there was no errors we continue
                if (!l_request.isNetworkError)
                {
                    //We are waiting for the response to arrive
                    while (!l_request.isDone)
                    {
                        yield return new WaitForSeconds(0.1f);

                        //If the time to received a response is too high, we are canceling the action
                        if ((DateTime.Now - l_time).TotalSeconds > p_timeout)
                        {
                            p_callback(new SendDataInfo { timedOut = true });
                            l_timedOut = true;
                            break;
                        }
                    }

                    if (!l_timedOut)
                    {
                        //Cast the received string to its json equivalent
                        var l_sendInfo = JsonUtility.FromJson<SendDataInfo>(l_request.downloadHandler.text);
                        l_sendInfo.lastModificationTime = DateTime.ParseExact(l_sendInfo.lastModification,
                            "d/M/yyyy H:m:s", CultureInfo.InvariantCulture);
                        //Send the received data to the callback
                        p_callback(l_sendInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Get the data from the specified sheet
        /// </summary>
        /// <param name="p_url">The url of the google scripts app</param>
        /// <param name="p_callback">The callback method called once the request is done</param>
        /// <param name="p_sheetName">The name of the sheet</param>
        /// <param name="p_timeout">The time in second to wait for a response from the google script before cancelling</param>
        private static IEnumerator DoGetData(string p_url, Action<GetDataInfo> p_callback, string p_sheetName, int p_timeout)
        {
            var l_time = DateTime.Now;
            var l_timedOut = false;

            //Set up a web request to te google scripts app
            using (UnityWebRequest l_request = UnityWebRequest.Get($"{p_url}?action=GetDataFromSpreadsheet&projectName={Application.productName}&sheetName={p_sheetName}"))
            {
                //Wait for the request to be sent
                yield return l_request.SendWebRequest();

                //If there was no errors we continue
                if (!l_request.isNetworkError)
                {
                    //We are waiting for the response to arrive
                    while (!l_request.isDone)
                    {
                        yield return new WaitForSeconds(0.1f);

                        //If the time to received a response is too high, we are canceling the action
                        if ((DateTime.Now - l_time).TotalSeconds > p_timeout)
                        {
                            p_callback(new GetDataInfo { timedOut = true });
                            l_timedOut = true;
                            break;
                        }
                    }

                    if (!l_timedOut)
                    {
                        //Cast the received string to its json equivalent
                        var l_dataInfo = JsonUtility.FromJson<GetDataInfo>(l_request.downloadHandler.text);
                        if (l_dataInfo.success)
                            l_dataInfo.lastModificationTime = DateTime.ParseExact(l_dataInfo.lastModification,
                                "d/M/yyyy H:m:s", CultureInfo.InvariantCulture);

                        //Send the received data to the callback
                        p_callback(l_dataInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Get the data from the specified sheet
        /// </summary>
        /// <param name="p_url">The url of the google scripts app</param>
        /// <param name="p_callback">The callback method called once the request is done</param>
        /// <param name="p_sheetsName">The name of the sheet</param>
        /// <param name="p_timeout">The time in second to wait for a response from the google script before cancelling</param>
        private static IEnumerator DoGetAllData(string p_url, Action<GetDataInfo> p_callback, string p_sheetsName, int p_timeout)
        {
            var l_time = DateTime.Now;
            var l_timedOut = false;

            var l_data = new WWWForm();
            l_data.AddField("sheetsName", p_sheetsName);
            l_data.AddField("projectName", Application.productName);
            l_data.AddField("action", "GetMultipleDataFromSpreadsheet");
            //Set up a web request to te google scripts app
            using (var l_request = UnityWebRequest.Post($"{p_url}", l_data))
            {
                //Wait for the request to be sent
                yield return l_request.SendWebRequest();

                //If there was no errors we continue
                if (!l_request.isNetworkError)
                {
                    //We are waiting for the response to arrive
                    while (!l_request.isDone)
                    {
                        yield return new WaitForSeconds(0.1f);

                        //If the time to received a response is too high, we are canceling the action
                        if ((DateTime.Now - l_time).TotalSeconds > p_timeout)
                        {
                            p_callback(new GetDataInfo { timedOut = true });
                            l_timedOut = true;
                            break;
                        }
                    }

                    if (!l_timedOut)
                    {
                        //Cast the received string to its json equivalent
                        var l_dataInfo = JsonUtility.FromJson<GetDataInfo>(l_request.downloadHandler.text);
                        if (l_dataInfo.success)
                            l_dataInfo.lastModificationTime = DateTime.ParseExact(l_dataInfo.lastModification,
                                "d/M/yyyy H:m:s", CultureInfo.InvariantCulture);

                        //Send the received data to the callback
                        p_callback(l_dataInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Get the last time the spreadsheet has been modified, used to check if updates are available
        /// </summary>
        /// <param name="p_url">The url of the google scripts app</param>
        /// <param name="p_callback">The callback method called once the request is done</param>
        /// <param name="p_timeout">The time in second to wait for a response from the google script before cancelling</param>
        private static IEnumerator DoGetLastUpdateTime(string p_url, Action<GetTimeInfo> p_callback, int p_timeout)
        {
            var l_time = DateTime.Now;
            var l_timedOut = false;

            //Set up a web request to te google scripts app
            using (UnityWebRequest l_request = UnityWebRequest.Get($"{p_url}?action=GetLastModificationTime&projectName={Application.productName}"))
            {
                //Wait for the request to be sent
                yield return l_request.SendWebRequest();

                //If there was no errors we continue
                if (!l_request.isNetworkError)
                {
                    //We are waiting for the response to arrive
                    while (!l_request.isDone)
                    {
                        yield return new WaitForSeconds(0.1f);

                        //If the time to received a response is too high, we are canceling the action
                        if ((DateTime.Now - l_time).TotalSeconds > p_timeout)
                        {
                            p_callback(new GetTimeInfo { timedOut = true });
                            l_timedOut = true;
                            break;
                        }
                    }

                    if (!l_timedOut)
                    {
                        //Cast the received string to its json equivalent
                        var l_timeInfo = JsonUtility.FromJson<GetTimeInfo>(l_request.downloadHandler.text);
                        if (l_timeInfo.success)
                            l_timeInfo.lastModificationTime = DateTime.ParseExact(l_timeInfo.lastModification, "d/M/yyyy H:m:s", CultureInfo.InvariantCulture);
                        //Send the received data to the callback
                        p_callback(l_timeInfo);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Simple class holding the response for a spread sheet creation
    /// </summary>
    public class SpreadsheetInfo
    {
        public string name;
        public string id;
        public string lastModification;
        public DateTime lastModificationTime;
        public bool timedOut;
    }

    /// <summary>
    /// Simple class holding the response for a data sending
    /// </summary>
    public class SendDataInfo
    {
        public bool success;
        public string lastModification;
        public DateTime lastModificationTime;
        public bool timedOut;
    }

    /// <summary>
    /// Simple class holding the response for a data getting
    /// </summary>
    public class GetDataInfo
    {
        public bool success;
        public string data;
        public string lastModification;
        public DateTime lastModificationTime;
        public bool timedOut;
    }

    /// <summary>
    /// Simple class holding the response for a last modification request
    /// </summary>
    public class GetTimeInfo
    {
        public bool success;
        public string lastModification;
        public DateTime lastModificationTime;
        public bool timedOut;
    }

    /// <summary>
    /// Utility class used to run ienumerator in editor
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// Start an ienumerator and run it in editor
        /// </summary>
        /// <param name="p_update">The ienumerator to run</param>
        /// <param name="p_end">An eventual callback for when the ienumerator end</param>
        public static void StartBackgroundTask(IEnumerator p_update, Action p_end = null)
        {
            EditorApplication.update += ClosureCallback;

            void ClosureCallback()
            {
                try
                {
                    if (p_update.MoveNext()) return;

                    p_end?.Invoke();
                    EditorApplication.update -= ClosureCallback;
                }
                catch (Exception l_ex)
                {
                    p_end?.Invoke();
                    Debug.LogException(l_ex);
                    EditorApplication.update -= ClosureCallback;
                }
            }
        }
    }
}