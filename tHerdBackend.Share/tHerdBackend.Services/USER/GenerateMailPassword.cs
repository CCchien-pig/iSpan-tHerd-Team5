using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Services.USER
{
	public class GenerateMailPassword
	{
		public static string GenerateNumericCode(int len = 6)
		{
			var rng = RandomNumberGenerator.Create();
			var bytes = new byte[len];
			rng.GetBytes(bytes);
			var sb = new StringBuilder(len);
			foreach (var b in bytes) sb.Append((b % 10).ToString());
			return sb.ToString();
		}

		public static string GenerateTempPassword(int len)
		{
			// 避免易混淆字元（移除 l I 1 O 0）
			const string lower = "abcdefghjkmnpqrstuvwxyz";
			const string upper = "ABCDEFGHJKMNPQRSTUVWXYZ";
			const string digits = "23456789";
			// 符號挑常見且兼容性好的集合（避免空白與引號）
			const string symbols = "!@#$%^&*_-+=?";

			string pool = lower + upper + digits + symbols;

			// 至少 8 碼，且要能放下四個類別
			int n = Math.Max(len, 8);

			var pwd = new char[n];

			int Next(int max) => RandomNumberGenerator.GetInt32(max);

			// 至少各一
			pwd[0] = lower[Next(lower.Length)];
			pwd[1] = upper[Next(upper.Length)];
			pwd[2] = digits[Next(digits.Length)];
			pwd[3] = symbols[Next(symbols.Length)];

			// 其餘位置從全集挑
			for (int i = 4; i < n; i++)
				pwd[i] = pool[Next(pool.Length)];

			// Fisher–Yates 洗牌（安全亂數）
			for (int i = n - 1; i > 0; i--)
			{
				int j = Next(i + 1);
				(pwd[i], pwd[j]) = (pwd[j], pwd[i]);
			}

			return new string(pwd);
		}
	}
}
