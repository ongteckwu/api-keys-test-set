using Aliyun.Acs.Core;
using Aliyun.Acs.Core.Profile;
using Aliyun.Acs.Dysmsapi.Model.V20170525;
using BackstageApi.Common;
using BackstageApi.MessageOut;
using BackstageApi.Models;
using EntityAmzaonHomeModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http;

namespace BackstageApi.Controllers
{
    public class CustomerAppointmentController : ApiController
    {
		static BuySystemEntities buy = new BuySystemEntities();
		List<CustomerAppointmentModel> list = new List<CustomerAppointmentModel>();
		
		[Route("api/CustomerAppointment/GetCustomerAppointment"), HttpGet]
		public HttpResponseMessage GetCustomerAppointment(string keyWord, int pageNum, int pagesize)
		{
			using (BuySystemEntities buy = new BuySystemEntities())
			{
				int count = 0;
				if (string.IsNullOrWhiteSpace(keyWord) == false)
				{
					int a; int.TryParse(keyWord, out a);
					count = buy.CustomerUsers.Where(c => c.Id== a || c.Phone.Contains(keyWord)).Count();
				}
				else
				{
					count = buy.CustomerUsers.Count();
				}
				var lists = GetCustomerModels(keyWord, pageNum, pagesize);
				var json = JsonConvert.SerializeObject(lists);
				StringBuilder sb = new StringBuilder();
				sb.Append("{");
				sb.Append("\"total\"");
				sb.Append(":");
				sb.Append("\"" + count + "\"");
				sb.Append(",");
				sb.Append("\"list\"");
				sb.Append(":");
				sb.Append(json);
				sb.Append("}");
				json = sb.ToString();
				return new HttpResponseMessage { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
			}
		}

		public static IList<CustomerModel> GetCustomerModels(string keyWord, int pageNum, int pagesize)
		{
			try
			{

				int rownum = pagesize * (pageNum - 1);
				string where = "";
				if (string.IsNullOrWhiteSpace(keyWord) == false)
				{
					where = "C.Phone like '%" + keyWord + "%' or C.Id="+ keyWord;
				}
				string sql = "select C.*,b.Name AS BackName,CF.AccountBalance from CustomerUsers C left join Recommend as R on C.RecommendId = R.Id left join BackUser as B on R.Id = B.JobNumber left join CustomerFinance as CF on c.Id = cf.CustomerId";
				if (string.IsNullOrWhiteSpace(where) == false)
				{
					sql += " where " + where;
				}
				sql += " order by Id desc  OFFSET " + rownum + " ROWS FETCH NEXT " + pagesize + " ROWS ONLY ";
				string connstring = buy.Database.Connection.ConnectionString;
				using (SqlConnection conn = new SqlConnection(connstring))
				{
					conn.Open();
					SqlCommand cmd = new SqlCommand(sql, conn);
					SqlDataReader dr = cmd.ExecuteReader();
					
					IList<CustomerModel> list = new List<CustomerModel>();
					while (dr.Read())
					{
						CustomerModel a = new CustomerModel();
						a.Id = (int)dr["Id"];
						//a.RecommendId = (int)dr["RecommendId"];
						a.Phone = dr["Phone"].ToString();
						a.CustomerName = dr["Name"].ToString();
						a.PassWord = dr["PassWord"].ToString();
						a.RecommendCode = (int)dr["RecommendCode"];
						a.Enabled = (int)dr["Enabled"];
						a.QQ = dr["QQ"].ToString();
						a.WeCate = dr["WeCate"].ToString();
						a.LoginTime = dr["LoginTime"].ToString();
						a.LoginIp = dr["LoginIp"].ToString();
						a.RegistrationTime = dr["RegistrationTime"].ToString();
						//a.BackName = dr["Name"].ToString();
						a.AccountBalance =dr["AccountBalance"].ToString();
						a.BackName = dr["BackName"].ToString();
						list.Add(a);
					}

					return list;
				}
			}
			catch (Exception ex)
			{
				throw;
			}
		}


		[Route("api/CustomerAppointment/ChangeCustomerState"), HttpPost]
		public IHttpActionResult ChangeCustomerState([FromBody]JObject value)
		{
			int result = 0;
			int id;
			string state= string.Empty;
			if (value != null)
			{
				id = (int)value["Id"];
				state = value["State"].ToString();
			}
			else
			{
				return Ok(Respone.No("请上传参数"));
			}
			var query = buy.CustomerUsers.Where(c => c.Id == id).FirstOrDefault();
			if (query != null)
			{
				if (state == "0")  //禁用
				{
					query.Enabled = 0;
					DbEntityEntry entry = buy.Entry(query);
					entry.State = System.Data.Entity.EntityState.Modified;
					result = buy.SaveChanges();
				}
				else
				{
					query.Enabled = 1;
					DbEntityEntry entry = buy.Entry(query);
					entry.State = System.Data.Entity.EntityState.Modified;

					result = buy.SaveChanges();
				}
			}
			if (result > 0)
			{
				return Ok(Respone.Success("修改成功"));
			}
			else
			{
				return Ok(Respone.No("发生了点问题，请稍后再试"));
			}
		}
		[Route("api/CustomerAppointment/ChangeAccountbalance"), HttpPost]
		public IHttpActionResult ChangeAccountbalance([FromBody]JObject value)
		{
			using (BuySystemEntities buy = new BuySystemEntities())
			{
				int result = 0;
				int sum = 0;
				int id;
				decimal accountbalance;
				string state = string.Empty;
				if (value != null)
				{
					id = (int)value["Id"];
					state = value["State"].ToString();
					accountbalance = (decimal)value["Accountbalance"];
				}
				else
				{
					return Ok(Respone.No("请上传参数"));
				}

				var query = buy.CustomerFinance.Where(c => c.CustomerId == id).FirstOrDefault();
				if (query != null)
				{
					if (state == "0") //扣除
					{
						query.AccountBalance = query.AccountBalance - accountbalance;
						query.AccumulatedExpenditure = query.AccumulatedExpenditure + accountbalance;
						DbEntityEntry entry = buy.Entry(query);
						entry.State = System.Data.Entity.EntityState.Modified;
						result = buy.SaveChanges();
						if (result > 0)
						{
							TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
							CustomerFinanceLog log = new CustomerFinanceLog
							{
								BusinessNumber = Convert.ToInt64(ts.TotalMilliseconds).ToString(),
								CustomerId = query.CustomerId,
								PaymentState = 2,
								TransactionType = 3,
								TransactionTime = DateTime.Now,
								TransactionAmount = accountbalance,
								Remarks = "扣款支出"
							};
							buy.CustomerFinanceLog.Add(log);
							sum = buy.SaveChanges();
						}
					}
					else   //充值
					{
						query.AccountBalance = query.AccountBalance + accountbalance;
						query.AccumulatedIncone = query.AccumulatedIncone + accountbalance;
						DbEntityEntry entry = buy.Entry(query);
						entry.State = System.Data.Entity.EntityState.Modified;
						result = buy.SaveChanges();

						if (result > 0)
						{
							TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
							CustomerFinanceLog log = new CustomerFinanceLog
							{
								BusinessNumber = Convert.ToInt64(ts.TotalMilliseconds).ToString(),
								CustomerId = query.CustomerId,
								PaymentState = 1,
								TransactionType = 4,
								TransactionTime = DateTime.Now,
								TransactionAmount = accountbalance,
								Remarks = "充值收入"
							};
							buy.CustomerFinanceLog.Add(log);
							sum = buy.SaveChanges();
						}
					}
				}
				else
				{

				}
				if (result > 0 && sum > 0)
				{
					return Ok(Respone.Success("修改成功"));
				}
				else
				{
					return Ok(Respone.No("发生了点问题，请稍后再试"));
				}
			}
		}

		[Route("api/CustomerAppointment/GetAccountbalance"), HttpGet]
		public HttpResponseMessage GetAccountbalance(int state,string keyWord, int pageNum, int pagesize,int userId)
		{
			var json = string.Empty;
			var query = (
				  from c in buy.CustomerFinanceLog
				  join f in buy.CustomerFinance on c.CustomerId equals f.CustomerId
				  where c.CustomerId == userId
				  select new { c,f.AccountBalance,f.AccumulatedIncone, f.AccumulatedExpenditure }
				);
			if (state > 0)
			{
				query = query.Where(t => t.c.PaymentState == state);
			}
			if (string.IsNullOrWhiteSpace(keyWord) == false)
			{
				query = query.Where(t => t.c.BusinessNumber.Contains(keyWord) || t.c.Remarks.Contains(keyWord));
			}
			int count = query.Count();
			var row = query.OrderByDescending(i => i.c.Id).Skip(pagesize * (pageNum - 1)).Take(pagesize).ToList().Select(e => new
			{
				Id = e.c.Id,
				BusinessNumber = e.c.BusinessNumber,
				CustomerId = e.c.CustomerId,
				PaymentState = e.c.PaymentState,
				TransactionType = e.c.TransactionType,
				TransactionTime = e.c.TransactionTime.Value.ToString("yyyy-MM-dd HH:mm:ss"),
				TransactionAmount = e.c.TransactionAmount,
				Remarks = e.c.Remarks,
				AccountBalance =e.AccountBalance,
				AccumulatedIncone=e.AccumulatedIncone,
				AccumulatedExpenditure=e.AccumulatedExpenditure,
			}).ToList();
			
			json = JsonConvert.SerializeObject(row);
			StringBuilder sb = new StringBuilder();
			sb.Append("{");
			sb.Append("\"total\"");
			sb.Append(":");
			sb.Append("\"" + count + "\"");
			sb.Append(",");
			sb.Append("\"list\"");
			sb.Append(":");
			sb.Append(json);
			sb.Append("}");
			json = sb.ToString();
			return new HttpResponseMessage { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
		}


		//客户交易详情
		[Route("api/CustomerAppointment/GetAccount"), HttpGet]
		public HttpResponseMessage GetAccount()
		{
			var query = (
				from c in buy.CustomerFinanceLog
				join f in buy.CustomerUsers on c.CustomerId equals f.Id
				select new { c, f.Name }
				).OrderByDescending(i => i.c.Id).ToList().Select(e => new
				{
					Id = e.c.Id,
					BusinessNumber = e.c.BusinessNumber,
					PaymentState=e.c.PaymentState,
					TransactionType=e.c.TransactionType,
					TransactionTime = e.c.TransactionTime.Value.ToString("yyyy-MM-dd HH:mm:ss"),
					TransactionAmount=e.c.TransactionAmount,
					Remarks=e.c.Remarks,
					CustomerId=e.c.CustomerId,
					Name=e.Name,

				}).ToList();
			var json = JsonConvert.SerializeObject(query);
			StringBuilder sb = new StringBuilder();
			sb.Append("{");
			sb.Append("\"list\"");
			sb.Append(":");
			sb.Append(json);
			sb.Append("}");
			json = sb.ToString();
			return new HttpResponseMessage { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
		}

		public static IList<AccountBalanceMode> GetAccountBalanceMode(int state, string keyWord, int pageNum, int pagesize, int userId)
		{
			int rownum = pagesize * (pageNum - 1);
			string where = "";
			if (string.IsNullOrWhiteSpace(keyWord) == false && state > 0)
			{
				where = "(CL.BusinessNumber like '%"+ keyWord + "%' or CL.Remarks like '%" + keyWord + "%')  and CL.PaymentState =" + state + " and"+" C.Id=" + userId;
			}
			else if (string.IsNullOrWhiteSpace(keyWord)==false)
			{
				where = "(CL.BusinessNumber like '%" + keyWord + "%' or CL.Remarks like '%" + keyWord + "%') and C.Id=" + userId;
			}
			else if (state > 0)
			{
				where = "CL.PaymentState =" + state + " and  C.Id=" + userId;
			}
			else
			{
				where = "C.Id=" + userId;
			}
			string sql = "select C.Id AS UserId,CL.Id,CF.AccountBalance,CF.AccumulatedExpenditure,CF.AccumulatedIncone,CL.BusinessNumber,CL.PaymentState,CL.TransactionAmount,CL.Remarks from CustomerUsers C left join CustomerFinance as CF on c.Id = CF.CustomerId left join[dbo].[CustomerFinanceLog] as CL on c.Id=CL.CustomerId";
			if (string.IsNullOrWhiteSpace(where) == false)
			{
				sql += " where " + where;
			}
			sql += " order by Id desc  OFFSET " + rownum + " ROWS FETCH NEXT " + pagesize + " ROWS ONLY ";
			string connstring = buy.Database.Connection.ConnectionString;
			using (SqlConnection conn = new SqlConnection(connstring))
			{
				conn.Open();
				SqlCommand cmd = new SqlCommand(sql, conn);
				SqlDataReader dr = cmd.ExecuteReader();
				IList<AccountBalanceMode> list = new List<AccountBalanceMode>();
				while (dr.Read())
				{
					AccountBalanceMode model = new AccountBalanceMode();
					model.UserId = (int)dr["UserId"];
					model.Number = dr["BusinessNumber"].ToString();
					model.State = dr["PaymentState"].ToString();
					model.AccountBalance = (decimal)dr["AccountBalance"];
					model.AccumulatedIncome = (decimal)dr["AccumulatedIncone"];
					model.AccumulatedExpenditure = (decimal)dr["AccumulatedExpenditure"];
					model.Remarks = dr["Remarks"].ToString();
					model.Monery = dr["TransactionAmount"].ToString();
					list.Add(model);
				}
				return list;
			}
		}

		[Route("api/CustomerAppointment/GetBackAppoinmentLog"), HttpGet]
		public HttpResponseMessage GetBackAppoinmentLog(string keyWord, int pageNum, int pagesize)
		{
			int count = 0;
			if (string.IsNullOrWhiteSpace(keyWord) == false)
			{
				long tt = 0;
				bool a = long.TryParse(keyWord,out tt);
				count = buy.CustomerFinanceLog.Where(c => (c.TransactionType==3 || c.TransactionType ==4 || c.TransactionType==5) && (c.BusinessNumber.Contains(keyWord) || c.CustomerId == tt)).Count();
			}
			else
			{
				count = buy.CustomerFinanceLog.Where(c=> c.TransactionType == 3 || c.TransactionType == 4 || c.TransactionType == 5).Count();
			}
			var list = customerFinanceLogsList(keyWord, pageNum, pagesize);
			var json = JsonConvert.SerializeObject(list);
			StringBuilder sb = new StringBuilder();
			sb.Append("{");
			sb.Append("\"total\"");
			sb.Append(":");
			sb.Append("\"" + count + "\"");
			sb.Append(",");
			sb.Append("\"list\"");
			sb.Append(":");
			sb.Append(json);
			sb.Append("}");
			json = sb.ToString();
			return new HttpResponseMessage { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
		}

		public static IList<CustomerFinanceLogModel> customerFinanceLogsList(string keyWord, int pageNum, int pagesize)
		{
			try
			{
				int rownum = pagesize * (pageNum - 1);
				string where = "";
				if (string.IsNullOrWhiteSpace(keyWord) == false)
				{
					where = " [TransactionType] in ('3','4','5') and ([BusinessNumber] like '%" + keyWord + "%' or [CustomerId] like '%" + keyWord + "%')";
				}
				string sql = "select * from [dbo].[CustomerFinanceLog]";
				if (string.IsNullOrWhiteSpace(where) == false)
				{
					sql += " where " + where;
				}
				else
				{
					sql += " where [TransactionType] in ('3','4','5')";
				}
				sql += " order by Id desc  OFFSET " + rownum + " ROWS FETCH NEXT " + pagesize + " ROWS ONLY ";
				string connstring = buy.Database.Connection.ConnectionString;
				using (SqlConnection conn = new SqlConnection(connstring))
				{
					conn.Open();
					SqlCommand cmd = new SqlCommand(sql, conn);
					SqlDataReader dr = cmd.ExecuteReader();
					IList<CustomerFinanceLogModel> list = new List<CustomerFinanceLogModel>();
					while (dr.Read())
					{
						CustomerFinanceLogModel c = new CustomerFinanceLogModel();
						c.Id = (int)dr["Id"];
						c.BusinessNumber = dr["BusinessNumber"].ToString();
						c.CustomerId = (int)dr["CustomerId"];
						c.TransactionTime = dr["TransactionTime"].ToString();
						c.TransactionAmount = dr["TransactionAmount"].ToString();
						c.Remarks = dr["Remarks"].ToString();
						list.Add(c);
					}
					return list;
				}
			}
			catch (Exception ex)
			{
				throw;
			}
		}


		//客户的二次分配
		[Route("api/CustomerAppointment/CustomerBindUser"), HttpPost]
		public IHttpActionResult CustomerBindUser([FromBody]JObject value)
		{
			int result = 0;
			int id = (int)value["customerId"];
			int bid = (int)value["userId"];
			var buser = from b in buy.BackUser
				    join r in buy.Recommend on b.JobNumber equals r.Id
				    where b.Id == bid
				    select new
				    { r.RecommentNumber,r.Id };

		        var json = JsonConvert.SerializeObject(buser);
			Newtonsoft.Json.Linq.JArray jsonArr = (Newtonsoft.Json.Linq.JArray)JsonConvert.DeserializeObject(json);
			int rcode = Convert.ToInt32(jsonArr[0]["RecommentNumber"]);
			int rid = Convert.ToInt32(jsonArr[0]["Id"]);
			var uid = buy.CustomerUsers.Where(u => u.Id == id).FirstOrDefault();
			if (uid != null)
			{
				uid.RecommendCode = rcode;
				uid.RecommendId = rid;
				DbEntityEntry entry = buy.Entry(uid);
				entry.State = System.Data.Entity.EntityState.Modified;
				result = buy.SaveChanges();
			}
			if (result > 0)
			{
				return Ok(Respone.Success("分配成功"));
			}
			else
			{
				return Ok(Respone.No("发生了点问题，请稍后再试"));
			}
		}

		public string SendSms(string PhoneNumbers, string SignName, string TemplateCode, string TemplateParam)
		{
			try
			{
				String product = "Dysmsapi";//短信API产品名称（短信产品名固定，无需修改）
				String domain = "dysmsapi.aliyuncs.com";//短信API产品域名（接口地址固定，无需修改）
				String accessKeyId = "LTAI4GDFrkcmPmLMV9mPbrxa";//"LTAIYcJupvlyI3Wj";//你的accessKeyId，参考本文档步骤2
				String accessKeySecret = "g8LY92a1cFv0eDHJVOOq7IVMIu2A0E";//"wwWGIV226n7O0Hmxyrah4zDqRq70RO";//你的accessKeySecret，参考本文档步骤2
				IClientProfile profile = DefaultProfile.GetProfile("cn-hangzhou", accessKeyId, accessKeySecret);
				//IAcsClient client = new DefaultAcsClient(profile);
				// SingleSendSmsRequest request = new SingleSendSmsRequest();
				//初始化ascClient,暂时不支持多region（请勿修改）
				DefaultProfile.AddEndpoint("cn-hangzhou", "cn-hangzhou", product, domain);
				IAcsClient acsClient = new DefaultAcsClient(profile);
				SendSmsRequest request = new SendSmsRequest();

				//必填:待发送手机号。支持以逗号分隔的形式进行批量调用，批量上限为1000个手机号码,批量调用相对于单条调用及时性稍有延迟,验证码类型的短信推荐使用单条调用的方式，发送国际/港澳台消息时，接收号码格式为00+国际区号+号码，如“0085200000000”
				request.PhoneNumbers = PhoneNumbers;
				//必填:短信签名-可在短信控制台中找到
				request.SignName = SignName;
				//必填:短信模板-可在短信控制台中找到
				request.TemplateCode = TemplateCode;
				//可选:模板中的变量替换JSON串,如模板内容为"亲爱的${name},您的验证码为${code}"时,此处的值为
				request.TemplateParam = TemplateParam;// "{\"name\":\"Tom\"， \"code\":\"123\"}";
								      //可选:outId为提供给业务方扩展字段,最终在短信回执消息中将此值带回给调用者
				request.OutId = "yourOutId";//可以忽略
							    //请求失败这里会抛ClientException异常
				SendSmsResponse sendSmsResponse = acsClient.GetAcsResponse(request);
				return sendSmsResponse.Message;
			}
			catch (Exception)
			{

				throw;
			}
		}
		[Route("api/CustomerAppointment/ChangePhoneCode"), HttpPost]
		public IHttpActionResult ChangePhoneCode([FromBody]JObject value)
		{
			int id = (int)value["Id"];
			string phone = value["Phone"].ToString();
			var bId = buy.BackUser.Where(b => b.Id == id).Select(b=>b.Phone).Single();
			if (phone != bId)
			{
				bId = "";
				return Ok(Respone.No("对不起没有操作权限"));
			}
			bId = "";
			return Sendmessage(phone, "SMS_190274446");
		}
		[Route("api/CustomerAppointment/YzChangePhoneCode"), HttpPost]
		public IHttpActionResult YzChangePhoneCode([FromBody]JObject value)
		{
			string code = value["Code"].ToString();
			if (getcookie("Code") != code)
			{
				return Ok(Respone.No("手机验证码错误"));
			}

			return Ok(Respone.Success("验证码正确"));
		}
		public bool IsMobi(string str)
		{
			return Regex.IsMatch(str, @"^1[3456789]\d{9}$", RegexOptions.IgnoreCase);
		}
		public void setcookieCode(string cname, string value, int exMM)
		{
			delcookie(cname);
			if (exMM > 0)  //0则是会话
				HttpContext.Current.Response.Cookies[cname].Expires = DateTime.Now.AddMinutes(exMM);
			HttpContext.Current.Response.Cookies[cname].Value = HttpUtility.UrlEncode(value, System.Text.Encoding.GetEncoding("utf-8"));
		}
		public string getcookie(string cname)
		{
			if (HttpContext.Current.Request.Cookies[cname] != null)
			{
				return HttpUtility.UrlDecode(HttpContext.Current.Request.Cookies[cname].Value, System.Text.Encoding.GetEncoding("utf-8"));
			}
			return "";
		}
		public void delcookie(string cname)
		{
			HttpCookie cok = HttpContext.Current.Request.Cookies[cname];
			if (cok != null)
			{
				TimeSpan ts = new TimeSpan(-1, 0, 0, 0);
				cok.Expires = DateTime.Now.Add(ts);//删除整个Cookie，只要把过期时间设置为现在
				HttpContext.Current.Response.AppendCookie(cok);
			}
		}
		public string Code()
		{
			ArrayList MyArray = new ArrayList();
			Random random = new Random();
			string str = null;
			//循环的次数     
			int Nums = 4;
			while (Nums > 0)
			{
				int i = random.Next(1, 9);

				if (MyArray.Count < 4)
				{
					MyArray.Add(i);
				}

				Nums -= 1;
			}
			for (int j = 0; j <= MyArray.Count - 1; j++)
			{
				str += MyArray[j].ToString();
			}
			return str;
		}
		public IHttpActionResult Sendmessage(string phone, string TemplateCode)
		{
			try
			{
				var result = string.Empty;
				var stre = HttpContext.Current.Request.InputStream;
				var jsonstr = new StreamReader(stre).ReadToEnd();
				var PhoneNumbers = phone;
				var code = Code();
				bool model = IsMobi(PhoneNumbers);
				var SignName = "触动力";
				var TemplateParam = "{\"code\":\"" + code + "\"}";
				if (model == true)
				{
					var sms = SendSms(PhoneNumbers, SignName, TemplateCode, TemplateParam);//执行发送短信

					if (sms == "OK")//状态去阿里云看看成功状态返回什么 填写返回成功状态的状态值
					{
						//将信息存入cookie
						setcookieCode("Code", code, 5);
						return Ok(Respone.Success("发送成功,请注意查收"));//短信发送成功，返回到前台验证码  这样就能和前台用户输入的验证码做对比 
					}
					else
					{
						return Ok(Respone.No("发送失败: "+ sms));
					}
				}
				else
				{
					return Ok(Respone.No("手机号错误"));
				}
			}
			catch (Exception ex)
			{
				return Ok(Respone.No(ex.Message));//系统异常  返回3
			}
		}
	}
}
