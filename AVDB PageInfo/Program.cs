using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace AVDB_PageInfo
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Please Input The FanHao:");
            //string fanhao = Console.ReadLine();
            int i = 1;
            string connectString = "Data Source=.;Initial Catalog=Media;Integrated Security=True";
            //string url = "http://avdb.lol//group/" + fanhao + "/currentPage/";
            string url = "http://avdb.lol/currentPage/";
            Regex regexMax = new Regex(@"/currentPage/(?<page>[\d]*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline);
            int LatestPage = GetLastPageNumber(url + 20000, regexMax);
            Console.WriteLine("Total Page:" + (LatestPage).ToString());

            //Regex regex = new Regex(@"<div class=""item"">[\s\S][^<]*[\s\S][^<]*[\s\S][^""]*""(?<hyperlink>[^""]*)[\s\S][^""]*[\s\S][^=]*=""(?<title>[^""]*)""[\s\S][^""]*""(?<coverpage>[^""]*)[\s\S][^/]*/[\s\S][^/]*/[\s\S][^=]*=[\s\S][^=]*=""(?<grouphyperlink>[^""]*)[\s\S][^\n]*\n[\s\S][^\n]*\n[\s\S][^\n]*\n[\s\S][^\>]*>(?<fanhao>[^<]*)[\s\S][^\d]*(?<releasedate>[^<]*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline);

            while (i <= LatestPage)
            {
                string pageHtml = GetUrltoHtml(url + i, "utf-8");

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(pageHtml);

                var htmlNode = htmlDocument.DocumentNode;                var container = htmlDocument.GetElementbyId("waterfall");
                var all = container.SelectNodes("div");
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
                        i++;
                    }
                }

                Console.WriteLine(DateTime.Now.ToString() + "   " + i.ToString() + ": " + all.Count.ToString());
                i++;
            }
        }


        public static string GetUrltoHtml(string Url, string type)
        {
            try
            {
                System.Net.WebRequest wReq = System.Net.WebRequest.Create(Url);
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
            int page = 100;
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
