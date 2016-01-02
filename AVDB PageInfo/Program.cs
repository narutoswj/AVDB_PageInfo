using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Data.SQLite;
using System.IO;
using System.Collections.Generic;

namespace AVDB_PageInfo
{
    class Program
    {
        static void Main(string[] args)
        {
            int i = 1;
            //string connectStringSQL = "Data Source=.;Initial Catalog=Media;Integrated Security=True";
            string connectStringSQLite = "Data Source =" + Environment.CurrentDirectory + "\\Media.db";

            //CreateSQLiteFile();
            CreateTable(connectStringSQLite);

            //Get last page number
            string url = "http://avdb.lol/currentPage/";
            Regex regexMax = new Regex(@"/currentPage/(?<page>[\d]*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline);
            int LatestPage = GetLastPageNumber(url + 20000, regexMax);
            Console.WriteLine("Total Page:" + (LatestPage).ToString());

            //Load page summary info into DB
            //while (i <= LatestPage)
            //{
            //    LoadOnePage(i, connectStringSQLite, url);
            //    i++;
            //}

            //Reload filed page
            while (GetFailedPage(connectStringSQLite).Count > 1)
            {
                foreach (int p in GetFailedPage(connectStringSQLite))
                {
                    try
                    {
                        LoadOnePage(p, connectStringSQLite, url);
                        DeleteFailRecord(connectStringSQLite, p);
                    }
                    catch (Exception e)
                    { }
                }
            }
        }

        private static void LoadOnePage(int i, string connectStringSQLite, string url)
        {
            string pageHtml = GetUrltoHtml(url + i, "utf-8");

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(pageHtml);

            var htmlNode = htmlDocument.DocumentNode;
            var container = htmlDocument.GetElementbyId("waterfall");
            try
            {
                var all = container.SelectNodes("div");
                if (all != null)
                {
                    foreach (var video in all)
                    {
                        var hyperlink = video.SelectNodes("div")[0].SelectNodes("a")[0].Attributes["href"];
                        var title = video.SelectNodes("div")[2].SelectNodes("a")[0].InnerText;
                        var coverPageImageLink = video.SelectNodes("div")[0].SelectNodes("a")[0].SelectNodes("img")[0].Attributes["data-original"];

                        string serialhyperlink, SerialNumber, ReleaseDate;

                        try
                        {
                            serialhyperlink = video.SelectNodes("div")[1].SelectNodes("a")[0].Attributes["href"].Value;
                        }
                        catch (System.Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            serialhyperlink = "";
                        }

                        try
                        {
                            SerialNumber = video.SelectNodes("div")[2].SelectNodes("span")[0].InnerText;
                        }
                        catch (System.Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            SerialNumber = "";
                        }

                        try
                        {
                            ReleaseDate = video.SelectNodes("div")[2].SelectNodes("span")[1].InnerText;
                        }
                        catch (System.Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            ReleaseDate = "";
                        }

                        SaveToSQLite(connectStringSQLite, hyperlink, title, coverPageImageLink, serialhyperlink, SerialNumber, ReleaseDate);


                        //SaveToSQLServer(connectStringSQL, hyperlink, title, coverPageImageLink, serialhyperlink, SerialNumber, ReleaseDate);
                    }
                }
                Console.WriteLine(DateTime.Now.ToString() + "   " + i.ToString() + ": " + all.Count.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(DateTime.Now.ToString() + "   " + i.ToString() + ": 0");
                SaveFailPage(connectStringSQLite, i.ToString());
                Console.WriteLine(e.Message);
            }
        }

        private static void SaveToSQLite(string connectStringSQLite, HtmlAttribute hyperlink, string title, HtmlAttribute coverPageImageLink, string serialhyperlink, string SerialNumber, string ReleaseDate)
        {
            SQLiteConnection conn = null;
            conn = new SQLiteConnection(connectStringSQLite);
            conn.Open();
            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "INSERT INTO [VideoSummary] ([HyperLink],[Title],[CoverPageImageLink],[SerialHyperLink],[SerialNumber],[ReleaseDate]) VALUES (@HyperLink,@Title,@CoverPageImageLink,@SerialHyperLink,@SerialNumber,@ReleaseDate)";
            cmd.Parameters.Add("@HyperLink", DbType.String);
            cmd.Parameters.Add("@Title", DbType.String);
            cmd.Parameters.Add("@CoverPageImageLink", DbType.String);
            cmd.Parameters.Add("@SerialHyperLink", DbType.String);
            cmd.Parameters.Add("@SerialNumber", DbType.String);
            cmd.Parameters.Add("@ReleaseDate", DbType.String);
            cmd.Parameters["@HyperLink"].Value = hyperlink.Value.ToString();
            cmd.Parameters["@Title"].Value = title.ToString();
            cmd.Parameters["@CoverPageImageLink"].Value = coverPageImageLink.Value.ToString();
            cmd.Parameters["@SerialHyperLink"].Value = serialhyperlink.ToString();
            cmd.Parameters["@SerialNumber"].Value = SerialNumber.ToString();
            cmd.Parameters["@ReleaseDate"].Value = ReleaseDate.ToString();
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        private static void SaveFailPage(string connectStringSQLite, string pagenumber)
        {
            SQLiteConnection conn = null;
            conn = new SQLiteConnection(connectStringSQLite);
            conn.Open();
            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "INSERT INTO [FailedPage] ([PageNumber]) VALUES (@PageNumber)";
            cmd.Parameters.Add("@PageNumber", DbType.String);
            cmd.Parameters["@PageNumber"].Value = pagenumber.ToString();
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        private static void CreateTable(string connectStringSQLite)
        {
            SQLiteConnection conn = null;
            conn = new SQLiteConnection(connectStringSQLite);
            conn.Open();
            string sql = @"CREATE TABLE IF NOT EXISTS [VideoSummary](
                          [HyperLink] [nvarchar](100) NULL,
	                      [Title] [nvarchar](200) NULL,
	                      [CoverPageImageLink] [nvarchar](100) NULL,
	                      [SerialHyperLink] [nvarchar](100) NULL,
	                      [SerialNumber] [nvarchar](100) NULL,
	                      [ReleaseDate] [nvarchar](100) NULL);

                          CREATE TABLE IF NOT EXISTS [FailedPage](
                          [PageNumber] [nvarchar](100) NULL);";

            SQLiteCommand cmdCreateTable = new SQLiteCommand(sql, conn);
            cmdCreateTable.ExecuteNonQuery();
            conn.Close();
        }

        private static void DeleteFailRecord(string connectStringSQLite, int pagenumber)
        {
            SQLiteConnection conn = null;
            conn = new SQLiteConnection(connectStringSQLite);
            conn.Open();
            string sql = @"DELETE FROM [FailedPage] WHERE [PageNumber] = " + pagenumber.ToString();

            SQLiteCommand cmdCreateTable = new SQLiteCommand(sql, conn);
            cmdCreateTable.ExecuteNonQuery();
            conn.Close();
        }

        private static List<int> GetFailedPage(string connectStringSQLite)
        {
            List<int> pagelist = new List<int>();
            int i = 0;
            SQLiteConnection conn = null;
            conn = new SQLiteConnection(connectStringSQLite);
            conn.Open();
            string sql = @"SELECT [PageNumber] FROM [FailedPage]";

            SQLiteCommand cmdFailedPage = new SQLiteCommand(sql, conn);
            SQLiteDataReader reader = cmdFailedPage.ExecuteReader();
            while (reader.Read())
            {
                pagelist.Add(int.Parse(reader.GetString(0)));

            }
            conn.Close();

            return pagelist;
        }

        private static void CreateSQLiteFile()
        {
            if (!File.Exists(Environment.CurrentDirectory + "\\Media.db"))
            {
                File.Create(Environment.CurrentDirectory + "\\Media.db");
            }
        }

        private static void SaveToSQLServer(string connectString, HtmlAttribute hyperlink, string title, HtmlAttribute coverPageImageLink, string serialhyperlink, string SerialNumber, string ReleaseDate)
        {
            SqlConnection sqlCnt = new SqlConnection(connectString);
            sqlCnt.Open();
            SqlCommand cmd = sqlCnt.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "INSERT INTO [dbo].[VideoSummary] ([HyperLink],[Title],[CoverPageImageLink],[SerialHyperLink],[SerialNumber],[ReleaseDate]) VALUES (@HyperLink,@Title,@CoverPageImageLink,@SerialHyperLink,@SerialNumber,@ReleaseDate)";
            cmd.Parameters.Add("@HyperLink", SqlDbType.NVarChar);
            cmd.Parameters.Add("@Title", SqlDbType.NVarChar);
            cmd.Parameters.Add("@CoverPageImageLink", SqlDbType.NVarChar);
            cmd.Parameters.Add("@SerialHyperLink", SqlDbType.NVarChar);
            cmd.Parameters.Add("@SerialNumber", SqlDbType.NVarChar);
            cmd.Parameters.Add("@ReleaseDate", SqlDbType.NVarChar);
            cmd.Parameters["@HyperLink"].Value = hyperlink.Value.ToString();
            cmd.Parameters["@Title"].Value = title.ToString();
            cmd.Parameters["@CoverPageImageLink"].Value = coverPageImageLink.Value.ToString();
            cmd.Parameters["@SerialHyperLink"].Value = serialhyperlink.ToString();
            cmd.Parameters["@SerialNumber"].Value = SerialNumber.ToString();
            cmd.Parameters["@ReleaseDate"].Value = ReleaseDate.ToString();
            try
            {
                cmd.ExecuteScalar();
                sqlCnt.Close();
                sqlCnt.Dispose();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
                sqlCnt.Close();
                sqlCnt.Dispose();
            }
        }

        public static string GetUrltoHtml(string Url, string type)
        {
            try
            {
                System.Net.WebRequest wReq = System.Net.WebRequest.Create(Url);
                wReq.Timeout = 600000;
                // Get the response instance.
                System.Net.WebResponse wResp = wReq.GetResponse();
                System.IO.Stream respStream = wResp.GetResponseStream();
                // Dim reader As StreamReader = New StreamReader(respStream)
                using (System.IO.StreamReader reader = new System.IO.StreamReader(respStream, Encoding.GetEncoding(type)))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (System.Exception ex)
            {
                return ex.Message;
            }

        }

        private static int GetLastPageNumber(string url, Regex regex)
        {
            int page = 0;
            try
            {
                MatchCollection matchCollection = regex.Matches(GetUrltoHtml(url, "utf-8"));
                int length = matchCollection.Count;
                page = int.Parse(matchCollection[length - 1].Groups[1].Value);
                return page;
            }
            catch
            {
                return 0;
            }
        }
    }
}
