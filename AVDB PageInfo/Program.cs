using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AVDB_PageInfo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Please Input The FanHao:");
            string fanhao = Console.ReadLine();
            int i = 1;
            string connectString = "Data Source=.;Initial Catalog=Media;Integrated Security=True";
            string url = "http://avdb.lol//group/" + fanhao + "/currentPage/";
            Regex regexMax = new Regex(@"/currentPage/(?<page>[\d]*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline);
            int LatestPage = GetLastPageNumber(url + 20000, regexMax);
            Console.WriteLine("Total Page:" + (LatestPage + 1).ToString());

            Regex regex = new Regex(@"<div class=""item"">[\s\S][^<]*[\s\S][^<]*[\s\S][^""]*""(?<hyperlink>[^""]*)[\s\S][^""]*[\s\S][^=]*=""(?<title>[^""]*)""[\s\S][^""]*""(?<coverpage>[^""]*)[\s\S][^/]*/[\s\S][^/]*/[\s\S][^=]*=[\s\S][^=]*=""(?<grouphyperlink>[^""]*)[\s\S][^\n]*\n[\s\S][^\n]*\n[\s\S][^\n]*\n[\s\S][^\>]*>(?<fanhao>[^<]*)[\s\S][^\d]*(?<releasedate>[^<]*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline);

            SqlConnection delCnt = new SqlConnection(connectString);
            delCnt.Open();
            SqlCommand del = delCnt.CreateCommand();
            del.CommandType = CommandType.Text;
            del.CommandText = "DELETE  FROM[Media].[dbo].[VideoSummary] WHERE SUBSTRING([SerialNumber], 0, CHARINDEX('-',[SerialNumber])) = '" + fanhao + "';";
            del.ExecuteScalar();
            delCnt.Close();
            delCnt.Dispose();

            while (i <= LatestPage + 1)
            {
                string pageHtml = GetUrltoHtml(url + i, "utf-8");
                MatchCollection matchCollection = regex.Matches(pageHtml);
                Console.WriteLine(DateTime.Now.ToString() + "   " + i.ToString() + ": " + matchCollection.Count.ToString());

                foreach (Match match in matchCollection)
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
                    cmd.Parameters["@HyperLink"].Value = match.Groups[1].Value.ToString();
                    cmd.Parameters["@Title"].Value = match.Groups[2].Value.ToString();
                    cmd.Parameters["@CoverPageImageLink"].Value = match.Groups[3].Value.ToString();
                    cmd.Parameters["@SerialHyperLink"].Value = match.Groups[4].Value.ToString();
                    cmd.Parameters["@SerialNumber"].Value = match.Groups[5].Value.ToString();
                    cmd.Parameters["@ReleaseDate"].Value = match.Groups[6].Value.ToString();
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
                    //string test = match.Groups[1].Value.ToString();
                }
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
